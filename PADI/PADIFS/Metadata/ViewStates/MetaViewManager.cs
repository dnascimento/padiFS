using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using Metadata.ViewStatus;
using SharedLib;
using SharedLib.MetadataObjects;
using log4net;

namespace Metadata.ViewStates
{
    public class MetaViewManager
    {
        private static readonly ILog log = LogManager.GetLogger( typeof( MetadataServer ) );


        private ServerStatus[] ViewElements = { ServerStatus.Unknown, ServerStatus.Unknown, ServerStatus.Unknown};
        public int ThisMetaserverId;
        public Dictionary<int, MetaserverId> MetadataServerList;
        public static Mutex ViewElementsMutex = new Mutex();
        public ViewState ServerViewState;
        public long[][] ServersStatusVector = new long[3][];

        /////////////////////////////////////////////////////////////////////////////////////
         // 1º Enviar msg a propor aos outros 2 servidores que passem a paused (parar de receber pedidos)
        //2º Quando mudam para paused, fazem logo ack mas vão continuar a trocar mensagens até conseguirem entregar as que faltam na camada superior
        //3º Quando entregam todas, passam a "ready" e aguardam pelo ready dos restantes servidores
        //4º O new, espera pelo ready de 1 servidor e começa logo a copiar esse ready. Quando termina, envia ready aos restantes
        //5º Quando todos recebem ready dos restantes, quer dizer que todos entregaram o que tinham pendente e que todos estão em igual ponto

        public MetaViewManager(int serverId, Dictionary<int, MetaserverId> metaserverList)
        {
            ThisMetaserverId = serverId;
            MetadataServerList = metaserverList;
            ServerViewState = new ViewOff(this);
            ToOff();
        }

        /////////////////////////////////////////// STATE SET //////////////////////////////////////////////
        public void ToPause( )
            {
            // Parar todos os processos pendentes
            ChangeState( new ViewPause( this ), ServerStatus.Pause );
            }
        public void ToOff( )
            {
            ChangeState( new ViewOff( this ), ServerStatus.Off );
            }

        public void ToOnline( )
            {
            ChangeState( new ViewOnline( this ), ServerStatus.Online );
            Console.WriteLine( "Server Online" );
            Console.WriteLine( ToString( ) );
            }


        public void ToNew(bool empty)
            {
            Console.WriteLine( "Change to New mode" );
            ChangeState( new ViewNew( this, empty ), ServerStatus.New );
            }

        public void ToReady()
        {
            Console.WriteLine("Change to Ready state");
            ChangeState( new ViewReady( this ), ServerStatus.Ready );
        }



        ///////////////////////////////////////////////// Input //////////////////////////////////////////////
        /// <summary>
        /// Remote Requests: Aqui recebo os pedidos remotos
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public ViewMsg BullyRequestsRetrival(ViewMsg msg)
        {
            switch (msg.Type)
            {
               case ViewMsgType.NewRowdy:
                    Console.WriteLine("Retrieve: New Rowdy from: " + msg.Source);
                    UpdateViewServerState( ServerStatus.New, msg.Source, null);
                    return ServerViewState.NewRowdyMsgRequest(msg.Source);
                case ViewMsgType.StatusUpdate:
                    Console.WriteLine("Retrieve: Status Update from: " + msg.Source+" : "+msg.Status);
                    UpdateViewServerState(msg.Status,msg.Source,msg.StateVector);
                    ServerViewState.ViewStatusChanged();
                    return new ViewMsg( ViewMsgType.StatusUpdate, GetStatus( ThisMetaserverId ), ThisMetaserverId, MetadataServer.GetQueueStateVector( ) );
                case ViewMsgType.ServerBetray:
                    int betrayed = msg.Betrayed;
                    Console.WriteLine("Server betray: "+betrayed);
                    if (betrayed == ThisMetaserverId)
                    {
                        // Detecta que alguem me está a expulsar e eu estou ligado
                        Console.WriteLine("Server " + msg.Source + " is cheating on me!!!! I will go to new");
                        ToNew(true);
                    }
                    ServerBetray(msg.Betrayed, msg.Source);
                    return null;
                default:
                    Console.WriteLine("Invalide Request type:" + msg.Type + " from " + msg.Source);
                    throw new Exception("Invalid Request msg: " + msg.Type);
            }
        }


    

