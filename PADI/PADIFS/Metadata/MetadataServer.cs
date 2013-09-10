using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading;
using Metadata.ViewStates;
using SharedLib;
using SharedLib.Exceptions;
using SharedLib.MetadataObjects;
using log4net;

namespace Metadata
    {
    public class MetadataServer : MarshalByRefObject, IMetaToPuppet, IMetaToClient, IMetaToMeta
    {

        private static readonly ILog log = LogManager.GetLogger( typeof( MetadataServer ) );

        
        public static Dictionary<int, MetaserverId> MetadataServerList = new Dictionary<int, MetaserverId>( );
        public static int ThisMetaserverId;
        public static int PortRecovery;
        //Request ID
        public static Int64 LocalRequestId = 0;
        public static MetaViewManager ViewManager;
        public static NamespaceManager NamespaceManager;
        private static EventWaitHandle freezeNewRequests = new EventWaitHandle( false, EventResetMode.ManualReset, "freezeNewRequests" + MetadataServer.ThisMetaserverId );

        //Metaserver Application core
        public static MetaCore core;

        public static void Main( string[] args )
            {

            log4net.Config.XmlConfigurator.Configure( );

            log.Debug( DateTime.Now.Ticks+" Metadata init..." );


            Console.WriteLine( "Metadata Init...." );
           
        //    if (!Debugger.IsAttached)
          //       Debugger.Launch();


            int i = 0;
            for ( i = 0; i < (args.Length - 2); i++ )
                {
                MetaserverId metaServer = new MetaserverId( );
                metaServer.Hostname = args[i++];
                metaServer.Port = Convert.ToInt32( args[i] );
                metaServer.Id = i / 2;
                MetadataServerList.Add( metaServer.Id, metaServer );
                }
            ThisMetaserverId = Convert.ToInt32( args[i++] );
            PortRecovery = Convert.ToInt32( args[i] ); ;
            System.Console.WriteLine( "System ID: " + ThisMetaserverId );
            Console.Title = "Metaserver: " + ThisMetaserverId;
            ViewManager = new MetaViewManager( ThisMetaserverId, MetadataServerList );
            NamespaceManager = new NamespaceManager( );


            System.Console.WriteLine( "Metadata servers list loaded..." );
            RemotingConfiguration.Configure( "../../App.config", true );
            System.Console.WriteLine( "Metadata Server init..." );

            CreateEmergencyChannel( );
            //CreateMetaserverChannel( );

            System.Console.WriteLine( "<enter> to exit..." );
            System.Console.ReadLine( );
            }


         /////////////////////////////////////////////////////////////// Thread Sync ///////////////////////////////////////////////
        public delegate void NewRequestDelegate( MetaRequest request );
         /**
          * Client Requests Retrival, process begins here:
          * Request stamp with localID+timestamp (Local Seq.Number)
          * Enqueue on this server
          * Deliver message to other view's servers
          * Ack client since other servers have this request, he can relax and wait.
          */
        public  MetaserverResponse ClientProcessRequest( MetaRequest request )
            {
                log.Debug( DateTime.Now.Ticks + " ClientProcessRequest: " + request );
                //if stop, enque this thread
                freezeNewRequests.WaitOne( );
                Console.WriteLine("Passed");
                //Stamp with local server ID
                request.SourceTimeStamp = "s-" + ThisMetaserverId + ":" + GetLocalTimeStamp();

                //Ack client 
                new NewRequestDelegate( NewRequestProcess ).BeginInvoke(request, null, null);
                Console.WriteLine( "Client process request" );
                return new MetaserverResponse( ResponseStatus.Ack, request, ThisMetaserverId ); 
            }

       

        /// <summary>
        /// Receber o pedido do anterior, verificar se sou o serializer. Se sim, coloco o meu
        /// timestamp nisto. entrego à app e depois encaminho ao seguinte. Se sou o ultimo, 
        /// </summary>
        /// <param name="request"></param>
        public void NewRequest(MetaRequest request)
        {
            //TODO Enviar um ack se ja conhecer este ficheiro ou nao (fazer o msg ack distinto)
            //SE JA CONHECE, Envia ao seguinte na mesma para que o seguinte trate disso.
            NewRequestDelegate del = new NewRequestDelegate( NewRequestProcess );
            del.Invoke(request);

        }

        /// <summary>
        /// Corre numa thread independente
        /// </summary>
        /// <param name="request"></param>
        private void NewRequestProcess(MetaRequest request){
            Console.WriteLine("New Request: " + request);
            log.Debug( DateTime.Now.Ticks + " New Request: " + request );

            //Get NamespaceId
            int namespaceId = NamespaceManager.GetNameSpaceID(request.FileName);

            //Get Serializer ID. if equal mine, get this sector next ID and stamp it
            int serializer = NamespaceManager.GetPartSerializer(namespaceId, ViewManager);


            //A mensagem nao tem serializador mas sou eu: entao serializo
            if (request.Serializer == -1)
            {
                if (serializer == ThisMetaserverId)
                {
                    //sou eu a serializar isto
                    request.Serializer = ThisMetaserverId;
                    //Se os servidores querem congelar, nao vou lancar mais pedidos. 
                    //Se nao serializar nem aceitar novos pedidos, o sistema fica isolado
                    freezeNewRequests.WaitOne( );
                    Console.WriteLine( "Passed Serializer" );
                    log.Debug( DateTime.Now.Ticks + " Start Serializing request: " + request );
                    request = NamespaceManager.SerializeRequest( request );
                    log.Debug( DateTime.Now.Ticks + " Request serialized: " + request );
                }
                else
                {
                    //passo a outro para ele serializar isto e espero que volte
                    SendToNext( request, null );
                    return;
                }
            }
            //agora tem serializador de certeza
            if (request.Serializer != serializer)
            {
                //Discordo quem seja o serializador
                Console.WriteLine("Dont agree: "+request.Serializer+" is the serializer. Should be: "+serializer);
            }

           //Concordo com o serializador, entregar
            Console.WriteLine( "Deliver to App " + request );
            //Deliver to app (if this request is newer (serializer tells it))
            MetaserverResponse result = AppDelivery(request);

            SendToNext(request,result);
        }

        /// <summary>
        /// Tentar enviar ao seguinte.
        /// O seguinte é serializer, responde ao client. Envio ao seguinte
        /// </summary>
        private void SendToNext(MetaRequest request,MetaserverResponse result)
        {
            log.Debug( DateTime.Now.Ticks + " Send to Next: " + request );
            //Stamp as know by this server
            if (result != null)
            {
                request.KnownByThisServersList.Add( ThisMetaserverId );
            }


            //Check if this is the last server in view to know it
            int nextServer = ThisMetaserverId;
            //Se o seguinte falha, faço ack ao anterior, nao ha mais


            while(true)
            {
                Boolean iAmLast = false;
                nextServer = ViewManager.GetNextServerInView( nextServer, request.KnownByThisServersList, out iAmLast );
                if ( iAmLast )
                {
                    //Pode ser null, se o serializer estava indisponivel, vamos recalcular
                    if ( result == null )
                    {
                        Console.WriteLine("Initial serializer is not available");
                        request.Serializer = -1;
                        NewRequestProcess(request);
                        return;
                    }
                    //If I am last, reply to client and then ack back to others else:
                    Debug.Assert(result != null);
                    SendResponseToClient(result);
                    Console.WriteLine("I am the last " + request);
                    ReceiveResponse( result ); 
                    return;
                }
                {
                try
                    {
                        //Tentar enviar ao seguinte
                        //TODO Se ele responder que desconhece, fico à espera do ack. Se ao fim de x tempo nao receber, enviar outra vez 
                        log.Debug( DateTime.Now.Ticks + " [M] Message Sent: " + request.FileName );
                        ConnectToMetaserver( nextServer ).NewRequest( request );
                        return;
                    }
                catch ( SocketException  )
                    {
                    //Actualizar a view, esta mensagem tem de ser entregue mas a view tem de ser actualizada,
                    Console.WriteLine("Next server fail");
                    ViewManager.ServerBetray( nextServer,ThisMetaserverId );
                    }
                }
            }
        }



    public void ReceiveResponse(MetaserverResponse response)
        {
             Console.WriteLine( "Received Response ");
             log.Debug( DateTime.Now.Ticks + " ReceiveResponse: " + response );

            response.KnownByThisServersList.Add(ThisMetaserverId);
            //TODO check threads waiting for this request
            //TODO Retirar a thread pendente
            int previous = ThisMetaserverId;
            while (true)
                {
                Boolean iAmLast = false;
                previous = ViewManager.GetPreviousServerInView( previous, response.KnownByThisServersList, out iAmLast );
                if ( iAmLast )
                    {
                    Console.WriteLine( "Source received the ack" );
                    return;
                    }
                    {
                        try
                        {
                            //Tentar enviar ao seguinte
                            //TODO Se ele responder que desconhece, fico à espera do ack. Se ao fim de x tempo nao receber, enviar outra vez 
                            log.Debug( DateTime.Now.Ticks + " [M] Response Sent: " + response.OriginalRequest.FileName );
                            ConnectToMetaserver(previous).ReceiveResponse(response);
                            return;
                        }
                        catch (SocketException e)
                        {
                            //Actualizar a view, esta mensagem tem de ser entregue mas a view tem de ser actualizada,
                            //O serializer pode ter morrido.
                            //TODO INVOKE UPDATE VIEW
                            Console.WriteLine("Previous server fail");
                            ViewManager.ServerBetray( previous, ThisMetaserverId );
                        }
                    }
                }
        }




        private static MetaserverResponse AppDelivery(MetaRequest request)
        {
            Console.WriteLine("App Delivery start:");
            try
            {
                NamespaceManager.OperationAuth(request);
            }
            catch (PadiException e)
            {
                //Already processed
               // return null;
            }

            //Recebe os pedidos que ja estao prontos a entregar segundo o middleware
            //Invoke a nivel local
            //Após enviar o ack, vai carregar a próxima mensagem se existir
            //Processar o pedido, check type, unbox and process

            MetaserverResponse response;
            switch (request.RequestType)
            {
                case RequestType.Create:
                    response = Create(request);
                    break;
                case RequestType.Open:
                    response = Open(request);
                    break;
                case RequestType.Close:
                    response = Close(request);
                    break;
                case RequestType.Delete:
                    response = Delete(request);
                    break;
                case RequestType.Registry:
                    response = Registry(request);
                    break;
                case RequestType.Balancing:
                    response = Balancing(request);
                    break;
                    
                default:
                    response = new MetaserverResponse(ResponseStatus.InvalidRequest, request, ThisMetaserverId);
                    break;
            }
            NamespaceManager.OperationDone(request);
            return response;
            }


        /// <summary>
        /// Send response back to client
        /// </summary>
        /// <param name="response"></param>
        private static void SendResponseToClient( MetaserverResponse response )
            {
            if (response == null || response.OriginalRequest.RequestType == RequestType.Balancing)
            {
                return;
            }
            log.Debug( DateTime.Now.Ticks + " Send Response To Client: " + response );

            String hostname = response.OriginalRequest.ClientHostname;
            int port = response.OriginalRequest.ClientPort;
            IClientToMeta client =
                (IClientToMeta)
                Activator.GetObject( typeof( IClientToMeta ), "tcp://" + hostname + ":" + port + "/PADIConnection" );
            client.ReceiveMetaserverResponse( response );
            }



        public ViewMsg BullyRequestsRetrival( ViewMsg msg )
            {
            return ViewManager.BullyRequestsRetrival( msg );
            }

        /////////////////////////////// Fail Recovery ////////////////////////////////////

        /// <summary>
        /// Invoked from ViewPaused at ViewManager, this method delivers all pendent message and 
        /// stop accepting new messages
        /// </summary>
        /// <returns></returns>
        public static void PauseServer()
        {
            //Lock mutex
            freezeNewRequests.Reset( );
        }

        public static void PlayServer()
        {
            //Release the locker
            freezeNewRequests.Set( );
        }

        public static long[] GetQueueStateVector()
        {
            return NamespaceManager.GetQueueStateVector();
        }

        private long GetLocalTimeStamp( )
            {
            return Interlocked.Increment( ref LocalRequestId );
            }


        ////////////////////// Invocacaoes ao core ///////////////////////////
        private static MetaserverResponse Delete( MetaRequest request )
            {
            RequestDelete deleteRequest = (RequestDelete) request;
            MetaserverResponse response;
            //Invocar o servidor
            try
                {
                Boolean success = core.Delete( deleteRequest.FileName, request.ClientId );
                if ( success )
                    response = new MetaserverResponse( ResponseStatus.Success, request, ThisMetaserverId );
                else
                    {
                    response = new MetaserverResponse( ResponseStatus.Exception, request, ThisMetaserverId );
                    response.Exception = new PadiException( PadiExceptiontType.DeleteFile, "File not deleted" );
                    }
                }
            catch ( Exception ex )
                {
                response = new MetaserverResponse( ResponseStatus.Exception, request, ThisMetaserverId );
                response.Exception = new PadiException( PadiExceptiontType.DeleteFile, ex.Message );
                }

            return response;
            }

        private static MetaserverResponse Close( MetaRequest request )
            {
            RequestClose closeRequest = (RequestClose) request;
            MetaserverResponse response;
            //Invocar o servidor
            try
                {
                Boolean success = core.Close( closeRequest.FileName, request.ClientId );
                if ( success )
                    response = new MetaserverResponse( ResponseStatus.Success, request, ThisMetaserverId );
                else
                    {
                    response = new MetaserverResponse( ResponseStatus.Exception, request, ThisMetaserverId );
                    response.Exception = new PadiException( PadiExceptiontType.CloseFile, "File not closed" );
                    }
                }
            catch ( Exception ex )
                {
                response = new MetaserverResponse( ResponseStatus.Exception, request, ThisMetaserverId );
                response.Exception = new PadiException( PadiExceptiontType.CloseFile, ex.Message );
                }

            return response;
            }

        private static MetaserverResponse Create( MetaRequest request )
            {
            RequestCreate createRequest = (RequestCreate) request;
            MetaserverResponse response;
            //Invoca o servidor
            try
                {
                MetadataEntry entry = core.Create( createRequest.FileName, createRequest.NbDataServer, createRequest.ReadQuorum,
                        createRequest.WriteQuorum, request.ClientId );
                response = new MetaserverResponse( ResponseStatus.Success, request, ThisMetaserverId );
                response.MetaEntry = entry;
                }
            catch ( Exception ex )
                {
                response = new MetaserverResponse( ResponseStatus.Exception, request, ThisMetaserverId );
                response.Exception = new PadiException( PadiExceptiontType.CreateFile, ex.Message );
                }
            return response;
            }

        private static MetaserverResponse Open( MetaRequest request )
            {
            RequestOpen openRequest = (RequestOpen) request;
            MetaserverResponse response;
            try
                {
                MetadataEntry entry = core.Open( openRequest.FileName, request.ClientId );
                response = new MetaserverResponse( ResponseStatus.Success, request, ThisMetaserverId );
                response.MetaEntry = entry;
                }
            catch ( Exception ex )
                {
                response = new MetaserverResponse( ResponseStatus.Exception, request, ThisMetaserverId );
                response.Exception = new PadiException( PadiExceptiontType.OpenFile, ex.Message );
                }
            return response;
            }

        private static MetaserverResponse Registry( MetaRequest request )
            {
            RequestRegistry registryRequest = (RequestRegistry) request;
            MetaserverResponse response;
            try
                {
                core.Registry( registryRequest.ServerId, registryRequest.ServerIp, registryRequest.ServerPort );
                response = new MetaserverResponse( ResponseStatus.Success, request, ThisMetaserverId );
                }
            catch ( Exception ex )
                {
                response = new MetaserverResponse( ResponseStatus.Exception, request, ThisMetaserverId );
                response.Exception = new PadiException( PadiExceptiontType.Registry, ex.Message );
                }
            return response;
            }

        private static MetaserverResponse Balancing( MetaRequest request )
            {
            Console.WriteLine( "New balancing message" );
            if (!LoadBalancer.Enabled)
            {
                Console.WriteLine( "Load balance not enable" );
                return new MetaserverResponse( ResponseStatus.Success, request, ThisMetaserverId );
            }
            Console.WriteLine( "Load balance  enable" );
            RequestBalancing balancingRequest = (RequestBalancing) request;
            MetaserverResponse response;
            try
                {
                core.Balancing( balancingRequest );
                response = new MetaserverResponse( ResponseStatus.Success, request, ThisMetaserverId );
                }
            catch ( Exception ex )
                {
                response = new MetaserverResponse( ResponseStatus.Exception, request, ThisMetaserverId );
                response.Exception = new PadiException( PadiExceptiontType.Balancing, ex.Message );
                }
            return response;
            }


        public static IMetaToMeta ConnectToMetaserver( int serverNumber )
            {
            MetaserverId serverId;
            if ( !MetadataServerList.TryGetValue( serverNumber, out serverId ) )
                throw new PadiException( PadiExceptiontType.InvalidRequest, "Can not connet to Metaserver: " + serverNumber );
            IMetaToMeta server;
            try
                {
                server =
                    (IMetaToMeta)
                    Activator.GetObject( typeof( IMetaToMeta ),
                                        "tcp://" + serverId.Hostname + ":" + serverId.Port + "/PADIConnection" );
                return server;
                }
            catch ( Exception e )
                {
                Console.WriteLine( "FAIL DETECTION: ConnectToMetaserver" );
                throw new PadiException( PadiExceptiontType.InvalidRequest, "Error connecting to Metaserver" + e.Message );
                }
            }

        //inteface ao puppet
        public String Dump( )
            {
            StringBuilder str = new StringBuilder( );
            str.Append("-------------- Metadata server " + ThisMetaserverId + " ------------\n");
            str.AppendLine( core.Dump( ) );
            str.AppendLine( ViewManager.Dump( ) );
            return str.ToString( );
            }


        ////////////////////////// COPY MANAGEMENT ////////////////////////////////////////////////

        public static bool UpdateServer( CopyStructMetadata metaCopyStruct )
            {
            Console.WriteLine( "Start loading process" );
            //Actualizar o espaco de nomes em atraso
            core.LoadFromStructMetadataCore( metaCopyStruct );
            //Fazer o maximo dos vectores para manter sempre o estado mais actualizado
           long[] currentStatus = GetQueueStateVector();
            for ( int i = 0; i < currentStatus.Length; i++ )
                {
                currentStatus[i] = Math.Max( currentStatus[i], metaCopyStruct.StatusVector[i] );
                }
            NamespaceManager.SetQueueStateVector(currentStatus);
            Console.WriteLine( "Load done to status:" + ArrayToString(currentStatus));
            return true;
            }

        /// <summary>
        ///  Compare our status with the requester status.
        ///  Send only the sections which we have more recent
        ///  and if he has a more recent sections, than, send request
        ///  too.
        /// </summary>
        /// <param name="sourceStatus"></param>
        /// <returns></returns>
        public CopyStructMetadata RequestUpdate(long[] sourceStatus,int sourceID)
        {
            long[] currentStatus = GetQueueStateVector();
            Console.WriteLine("Start Copy Sending Process");
            Console.WriteLine( "My state:" + ArrayToString(currentStatus) );
            Console.WriteLine( "Requester state:" + ArrayToString(sourceStatus) );
            Boolean needUpdate = false;
            List<int> sectionsToSend = new List<int>();
            //Comparar o array recebido com o nosso
            for (int i = 0; i < currentStatus.Length; i++)
            {
            if ( currentStatus[i] < sourceStatus[i] )
                    needUpdate = true;
            //If we have more recent version, than update source status
            if ( currentStatus[i] > sourceStatus[i] )
                {
                    sectionsToSend.Add(i);
                    sourceStatus[i] = currentStatus[i];
                }
            }

            //Request core the sections left
            CopyStructMetadata metaCopyStruct = core.Copy(NamespaceManager,sectionsToSend);

            //Attach our state
            metaCopyStruct.StatusVector = sourceStatus;

            //If need update, enviar o pedido de update (sourceId)
            if (needUpdate)
            {
                Console.WriteLine("I need update too, lets ask to new peer to update me too");
                RequestUpdateDelegate exec = new RequestUpdateDelegate( RequestDirectUpdate );
                exec.Invoke( sourceID );
                Console.WriteLine( "Sending Copy" );
            }

            return metaCopyStruct;
        }

    

        public delegate void RequestUpdateDelegate( int metaId );


        private static String ArrayToString(long[] array)
        {
            StringBuilder builder = new StringBuilder();
            foreach (long l in array)
            {
                builder.Append(l + ",");
            }
            return builder.ToString();
        }

        private void RequestDirectUpdate( int metaId )
        {
            Console.WriteLine("Im wolder, request direct to: "+metaId);
            UpdateServer( ConnectToMetaserver( metaId ).RequestUpdate( GetQueueStateVector( ), ThisMetaserverId ) );
        }


        /////////////////////////////// Channel Manager /////////////////////////////////
        public static TcpChannel MetaServerChannel;
        public static TcpChannel EmergencyChannel;

        public String Fail( )
            {
            Console.WriteLine( "FAIL!!!!!!!!!" );
            Console.Clear( );
            //Unbind current channel
            if ( MetaServerChannel == null )
                return "";
            ChannelServices.UnregisterChannel( MetaServerChannel );

            //Bind object launchmeta
            CreateEmergencyChannel( );
            return "Metaserver " + ThisMetaserverId + "fail";
            }

        public static void CreateEmergencyChannel( )
            {
            EmergencyChannel = new TcpChannel( PortRecovery );
            
            ChannelServices.RegisterChannel( EmergencyChannel, false );
            
            RemotingConfiguration.RegisterWellKnownServiceType(
                  typeof( MetaLauncher ),
                  "PADIConnection",
                  WellKnownObjectMode.Singleton );
            }


        public static void CreateMetaserverChannel( )
            {
            MetaserverId thisServer;
            if ( !MetadataServerList.TryGetValue( ThisMetaserverId, out thisServer ) )
                throw new PadiException( PadiExceptiontType.InvalidRequest, "Cannot create channel to metaserver. Invalid ID: " + ThisMetaserverId );

            MetaServerChannel = new TcpChannel( thisServer.Port );
            ChannelServices.RegisterChannel( MetaServerChannel, false );
            RemotingConfiguration.RegisterWellKnownServiceType(
                typeof( MetadataServer ),
                "PADIConnection",
                WellKnownObjectMode.Singleton );
            }



        public static void DestroyEmergencyChannel( )
            {
            if ( EmergencyChannel == null )
                return;
            ChannelServices.UnregisterChannel( EmergencyChannel );
            }


        public override object InitializeLifetimeService( )
            {
            return null;
            }

        }


    public class MetaLauncher : MarshalByRefObject, IRecovery
        {
        public String Recover( )
            {
            Console.WriteLine( "Recovered!!!!!!!!!" );
            //Unbind Old
            MetadataServer.DestroyEmergencyChannel( );
            //Create Channel again
            MetadataServer.core = new MetaCore();

            MetadataServer.PauseServer();
            MetadataServer.CreateMetaserverChannel( );

            MetadataServer.ViewManager.RecoverServer( );
            Console.Write( MetadataServer.ViewManager );

            return "Metadataserver " + MetadataServer.ThisMetaserverId + " recovered \r\n " + MetadataServer.ViewManager;
                        }
        }

    public struct MetaserverId
        {
        public int Id;
        public int Port;
        public String Hostname;
        }


    }
