using System;

namespace Neon.Networking.Udp.Exceptions
{
    public class ConnectionException : Exception
    {
        public DisconnectReason DisconnectReason { get; }
        
        public ConnectionException(DisconnectReason disconnectReason, string message) : base(message)
        {
            this.DisconnectReason = disconnectReason;
        }

        // public static ConnectionException CreateFromReason(DisconnectReason reason)
        // {
        //     switch (reason)
        //     {
        //         case DisconnectReason.Error:
        //             return new ConnectionException(reason, "Connection has encountered errors");
        //         case DisconnectReason.Timeout:
        //             return new ConnectionException(reason, "Connection is closed by timeout");
        //         case DisconnectReason.ClosedByOtherPeer:
        //             return new ConnectionException(reason, "Connection is closed by the other peer");
        //         case DisconnectReason.ClosedByThisPeer:
        //             return new ConnectionException(reason, "Connection is closed by this peer");
        //         default:
        //             return new ConnectionException(reason, "Connection closed");
        //     }
        // }
        //
        public static ConnectionException CreateFromReasonForConnect(DisconnectReason reason)
        {
            switch (reason)
            {
                case DisconnectReason.Error:
                    return new ConnectionException(reason, "Connection has encountered errors");
                case DisconnectReason.Timeout:
                    return new ConnectionException(reason, "Connect timeout");
                case DisconnectReason.ClosedByOtherPeer:
                    return new ConnectionException(reason, "The other peer rejected the connection");
                case DisconnectReason.ClosedByThisPeer:
                    return new ConnectionException(reason, "Connection was closed prematurely");
                default:
                    return new ConnectionException(reason, "Connection closed");
            }
        }
        
        public ConnectionException() : base()
        {
            
        }
    }
}