        ///////////////////////////////////////////////// OUTPUT //////////////////////////////////////////////
        private delegate void ProcessResponseDel(ViewMsg msg);

        private delegate ViewMsg BullyDel(ViewMsg msg);

        /// <summary>
        /// Aqui recebo as respostas
        /// </summary>
        /// <param name="ar"></param>
        private void BullyResponse(IAsyncResult ar)
        {
            BullyDel del = (BullyDel) ((AsyncResult) ar).AsyncDelegate;
            ViewMsg msg = del.EndInvoke(ar);
            new ProcessResponseDel(ProcessResponse).Invoke(msg);
        }


        private void ProcessResponse(ViewMsg msg)
        {
            if (msg == null)
                return;
           switch (msg.Type)
            {
               case ViewMsgType.StatusUpdate:
                     Console.WriteLine("Retrieve: Status Update from: " + msg.Source+" : "+msg.Status);
                    UpdateViewServerState(msg.Status,msg.Source, msg.StateVector);
                   ServerViewState.ViewStatusChanged();
                break;
                default:
                    Console.WriteLine("Invalide Response type:" + msg.Type + " from " + msg.Source);
                    throw new Exception("Invalid msg type received:" + msg.Type);
            }
        }



        ////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Change Current Status to Update to a new View. Start the state chain
        /// </summary>
        public void UpdateView( )
            {
            ResetView( );
            ToNew(false);
            }

        public void RecoverServer()
        {
            Console.WriteLine("RecoverServer: Reset");
            ResetView( );
            ToNew(true);
        }


        ///////////////////////////////////// Auxiliares /////////////////////////////////////////////        
        private void ResetView()
        {
            ViewElementsMutex.WaitOne();
            {
                for (int i = 0; i < ViewElements.Length; i++)
                {
                    if(i == ThisMetaserverId)
                        UpdateViewServerState( ServerStatus.New, i, null); 
                    else
                        UpdateViewServerState( ServerStatus.Unknown, i, null); 
                }
            }
            ViewElementsMutex.ReleaseMutex();
        }

        public void MulticastMsg(ViewMsg msg)
        {
            msg.Source = ThisMetaserverId;
            msg.Status = GetStatus(ThisMetaserverId);

            ViewElementsMutex.WaitOne( );
            //Contact other servers
            for ( int id = 0; id < ViewElements.Length; id++ )
            {
                if (id == ThisMetaserverId)
                    continue;
               // if ( GetStatus( id ) == ServerStatus.Off )
                 //   continue;
                msg.Destination = id;
                log.Debug( DateTime.Now.Ticks + " [M] Multicast View: " + msg.Status );
                IMetaToMeta server = MetadataServer.ConnectToMetaserver(id);
                BullyDel invokeDel = new BullyDel( server.BullyRequestsRetrival );
                AsyncCallback callback = new AsyncCallback(BullyResponse);
                try
                {
                    Console.WriteLine("Multicast: "+msg+" to "+msg.Destination);
                    invokeDel.BeginInvoke(msg, callback, null);
                }
                catch (SocketException)
                {
                UpdateViewServerState( ServerStatus.Off, id, null);
                }
            }
            ViewElementsMutex.ReleaseMutex( );
        }

        public void UpdateViewServerState(ServerStatus status, int serverId, long[] stateVector)
        {
            ViewElementsMutex.WaitOne( );
            ViewElements[serverId] = status;
            ServersStatusVector[serverId] = stateVector;
            ViewElementsMutex.ReleaseMutex( );

            //Notify
            ServerViewState.ViewStatusChanged();
         }

        public long[][] GetServersStatusVector()
        {
            long[][] result = new long[3][];
             ViewElementsMutex.WaitOne( );
            for (int i = 0; i < 3; i++)
            {
                result[i] = ServersStatusVector[i];
            }
            ViewElementsMutex.ReleaseMutex( );
            return result;
        }

