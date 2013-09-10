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
    public class ViewPause : ViewState
    {


    /// <summary>
    /// Start bully algorithm
    /// </summary>
        public ViewPause( MetaViewManager manager )
            : base( manager, ServerStatus.Pause )
        {
        }


        public override void Start()
        {
            MetadataServer.PauseServer( );
            long[] state = MetadataServer.GetQueueStateVector( );
            Console.WriteLine( "View Pause with state: " + state.ToArray() );
            Manager.UpdateViewServerState(ServerStatus.Pause, MetadataServer.ThisMetaserverId, state);

            // ask Paused to other
            Manager.MulticastMsg( new ViewMsg( ViewMsgType.StatusUpdate,ServerStatus.Pause, Manager.ThisMetaserverId, state) );
            //Wait to know the state of all new servers
            //Se houver algum online, paused ou ready, queremos saber o estado dele
            //Verificar quais as maiores posicoes
            long[] lastestStatus = WaitUntilSameStatus(state );
            //Deliver all message on application layer
           Console.WriteLine("All message delivered");
            
            //Change state to ready
            Manager.ToReady();
        }


        public long[] WaitUntilSameStatus( long[] state )
        {
            Console.WriteLine( "Waiting for same status" );
            ServerStatus[] snapshot = Manager.GetViewStatus( );
            long[][] status = Manager.GetServersStatusVector( );
            long[] lastStatus = state;
            for ( int i = 0; i < 3; i++ )
                {
                //Se existe um online ou em pause, vou esperar para receber o seu estado.
                if ( status[i] == null && (snapshot[i] == ServerStatus.Online || snapshot[i] == ServerStatus.Pause || snapshot[i] == ServerStatus.Ready) )
                    {
                    Console.WriteLine("Status missing from:"+i);
                    Thread.Sleep( 100 );
                    WaitUntilSameStatus( state );
                    }
                if (status[i] == null)
                {
                    Console.WriteLine("I dont need to know the state of server: "+i);
                    continue;
                }
                Console.WriteLine("I know the status of server: "+i);
                for ( int k = 0; k < status[i].Length; k++ )
                    {
                    lastStatus[k] = Math.Max( status[i][k], lastStatus[k] );
                    }
                }
            return lastStatus;
        }






        public override void ViewStatusChanged()
        {
            //Ignore
        }


        public override ViewMsg NewRowdyMsgRequest( int source )
            {
            return new ViewMsg( ViewMsgType.StatusUpdate, ServerStatus.Pause, Manager.ThisMetaserverId, MetadataServer.GetQueueStateVector( ) );
            }
    }
    }
