using System;
using System.Collections.Generic;
using SharedLib.Exceptions;

namespace SharedLib.MetadataObjects
    {
    [Serializable]
    public class MetaserverResponse
    {

        public PadiException Exception;
        public ResponseStatus Status;
        public MetadataEntry MetaEntry;
        public MetaRequest OriginalRequest;
        public List<int> KnownByThisServersList = new List<int>();
        //Metaserver ID which create this response
        public int ResponseSource;

        public MetaserverResponse( ResponseStatus status, MetaRequest originalRequest,int responseSource )
        {
            Status = status;
            OriginalRequest = originalRequest;
            ResponseSource = responseSource;
        }

        public override string ToString( )
        {
            return "Response to: " + OriginalRequest.ClientReqStamp;
        }

    }


        public enum ResponseStatus
        {
            Ack,AckDeliver,Success,InvalidRequest,Exception
        }
    }
