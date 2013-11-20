using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using SharedLib.MetadataObjects;

namespace Metadata
    {
    /// <summary>
    /// Guardar os dados deste servidor em memória persistente.
    /// Sao guardados em varios ficheiros. Cada ficheiro e uma particao do espaco de nomes.
    /// A operacao e ordenada por espaco de nomes. 
    /// Pode ser criado, eliminado ou actualizado (a custa das 2 anteriores)
    /// </summary>
    public class BackupManager
        {
        public static String STORAGE_DIR = null;


        //public static void Main( string[] args )
        //    {
        //    BackupManager manager = new BackupManager( );
        //    manager.AddEntry( new MetadataEntry( "DARIO", 2, 3, 1 ) );
        //    manager.AddEntry( new MetadataEntry( "DARIO", 2, 69, 1 ) );
        //    manager.RemoveEntry( new MetadataEntry( "DARIO", 3, 1, 2 ) );
        //    Console.WriteLine( manager.LoadMetaTable( ) );

        //    }


        private const int NUMBER_OF_BACKUP_FILES = 6;
        private List<BackupFile> _backupFiles;

        public BackupManager( )
            {
            STORAGE_DIR = "C:/PADIFS/Metaserver-" + MetadataServer.ThisMetaserverId + "/";

            _backupFiles = new List<BackupFile>( );
            for ( int i = 0; i < NUMBER_OF_BACKUP_FILES; i++ )
                {
                _backupFiles.Add( new BackupFile( i ) );
                }
            }


        public void AddEntry( MetadataEntry entry )
            {
            int fileId = GetFileID( entry.FileName );
            _backupFiles[fileId].AddEntry( entry );
            }

        public void RemoveEntry( MetadataEntry entry )
            {
            int fileId = GetFileID( entry.FileName );
            _backupFiles[fileId].RemoveEntry( entry );
            }


        private int GetFileID( String filename )
            {
            if ( filename == null )
                return 0;
            using ( var alg = SHA256.Create( ) )
                {
                alg.ComputeHash( Encoding.ASCII.GetBytes( filename ) );
                byte[] hash = alg.Hash;
                int firstLetterId = (int) hash[0];
                int part = firstLetterId % NUMBER_OF_BACKUP_FILES;
                //Console.WriteLine( "Hash:" + part );
                return part;
                }
            }


        public ConcurrentDictionary<String, MetadataEntry> LoadMetaTable( )
            {
            ConcurrentDictionary<String, MetadataEntry> table = new ConcurrentDictionary<string, MetadataEntry>( );
            foreach ( BackupFile backupFile in _backupFiles )
                {
                backupFile.LoadFromFile( table );
                }
            Console.WriteLine(table.Count+" Files loaded from storage");
            return table;
            }
        }

    internal class BackupFile
        {
        private object locker = new object( );
        private String _filePath;
        public BackupFile( int fileId )
            {
            _filePath = BackupManager.STORAGE_DIR + "backup_" + fileId + ".txt";
            //Create file if not exists, load if existsInitFile
            if ( !File.Exists( _filePath ) )
                {
                Directory.CreateDirectory( BackupManager.STORAGE_DIR );
                File.Create( _filePath ).Close() ;
                File.WriteAllText( _filePath, "<?xml version='1.0' encoding='utf-8'?><ROOT>" );
                }
            }

        public void AddEntry( MetadataEntry entry )
            {
            lock ( locker )
                {
                StreamWriter twAppend = File.AppendText( _filePath );
                XmlTextWriter xtw = new XmlTextWriter( twAppend );
                xtw.WriteStartElement( "entry" );
                xtw.WriteElementString( "FN", entry.FileName );
                xtw.WriteElementString( "NB", entry.NBDataServers.ToString( ) );
                xtw.WriteElementString( "RQ", entry.ReadQuorum.ToString( ) );
                xtw.WriteElementString( "WQ", entry.WriteQuorum.ToString( ) );
                xtw.WriteStartElement( "SL" );
                foreach ( var pair in entry.ServerFileList )
                    {
                    //Unique server name
                    xtw.WriteStartElement( "sF" );
                    xtw.WriteElementString( "id", pair.Key.id );
                    xtw.WriteElementString( "h", pair.Key.hostname );
                    xtw.WriteElementString( "p", pair.Key.port.ToString( ) );
                    xtw.WriteElementString( "rp", pair.Key.recoverPort.ToString( ) );
                    xtw.WriteElementString( "F", pair.Value );
                    xtw.WriteEndElement( );
                    }
                xtw.WriteEndElement( );
                xtw.WriteEndElement( );
                xtw.Flush( );
                twAppend.Close();
                xtw.Close();
                }
            }

        public void RemoveEntry( MetadataEntry entry )
            {
            lock ( locker )
                {
                StreamWriter twAppend = File.AppendText( _filePath );
                XmlTextWriter xtw = new XmlTextWriter( twAppend );
                xtw.WriteStartElement( "entry" );
                xtw.WriteElementString( "Delete", entry.FileName );
                xtw.WriteEndElement( );
                xtw.Flush();
                 twAppend.Close();
                 xtw.Close();
                }
            }

        //Os updates sao apenas adicionar, considera apenas o mais recente
        public void UpdateEntry( MetadataEntry entry )
            {
            lock ( locker )
                {
                AddEntry( entry );
                }
            }

        public void LoadFromFile( ConcurrentDictionary<string, MetadataEntry> table )
            {
            XmlDocument doc = new XmlDocument( );
            doc.XmlResolver = null;
            try
                {
                doc.LoadXml( File.ReadAllText( _filePath ) + "</ROOT>" );
                }
            catch ( Exception )
                {
                Console.WriteLine( "Empty xml file" );
                return;
                }

            List<String> filesDeleted = new List<string>( );
            //Ler do fim para o inicio, se ja existe, ignora porque e mais antigo
            XmlNodeList nodeList = doc.SelectNodes( "//entry" );
            for ( int i = nodeList.Count - 1; i >= 0; i-- )
                {
                XmlNode element = nodeList[i];
                String operation = element.ChildNodes[0].LocalName;

                //FileName
                String filename = element.ChildNodes[0].InnerText;
                MetadataEntry ignore;
                if ( operation == "Delete" )
                    filesDeleted.Add( filename );
                if ( filesDeleted.Contains( filename ) )
                    {
                    continue;
                    }

                if ( table.TryGetValue( filename, out ignore ) )
                    continue;

                //NBDataServers
                int nBDataServers = Convert.ToInt32( element.ChildNodes[1].InnerText );
                //ReadQuorum
                int readQuorum = Convert.ToInt32( element.ChildNodes[2].InnerText );
                //WriteQuorum
                int writeQuorum = Convert.ToInt32( element.ChildNodes[3].InnerText );

                MetadataEntry entry = new MetadataEntry( filename, nBDataServers, readQuorum, writeQuorum );
                //ServerList
                foreach ( XmlNode node in element.ChildNodes[4] )
                    {
                    ServerId server;
                    server.id = node.ChildNodes[0].InnerText;
                    server.hostname = node.ChildNodes[1].InnerText;
                    server.port = Convert.ToInt32( node.ChildNodes[2].InnerText );
                    server.recoverPort = Convert.ToInt32( node.ChildNodes[3].InnerText );
                    String fileName = node.ChildNodes[4].InnerText;
                    entry.ServerFileList.TryAdd( server, fileName );
                    }
                table.TryAdd( filename, entry );
                }
            }

        private void CleanLogFile( )
            {
            lock (locker)
            {
                ConcurrentDictionary<string, MetadataEntry> table = new ConcurrentDictionary<string, MetadataEntry>();
                LoadFromFile(table);
                File.Delete(_filePath);
                foreach (KeyValuePair<string, MetadataEntry> metadataEntry in table)
                {
                    AddEntry(metadataEntry.Value);
                }
            }      
            }
        }
    }
