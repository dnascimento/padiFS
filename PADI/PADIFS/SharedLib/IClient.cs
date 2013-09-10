using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedLib.MetadataObjects;
using SharedLib.DataserverObjects;

namespace SharedLib
{
    public interface  IClientToMeta
    {
        Boolean ReceiveMetaserverResponse(MetaserverResponse response);
    }
    public interface IClientToPuppet
    {
        
        String Open(String filename);
        String Close(String filename);
        String Create(String filename, int nbDataServers, int readQuorum, int writeQuorum);
        String Delete(String filename);


        String Read(int fileRegister, SemanticType semantic, int stringRegister);
        String Write(int fileRegister, byte[] data);
        String Write(int fileRegister, int byteArrayRegister);
        String Copy(int fileRegisterRead, SemanticType semantic, int fileRegisterWrite, byte[] stringSalt);
        String ExeScript(Queue<String> commandList);
        String Dump( );
    }
}
