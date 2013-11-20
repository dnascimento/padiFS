using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Metadata.ViewStates;
using Metadata.ViewStatus;
using SharedLib;
using SharedLib.MetadataObjects;

namespace Metadata.ViewStates
    {
    /// <summary>
    /// Its a new server, request to stop the cluster to copy the data. After it will move to Updating
    /// </summary>
    public class ViewNew : ViewState
    {
        private object locker = new Object();
        private object serversReadyLocker = new object();
        //The server needs copy from External
        private Boolean __needsCopyFromExternal;

        public ViewNew(MetaViewManager manager, bool needsCopyFromExternal)
            : base( manager, ServerStatus.New )
        {
        __needsCopyFromExternal = needsCopyFromExternal;
        }

    

        public override void Start()
        {
            Console.WriteLine("View new");
            MetadataServer.PauseServer();
            //Multicast All to STOP. All servers will change to Paused.
            Manager.MulticastMsg( new ViewMsg( ViewMsgType.NewRowdy, ServerStatus.New, Manager.ThisMetaserverId, MetadataServer.GetQueueStateVector( ) ) );

            //ViewStatusChanged will finish and change status: 
            //Aguardar que os restantes passem a off ou a paused (ou se for novo, segue
            while (!CheckIfStatusIsKnown())
            {
                lock (locker)
                {
                    Monitor.Wait(locker, 1000);
                }
            }


            CopyProcess( );

            if ( Manager.CountOnServers( ) == 1 )
                {
                Console.WriteLine( "Single server " );
                Manager.ToOnline( );
                return;
                }
    
            //Change state to ready
            Manager.ToReady();
        }

        private void CopyProcess()
        {
             //All servers are paused or off
            if ( CheckNeedToDoCopy( ) && __needsCopyFromExternal )
            {
            System.Console.WriteLine( "Server needs to copy...." );

            // Wait until at least on server is ready
            int i = 0;
            while ( !CheckIfServerReady( ) )
                {
                lock ( serversReadyLocker )
                    {
                    Monitor.Wait( serversReadyLocker, 1000 );
                    }
                    //Ao fim de X, voltar atrás e ver se falharam
                    if (i++ == 5)
                    {
                        CopyProcess();
                    }
                }
                List<int> serversReady;
                lock ( serversReadyLocker )
                    {
                    serversReady = Manager.GetReadyStateServer( );
                    }
                //Copy that server
                if ( !UpdateFromServers( serversReady ) )
                {
                     CopyProcess( );
                }
            }
        }

        /// <summary>
        /// Check if at least on server is Ready (to copy it)
        /// </summary>
        /// <returns></returns>
        private bool CheckIfServerReady( )
        {
            try
            {
                Manager.GetReadyStateServer();
                lock ( serversReadyLocker )
                    {
                    Monitor.PulseAll( serversReadyLocker );
                    return true;
                    }
            }
            catch (Exception)
            {
                return false;
            }
        }


        private Boolean UpdateFromServers(List<int> serverIds)
        {
            foreach(int serverId in serverIds){
                Console.WriteLine( "Copy from : " + serverId );
                IMetaToMeta server = MetadataServer.ConnectToMetaserver( serverId );
                //Request copy from master
                try
                {
                    CopyStructMetadata dataStruct = server.RequestUpdate(MetadataServer.GetQueueStateVector(),MetadataServer.ThisMetaserverId);
                    //Update the server
                    MetadataServer.UpdateServer( dataStruct );
                }
                catch (SocketException)
                {
                    return false;
                }
            }
            return true;
        }

        public override ViewMsg NewRowdyMsgRequest( int source )
        {
        return new ViewMsg( ViewMsgType.StatusUpdate, ServerStatus.New, Manager.ThisMetaserverId, MetadataServer.GetQueueStateVector( ) );
        }

        public override void ViewStatusChanged( )
            {
            CheckIfStatusIsKnown( );
            CheckIfServerReady( );
            }


        private Boolean CheckNeedToDoCopy()
        {
            var availableServerStatus = new List<ServerStatus> { ServerStatus.Off,ServerStatus.New};
            return !Manager.CheckServerStatus( availableServerStatus, false );
        }

        //////////////////////////////////////// Manager Notifications /////////////////////////////////////////////
        private Boolean CheckIfStatusIsKnown( )
        {
            var availableServerStatus = new List<ServerStatus> {ServerStatus.Off, ServerStatus.New, ServerStatus.Pause, ServerStatus.Ready};
            bool result = Manager.CheckServerStatus(availableServerStatus, false);
            if (result)
            {
                  lock ( locker )
                {
                Monitor.PulseAll( locker );
                }
                return true;
            }
            return false;
        }
     }
 }