        /// <summary>
        /// Count servers where status is not unknown
        /// </summary>
        /// <returns></returns>
        public int CountKnownServerState()
        {
            int knownstatus = 0;
            ViewElementsMutex.WaitOne();
            foreach (ServerStatus status in ViewElements)
            {
            if ( status != ServerStatus.Unknown )
                    knownstatus++;
            }
            ViewElementsMutex.ReleaseMutex();
            return knownstatus;
        }

  

        public List<int> GetOnlineServers( )
        {
            List<int> online = new List<int>();
            ViewElementsMutex.WaitOne();
            for ( int i = 0; i < ViewElements.Length; i++)
            {
                ServerStatus status = ViewElements[i];
                if ( status == ServerStatus.Online )
                    online.Add(i);
           }
            ViewElementsMutex.ReleaseMutex();
            return online;
        }



        private void ChangeState( ViewState state, ServerStatus status )
        {
            ViewElementsMutex.WaitOne();
            ViewElements[ThisMetaserverId] = status;
            ViewElementsMutex.ReleaseMutex();
            lock (ServerViewState)
            {
                ServerViewState = state;
            }
            ServerViewState.Start();
        }

        public override String ToString()
        {
            StringBuilder str = new StringBuilder();
            str.AppendLine("Current View of Metaserver " +ThisMetaserverId+":");
            ViewElementsMutex.WaitOne();
            for (int i = 0; i < ViewElements.Length; i++)
            {
                str.AppendLine("Server: " + i + " is " + ViewElements[i]);
            }
            ViewElementsMutex.ReleaseMutex();
            return str.ToString();
        }

        public ServerStatus GetStatus( int serverId )
        {
            ViewElementsMutex.WaitOne();
            ServerStatus status = ViewElements[serverId];
            ViewElementsMutex.ReleaseMutex();
            if(status == ServerStatus.Unknown)
                throw  new Exception("Unknown state");
            return status;
        }

 

        public string Dump()
        {
            return this.ToString();
        }


        public Boolean RemoveServerFromView(int id)
        {
            ViewElementsMutex.WaitOne( );
            if (id > ViewElements.Length)
            {
                Console.WriteLine("RemoveServerFromView: " + id + " out of scope");
                return false;
            }
            ViewElements[id] = ServerStatus.Off;
            ViewElementsMutex.ReleaseMutex( );
            return true;
        }

        public int CountOnlineServers()
        {
            return GetOnlineServers().Count;
        }

        public ServerStatus[] GetViewStatus( )
            {
             ServerStatus[] snapshot = new ServerStatus[3];
            ViewElementsMutex.WaitOne( );
            for (int i = 0; i < ViewElements.Length; i++)
                snapshot[i] = ViewElements[i];
            ViewElementsMutex.ReleaseMutex( );
            return snapshot;
            }


        /// <summary>
        /// Check if servers are on of allowedStatus. 
        /// </summary>
        /// <param name="allowedStatus"></param>
        /// <param name="checkThisServer">Flag: This server must be on one of this states too?</param>
        /// <returns></returns>
          public Boolean CheckServerStatus(List<ServerStatus> allowedStatus,Boolean checkThisServer)
        {
            ViewElementsMutex.WaitOne();
            for (int i = 0; i < ViewElements.Length; i++)
            {
                if (!checkThisServer && i == ThisMetaserverId)
                    continue;
                ServerStatus status = ViewElements[i];
                if (!allowedStatus.Contains(status))
                {
                ViewElementsMutex.ReleaseMutex( );
                return false;
                }
            }
            ViewElementsMutex.ReleaseMutex();
            return true;
        }

        /// <summary>
        /// Get a server which state is ready
        /// </summary>
        /// <returns></returns>
        public List<int> GetReadyStateServer()
        {
            List<int> readyServers = new List<int>();
            ViewElementsMutex.WaitOne( );
            for ( int i = 0; i < ViewElements.Length; i++ )
                {
                    if (i == ThisMetaserverId )
                        continue;
                    ServerStatus status = ViewElements[i];
                    if (status == ServerStatus.Ready)
                    {
                        readyServers.Add(i);
                    }
                }
          ViewElementsMutex.ReleaseMutex( );
          if(readyServers.Count == 0)
            throw new Exception("There is no ReadyState servers");
          return readyServers;
        }



