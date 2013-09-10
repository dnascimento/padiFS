using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLib.DataserverObjects
{

    [Serializable]
    public class VersionFile
    {
        public long versionNumber;
        public String responseServerId;

        public VersionFile() { }
        public VersionFile(string serverID, long version)
        {
            versionNumber = version;
            responseServerId = serverID;
        }

    }


    [Serializable]
    public class TFile
    {
        public long VersionNumber;
        public long Size;
        public byte[] Data;
        public String responseServerId;


        public TFile()
        {

        }
        public TFile(long versionNumber, byte[] data)
        {
            VersionNumber = versionNumber;
            Data = data;
            this.Size = data.Length * 8;
        }

        public override string ToString()
        {
            String txt = Encoding.ASCII.GetString(Data);
            StringBuilder builder = new StringBuilder();
            builder.Append("VersionNumber: " + VersionNumber);
            builder.Append(", Size: " + Size);
            builder.Append(", Data: " + txt);
            return builder.ToString();
        }
    }

    [Serializable]
    public class LocalFileStatistics
    {
        public LocalFileStatistics(string filename, string localFileName)
        {
            this.filename = filename;
            this.localFileName = localFileName;
        }

        public String filename = null;
        public String localFileName = null;
        public long version = 0;
        public long size = 0;
        public long readTraffic = 0;
        public long writeTraffic = 0;
        public readonly object mutex = new object();
    }

    [Serializable]
    public class InvalidVersionException : ApplicationException
    {
        public String campo;

        public InvalidVersionException()
        {

        }

        public InvalidVersionException(String c)
        {
            campo = c;
        }

        public InvalidVersionException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
            campo = info.GetString("campo");
        }

        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("campo", campo);
        }
    }

}
