using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using SharedLib;
using SharedLib.DataserverObjects;
using SharedLib.Exceptions;
using SharedLib.MetadataObjects;
using System.Collections;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Net.Sockets;
using System.Threading;
using log4net;


namespace Client
{


    /// <summary>
    /// Classe que representa uma  Aplicação cliente que contacta os Servidores de Dados e Metadados no entanto oferece uma interface
    /// para o puppet master que envoca as operaçoes Remotamente
    /// </summary>
    public class ClientEntry : MarshalByRefObject, IClientToPuppet, IClientToMeta
    {
        private static readonly ILog log = LogManager.GetLogger( typeof( ClientEntry ) );


        public MetadataEntry[] FileRegister = new MetadataEntry[10];
        public byte[][] DataRegister = new byte[10][];
        public static MetaserverAsyncClient MetaserverClient;

        private readonly String _baseDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);


        public static int clientId;

        public ClientEntry()
        {
        }


        static void Main(String[] args)
        {

        log4net.Config.XmlConfigurator.Configure( );

        log.Debug( DateTime.Now.Ticks + " Metadata init..." );

         //   if (!Debugger.IsAttached)
           //     Debugger.Launch();

            int clientPort;
            String clientHostName;
            List<ServerId> metaserverList = new List<ServerId>();

            int i = 0;
            for (i = 0; i < (args.Length - 2); i++)
            {
                ServerId metaServer = new ServerId();
                metaServer.hostname = args[i++];
                metaServer.port = Convert.ToInt32(args[i]);
                metaServer.id = Convert.ToString(i / 2);
                metaserverList.Add(metaServer);
            }
            clientPort = Convert.ToInt32(args[i++]);
            clientId = Convert.ToInt32(args[i]);
            clientHostName = ClientEntry.GetCurrentIp();

            RemotingConfiguration.Configure("../../App.config", true);
            ClientEntry client = new ClientEntry();

            Console.WriteLine("Start client: " + clientId);

            //Create interfce to metaserver
            MetaserverClient = new MetaserverAsyncClient(clientHostName, clientPort, metaserverList, clientId);

            // TCP Channel
            TcpChannel channel = new TcpChannel( MetaserverClient.ClientPort );

            ChannelServices.RegisterChannel(channel, false);
            Console.WriteLine("Starting Client Socket for puppet master at port: " + MetaserverClient.ClientPort);
            RemotingConfiguration.RegisterWellKnownServiceType(
            typeof(ClientEntry),
            "PADIConnection",
            WellKnownObjectMode.Singleton);

            System.Console.WriteLine("<enter> to exit...");
            System.Console.ReadLine();
        }


        public String Dump()
        {
            Console.WriteLine("#DUMP Sending System Dump to PuppetMaster");
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("DUMP of Client \r\n");

            builder.AppendLine("FileRegister \r\n");
            for (int i = 0; i < 10; i++)
            {
                builder.Append("File Number: " + i + "  |  ");

                if (FileRegister[i] != null)
                {
                    builder.Append(FileRegister[i] + "\r\n");
                }
                else
                {
                    builder.Append(" Empty \r\n");
                }
            }


            builder.AppendLine("DataRegister");
            for (int i = 0; i < 10; i++)
            {
                builder.Append("File Number: " + i + "  |  ");

                if (DataRegister[i] == null)
                {
                    builder.Append(" Empty \r\n");
                }
                else
                {
                    builder.Append(Encoding.ASCII.GetString(DataRegister[i]));
                    builder.Append("\r\n");
                }
            }

            return builder.ToString();
        }


