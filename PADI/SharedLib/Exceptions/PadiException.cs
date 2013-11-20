using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedLib.MetadataObjects;

namespace SharedLib.Exceptions
{
        [Serializable]
        public enum PadiExceptiontType { OpenFile, CreateFile, DeleteFile, CloseFile, Registry, ReadFile,WriteFile,InvalidRequest,SlavesDontReply,
            AlreadyProcessed,
            Balancing
        }


        [Serializable]
        public class PadiException : ApplicationException
        {

            public PadiExceptiontType Cause;
            public String Description;

            public PadiException()
            {
                
            }

            public PadiException(PadiExceptiontType cause, String description)
            {
                this.Cause = cause;
                this.Description = description;
            }

            public PadiException(System.Runtime.Serialization.SerializationInfo info,
		System.Runtime.Serialization.StreamingContext context)
		: base(info, context) {
            Description = info.GetString("Description");
            Cause = (PadiExceptiontType)info.GetValue("Cause", typeof(PadiExceptiontType));
	}

	public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) {
		base.GetObjectData(info, context);
        info.AddValue("Description", Description);
        info.AddValue("Cause", Cause);
	    }

        }
}
