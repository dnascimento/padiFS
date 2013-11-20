using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedLib.MetadataObjects;

namespace Metadata
    {
    class AlreadySerialized : Exception
    {
        public MetaRequestIdAssociation Association;
        public AlreadySerialized(MetaRequestIdAssociation association)
        {
            Association = association;
        }
    }
    }
