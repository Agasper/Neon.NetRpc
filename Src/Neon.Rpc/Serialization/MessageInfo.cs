using System;
using System.Reflection;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace Neon.Rpc.Serialization
{
    public struct MessageInfo
    {
        /// <summary>
        /// String representation of network type
        /// </summary>
        public string MessageType { get; }
        /// <summary>
        /// Protobuf message descriptor
        /// </summary>
        public MessageDescriptor Descriptor { get;  }
        /// <summary>
        /// Protobuf message parser
        /// </summary>
        public MessageParser Parser { get;  }
        /// <summary>
        /// Optional message id
        /// </summary>
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