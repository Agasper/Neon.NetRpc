using Google.Protobuf.WellKnownTypes;

namespace Neon.Rpc
{
    public class AuthenticationInfo
    {
        public object State { get;set; }
        public Any Argument { get;set; }

        public AuthenticationInfo(Any argument, object state)
        {
            State = state;
            Argument = argument;
        }
        
        public AuthenticationInfo(Any argument)
        {
            Argument = argument;
        }
        
        public AuthenticationInfo()
        {
        }
    }
}