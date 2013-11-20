using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading;
using System.Runtime.Remoting.Channels.Tcp;

using SharedLib.DataserverObjects;
using SharedLib.MetadataObjects;
using SharedLib;
using log4net;

namespace DataServer
{
    class DataServer : MarshalByRefObject, IDataToClient, IDataToMeta, IDataToPuppet, IClientToMeta, IDataToData
    {

        private static readonly ILog log = LogManager.GetLogger( typeof( DataServer ) );


        static public List<ServerId> MetadataServerList = new List<ServerId>();
        static public int ServerId;
        static public String ServerIp;
        static public int ServerPort;
        static public int RecoveryPort;
        static public StorageManager storage;
        static private Boolean isFreezed = true;
        static private Boolean isFailed = false;
        static private object freezeMon = new object();
        //LocalFilename/Version
        static private ConcurrentDictionary<String, LocalFileStatistics> _localFileNameList;

        public static MetaserverAsyncClient MetaserverClient;

        public DataServer()
        {
        }



        static void Main(string[] args)
        {
            foreach ( var item in args )
                Console.Write( item + " " );

            System.Console.WriteLine("Dataserver Init....");


            log4net.Config.XmlConfigurator.Configure( );

            log.Debug( DateTime.Now.Ticks + " Metadata init..." );

          //  if (!Debugger.IsAttached)
            //    Debugger.Launch();


            int i = 0;
            for (i = 0; i < (args.Length - 4); i++)
            {
                ServerId metaServer = new ServerId();
                metaServer.hostname = args[i++];
                metaServer.port = Convert.ToInt32(args[i]);
                metaServer.id = Convert.ToString(i / 2);
                MetadataServerList.Add(metaServer);
            }

            ServerPort = Convert.ToInt32(args[i++]);
            ServerId = Convert.ToInt32(args[i++]);
            RecoveryPort = Convert.ToInt32(args[i++]);
            ServerIp = DataServer.GetCurrentIp();
            storage = new StorageManager(ServerId);


            System.Console.WriteLine("System ID: " + ServerId
                );
            System.Console.WriteLine("System IP Adress: " + ServerIp + ":" + ServerPort + "#" + RecoveryPort);

            // Cria objecto remoto para ser envocado apartir do cliente
            RemotingConfiguration.Configure("../../App.config", true);


            DataServer.CreateNormalChannel();
            _localFileNameList = new ConcurrentDictionary<String, LocalFileStatistics>();



            // Espera ate receber unfreze do puppet Online
            System.Console.WriteLine("DataServer Started, In Freeze Mode");
            while (isFreezed)
            {
                Thread.Sleep(250);
            }
            System.Console.WriteLine("DataServer Started Defrost!!!");


            // Regista-se no Metadata e inicia Objectos Remotos
            MetaserverClient = new MetaserverAsyncClient(ServerIp, ServerPort, MetadataServerList, ServerId);
            RegisterAtMetadata();

            // Releses the Request Thread and Returns to Puppet
            Monitor.Enter(freezeMon);
            Monitor.PulseAll(freezeMon);
            Monitor.Exit(freezeMon);


            System.Console.WriteLine("<enter> para sair...");
            System.Console.ReadLine();
        }

        // Bloqueante ate se rgistar
        public static void RegisterAtMetadata()
        {
            RequestRegistry request = new RequestRegistry(ServerId.ToString(), ServerIp, ServerPort);
            try
            {
                MetaserverResponse response = MetaserverClient.SendRequestToMetaserver(request);
                if (response.Status == ResponseStatus.Success)
                    Console.WriteLine("Registered on Metadata Servers  ");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Dataserver: Error: Registry at metaserve" + ex.Message);
            }
        }


        // Evita que o objecto é reciclado
        public override object InitializeLifetimeService()
        {
            return null;

        }


        // Devolve IP local da maquina
        public static String GetCurrentIp()
        {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("Dataserver " + ServerId + "cannot get LocalIP");
        }


        public Boolean ReceiveMetaserverResponse(MetaserverResponse response)
        {
            return MetaserverClient.ReceiveMetaserverResponse(response);
        }



        // Operaçoes De Storage