        public int GetNextServerInView( int server, List<int> knownByThisServersList, out bool iAmLast )
        {
        return GetRelativeServerInView( server, knownByThisServersList, out iAmLast, true );
        }
        public int GetPreviousServerInView(int server, List<int> knownByThisServersList, out bool iAmLast)
        {
        return GetRelativeServerInView( server, knownByThisServersList, out iAmLast, false );
        }


        private int GetRelativeServerInView(int server,List<int> knownByThisServersList, out bool iAmLast, bool next)
        {
            List<int> onlineServers = GetOnlineServers();
            List<int> serversLeft = onlineServers.Except(knownByThisServersList).ToList();
            //Avoid loops
            serversLeft.Remove(ThisMetaserverId);

            //If no server remaining or Im the only  remaining
            if (serversLeft.Count == 0 || (serversLeft.Count == 1 &&  serversLeft[0] == ThisMetaserverId ))
            {
                iAmLast = true;
                return -1;
            }

            iAmLast = false;
            if (serversLeft.Count == 1)
            {
                return serversLeft[0];
            }
            

            int index = 0;
            if (!onlineServers.Contains(server))
            {
                throw new Exception( "This server is not online anymore" );
            }

            while (true)
            {                    
                //Go to our position in circle
                if (onlineServers[index] != server)
                {
                    index++;
                    continue;
                }
                //Start walking from there
                while (true)
                    {
                    //Current on right index, go to next
                    if ( next )
                        index += 1;
                    else
                        index -= 1;

                    //Index belongs to [0,2]
                    index = Math.Abs (index % onlineServers.Count);
                    int proposal = onlineServers[index];
                    //If this proposal element is the missing, return
                    if (serversLeft.Contains(proposal) && proposal != ThisMetaserverId)
                        return proposal;
                    }
            }
        }

        public int CountOnServers( )
        {
            int count = 0;
            ViewElementsMutex.WaitOne( );
            for ( int i = 0; i < ViewElements.Length; i++ )
                {
                ServerStatus status = ViewElements[i];
                    if (!(status == ServerStatus.Off || status == ServerStatus.Unknown))
                        count++;
                }
            ViewElementsMutex.ReleaseMutex( );
            return count;
            }


        public bool IsBiggestId()
        {
            foreach (int onlineServer in GetOnlineServers())
            {
                if (onlineServer > ThisMetaserverId)
                    return false;
            }
            return true;
        }

       /// <summary>
        /// Deunnciar que existe um servidor que falhou
       /// </summary>
       /// <param name="betrayedServer"></param>
       /// <param name="knownFrom">Quem o avisou</param>
        public void ServerBetray(int betrayedServer,int knownFrom)
        {
            //Anunciar aos restantes da view que este falhou.
            ViewMsg msg = new ViewMsg(ViewMsgType.ServerBetray, GetStatus( ThisMetaserverId ), ThisMetaserverId, MetadataServer.GetQueueStateVector( ) );
            msg.Source = ThisMetaserverId;
            msg.Status = GetStatus(ThisMetaserverId);
            msg.Betrayed = betrayedServer;

            ViewElementsMutex.WaitOne( );
            //Contact other servers
            for ( int id= 0; id < ViewElements.Length; id++ )
            {
                if (id == ThisMetaserverId || id == knownFrom)
                    continue;
                
                if (ViewElements[id] == ServerStatus.Off)
                {
                    Console.WriteLine("Already betrayed"+id);
                    continue;
                }

                msg.Destination = id;
                try
                {
                    Console.WriteLine("Betray: "+betrayedServer+" to "+msg.Destination);
                    MetadataServer.ConnectToMetaserver(id).BullyRequestsRetrival(msg);
                }
                catch (SocketException){
                    UpdateViewServerState( ServerStatus.Off, id, null);
                    if ( id != betrayedServer && !(ViewElements[id].Equals(ServerStatus.Off)))
                         ServerBetray(id,ThisMetaserverId);
                }
            }
            //Retirar este servidor da minha view
            Console.WriteLine("Remove betrayed: "+betrayedServer);
            RemoveServerFromView(betrayedServer);
            ViewElementsMutex.ReleaseMutex( );         
        }
    }
}
