using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace SharedLib.MetadataObjects
{
    [Serializable()]
    public class MetadataEntry
    {
        public String FileName;
        public int NBDataServers;
        public int ReadQuorum;
        public int WriteQuorum;
        //ServerEntry, localfilename
        public ConcurrentDictionary<ServerId, String> ServerFileList = new ConcurrentDictionary<ServerId, string>();
        private HashSet<int> ClientsWithFileOpenSet = new HashSet<int>( );
        public long monotonicReadVersion = -1;

     


        public MetadataEntry(String fileName, int nbDataServers, int readQuorum, int writeQuorum)
        {
            this.FileName = fileName;
            this.NBDataServers = nbDataServers;
            this.ReadQuorum = readQuorum;
            this.WriteQuorum = writeQuorum;
        }

        public Boolean IsOpen()
        {
        lock ( ClientsWithFileOpenSet )
        {
            int open = ClientsWithFileOpenSet.Count;
            return (open != 0);
        }
        }


        public override String ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append( "FileName: " + FileName + ", D/R/W/O: " + NBDataServers + "/" + ReadQuorum + "/" + WriteQuorum + "/" + PrintClientSet() + "\r\n" );
            foreach (KeyValuePair<ServerId, String> data in ServerFileList)
            {
                builder.Append("    ServerID: " + data.Key.id + " (" + data.Key.hostname + ":"+ data.Key.port + ")");
                builder.Append("   |   LocalFileName: " + data.Value + "\r\n");
            }
            return builder.ToString();
        }

        private String PrintClientSet(  )
        {
            StringBuilder builder = new StringBuilder();
            int counter = ClientsWithFileOpenSet.Count;
            if (counter == 0)
            {
                return "This file is not open";
            }
            foreach (int client in ClientsWithFileOpenSet)
            {
                builder.Append("|"+client);
            }
            return builder.ToString();
        }



        public Boolean AddClient( int clientId )
        {
            lock (ClientsWithFileOpenSet)
            {
                ClientsWithFileOpenSet.Add(clientId);
            }
            return true;
        }

        public Boolean RemoveClient(int clientId)
        {
            lock (ClientsWithFileOpenSet)
            {
                Console.WriteLine("RemoveClient:"+clientId);
                Console.WriteLine(ClientsWithFileOpenSet);
                return ClientsWithFileOpenSet.Remove(clientId);
            }
        }

        public MetadataEntry Clone()
        {
            MetadataEntry entry = new MetadataEntry(FileName,NBDataServers,ReadQuorum,WriteQuorum);
            entry.ServerFileList = new ConcurrentDictionary<ServerId, string>(ServerFileList);
            entry.ClientsWithFileOpenSet = new HashSet<int>(ClientsWithFileOpenSet);
            entry.monotonicReadVersion = monotonicReadVersion;
            return entry;
        }
    }

    [Serializable]
    public struct ServerId
    {
        //Unique server name
        public String id;
        //Server IP
        public String hostname;
        //Server port
        public int port;
        public int recoverPort;
    }
}