        //////////////////////////////////// CLient Interface //////////////////////////////////
        public String Create(String filename, int nbDataServers, int readQuorum, int writeQuorum)
        {
            log.Debug( DateTime.Now.Ticks + " Create: "+filename );

            Console.WriteLine("#CREATE Create file: " + filename + " nbDataServer: " + nbDataServers + " readQuorum: " + readQuorum + " writeQuorum: " + writeQuorum);
            RequestCreate createRequest = new RequestCreate(filename, nbDataServers, readQuorum, writeQuorum);
            log.Debug( DateTime.Now.Ticks + " [M] Send Create: " + filename );
            MetaserverResponse response = MetaserverClient.SendRequestToMetaserver(createRequest);
            //Processar a resposta
            MetadataEntry metaEntry = response.MetaEntry;

            if (metaEntry != null)
            {
                Console.WriteLine("#CREATE Create Successeful");
                log.Debug( DateTime.Now.Ticks + " Create Done: " + filename );
                return metaEntry.ToString( );
            }
            else
            {
                throw new PadiException(PadiExceptiontType.CreateFile, "Response without entry");
            }
        }


        public String Open(String filename)
        {
        log.Debug( DateTime.Now.Ticks + " Open: " + filename );


            Console.WriteLine("#OPEN  Open: " + filename);
            //            if (GetFileMetadataEntry(filename) != null)
            //              throw new PadiException(PadiExceptiontType.OpenFile, "ClientCore: File already opened: " + filename);

            RequestOpen openRequest = new RequestOpen(filename);
            log.Debug( DateTime.Now.Ticks + " [M] Send Open: " + filename );
            MetaserverResponse response = MetaserverClient.SendRequestToMetaserver(openRequest);
            MetadataEntry metaEntry = response.MetaEntry;
            if (metaEntry == null)
            {
                throw new PadiException(PadiExceptiontType.OpenFile, "Response without entry");
            }

            if (GetFileMetadataEntry(filename) != null)
            {
                UpdateMetadataEntry(metaEntry);
                Console.WriteLine("#Open: Value Updated");
                log.Debug( DateTime.Now.Ticks + " Open Done: " + filename );
                return metaEntry.ToString();
            }

            AddFileMetadataEntry(metaEntry);
            log.Debug( DateTime.Now.Ticks + " Open done: " + filename );
            Console.WriteLine("#Open: Complete");
            return metaEntry.ToString();
        }

        public String Close(String filename)
        {
            log.Debug( DateTime.Now.Ticks + " Close: " + filename );
            Console.WriteLine("#Close: " + filename);

            MetadataEntry entry = this.GetFileMetadataEntry(filename);
            if (entry == null)
            {
                Console.WriteLine("#CLOSE File was not open on this client " + filename);
                return "#CLOSE File was not open on this client " + filename;
            }

            RequestClose closeRequest = new RequestClose(filename);
            log.Debug( DateTime.Now.Ticks + " [M] Send Close: " + filename );
            MetaserverResponse response = MetaserverClient.SendRequestToMetaserver(closeRequest);
            if (response.Status != ResponseStatus.Success)
            {
                Console.WriteLine("#CLOSE  Error closing on server " + filename);
                log.Debug( DateTime.Now.Ticks + " Close: " + filename );
                return "#CLOSE  Error closing on server " + filename;
            }
            else
            {
                Console.WriteLine("#CLOSE: Success: " + filename);
                log.Debug( DateTime.Now.Ticks + " Close done: " + filename );
                return "#CLOSE: Success: " + filename;
            }

        }

        public String Delete(String filename)
        {
            log.Debug( DateTime.Now.Ticks + " Delete: " + filename );
            Console.WriteLine("#Delete: " + filename);
            RequestDelete deleteRequest = new RequestDelete(filename);
            log.Debug( DateTime.Now.Ticks + " [M] Send Delete: " + filename );
            MetaserverResponse response = MetaserverClient.SendRequestToMetaserver(deleteRequest);
            if (response.Status != ResponseStatus.Success)
            {
                Console.WriteLine("#DELETE  Error closing on server " + filename);
                log.Debug( DateTime.Now.Ticks + " Delete done: " + filename );
                return "#DELETE  Error deleting on server " + filename;
            }
            Console.WriteLine("#DELETE: Success: " + filename);
            log.Debug( DateTime.Now.Ticks + " Delete done: " + filename );
            return "#DELETE: Success: " + filename;
        }


