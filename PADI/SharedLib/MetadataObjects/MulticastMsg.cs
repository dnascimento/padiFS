using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLib.MetadataObjects
{
    [Serializable]
   public class MulticastMsg
        {
        public List<int> ServersWhichKnowThisMsg = new List<int>( );
        public MetaMsg Msg = null;
        public String Id;
        public int SourceServer;

        public MulticastMsg( MetaMsg msg, String id,int sourceServer )
            {
            Msg = msg;
            Id = id;
            SourceServer = sourceServer;
            }

        public override string ToString( )
        {
            return "from:" + SourceServer + ": " + Msg.ToString();
        }
        }

     [Serializable]
    public class MulticastMsgAck
        {
        public List<int> ServersWhichKnowThisMsg = new List<int>();
        public String Id;
        public MulticastMsgAck(  String id )
         {
           Id = id;
         }
     }
    
}
