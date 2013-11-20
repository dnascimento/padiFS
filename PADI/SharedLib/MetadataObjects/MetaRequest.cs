using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLib.MetadataObjects
{
    [Serializable]
    public class MetaMsg
    {
        public MetaRequest Request;
        public MetaRequestIdAssociation Association;
        public MetaMsgType MsgType;
        public MetaMsg( MetaRequest request )
        {
            Request = request;
            MsgType = MetaMsgType.Request;
        }
        public MetaMsg(MetaRequestIdAssociation association)
        {
            Association = association;
            MsgType = MetaMsgType.MetaRequestIdAssociation;
        }

        public override string ToString( )
        {
        return "MetaMsg:" + MsgType + " : " + Request +" : "+Request.ClientReqStamp;
        }
    }
    [Serializable]
    public class MetaRequestIdAssociation 
    {
        //Partition id: eqNumber
        public long PartitionReqOrder;
        //Source ID: S1-timestamp
        public String SourceServerReqStamp;
        public String Filename;
        public MetaRequestIdAssociation( String sourceServerReqStam, long partitionReqOrder, String filename )
        {
            PartitionReqOrder = partitionReqOrder;
            SourceServerReqStamp = sourceServerReqStam;
            Filename = filename;
        }
    }

    [Serializable]
    public enum MetaMsgType
    {
        Request,MetaRequestIdAssociation
    }

    /////////////////////////////////// REQUESTS ////////////////////////////////////////


    [Serializable]
   public enum RequestType
    {
        Open,Close,Create,Delete,Registry,Balancing
    }
 
        [Serializable]
public abstract class MetaRequest
        {
         public RequestType RequestType;
        public String FileName;

       //Unique client request ID
        public int ClientId;
        public String ClientReqStamp;
        public int ClientPort;
        public String ClientHostname;
       //Numero de tentativas realizadas pelo cliente para ver se o pedido e repetido
        public int Attempt = 0;

        public long NamespaceSeqNumber;
        public int Serializer = -1;
        public string SourceTimeStamp;

        public List<int> KnownByThisServersList = new List<int>();

        public override string ToString( )
        {
            return "Request: " + RequestType + " : " + FileName;
        }
        
       }

        [Serializable]

public class RequestOpen : MetaRequest
{
    public RequestOpen(String fileName)
    {
        FileName = fileName;
        RequestType = RequestType.Open;
    }
}

    [Serializable]
public class RequestClose : MetaRequest
    {
    public RequestClose( String fileName)
        {
        FileName = fileName;
        RequestType = RequestType.Close;
        }
    }


    [Serializable]
    public class RequestBalancing : MetaRequest
    {
        public BalancingStatus Reads = BalancingStatus.Ok;
        public BalancingStatus Writes = BalancingStatus.Ok;

        public RequestBalancing(String fileName)
        {
            FileName = fileName;
            RequestType = RequestType.Balancing;
        }
   }



        [Serializable]
public class RequestCreate : MetaRequest
    {
    public int NbDataServer;
    public int ReadQuorum;
    public int WriteQuorum;
    public RequestCreate( String fileName, int nbDataServer, int readQuorum, int writeQuorum)
        {
        FileName = fileName;
        NbDataServer = nbDataServer;
        ReadQuorum = readQuorum;
        WriteQuorum = writeQuorum;
        RequestType = RequestType.Create;
        }
    }

        [Serializable]
public class RequestDelete : MetaRequest
    {
    public RequestDelete( String fileName)
        {
        FileName = fileName;
        RequestType = RequestType.Delete;
        }
    }

    

    [Serializable]
    public class RequestRegistry:MetaRequest
    {
        public String ServerId;
        public String ServerIp;
        public int ServerPort;

        public RequestRegistry( String serverId, String hostname, int serverPort )
        {
            ServerId = serverId;
            ServerIp = hostname;
            ServerPort = serverPort;
            RequestType = RequestType.Registry;
        }
   }

    public enum BalancingStatus
    {
        Excess,Few,Ok
    }
}
