using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Metadata.ViewStatus;
using SharedLib.MetadataObjects;

namespace Metadata.ViewStates
{
    /// <summary>
    /// The server is running as master or slave
    /// </summary>
    public class ViewOnline : ViewState
    {
        public ViewOnline( MetaViewManager manager)
            : base( manager, ServerStatus.Online )
    {
                
    }

        public override ViewMsg NewRowdyMsgRequest(int source)
        {
            Console.WriteLine("Was online, change to pause");
            Manager.ToPause();
            //There is a new server here.
            return null;
        }

        public override void Start()
        {
             Console.WriteLine( "View online" );
             Manager.MulticastMsg( new ViewMsg( ViewMsgType.StatusUpdate, ServerStatus.Online, Manager.ThisMetaserverId, MetadataServer.GetQueueStateVector( ) ) );
        
            // verificar que todos os estao online ou off agora
            var availableServerStatus = new List<ServerStatus> { ServerStatus.Off, ServerStatus.Online };
            while (!Manager.CheckServerStatus(availableServerStatus, false))
                Thread.Sleep(100);

            //Restart receiving requests
            MetadataServer.PlayServer();
            Console.WriteLine("Server recovered!! ");
        }

        public override void ViewStatusChanged()
        {
            //Ignore
        }
    }
}
