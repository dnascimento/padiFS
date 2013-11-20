using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibs
{
    public interface IRemoteDataServer
    {
        String GetServerID();

        int Save(String fName, TFile file);

        TFile Read(String fName);

        TFile MonotonicRead(String fName, int Version);

    }

    [Serializable]
    public class TFile
    {
        int VersionNumber;
        long Size;
        byte[] Data;

        public TFile(int versionNumber, byte[] data)
        {
            VersionNumber = versionNumber;
            Data = data;
            this.Size = data.Length * 8;
        }
    }

}