        //////////////////////////////////////////////// Async READ
        static object monReadLock = new object();
        private static int targetReadQuorum = 0;
        private static List<String> requestedReadIDs = new List<String>();
        private static List<String> returnedReadIDs = new List<String>();
        private static List<TFile> returnedReadFiles = new List<TFile>();

        public delegate TFile AsyncReadDel(String localFileName);


        public static void CBDoWhenReturnFromRead(IAsyncResult ar)
        {
            AsyncReadDel del = (AsyncReadDel)((AsyncResult)ar).AsyncDelegate;
            //Read
            TFile resp = del.EndInvoke(ar); // Pode Mandar Exepção

            System.Console.WriteLine("#READ *Data Server " + resp.responseServerId + " Returned from Read");
            returnedReadIDs.Add(resp.responseServerId);
            returnedReadFiles.Add(resp);

            // Se somos o ultimo vamos informar a thread em espera no monitor
            if (returnedReadIDs.Count >= targetReadQuorum)
            {
                Thread.Yield();
                Thread.Sleep(50);
                Thread.Yield();
                System.Console.WriteLine("#READ *Last Data Server,  Realeasing Main Thread");
                Monitor.Enter(monReadLock);
                Monitor.Pulse(monReadLock);
                Monitor.Exit(monReadLock);
               
            }
            return;
        }

        private bool ReadAux(string serverIP, int serverPort, string localFileName, SemanticType semantic, long minVersion)
        {

            IDataToClient dataServer = (IDataToClient)Activator.GetObject(
                typeof(IDataToClient),
                "tcp://" + serverIP + ":" + serverPort + "/PADIConnection");

            AsyncReadDel RemoteDel = new AsyncReadDel(dataServer.Read);
            AsyncCallback RemoteCallback = new AsyncCallback(CBDoWhenReturnFromRead);

            try
            {
                IAsyncResult RemAr = RemoteDel.BeginInvoke(localFileName, RemoteCallback, null);
            }
            catch (Exception)
            {
                System.Console.WriteLine("#READ Could not locate server");
                return false;

            }
            return true;
        }

