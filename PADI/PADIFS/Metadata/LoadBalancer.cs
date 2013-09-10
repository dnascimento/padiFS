using System;
using System.Collections.Generic;
using SharedLib.MetadataObjects;
using System.Collections.Concurrent;
using SharedLib.DataserverObjects;
using SharedLib;

namespace Metadata
    {
    public class LoadBalancer
        {
        public static bool Enabled = true;
        private MetaCore _core;
        //Tolerancia em decimal (0 a 1)
        private double TOLERANCE = 0.10;
        private MetaserverAsyncClient connectionToMeta;

        public int UPDATE_INTERVAL = 180;

        private DateTime lastUpdate = new DateTime( );

        public LoadBalancer( MetaCore metaCore )
            {
            _core = metaCore;
            MetaserverId thisServer;
            MetadataServer.MetadataServerList.TryGetValue( MetadataServer.ThisMetaserverId,
                                                                                    out thisServer );
             List<ServerId> metadataServerList = new List<ServerId>( );
            foreach (MetaserverId metaserver in MetadataServer.MetadataServerList.Values)
            {
                ServerId serverId = new ServerId();
                serverId.hostname = metaserver.Hostname;
                serverId.id = metaserver.Id.ToString();
                serverId.port = metaserver.Port;
                metadataServerList.Add( serverId );
            }
            connectionToMeta = new MetaserverAsyncClient( thisServer.Hostname, thisServer.Port + 5000, metadataServerList, MetadataServer.ThisMetaserverId + 100 );
            }


        public void OnFileCloseLoadBalance(MetadataEntry metaDados)
        {
            if (!Enabled)
            {
                return;
            }

            Console.WriteLine("balance enabled");
            DateTime now = DateTime.Now;

            if ( ((now - lastUpdate).TotalSeconds) < UPDATE_INTERVAL )
            {
                return;
            }

            new OnCloseDel(OnFileClosedRunning).BeginInvoke(metaDados, null, null);
        }

        public delegate void OnCloseDel(MetadataEntry entry);

        public void OnFileClosedRunning(MetadataEntry metaDados){
            List<DataserverInfo> dataservers = _core.RequestDataserverList( );
            Dictionary<DataserverInfo, ICollection<LocalFileStatistics>> globalState = new Dictionary<DataserverInfo, ICollection<LocalFileStatistics>>( );

            Console.WriteLine( "------>Statistics Begin" );
            long totalReadsSystem = 0;
            long totalWritesSystem = 0;
            long totalFiles = 0;
            foreach ( DataserverInfo info in dataservers )
                {
                Console.WriteLine( "ID: " + info.IdStruct.id + "  |  " + info.IdStruct.hostname + "  |  " + info.IdStruct.port + "  |  " + info.IdStruct.recoverPort );
                ICollection<LocalFileStatistics> statistics = GetServerStatistics( info.IdStruct.hostname, info.IdStruct.port );
                if ( statistics != null )
                    {
                    DataserverInfo update = ProcessStatitics( info, statistics );
                    totalReadsSystem += update.TotalRead;
                    totalWritesSystem += update.TotalWrite;
                    totalFiles += update.TotalFiles;
                    globalState.Add( update, statistics );
                    }
                }

            BalancerEngine( globalState, totalReadsSystem, totalWritesSystem, totalFiles, globalState.Count );


            Console.WriteLine( "--------->Statistics End" );
            }

        private void BalancerEngine( Dictionary<DataserverInfo, ICollection<LocalFileStatistics>> globalState, long totalReadsSystem, long totalWritesSystem, long totalFiles, int numServers )
            {
            long avgTraffic = (totalReadsSystem + totalWritesSystem) / numServers;
            long avgWrite = totalWritesSystem / numServers;
            long avgRead = totalReadsSystem / numServers;

            //Reads+Writes
            Dictionary<String, RequestBalancing> entriesToChange = new Dictionary<string, RequestBalancing>( );
            foreach ( KeyValuePair<DataserverInfo, ICollection<LocalFileStatistics>> serverStaticsPair in globalState )
                {
                // PROCESSAR O SERVIDOR COMO QUEIRAMOS
                foreach ( LocalFileStatistics file in serverStaticsPair.Value )
                    {
                    String filename = file.filename;
                    RequestBalancing status = new RequestBalancing( filename );

                    long writeDiff = file.writeTraffic - avgWrite;
                    if ( writeDiff > avgWrite * TOLERANCE )
                        {
                        //Escritas em excesso
                        status.Writes = BalancingStatus.Excess;
                        }
                    if ( writeDiff < (-avgWrite * TOLERANCE) )
                        {
                        //Menos escritas que o normal
                        status.Writes = BalancingStatus.Few;
                        }
                    long readDiff = file.readTraffic - avgRead;
                    if ( readDiff > avgRead * TOLERANCE )
                        {
                        //Leituras em excesso
                        status.Reads = BalancingStatus.Excess;
                        }
                    if ( readDiff < (-avgRead * TOLERANCE) )
                        {
                        //Menos Leituras que o normal
                        status.Reads = BalancingStatus.Few;
                        }
                    //Was updated?
                    if ( status.Reads == BalancingStatus.Ok && status.Writes == BalancingStatus.Ok )
                        continue;

                    if ( entriesToChange.ContainsKey( filename ) )
                        {
                        //TODO NORMALIZE (se ja existe, outros servidores tambem estao mal)
                        }
                    else
                        {
                        entriesToChange.Add( filename, status );
                        }
                    }
                }

            foreach ( RequestBalancing request in entriesToChange.Values )
                {
                connectionToMeta.SendRequestToMetaserver( request );
                }
            }


        private void GetEntry( String filename, MetadataEntry original, out MetadataEntry outEntry )
            {
            if ( original == null )
                {
                outEntry = _core.GetMetaentry( filename );
                }
            else
                {
                outEntry = original;
                }
            }


        private ICollection<LocalFileStatistics> GetServerStatistics( String ip, int port )
            {
            IDataToMeta dataServer = (IDataToMeta) Activator.GetObject(
                    typeof( IDataToMeta ),
                    "tcp://" + ip + ":" + port + "/PADIConnection" );

            ICollection<LocalFileStatistics> r;
            try
                {
                r = dataServer.GetFileStatistics( ).Values;
                }
            catch ( Exception )
                {
                return null;
                }

            return r;
            }

        public DataserverInfo ProcessStatitics( DataserverInfo server, ICollection<LocalFileStatistics> statistics )
            {
            server.TotalRead = 0;
            server.TotalWrite = 0;
            server.TotalFiles = 0;
            foreach ( LocalFileStatistics statics in statistics )
                {
                server.TotalRead += statics.readTraffic;
                server.TotalWrite += statics.writeTraffic;
                server.TotalFiles++;
                }
            return server;
            }
        }
    }
