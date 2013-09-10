using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Metadata.ViewStates;
using SharedLib.Exceptions;
using SharedLib.MetadataObjects;

namespace Metadata
{
    /// <summary>
    /// Tem de permitir:
    /// - Adicionar Requests novos
    /// - Atribuir um ID de ordem aos requests existentes
    /// - Obter o próximo request
    /// </summary>
    public class NamespaceManager
    {
        private const int NUMBER_OF_SPACE_SPLIT = 6;
        private List<NamespacePart> _namespaceParts;
        private string STORAGE_DIR;
        private object locker = new object();

        public NamespaceManager()
        {
            STORAGE_DIR = "C:/PADIFS/Metaserver-" + MetadataServer.ThisMetaserverId + "/" + "queues.txt";
            if (!File.Exists(STORAGE_DIR))
            {
                Directory.CreateDirectory("C:/PADIFS/Metaserver-" + MetadataServer.ThisMetaserverId + "/");
                File.Create(STORAGE_DIR).Close();
            }

            
            _namespaceParts = new List<NamespacePart>();
            for (int i = 0; i < NUMBER_OF_SPACE_SPLIT; i++)
            {
                _namespaceParts.Add(new NamespacePart(i,this));
            }
        }


        /// <summary>
        /// Dado o filename, em que fila devo colocar o file
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public  int GetNameSpaceID( String filename )
        {
            if ( filename == null )
                return 0;
            using ( var alg = SHA256.Create( ) )
                {
                alg.ComputeHash( Encoding.ASCII.GetBytes( filename ) );
                byte[] hash = alg.Hash;
                int firstLetterId = (int) hash[0];
                int part = firstLetterId % NUMBER_OF_SPACE_SPLIT;
               // Console.WriteLine( "Hash:" + part );
                return part;
                }
        }

       

        /// <summary>
        /// Dada a view actual, quem vai ser o serializador desta queue?
        /// </summary>
        /// <param name="queueNumber"></param>
        /// <param name="manager"></param>
        /// <returns></returns>
        public int GetPartSerializer( int queueNumber, MetaViewManager manager )
            {
            List<int> onlineServers = manager.GetOnlineServers( );
            int numberQueuesThatEachServersResponsible = NUMBER_OF_SPACE_SPLIT / onlineServers.Count;
            for ( int i = 0; i < onlineServers.Count; i++ )
                {
                int max = (i + 1) * numberQueuesThatEachServersResponsible;
                if ( queueNumber < max )
                    {
                    return onlineServers[i];
                    }
                }
            throw new Exception( "Error getting the part responsible" );
            }

        public MetaRequest SerializeRequest(MetaRequest request)
        {
            int namespaceId = GetNameSpaceID(request.FileName);
            long seqNumber = _namespaceParts[namespaceId].GetNewSeqNumber();
            request.NamespaceSeqNumber = seqNumber;
            return request;
        }


        public void OperationAuth(MetaRequest request)
        {
            int namespaceId = GetNameSpaceID( request.FileName );
            _namespaceParts[namespaceId].OperationAuth( request.FileName,request.NamespaceSeqNumber );
        }

        public void OperationDone(MetaRequest request)
        {
            int namespaceId = GetNameSpaceID( request.FileName );
            _namespaceParts[namespaceId].AllowNext( request.FileName, request.NamespaceSeqNumber );
        }

        public long[] GetQueueStateVector()
        {
            long[] result = new long[NUMBER_OF_SPACE_SPLIT];
            for (int i = 0; i < NUMBER_OF_SPACE_SPLIT; i++)
            {
                result[i] = _namespaceParts[i].GetState();
            }
            return result;
        }

        public void SetQueueStateVector(long[] newStatus)
        {
        for ( int i = 0; i < NUMBER_OF_SPACE_SPLIT; i++ )
            {
            _namespaceParts[i].setState( newStatus[i] );
            }
        }