        public TFile Read(int fileRegister, SemanticType semantic)
        {
            log.Debug( DateTime.Now.Ticks + " Read: " );

            Console.WriteLine("#READ Read register: " + fileRegister + " Semantic: " + semantic);
            MetadataEntry entry;
            long lastMonotonicVersion = -1;

            if (FileRegister[fileRegister] != null)
            {
                entry = FileRegister[fileRegister];
                lastMonotonicVersion = entry.monotonicReadVersion;  //Guardar a vista Monotonica
            }
            else
            {
                throw new PadiException(PadiExceptiontType.ReadFile, "#READ Client: File Registry is empty, no metadata");
            }

            if (entry.ServerFileList.Values.Count < entry.ReadQuorum)
            {
                // Update the Registry
                Open(entry.FileName);
                if (FileRegister[fileRegister] == null)
                {
                    throw new PadiException(PadiExceptiontType.ReadFile, "#READ Client: File Registry is empty, no metadata");
                }
                entry = FileRegister[fileRegister];
                entry.monotonicReadVersion = lastMonotonicVersion;  //Aplicar A vista Monotonica
                if (entry.ServerFileList.Values.Count < entry.ReadQuorum)
                {
                    throw new PadiException(PadiExceptiontType.OpenFile, "#READ Client: Cannot read file: Servers available are not enought");
                }
            }

            targetReadQuorum = entry.ReadQuorum;
            requestedReadIDs = new List<String>();
            returnedReadIDs = new List<String>();
            returnedReadFiles = new List<TFile>();
            long version = -1;
            TFile file = null;


            Console.WriteLine("#READ last Read version was : " + lastMonotonicVersion);

            //Utilizar esta Pool ate conseguir fazer uma leitura
            KeyValuePair<ServerId, string>[] serverPool = entry.ServerFileList.ToArray();
            int index = -1;
            int maxIndex = serverPool.Length;
            int LoopCount = 0;

            // Começar por um Servidor Random (SoftLoad Balance)
            Random rnd = new Random();
            index = rnd.Next(0, maxIndex);


            // Exectar ate obter um Quorm de leituras
            // Serviodres podem recuperar temos que passar a lista varias vezes
            while (true)
            {

                // Server Poll Array Rotate
                if (index == maxIndex)
                {
                    index = 0;
                    LoopCount++;

                    // se já estamos no loop há bastante tempo verificamos
                    // se os servidores quais retornaram têm um valor mais
                    // recente  ( +- 50*100 = 5000ms)
                    if (LoopCount % 10 == 0 && semantic.Equals(SemanticType.Monotonic))
                    {
                        returnedReadIDs = new List<String>();
                        requestedReadIDs = new List<String>();
                        returnedReadIDs = new List<String>();
                        Console.WriteLine("#READ Requesting Servers Againf for Updated Version");
                    }
                    // Commo Precorremos o Array todo isto indica que
                    // ou não conseguimos contactar os servidores todos
                    // ou não conseguimos efectuar uma leitura monotonica
                    // vamos efectuar um sleep e repetir o ciclo
                    Thread.Sleep(100);

                }
                else
                {
                    index++;
                }

                //Limitar o index
                index = index % maxIndex;

                ServerId serverId = serverPool[index].Key;
                String localFileName = serverPool[index].Value;

                // Se efectuamos um pedido valido ignoramos este servidor
                if (requestedReadIDs.Contains(serverId.id))
                {
                    continue;
                }

                //Verificamos se Conseguimos Envia o pedido se sim vamos adicionar a nossa lista de servidores a quais fizemos o pedido
                Boolean sent = ReadAux(serverId.hostname, serverId.port, localFileName, semantic, entry.monotonicReadVersion);
                if (sent)
                {
                    requestedReadIDs.Add(serverId.id);
                }
                else
                {
                    continue;
                }


                // Se já enviamos a um quorum de servidores ficamos a espera no monitor
                if (requestedReadIDs.Count >= entry.ReadQuorum)
                {

                    if (returnedReadIDs.Count < entry.ReadQuorum)
                    {

                        System.Console.WriteLine("#READ Main Thread is Waiting for the Dataservers to Respond");
                        Monitor.Enter(monReadLock);
                        Monitor.Wait(monReadLock);
                        Monitor.Exit(monReadLock);
                        System.Console.WriteLine("#READ Client Released from Wait");
                    }
                }

                // Se todos retornaram vamos verificar se podemos sair
                if (returnedReadIDs.Count >= entry.ReadQuorum)
                {
                    //Verificar a maxima versão retornada
                    foreach (TFile t in returnedReadFiles)
                    {
                        if (t.VersionNumber > version)
                        {
                            version = t.VersionNumber;
                            file = t;
                        }
                    }
                    String txt = Encoding.ASCII.GetString(file.Data);
                    Console.WriteLine("#READ Max Ver found is at Ver: " + version);
                    Console.WriteLine("#READ Content is : "+txt);
                    if(version < lastMonotonicVersion && semantic.Equals(SemanticType.Monotonic)){
                        Console.WriteLine("#READ Not Monotonic Continuing Trying");
                    }

                    // se não for uma leitura monotonica podemos retornar
                    if (semantic.Equals(SemanticType.Default))
                    {
                        break;
                    }

                    // Caso uma leitura monotonica vamos verificar se a versão lida
                    // é superior a anteriormente lida
                    else
                    {
                        if (version >= lastMonotonicVersion)
                        {
                            break;
                        }

                        // Caso uma leitura monotonica efectuada é inferior a lida anteriormente
                        // Fazemos o pedido a mais um servidor
                        else
                        {
                            continue;
                        }
                    }
                }

            }


            // Actualizar valor util para leituras monotonicas
            if (version > FileRegister[fileRegister].monotonicReadVersion)
            {
                FileRegister[fileRegister].monotonicReadVersion = version;
            }
            log.Debug( DateTime.Now.Ticks + " Read done: " );

            System.Console.WriteLine("#READ Read Complete Returning Read data at Version: " + version);

            return file;
        }

