using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Metadata.ViewStates;
using SharedLib.MetadataObjects;

namespace Metadata.ViewStatus
    {
    public class ViewReady : ViewState
    {

    private object serversReadyLocker = new object( );
    List<int> __serversReady = new List<int>( );

    /// <summary>
    /// Start bully algorithm
    /// </summary>
        public ViewReady( MetaViewManager manager )
            : base( manager,ServerStatus.Ready )
        {
            
        }



        public override void Start()
        {
            Console.WriteLine("View Ready");

            //Multicast I'm ready
            //Multicast All to STOP. All servers will change to Paused.
            Manager.MulticastMsg( new ViewMsg( ViewMsgType.StatusUpdate, ServerStatus.Ready, Manager.ThisMetaserverId, MetadataServer.GetQueueStateVector( ) ) );

            //Wait for all server being ready


            //All servers are paused or off
            // Wait until at least on server is ready
            while ( !CheckIfAllServerReady( ) )
                {
                lock ( serversReadyLocker )
                    {
                    Monitor.Wait( serversReadyLocker, 1000 );
                    }
                //TODO ao fim de X, voltar atrás e ver se falharam
                }

            //Change to Online
            Manager.ToOnline();

        }

        public override void ViewStatusChanged()
        {
            CheckIfAllServerReady();
        }

        public override ViewMsg NewRowdyMsgRequest( int source )
            {
            //This server is already Ready so just waiting
            return new ViewMsg( ViewMsgType.StatusUpdate, ServerStatus.Ready, Manager.ThisMetaserverId, MetadataServer.GetQueueStateVector( ) );
            }



        private bool CheckIfAllServerReady()
        {
            List<ServerStatus> statusAllowed = new List<ServerStatus>
                {
                    ServerStatus.Ready,
                    ServerStatus.Online,
                    ServerStatus.Off
                };
            if(Manager.CheckServerStatus(statusAllowed, true))
                lock (serversReadyLocker)
                {
                    Monitor.PulseAll(serversReadyLocker);
                    return true;
                }
            return false;
        }
    }
    }
