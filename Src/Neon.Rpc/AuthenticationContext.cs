using System.Threading;
using Google.Protobuf.WellKnownTypes;

namespace Neon.Rpc
{
    public class AuthenticationContext
    {
        public IRpcConnection Connection { get; }
        public Any AuthenticationArgument { get; }
        public object AuthenticationState { get; set; }
        public Any AuthenticationResult { get; set; }

        public AuthenticationContext(IRpcConnection connection, Any argument)
        {
            Connection = connection;
            AuthenticationArgument = argument;
        }
    }
}