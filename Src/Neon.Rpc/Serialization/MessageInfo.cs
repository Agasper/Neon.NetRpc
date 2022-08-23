using System;
using System.Reflection;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace Neon.Rpc.Serialization
{
    public struct MessageInfo
    {
        public string MessageType { get; }
        public MessageDescriptor Descriptor { get;  }
        public MessageParser Parser { get;  }
        public uint? Id { get; }

        internal MessageInfo(MessageDescriptor messageDescriptor, MessageParser messageParser, uint? id)
        {
            this.Id = id;
            this.Descriptor = messageDescriptor;
            this.Parser = messageParser;
            this.MessageType = messageDescriptor.File.Package + "/" + messageDescriptor.Name;
        }
    }
}