        public void writeToLog(long queue, long biggestId)
        {
            lock (locker)
            {
                StreamWriter writeToBackup = new StreamWriter( STORAGE_DIR );
                writeToBackup.WriteLine( queue + ":" + biggestId );
                writeToBackup.Flush( );
                writeToBackup.Close();
            }
        }

        public void loadFromLog()
        {
            long[] nums = new long[NUMBER_OF_SPACE_SPLIT];
            for (int k = 0; k < NUMBER_OF_SPACE_SPLIT; k++)
            {
                nums[k] = 0;
            }

            lock ( locker )
            {
           
            String[] openFile = File.ReadAllLines( STORAGE_DIR );
            Boolean done = false;
            int i = openFile.Length - 1;
                do
                {
                    String[] args = openFile[i].Split(':');
                    int queue = Convert.ToInt32(args[0]);
                    long numb = Convert.ToInt64(args[1]);
                    if (nums[queue] == -1)
                    {
                        nums[queue] = numb;
                    }
                    done = true;
                    for (int k = 0; k < NUMBER_OF_SPACE_SPLIT; k++)
                    {
                        if (nums[k] == 0)
                        {
                            done = false;
                            continue;
                        }
                    }
                } while (i >= 0 && !done);

                SetQueueStateVector(nums);
                File.Delete( STORAGE_DIR );
            }   
        }
    }

    internal class NamespacePart
    {
        public long SequenceNumber = 0;
        public long LastDeliveredMessage = 0;
        private object locker = new object();
        private  EventWaitHandle _freezeNewRequests = new EventWaitHandle( false, EventResetMode.ManualReset );
        private List<String> filenamesInUse = new List<string>();
        private NamespaceManager _manager;
        private int _thisNamespacePartNumber;

        public NamespacePart( int id, NamespaceManager namespaceManager )
        {
            _thisNamespacePartNumber = id;
            _manager = namespaceManager;
        }

        public long GetNewSeqNumber()
        {
            return Interlocked.Increment(ref SequenceNumber);
        }

        /// <summary>
        /// Fazer esta thread esperar ate que esteja pronta para entregar
        /// </summary>
        /// <param name="reqId"></param>
        public void cOperationAuth(String filename, long reqId)
        {
            long next;
            Boolean filenameInUse;
            lock (locker)
            {
            next = Interlocked.Read( ref LastDeliveredMessage ) + 1;
                if (filename == null)
                    filenameInUse = false;
                else
                  filenameInUse = filenamesInUse.Contains( filename );
            }
            while ( next != reqId || filenameInUse )
            {
                _freezeNewRequests.WaitOne(1000);
                lock ( locker )
                    {
                    next = Interlocked.Read( ref LastDeliveredMessage ) + 1;
                     if (reqId < next)
                     {
                         throw new PadiException(PadiExceptiontType.AlreadyProcessed,"OperationAuth: This request is already done");
                     }
                    filenameInUse = filenamesInUse.Contains( filename );
                    }
            }
            //Maike all wait (only 1 could exit)
            _freezeNewRequests.Reset();
            //Update state (allows next request if different filename)
            lock ( locker )
                {
                filenamesInUse.Add( filename );
                LastDeliveredMessage = Math.Max( LastDeliveredMessage, reqId );
                _manager.writeToLog( _thisNamespacePartNumber ,LastDeliveredMessage);
                }
        }

        /// <summary>
        /// Remove the use of this filename and increment last deliver
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="reqId"></param>
        public void AllowNext( String filename, long reqId )
        {
            lock (locker)
            {
                LastDeliveredMessage = Math.Max(LastDeliveredMessage, reqId );
                filenamesInUse.Remove(filename);
            }
            _freezeNewRequests.Set( );
        }

        public long GetState()
        {
            lock (locker)
            {
                return Math.Max(LastDeliveredMessage, SequenceNumber);
            }
        }

        public void setState(long newStatus)
        {
            lock (locker)
            {
                SequenceNumber = newStatus;
                LastDeliveredMessage = newStatus;
            }
        }
    }
}