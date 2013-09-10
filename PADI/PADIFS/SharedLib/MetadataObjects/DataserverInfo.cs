using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedLib.MetadataObjects;

namespace Metadata
{
    [Serializable]
    public class DataserverInfo
    {
        public ServerId IdStruct = new ServerId( );
        public List<String> LocalfileNames = new List<string>();
        public long TotalRead;
        public long TotalWrite;
        public int TotalFiles;

        public DataserverInfo(String serverId, String hostname, int port)
        {
            IdStruct.hostname = hostname;
            IdStruct.port = port;
            IdStruct.id = serverId;
        }


    }
}