        public String Write(string localFileName, byte[] data, long newVersion)
        {

            Console.WriteLine("Write: " + localFileName + " TFile: " + data);
            if (isFreezed)
            {
                Console.WriteLine("Write: Freeze");
                Monitor.Enter(freezeMon);
                Monitor.Wait(freezeMon);
                Monitor.Exit(freezeMon);
                Console.WriteLine("Write: Defrost");
            }

            LocalFileStatistics currentStatistics;
            _localFileNameList.TryGetValue(localFileName, out currentStatistics);


            // Se estiver outra escrita em curso ficamos a bloqueados
            // caso contrario reservamos a escrita
            lock (currentStatistics.mutex)
            {
                //Verifica Versão Proposta
                if (newVersion > currentStatistics.version)
                {
                    currentStatistics.version = newVersion;
                }
                else
                {
                    currentStatistics.version += 1;
                }

                //Actualiza as variaveis de estatistica
                currentStatistics.writeTraffic += data.Length;
                currentStatistics.size = data.Length;

                //Guardar File em Disco
                TFile t = new TFile(currentStatistics.version, data);
                storage.WriteFile(localFileName, t);

                Console.WriteLine("Write: New File Version is:" + currentStatistics.version);

                return ServerId.ToString();
            }
        }

        public TFile Read(string localFileName)
        {
            Console.WriteLine("Read: " + localFileName);

            if (isFreezed)
            {
                Console.WriteLine("Read: Freeze");
                Monitor.Enter(freezeMon);
                Monitor.Wait(freezeMon);
                Monitor.Exit(freezeMon);
                Console.WriteLine("Read: Defrost");
            }

            LocalFileStatistics currentStatistics;
            _localFileNameList.TryGetValue(localFileName, out currentStatistics);

           

            //Zona Critica Sincroniza com as Escritas
            lock (currentStatistics.mutex)
            { 
                TFile t;
                t = storage.ReadFile(localFileName);
                t.responseServerId = ServerId.ToString();
                currentStatistics.readTraffic += currentStatistics.size;
            return t;
            }

        }

        public void CreateEmptyFile(String filename, string localFilename)
        {
            LocalFileStatistics value;
            Boolean exists = _localFileNameList.TryGetValue(localFilename, out value);
            
            if (exists == true)
            {
                //Console.WriteLine("Create: File already exists, ignore: " + localFilename);
                return;
            }

            TFile t = new TFile(0, new byte[0]);
            storage.WriteFile(localFilename, t);
            Console.WriteLine("Create new Empty File: " + localFilename);

            LocalFileStatistics currentStatistics = new LocalFileStatistics(filename, localFilename);
            currentStatistics.localFileName = localFilename;
            _localFileNameList.TryAdd(localFilename, currentStatistics);
        }




        /////////////////////////////// For Puppet Master /////////////////////////////////

        public string Freeze()
        {
            isFreezed = true;
            Console.WriteLine("DataServer " + ServerId + " Frozen!!!");
            return "DataServer " + ServerId + " Freezed State";
        }

        public string UnFreeze()
        {
            if (isFreezed)
            {
                isFreezed = false;
                // Releses the Request Thread and Returns to Puppet
                Monitor.Enter(freezeMon);
                Monitor.Pulse(freezeMon);
                Monitor.Exit(freezeMon);
                Console.WriteLine("DataServer " + ServerId + " Unfreezed");
                return "DataServer " + ServerId + " Unfreezed";
            }
            else
            {
                Console.WriteLine("Server was not in Freeze State");
                return "Server was not in Freeze State";
            }
        }

        public string Fail()
        {
            isFailed = true;
            DestroyNormalChannel();
            CreateEmergencyChannel();
            Console.WriteLine("DataServer " + ServerId + " Failed, Listening PM on the Emergency port");
            return "DataServer " + ServerId + " Failed";
        }

        public string Recover()
        {
            isFailed = false;
            DestroyEmergencyChannel();
            CreateNormalChannel();
            RegisterAtMetadata();
            Console.WriteLine("DataServer " + ServerId + " Recovered");
            return "DataServer " + ServerId + " Recovered";
        }

