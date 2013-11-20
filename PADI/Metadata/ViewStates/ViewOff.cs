using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Metadata.ViewStatus;
using SharedLib.MetadataObjects;

namespace Metadata.ViewStates
{
    /// <summary>
    /// The server is running as master or slave
    /// </summary>
    public class ViewOff : ViewState
    {
        public ViewOff( MetaViewManager manager )
            : base( manager, ServerStatus.Off )
    {
                
    }

        public override ViewMsg NewRowdyMsgRequest(int source)
        {
        return new ViewMsg( ViewMsgType.StatusUpdate, ServerStatus.Off, Manager.ThisMetaserverId, MetadataServer.GetQueueStateVector( ) );
        }

        public override void Start()
        {
            Console.WriteLine( "View off" );   
        }

        public override void ViewStatusChanged()
        {
            //Ignore
        }
    }
}