        public String Read(int fileRegister, SemanticType semantic, int dataRegister)
        {
            TFile file = Read(fileRegister, semantic);
            DataRegister[dataRegister] = file.Data;
            Console.WriteLine("#READ Read Action Saved At DataRegister " + dataRegister);

            return "#READ Result Data is : " + Encoding.ASCII.GetString(file.Data) + " froam Server " + file.responseServerId + " at Version " + file.VersionNumber;
        }


        //////////////////////////////////////////////// Async WRITE
        static object monWriteLock = new object();
        private static int targetWriteQuorum = 0;
        private static List<String> requestedWriteIDs = new List<String>();
        private static List<String> returnedWriteIDs = new List<String>();

        public delegate String AsyncWriteDel(String localFileName, byte[] data, long version);

        // This is the call that the AsyncCallBack delegate will reference.
        public static void CBDoWhenReturnFromWrite(IAsyncResult ar)
        {
            AsyncWriteDel del = (AsyncWriteDel)((AsyncResult)ar).AsyncDelegate;

            String id = del.EndInvoke(ar);
            System.Console.WriteLine("#WRITE *Data Server " + id + " Returned from Write");
            returnedWriteIDs.Add(id);

            // Se somos o ultimo vamos informat a thread em espera no monitor
            if (returnedWriteIDs.Count.Equals(targetWriteQuorum))
            {
                Thread.Yield();
                Thread.Sleep(50);
                Thread.Yield();
                System.Console.WriteLine("#WRITE *Last Data Server Realeasing Main Thread");
                Monitor.Enter(monWriteLock);
                Monitor.Pulse(monWriteLock);
                Monitor.Exit(monWriteLock);
            }

            return;
        }

        // Metodo que faz a invocação assincrona, devolve false se não encontrar o servidor
        private Boolean WriteAux(String serverIP, int serverPort, String localFileName, byte[] data, long version)
        {

            IDataToClient dataServer = (IDataToClient)Activator.GetObject(
                typeof(IDataToClient),
                "tcp://" + serverIP + ":" + serverPort + "/PADIConnection");

            AsyncWriteDel RemoteDel = new AsyncWriteDel(dataServer.Write);
            AsyncCallback RemoteCallback = new AsyncCallback(ClientEntry.CBDoWhenReturnFromWrite);

            try
            {
                IAsyncResult RemAr = RemoteDel.BeginInvoke(localFileName, data, version, RemoteCallback, null);
            }
            catch (SocketException)
            {
                System.Console.WriteLine("#WRITE Could not locate server");
                return false;
            }
            return true;
        }

        public String Write(int fileRegister, int byteArrayRegister)
        {
            Console.WriteLine("#WRITE Write: fileRegister" + fileRegister + " byteArrayRegister: " + byteArrayRegister);
            //TODO tratar excepcao de array out of bounds
            byte[] data = DataRegister[byteArrayRegister];

            return Write(fileRegister, data);
        }

