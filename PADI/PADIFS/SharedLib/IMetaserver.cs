using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedLib.MetadataObjects;

namespace SharedLib
{

    public enum MetaCallType
    {
        ToMaster, ToSlave, FromClient
    }

    public interface IMetaToClient
    {
         MetaserverResponse ClientProcessRequest( MetaRequest request );

    }

    public interface  IMetaToMeta
    {
        void NewRequest( MetaRequest msg );
        ViewMsg BullyRequestsRetrival(ViewMsg msg);
        //Copy 
        CopyStructMetadata RequestUpdate( long[] sourceState,int sourceId);
        //MetaserverResponse
        void ReceiveResponse(MetaserverResponse response);
    }



    public interface IMetaToPuppet
    {
        String Dump();
        String Fail();
    }



     public interface IMetaToData
     {
         //The serverId will be the unique identifier
         Boolean Registry(String serverId,String serverIp,int serverPort);
         //Void PushStatistics()
     }

    public interface IRecovery
    {
        String Recover();
    }

 

}
