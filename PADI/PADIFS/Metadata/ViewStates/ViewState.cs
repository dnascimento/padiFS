using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Metadata.ViewStatus;
using SharedLib.MetadataObjects;

namespace Metadata.ViewStates
    {

    public abstract class ViewState
        {
        public ServerStatus Status;
        public MetaViewManager Manager;

        public ViewState( MetaViewManager manager, ServerStatus status )
        {
            Status  = status;
            Manager = manager;
            }

        public delegate void ChangeStateDelegate( );
        public void ChangeStateCallback( IAsyncResult ia )
            {
            Console.WriteLine( "State changed Async" );
            }

        public abstract ViewMsg NewRowdyMsgRequest(int source);
        public abstract void Start( );
        public abstract void ViewStatusChanged( );
        }
    }
