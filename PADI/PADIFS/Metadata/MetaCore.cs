using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using Metadata.ViewStates;
using SharedLib.Exceptions;
using SharedLib.MetadataObjects;
using SharedLib;



namespace Metadata
    {
    // Middleware to receive clients requests
    public class MetaCore
        {
        private LoadBalancer _loadBalancer;
        private BackupManager _backup;
        private int _lastDataserver = 0;
        public MetaCore( )
            {
            if (  LoadBalancer.Enabled )
            {
                Console.WriteLine( "Balacing enable" );
                _loadBalancer = new LoadBalancer( this );
            }
            _backup = new BackupManager( );
            _metaTable = _backup.LoadMetaTable( );
            _lastDataserver = 0;
            }

        //ServerState: Ficheiro vs Metadados
        private ConcurrentDictionary<String, MetadataEntry> _metaTable =
            new ConcurrentDictionary<String, MetadataEntry>( );

        private Random _randomFileNameGenerator = new Random( 1000 );

        //Lista de dataservers
        private List<DataserverInfo> _dataServerTable = new List<DataserverInfo>( );

        private List<String> _used = new List<string>( );


        public delegate void BackupDelegate( MetadataEntry entry );


        public MetadataEntry Open( String filename, int clientId )
            {
            Console.WriteLine( "Opening file " + filename );

            MetadataEntry entry;
            Mutex locker = new Mutex( false, filename );
            locker.WaitOne( );
            Console.WriteLine( "Openfile: " + filename );
            Boolean status = _metaTable.TryGetValue( filename, out entry );
            if ( status )
                entry.AddClient( clientId );
            else
                {
                locker.ReleaseMutex( );
                throw new Exception( "Can not open the file: " + filename );
                }

            new BackupDelegate( _backup.AddEntry ).BeginInvoke( entry, null, null );

            locker.ReleaseMutex( );
            return entry;
            }


        public MetadataEntry Create( String filename, int nbDataServers, int readQuorum, int writeQuorum, int clientId )
            {
            Console.WriteLine( "Create file: " + filename );
            MetadataEntry entry;
            Mutex locker = new Mutex( false, filename );
            locker.WaitOne( );
            if ( _metaTable.TryGetValue( filename, out entry ) )
                {
                locker.ReleaseMutex( );
                throw new Exception( "File already exists: " + filename );
                }
            entry = new MetadataEntry( filename, nbDataServers, readQuorum, writeQuorum );


            //Alocar tantos servidores quanto possivel, o que faltar devolve como falta
            AllocFileOnDataServer( entry );

            new BackupDelegate( _backup.AddEntry ).BeginInvoke( entry, null, null );

            if (!(_metaTable.TryAdd(entry.FileName, entry)))
            {
                locker.ReleaseMutex( );
                throw new Exception("Cannot save file: " + filename);
            }
            locker.ReleaseMutex( );
            return entry;
            }







        public Boolean Delete( String filename, int clientId )
            {
            Console.WriteLine( "Try delete: " + filename );
            //Lock the file access until delete it. No one can open it
            MetadataEntry entry;
            Mutex locker = new Mutex( false, filename );
            locker.WaitOne( );

            //Exclusive access:
            Boolean status = _metaTable.TryGetValue( filename, out entry );
            if ( !status )
                {
                Console.WriteLine( "File " + filename + " is still open" );
                locker.ReleaseMutex( );
                return false;
                }
            if ( entry.IsOpen( ) )
                {
                locker.ReleaseMutex( );
                Console.WriteLine( "File " + filename + " is still open" );
                return false;
                }

            // NOTIFY BALANCING
            EventWaitHandle lockerBalancing = EventWaitHandle.OpenExisting(
                "padi" + MetadataServer.ThisMetaserverId + filename + "balancing");
            if ( LoadBalancer.Enabled)
                {
                //Wake pendent balancing thread
                lockerBalancing.Set( );
                }
            Console.WriteLine( "File deleted, no user before: " + filename );
            status = _metaTable.TryRemove( filename, out entry );
            if ( status )
                Console.WriteLine( "File " + filename + " has been deleted" );

            new BackupDelegate( _backup.RemoveEntry ).BeginInvoke( entry, null, null );

            locker.ReleaseMutex( );
            return status;
            }


        /// <summary>
        ///  Remove the entry from read clients
        /// </summary>
        public Boolean Close( String filename, int clientId )
            {
            Console.WriteLine( "Try close: " + filename );
            MetadataEntry entry;
            Mutex locker = new Mutex( false, filename );
            locker.WaitOne( );

            Console.WriteLine( "Go close file" );
            Boolean status = _metaTable.TryGetValue( filename, out entry );
            if ( status )
                {
                if ( !entry.RemoveClient( clientId ) )
                    {
                    locker.ReleaseMutex( );
                    Console.WriteLine( "This client didnt open this file" );
                    return false;
                    }

                // Se o Ficheiro esta Fechado vamos chamar o Algoritmo de Load Balancing
                if ( !entry.IsOpen( ) && LoadBalancer.Enabled && MetadataServer.ViewManager.IsBiggestId( ) )
                    {
                    Console.WriteLine( "File can be balanced: " + filename );
                    _loadBalancer.OnFileCloseLoadBalance( entry );

                    //Allow to balance this file
                    try
                        {
                        EventWaitHandle lockerBalancing = EventWaitHandle.OpenExisting(
                            "padi" + MetadataServer.ThisMetaserverId + filename + "balancing" );

                        //Wake pendent balancing thread
                        lockerBalancing.Set( );
                        }
                    catch ( Exception e )
                        {
                        Console.WriteLine( e.Message );
                        }
                    }
                }
            new BackupDelegate( _backup.AddEntry ).BeginInvoke( entry, null, null );
            locker.ReleaseMutex( );
            if ( status )
                Console.WriteLine( "File has been closed" );
            return status;
            }



        public String Dump( )
            {
            StringBuilder builder = new StringBuilder( "\n Metadata table: \n" );

            foreach ( KeyValuePair<String, MetadataEntry> entry in _metaTable )
                {
                builder.AppendLine( entry.Value.ToString( ) );
                }
            builder.AppendLine( "\n Known dataservers:" );
            foreach ( DataserverInfo info in _dataServerTable )
                {
                builder.AppendLine( "dataserver: " + info.IdStruct.id );
                }
            return builder.ToString( );
            }


        private delegate void BalacingThreadDel( RequestBalancing entry );

        public void Balancing( RequestBalancing entry )
            {
            if ( LoadBalancer.Enabled )
                {
                //New balancing thread to wait for close
                new BalacingThreadDel( BalancingThread ).BeginInvoke( entry, null, null );
                //Continue 
                }
            }


        private void BalancingThread( RequestBalancing newEntry )
            {
            //Mutex locker = new Mutex( false, newEntry.FileName );
            MetadataEntry currentEntry;

            EventWaitHandle monitor = new EventWaitHandle( true, EventResetMode.AutoReset, "padi" + MetadataServer.ThisMetaserverId + newEntry.FileName + "balancing" );
            while ( LoadBalancer.Enabled )
                {
                Console.WriteLine( "Balacing operation start " );
                monitor.WaitOne( );
                Console.WriteLine( "Balacing operation unlocked " );

                //locker.WaitOne( );
                Console.WriteLine( "Ballancer Thread checking " );
                Boolean status = _metaTable.TryGetValue( newEntry.FileName, out currentEntry );
                if ( status == false )
                    {
                    //Ficheiro foi eliminado
                    //locker.ReleaseMutex( );
                    return;
                    }
                if ( !currentEntry.IsOpen( ) )
                    {
                    BalanceEntries( currentEntry, newEntry );
                   // locker.ReleaseMutex( );
                    return;
                    }
                //FILE IS OPEN YET, IF YES, WAIT
                //locker.ReleaseMutex( );
                Console.WriteLine( "Balacing operation locked: " );
                monitor.WaitOne( );
                Console.WriteLine( "Balacing operation unlocked" );
                monitor.Set( );
                }
            }

        private void BalanceEntries( MetadataEntry currentEntry, RequestBalancing balanceRequest )
            {
            Console.WriteLine( "Ballance entries please" );
            MetadataEntry newEntry = currentEntry.Clone();
            switch (balanceRequest.Writes)
            {
                    case BalancingStatus.Excess:
                        newEntry.NBDataServers++;
                        newEntry.ReadQuorum++;
                        break;
                    case BalancingStatus.Few:
                        break;
                    case BalancingStatus.Ok:
                        break;
            }

         switch (balanceRequest.Reads)
            {
                case BalancingStatus.Excess:
                    newEntry.NBDataServers++;
                    newEntry.WriteQuorum++;
                    break;
                case BalancingStatus.Few:
                    break;
                 case  BalancingStatus.Ok:
                    break;
            }


            int difference = newEntry.NBDataServers - currentEntry.NBDataServers;

            //Update values
            currentEntry.NBDataServers = newEntry.NBDataServers;
            currentEntry.ReadQuorum = newEntry.ReadQuorum;
            currentEntry.WriteQuorum = newEntry.WriteQuorum;

            if ( difference < 0 )
                {
                KeyValuePair<ServerId, String>[] entriesList = currentEntry.ServerFileList.ToArray( );
                ConcurrentDictionary<ServerId, String> newlist = new ConcurrentDictionary<ServerId, string>( );
                //Temos de alocar menos servidores
                for ( int i = 0; i < currentEntry.NBDataServers; i++ )
                    {
                    //Reduce list
                    newlist.TryAdd( entriesList[i].Key, entriesList[i].Value );
                    }
                //TODO Delete files on dataservers
                }
            if ( difference > 0 )
                {
                //Temos de alocar mais servidores
                CopyEntryBetweenDataservers( currentEntry );
                }
            }





        ////////////////////////////// Dataserver's interface ///////////////
        /// Alloc "serversToAlloc" dataservers to file in entry. 
        private void CopyEntryBetweenDataservers( MetadataEntry entry )
            {
            //A entry tem de estar trancada com o lock do filename
            lock ( _dataServerTable )
                {
                int serversLeft = entry.NBDataServers - entry.ServerFileList.Count;

                if ( serversLeft <= 0 )
                    return;

                List<DataserverInfo> toRemove = new List<DataserverInfo>( );


                DataserverInfo src = null;
                    String srcLocalfilename = "";
                foreach (DataserverInfo dataserverInfo in _dataServerTable)
                {
                    if (entry.ServerFileList.ContainsKey(dataserverInfo.IdStruct))
                    {
                        src = dataserverInfo;
                        entry.ServerFileList.TryGetValue(src.IdStruct, out srcLocalfilename);
                        break;
                    }
                }
                if (src == null)
                {
                    Console.WriteLine("ALERT: Cant move, this file dont have any server. Aloloc emptyfiles");
                    AllocFileOnDataServer( entry );
                    return;
                }


                foreach ( DataserverInfo dataserverInfo in _dataServerTable )
                    {
                    if ( entry.ServerFileList.ContainsKey( dataserverInfo.IdStruct ) ) continue;
                    if ( serversLeft == 0 ) return;

                    // Generate Fresh LocalNames
                    String destLocalFilename = GenerateLocalFileName( entry.FileName, serversLeft );
                    Boolean successFul = CopyFileBetweenServer(src.IdStruct.hostname,src.IdStruct.port, dataserverInfo.IdStruct.hostname,
                                            dataserverInfo.IdStruct.port, srcLocalfilename, destLocalFilename );

                    if ( !successFul )
                        {
                        toRemove.Add( dataserverInfo );
                        continue;
                        }

                    Console.WriteLine( "Add local file" );
                    _used.Add( destLocalFilename );
                    entry.ServerFileList.TryAdd( dataserverInfo.IdStruct, destLocalFilename );

                    serversLeft--;
                    }

                foreach ( DataserverInfo dataserverInfo in toRemove )
                    {
                    if ( _dataServerTable.Remove( dataserverInfo ) )
                        {
                        Console.WriteLine( "Unvailable server removed: " + dataserverInfo.IdStruct.id );
                        }
                    }


                if ( serversLeft > 0 )
                    Console.WriteLine( "Metadata cant alloc enough dataservers : " + entry.FileName + "missing: " +
                                  serversLeft );
                }
            }





        ////////////////////////////// Dataserver's interface ///////////////
        /// Alloc "serversToAlloc" dataservers to file in entry. 
        private int AllocFileOnDataServer( MetadataEntry entry)
            {
            //A entry tem de estar trancada com o lock do filename
            lock ( _dataServerTable )
                {
                int serversLeft = entry.NBDataServers - entry.ServerFileList.Count;

                if ( serversLeft <= 0 )
                    return 0;

                List<DataserverInfo> toRemove = new List<DataserverInfo>( );

                int countDataServers = _dataServerTable.Count;
                int iterator = countDataServers;


                while ( iterator-- > 0 )
                {
                    int index = (_lastDataserver++) % countDataServers;
                    DataserverInfo dataserverInfo = _dataServerTable[index];

                    if ( entry.ServerFileList.ContainsKey( dataserverInfo.IdStruct ) ) continue;
                    if ( serversLeft == 0 ) return 0;

                    // Generate Fresh LocalNames
                    String localFilename = GenerateLocalFileName( entry.FileName, serversLeft );
                        Boolean successFul = CreateEmptyFileOnDataServer( dataserverInfo.IdStruct.hostname,
                                                dataserverInfo.IdStruct.port, localFilename, entry.FileName );
   
                    if ( !successFul )
                        {
                        toRemove.Add( dataserverInfo );
                        continue;
                        }

                    Console.WriteLine( "Add local file" );
                    _used.Add( localFilename );
                    entry.ServerFileList.TryAdd( dataserverInfo.IdStruct, localFilename );

                    serversLeft--;
                    }

                foreach ( DataserverInfo dataserverInfo in toRemove )
                    {
                    if ( _dataServerTable.Remove( dataserverInfo ) )
                        {
                        Console.WriteLine( "Unvailable server removed: " + dataserverInfo.IdStruct.id );
                        }
                    }


                if ( serversLeft > 0 )
                    Console.WriteLine( "Metadata cant alloc enough dataservers : " + entry.FileName + "missing: " +
                                  serversLeft );

                return serversLeft;
                }
            }

        // Cria um ficheiro vazio no servidor de dados
        private Boolean CreateEmptyFileOnDataServer( string hostname, int port, string localFilename, String filename )
            {
                {
                IDataToMeta dateserver = (IDataToMeta) Activator.GetObject(
                    typeof( IDataToMeta ), "tcp://" + hostname + ":" + port + "/PADIConnection" );
                try
                    {
                    dateserver.CreateEmptyFile( filename, localFilename );
                    return true;
                    }
                catch ( SocketException e )
                    {
                    Console.WriteLine( "Dataserver unvailable" );
                    return false;
                    }
                }
            }

        //Solicitar a copia entre 2 dataservers
        private Boolean CopyFileBetweenServer( string srcHost, int srcPort, string destHost, int destPort, string srcLocalFilename, String destLocalFilename )
            {
                {
                IDataToMeta dateserver = (IDataToMeta) Activator.GetObject(
                    typeof( IDataToMeta ), "tcp://" + srcHost + ":" + srcPort + "/PADIConnection" );
                try
                    {
                    dateserver.CopyFileToOtherData( srcLocalFilename, destLocalFilename, destHost, destPort );
                    return true;
                    }
                catch ( SocketException e )
                    {
                    Console.WriteLine( "Dataserver unvailable" );
                    return false;
                    }
                }
            }



        /// <summary>
        /// Dataserver Registry
        /// </summary>
        /// <param name="serverId"></param>
        /// <param name="serverIP"></param>
        /// <param name="serverPort"></param>
        /// <returns></returns>
        public void Registry( String serverId, String serverIP, int serverPort )
            {
            Console.WriteLine( "Tentativa de registo do servidor id: " + serverId + " IP: " + serverIP + ":" +
                              serverPort );
            lock ( _dataServerTable )
                {
                foreach ( DataserverInfo info in _dataServerTable )
                    {
                    if ( info.IdStruct.id.Equals( serverId ) )
                        throw new Exception( "Ignored: Dataserver already registed: " + serverId );
                    }
                _dataServerTable.Add( new DataserverInfo( serverId, serverIP, serverPort ) );
                }


            //Check if there is some file with dataservers missing
            foreach ( KeyValuePair<String, MetadataEntry> keyValuePair in _metaTable )
                {
                Mutex locker = new Mutex( false, keyValuePair.Key );
                locker.WaitOne( );
                AllocFileOnDataServer( keyValuePair.Value );
                locker.ReleaseMutex( );
                }
            }


        public List<DataserverInfo> RequestDataserverList( )
            {
            //TODO IMPLEMENTAR COM OS LOCKS CERTOS
            return new List<DataserverInfo>( _dataServerTable.ToArray( ) );
            }



        ///////////////////////// copy & Load ////////////////////////////////
        public CopyStructMetadata Copy( NamespaceManager namespaceManager, List<int> sectionsToSend )
            {
            CopyStructMetadata dataStruct = new CopyStructMetadata( );
            dataStruct.DataServerTable = new List<DataserverInfo>( _dataServerTable.ToArray( ) );
            dataStruct.MetaTable = GetParcialMetaTable( namespaceManager, sectionsToSend );
            dataStruct.Used = new List<string>( _used );
            dataStruct.RandomGenerator = _randomFileNameGenerator;
            return dataStruct;
            }

        private ConcurrentDictionary<string, MetadataEntry> GetParcialMetaTable( NamespaceManager namespaceManager, List<int> sectionsToSend )
            {
            ConcurrentDictionary<string, MetadataEntry> tableToSend = new ConcurrentDictionary<string, MetadataEntry>( );
            foreach ( KeyValuePair<string, MetadataEntry> metadataEntry in _metaTable )
                {
                int queue = namespaceManager.GetNameSpaceID( metadataEntry.Key );
                if ( sectionsToSend.Contains( queue ) )
                    {
                    tableToSend.TryAdd( metadataEntry.Key, metadataEntry.Value );
                    }
                }
            return tableToSend;
            }


        public void LoadFromStructMetadataCore( CopyStructMetadata dataStruct )
            {
            _dataServerTable = new List<DataserverInfo>( dataStruct.DataServerTable );
            foreach ( KeyValuePair<string, MetadataEntry> metadataEntry in dataStruct.MetaTable )
                {
                MetadataEntry ignore;
                _metaTable.TryRemove( metadataEntry.Key, out ignore );
                _metaTable.TryAdd( metadataEntry.Key, metadataEntry.Value );
                }
            _used = new List<string>( dataStruct.Used );
            _randomFileNameGenerator = dataStruct.RandomGenerator;
            }




        public String GenerateLocalFileName( String originalFileName, int dataserver )
            {
            char[] nameChars = new char[16];
            char[] chars = "1234567890ABCDEFGHJKLMNPQRTUVWXYZabcdefghjkmnpqrtuvwxyz".ToCharArray( );
            for ( int i = 0; i < nameChars.Length; i++ )
                {
                nameChars[i] = chars[_randomFileNameGenerator.Next( 0, chars.Length )];
                }
            String name = new String( nameChars );
            // System.Console.WriteLine("Generated Name: " + name);
            if ( _used.Contains( name ) )
                {
                System.Console.WriteLine( "# FileLocalname Already in Use Generating Other localname" );
                return GenerateLocalFileName( originalFileName, dataserver );
                }
            return name;
            }


        public MetadataEntry GetMetaentry( String filename )
            {
            MetadataEntry entry = null;
            Mutex locker = new Mutex( false, filename );
            locker.WaitOne( );
            _metaTable.TryGetValue( filename, out entry );
            locker.ReleaseMutex( );
            return entry;
            }
        }
    }
