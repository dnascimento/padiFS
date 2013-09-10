using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Metadata;

//Estrutura usada para enviar a copia do Core
namespace SharedLib.MetadataObjects
    {
    [Serializable]
    public class CopyStructMetadata
        {
        //ServerState: Ficheiro vs Metadados
        public ConcurrentDictionary<String, MetadataEntry> MetaTable;
        //Lista de dataservers
        public List<DataserverInfo> DataServerTable;
        public List<String> Used;
        public  Int64 RequestIdMonotonic;
        public Random RandomGenerator;
        public long[] StatusVector;

        }
    }