        public string Dump()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Server :" + ServerId + " at " + ServerIp + ":" + ServerPort);
            builder.AppendLine("Server is Online? :" + !isFailed);
            builder.AppendLine("Server is Freezed? :" + isFreezed);
            builder.AppendLine("|     Name     | Ver | Size |ReadsTraffic|WritesTraffic| Content     |");
            foreach (KeyValuePair<string, LocalFileStatistics> keyValuePair in _localFileNameList)
            {
                TFile file = storage.ReadFile(keyValuePair.Key);
                builder.AppendLine(value: "|" + keyValuePair.Key + "  |  " + keyValuePair.Value.version + "  |  " +
                    keyValuePair.Value.size + "|  " + keyValuePair.Value.readTraffic + "  |  " + keyValuePair.Value.writeTraffic + "  |" +
                    Encoding.ASCII.GetString(file.Data));
            }
            return builder.ToString();
        }


        /// <summary>
        /// Copiar o ficheiro de um dataserver para outro servidor de dataserver.
        /// Utilizado pela classe de LoadBalancer dos metadados
        /// </summary>
        /// <param name="localFileName"></param>
        /// <param name="destHostname"></param>
        /// <param name="destPort"></param>
        public void CopyFileToOtherData( String srcFileName, String destLocalFilename, String destHostname, int destPort )
        {
            Console.WriteLine( "BALANCE COPY:::Copy File: " + srcFileName + " to: " + destHostname );
            


            //Get TFile e LocalFileStatitics
            LocalFileStatistics value;
            Boolean exists = _localFileNameList.TryGetValue( srcFileName, out value );
            if (!exists)
            {
                Console.WriteLine( "BALANCE COPY REQUEST: File not exists, ignore: " + srcFileName );
                return;
            }


            //Converter para um read directo
            TFile t = Read(srcFileName);
            try
                {
                IDataToData server =
                    (IDataToData)
                    Activator.GetObject( typeof( IDataToData ),
                                        "tcp://" + destHostname + ":" + destPort + "/PADIConnection" );

                server.ReceiveFileCopy( t, value,destLocalFilename );
                }
            catch ( Exception e )
                {
                Console.WriteLine( "BALANCE COPY REQUEST:  Connection to dataserver fail" );
                }

            Console.WriteLine( "BALANCE COPY::Copy done: " + srcFileName );
        }





        public void ReceiveFileCopy( TFile t, LocalFileStatistics stats,String newLocalfilename)
        {


            Console.WriteLine( "BALANCE COPY: receive file copy: " + newLocalfilename );
            //Receber um TFile e adicionar
            LocalFileStatistics value;
            stats.localFileName = newLocalfilename;
            Boolean exists = _localFileNameList.TryGetValue( newLocalfilename, out value );

            if ( exists )
                {
                Console.WriteLine( "BALANCE COPY: File already exists, ignore: " + newLocalfilename );
                return;
                }

            LocalFileStatistics currentStatistics = new LocalFileStatistics( stats.filename, stats.localFileName );
            _localFileNameList.TryAdd( stats.localFileName, currentStatistics );

            Write(newLocalfilename, t.Data, t.VersionNumber );
            Console.WriteLine( "BALANCE COPY: File wrote to storage: " + newLocalfilename );
            Console.WriteLine( "BALANCE COPY: receive file DONE: " + stats.localFileName );
        }










        /////////////////////////////// Channel Manager /////////////////////////////////

        public static TcpChannel NormalChannel;
        public static TcpChannel EmergencyChannel;

        public static void CreateEmergencyChannel()
        {
            EmergencyChannel = new TcpChannel(DataServer.RecoveryPort);
            ChannelServices.RegisterChannel(EmergencyChannel, false);
            RemotingConfiguration.RegisterWellKnownServiceType(
              typeof(DataServer),
              "PADIConnection",
              WellKnownObjectMode.Singleton);
        }

        public static void CreateNormalChannel()
        {
            NormalChannel = new TcpChannel(DataServer.ServerPort);
            ChannelServices.RegisterChannel(NormalChannel, false);
            RemotingConfiguration.RegisterWellKnownServiceType(
              typeof(DataServer),
              "PADIConnection",
              WellKnownObjectMode.Singleton);

        }

        public static void DestroyEmergencyChannel()
        {
            if (EmergencyChannel == null)
                return;
            ChannelServices.UnregisterChannel(EmergencyChannel);
        }

        public static void DestroyNormalChannel()
        {
            if (NormalChannel == null)
                return;
            ChannelServices.UnregisterChannel(NormalChannel);
        }




        public ConcurrentDictionary<String, LocalFileStatistics> GetFileStatistics() {
            return _localFileNameList;   
        }




    }
}