        public String Write(int fileRegister, byte[] data)
        {
            log.Debug( DateTime.Now.Ticks + " Write: " );
            String txt = Encoding.ASCII.GetString(data);
            Console.WriteLine("#WRITE Write: fileRegister: " + fileRegister + ",  Data: " + txt);
            MetadataEntry entry;
            long lastMonotonicVersion;

            if (FileRegister[fileRegister] != null)
            {
                entry = FileRegister[fileRegister];
                lastMonotonicVersion = entry.monotonicReadVersion;  //Guardar a vista Monotonica
            }
            else
            {
                throw new PadiException(PadiExceptiontType.WriteFile, "#WRITE Client: Cannot write file: File registry not exists");
            }

            // Se não conseguiremos satisfazer o Quorum vamos tentar actualizar a entrada
            if (entry.ServerFileList.Values.Count < entry.WriteQuorum)
            {
                // Update the Registry
                Open(entry.FileName);
                entry = FileRegister[fileRegister];
                entry.monotonicReadVersion = lastMonotonicVersion;  //Aplicar A vista Monotonica da versão

                if (entry.ServerFileList.Values.Count < entry.WriteQuorum)
                {
                    throw new PadiException(PadiExceptiontType.OpenFile, "#WRITE Client: Cannot write file: Servers available are not enought");
                }
            }

            targetWriteQuorum = entry.WriteQuorum;
            requestedWriteIDs = new List<String>();
            returnedWriteIDs = new List<String>();

            // Buscar o ultimo valor de Leitura se existir
            long lastReadVersion = entry.monotonicReadVersion;


            // Exectar ate obter um Quorm de escritas
            // Serviodres podem recuperar temos que passar a lista varias vezes
            while (true)
            {
                foreach (KeyValuePair<ServerId, String> server in entry.ServerFileList)
                {
                    ServerId serverId = server.Key;
                    String localFileName = server.Value;

                    if (requestedWriteIDs.Contains(serverId.id))
                    {
                        continue;
                    }

                    Boolean sent = WriteAux(serverId.hostname, serverId.port, localFileName, data, lastReadVersion + 1);

                    if (sent)
                    {
                        requestedWriteIDs.Add(serverId.id);
                    }

                    // Se já enviamos a um quorum de servidores ficamos a espera no monitor
                    if (requestedWriteIDs.Count.Equals(entry.WriteQuorum))
                    {
                        System.Console.WriteLine("#WRITE Main Thread is Waiting for the Dataservers to respond");
                        Monitor.Enter(monWriteLock);
                        Monitor.Wait(monWriteLock);
                        Monitor.Exit(monWriteLock);
                        System.Console.WriteLine("#WRITE Client Released from Wait");
                    }

                    // Se todos retornaram vamos sair do ciclo
                    if (returnedWriteIDs.Count.Equals(entry.WriteQuorum))
                    {
                        break;
                    }
                }

                if (returnedWriteIDs.Count.Equals(entry.WriteQuorum))
                {
                    break;
                }
            }
            log.Debug( DateTime.Now.Ticks + " Write done: " );
            System.Console.WriteLine("#WRITE Write Complete Returning");
            return "#WRITE Client: fileRegister " + fileRegister + " with success";
        }

        public String Copy(int fileRegisterRead, SemanticType semantic, int fileRegisterWrite, byte[] stringSalt)
        {
            Console.WriteLine("#COPY From File at Register: " + fileRegisterRead + " with " + semantic.ToString() + " Semantic  and Writes to File at Register: " +
                fileRegisterWrite + " adding the salt: " + (new ASCIIEncoding()).GetString(stringSalt));

            TFile readFile = Read(fileRegisterRead, semantic);
            String parte1 = Encoding.ASCII.GetString(readFile.Data);
            String parte2 = Encoding.ASCII.GetString(stringSalt);
            String data = parte1 + parte2;
            Write(fileRegisterWrite, (new ASCIIEncoding()).GetBytes(data));

            return "#COPY Complete";
        }



