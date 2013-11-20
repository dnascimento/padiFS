using System;
using SharedLib.DataserverObjects;
using SharedLib.MetadataObjects;
using System.Collections.Concurrent;

namespace SharedLib
{
    public interface IDataToClient
    {
        TFile Read(String localFileName);
        String Write(String localFileName, byte[] data, long version);
    }


    public interface IDataToPuppet
    {
        String Freeze();
        String UnFreeze();
        String Fail();
        String Recover();
        String Dump();
    }

    
    public interface IDataToMeta
    {
        void CreateEmptyFile(String filename, string localFilename);
        void CopyFileToOtherData(String srcFileName, String destLocalFilename, String destHostname, int destPort);
        ConcurrentDictionary<String, LocalFileStatistics> GetFileStatistics();
    }
    public interface  IDataToData
    {
         void ReceiveFileCopy(TFile t, LocalFileStatistics stats,String newLocalFilename);
    }

}
