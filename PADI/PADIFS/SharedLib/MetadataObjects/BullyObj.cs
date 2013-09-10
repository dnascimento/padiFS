using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLib.MetadataObjects
    {
    [Serializable]
    public class ViewMsg
    {
        public ViewMsgType Type;
        public int Source;
        public int Destination;
        public int Betrayed;
        public ServerStatus Status;
        public long[] StateVector;

        public ViewMsg(ViewMsgType type, ServerStatus status, int source,long[] stateVector)
        {
            Type = type;
            Status = status;
            Source = source;
            StateVector = stateVector;
        }
        public override string ToString( )
            {
            return "View Msg: source:" + Source + " Dest: " + Destination+" : "+Status;
            }
 
    }
    
    [Serializable]
    public enum ViewMsgType
        {
            NewRowdy, StatusUpdate,ServerBetray, ServerBetrayAck
        }

    [Serializable]
    public enum ServerStatus
        {
        Unknown, Online, Off, New, Pause, Ready
        }

    [Serializable]
    public struct ViewServerStatus
    {
        public int ServerId;
        public long LastDeliveredId;
        public long LastKnownId;
    }


    }