        //  The client may ignore the PROCESS parameter in all commands.
        public String ExeScript(Queue<String> commandList)
        {
            Console.WriteLine("#EXESCRIPT ExeScript: ");
            ASCIIEncoding ascii = new ASCIIEncoding();
            String command;
            try
            {
                while ((command = commandList.Dequeue()) != null)
                {
                    String[] words = command.Split(' ', ',');
                    switch (words[0])
                    {
                        case "CREATE":
                            Console.Write("####EXESCRIPT CREATE");
                            Console.WriteLine(Create(words[3], Convert.ToInt32(words[5]), Convert.ToInt32(words[7]),
                                                     Convert.ToInt32(words[9])));
                            break;
                        case "OPEN":
                            Console.Write("####EXESCRIPT OPEN");
                            Console.WriteLine(Open(words[3]));
                            break;
                        case "CLOSE":
                            Console.Write("####EXESCRIPT CLOSE");
                            Console.WriteLine(Close(words[3]));
                            break;
                        case "READ":
                            Console.Write("####EXESCRIPT READ");
                            SemanticType type = (words[5].Equals("monotonic"))
                                                    ? SemanticType.Monotonic
                                                    : SemanticType.Default;
                            Console.WriteLine(Read(Convert.ToInt32(words[3]), type, Convert.ToInt32(words[7])));
                            break;
                        case "WRITE":
                            Console.Write("####EXESCRIPT WRITE");
                            if (command.Split('"').Length > 1)
                            {
                                Console.WriteLine(Write(Convert.ToInt32(words[3]), ascii.GetBytes(command.Split('"')[1])));
                            }
                            else
                            {
                                Console.WriteLine(Write(Convert.ToInt32(words[3]), Convert.ToInt32(words[5])));
                            }
                            break;
                        case "COPY":
                            Console.Write("####EXESCRIPT COPY");
                            SemanticType type2 = (words[5].Equals("monotonic"))
                                                     ? SemanticType.Monotonic
                                                     : SemanticType.Default;
                            Console.WriteLine(Copy(Convert.ToInt32(words[3]), type2, Convert.ToInt32(words[7]),
                                                   ascii.GetBytes(command.Split('"')[1])));
                            break;
                        case "DUMP":
                            Console.Write("####EXESCRIPT DUMP");
                            Console.WriteLine(Dump());
                            break;
                    }
                }
            }
            catch (InvalidOperationException)
            {
                return "ExecScript Finish";
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return "ExecScript Finish";
        }


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
            throw new Exception("Client  cannot get LocalIP");
        }


        //
        // Manipulação dos Registos 
        //
        private void AddFileMetadataEntry(MetadataEntry metaEntry)
        {
            for (int x = 0; x < 10; x++)
            {
                if (FileRegister[x] == null)
                {
                    FileRegister[x] = metaEntry;
                    return;
                }
            }
            throw new PadiException(PadiExceptiontType.Registry, "FileRegisters Are Full, cant open more than 10 Files");

        }


        private MetadataEntry GetFileMetadataEntry(String filename)
        {
            foreach (MetadataEntry entry in FileRegister)
            {

                if (entry != null && entry.FileName.Equals(filename))
                {
                    return entry;
                }

            }
            return null;
        }

        private void UpdateMetadataEntry(MetadataEntry meta)
        {
            for (int x = 0; x < 10; x++)
            {
                if (FileRegister[x] != null && FileRegister[x].FileName.Equals(meta.FileName))
                {
                    FileRegister[x] = meta;
                    return;
                }
            }
        }


        // Remove a referencia para o Registo de metadata
        private Boolean RemoveFileMetadataEntry(String filename)
        {
            for (int x = 0; x < 10; x++)
            {
                if (FileRegister[x] != null && FileRegister[x].FileName.Equals(filename))
                {
                    FileRegister[x] = null;
                    return true;
                }
            }
            return false;
        }


        public override object InitializeLifetimeService()
        {
            return null;

        }

        public bool ReceiveMetaserverResponse(MetaserverResponse response)
        {
            return MetaserverClient.ReceiveMetaserverResponse(response);
        }
    }
}