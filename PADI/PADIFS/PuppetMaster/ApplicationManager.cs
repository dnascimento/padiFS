using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedLib.MetadataObjects;

namespace PuppetMaster
{
    /// <summary>
    /// Process manager class. Todos os processos estao aqui. Pode ou nao lancar processos para que possam ser
    /// maquinas remotas inves de processos. Os ids de metadados sao estaticos
    /// </summary>
    public class ApplicationManager
    {
        private readonly  String _baseDirectory = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "..\\..\\..\\");
        
        //Metaserver ms
        private const String ms0IP = "localhost";
        private const int ms0Port = 8020;
        private const int ms0RecPort = 8010;

        private const String ms1IP = "localhost";
        private const int ms1Port = 8021;
        private const int ms1RecPort = 8011;


        private const String ms2IP = "localhost";
        private const int ms2Port = 8022;
        private const int ms2RecPort = 8012;

        public List<ServerId> ClientList = new List<ServerId>();
        public List<ServerId> DataserverList = new List<ServerId>();
        public List<ServerId> MetaserverList = new List<ServerId>();


        private  int _nextClientPort = 8090;
        private int _nextClientId = 1;

        private int _nextDataServerPort = 8030;
        private int _nextDataServerRecoverPort = 8050;
        private int _nextDataServerId = 1;


        /// <summary>
        /// Puppet: Nao tem nem porto nem endereco de IP. Só faz contactos para fora
        /// Client: 8090/ClientConnection
        /// DataServer: 8030 /DataserverConnection    
        /// Metadata: 8020, 8021,8022 /MetaserverConnection    
        /// </summary>
        public void LaunchServers()
        {
           // AddNewMetaserver(0);
            //AddNewMetaserver(1);
            //AddNewMetaserver(2);

       //     AddNewClient("localhost");
      //      AddNewClient( "localhost" );

        //    AddNewDataserver("localhost");
        //    AddNewDataserver("localhost");
          //  AddNewDataserver( "localhost");
            
        }

        public void LoadLocalServers()
        {
            AddNewMetaserver(0, false);
            AddNewClient("localhost", false);
            AddNewDataserver("localhost", false);
        }

        //////////////////////////////////// Create //////////////////////////////////////////////////
        public void AddNewClient(String hostname,Boolean launch = true)
        {
            int id = _nextClientId++;
            int port = _nextClientPort++;

            ServerId server = new ServerId();
            server.id = "c-" + id;
            server.hostname = hostname;
            server.port = port;
            ClientList.Add(server);

            if (launch)
            {
                 Process client = new Process();
                 client.StartInfo.FileName = Path.Combine(_baseDirectory, "Client\\bin\\Debug\\Client.exe");
                 client.StartInfo.Arguments = ms0IP + " " + ms0Port + " " + ms1IP + " " + ms1Port + " " + ms2IP+" "+ms2Port + " "  +port+ " "+id;
                 client.Start();
            }
        }

        public void AddNewDataserver(String hostname, Boolean launch = true)
        {
            int serverNumber = _nextDataServerId++;
            int port = _nextDataServerPort++;
            int recoverPort = _nextDataServerRecoverPort++;

            ServerId server = new ServerId();
            server.id = "d-"+serverNumber;
            server.hostname = hostname;
            server.port = port;
            server.recoverPort = recoverPort;
            DataserverList.Add(server);

            if (launch)
            {
                 Process dataServer = new Process();
                 dataServer.StartInfo.FileName = Path.Combine(_baseDirectory, "DataServer\\bin\\Debug\\DataServer.exe");
                 dataServer.StartInfo.Arguments = ms0IP + " " + ms0Port + " " + ms1IP + " " + ms1Port + " " + ms2IP + " " + ms2Port + " " + port + " " + serverNumber + " " + recoverPort;
                 dataServer.Start();
            }
        }

        public void AddNewMetaserver(int serverNumber,Boolean launch = true)
        {
            ServerId server = new ServerId();
            server.id = "m-"+serverNumber;
            switch (serverNumber)
            {
                case 0:
                     server.hostname = ms0IP;
                    server.port = ms0Port;
                    server.recoverPort = ms0RecPort;
                    break;
                case 1:
                    server.hostname = ms1IP;
                    server.port = ms1Port;
                    server.recoverPort = ms1RecPort;
                    break;
                case 2:
                    server.hostname = ms2IP;
                    server.port = ms2Port;
                    server.recoverPort = ms2RecPort;
                    break;
                default:
                    throw new Exception("Invalid metaserver number");
            }
            MetaserverList.Add(server);
            if (launch)
            {
                 Process metaServer = new Process();
                 metaServer.StartInfo.FileName = Path.Combine(_baseDirectory, "Metadata\\bin\\Debug\\Metadata.exe");
                 metaServer.StartInfo.Arguments = ms0IP + " " + ms0Port + " " + ms1IP + " " + ms1Port + " " + ms2IP + " " + ms2Port + " " + serverNumber + " " + server.recoverPort;
                 metaServer.Start();
            }
        }

        /// <summary>
        /// Get Process's port and IP
        /// </summary>
        /// <param name="process">Client,MetaServer,DataServer</param>
        /// <param name="number">0,1....</param>
        /// <param name="ip">output hostname</param>
        /// <param name="port">output port</param>
        /// <returns>true if found</returns>
        public Boolean GetProcessIPandPort( ProcessType process, int number, Boolean recovery, out String ip, out int port )
        {
            String processId;
            List<ServerId> serverList;
            switch (process)
            {
                 case ProcessType.Client:
                    processId = "c-" + number;
                    serverList = ClientList;
                    break;
                case ProcessType.DataServer:
                    processId = "d-" + number;
                    serverList = DataserverList;
                    break;
                case ProcessType.MetaServer:
                    processId = "m-" + number;
                    serverList = MetaserverList;
                    break;
                default:
                    throw new Exception("Invalid Process type");
            }
            foreach (ServerId server in serverList)
            {
                if (server.id.Equals(processId))
                {
                    if (recovery && process != ProcessType.Client)
                       port = server.recoverPort;
                    else
                        port = server.port;
                   
                    ip = server.hostname;
                    return true;
                }
            }
            ip = null;
            port = 0;
            return false;
        }

    }

    public enum ProcessType
    {
        Client,DataServer,MetaServer
    }
}
