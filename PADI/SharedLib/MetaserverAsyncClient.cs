using System;
using System.Collections.Generic;
using System.Net.Sockets;
using SharedLib;
using SharedLib.Exceptions;
using SharedLib.MetadataObjects;
using System.Threading;



namespace SharedLib
{
    /// <summary>
    /// Client which will interact with metaserver. Responses are handle by main object
    /// </summary>
    public class MetaserverAsyncClient : IClientToMeta
    {
        public  List<ServerId> MetadataServerList = new List<ServerId>();
        public  String ClientHostname;
        public  int ClientPort;
        private Random _randomGenerator;
        private int _lastMetaserver;
        private int MAX_NUM_ATTEMPS = 4;
        //Response Array. Int: RequestID
        public Dictionary<String, MetaserverResponse> ResponseList = new Dictionary<String, MetaserverResponse>( );

        // Reduzir para colocar em producao
        public static int MetaServerResponseTimeout = 10000000;

        public int ClientRequestId = 0;
        public int ClientId;

        public MetaserverAsyncClient(String hostname, int clientPort, List<ServerId> metaServerList, int clientId)
        {
            ClientHostname = hostname;
            ClientPort = clientPort;
            MetadataServerList = metaServerList;
            ClientId = clientId;
            _randomGenerator = new Random( DateTime.Now.Millisecond );
           _lastMetaserver = _randomGenerator.Next( 0, 3 );
        }



        /// <summary>
        /// This method is called my metaserver to deliver a response.
        /// Adiciona o pedido a lista de respostas e desbloqueia o processo pendente
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public Boolean ReceiveMetaserverResponse( MetaserverResponse response )
            {
            lock ( ResponseList )
                {
                ResponseList.Add( response.OriginalRequest.ClientReqStamp, response );
                EventWaitHandle requestLocker;
                    requestLocker = EventWaitHandle.OpenExisting("Client" + response.OriginalRequest.ClientReqStamp);
                if(requestLocker != null)
                     requestLocker.Set( );
                }
            return true;
            }



        /// <summary>
        /// Send the request to metaserver, lock until response. If timeout, repeat request
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public MetaserverResponse SendRequestToMetaserver( MetaRequest request )
            {
            //Fill the request source details:
            int requestId = GetNewRequestId( );
            request.ClientHostname = ClientHostname;
            request.ClientReqStamp = "c:"+ClientId+":"+ClientHostname+":"+ClientPort+":"+requestId;
            request.ClientPort = ClientPort;
            request.ClientId = ClientId;

            EventWaitHandle monitor = new EventWaitHandle( false, EventResetMode.AutoReset, "Client" + request.ClientReqStamp );
            MetaserverResponse response = null;
            while ( true )
                {
                request.Attempt++;
                //Do remote sync request


                    response = ConnectToMetaserver(request);
                //Pedido foi entregue ao master


                //Wait no monitor com o ID do pedido ate receber a resposta
                monitor.WaitOne( MetaServerResponseTimeout, true );

                //Este wait tem timeout e é desempedido a quando da resposta dos meta
                lock ( ResponseList )
                    {
                    if ( ResponseList.TryGetValue( request.ClientReqStamp, out response ) )
                        if (response != null)
                        {
                            break;
                        }
                    }
                //Se a resposta nao veio, vamos realizar repeticao de pedido   
                }
            
             if ( response.Status.Equals( ResponseStatus.Exception ) )
                {
                    PadiException exception = response.Exception;
                    throw exception;
                }
            return response;
            }

        private int GetNewRequestId()
        {
            return  Interlocked.Increment(ref ClientRequestId);
        }

        private MetaserverResponse ConnectToMetaserver(MetaRequest request)
        {
            int i = 0;
            while (true)
            {
            Console.WriteLine( "ConnectToMetaserver: " + _lastMetaserver+" ...");
              ServerId server =  MetadataServerList[_lastMetaserver];
                            IMetaToClient serverInterface = (IMetaToClient)Activator.GetObject(
                    typeof(IMetaToClient),
                    "tcp://" + server.hostname + ":" + server.port + "/PADIConnection");
                try
                {
                    return serverInterface.ClientProcessRequest(request);
                }
                catch (SocketException)
                {
                    Console.WriteLine("Server: "+server.id+" is not available");
                    _lastMetaserver = ((_lastMetaserver + 1)%3);
                    if ( MAX_NUM_ATTEMPS == i++ )
                    {
                        throw new Exception("Client: No metadataserver available");
                    }
                }            
            }
            }

           
    }
}