using System;
using Neon.Networking.Messages;

namespace Neon.Rpc.Serialization
{
    public interface IRpcSerializer
    {
        /// <summary>
        /// Deserializes object from the message
        /// </summary>
        /// <param name="message">Message</param>
        /// <returns>An instance of new object</returns>
        /// <exception cref="ArgumentNullException">Message is null</exception>
        /// <exception cref="ArgumentException">Wrong message format</exception>
        /// <exception cref="InvalidOperationException">Message type is not registered in this serializer, or message has wrong payload type</exception>
        object ParseBinary(IRawMessage message);

        /// <summary>
        /// Serializes object to the message
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="value">Object instance</param>
        /// <exception cref="ArgumentNullException">Message is null</exception>
        /// <exception cref="ArgumentException">Object has wrong type (not protobuf and not primitive)</exception>
        /// <exception cref="InvalidOperationException">Message type is not registered in this serializer</exception>
        /// <exception cref="NotImplementedException"></exception>
        void WriteBinary(IRawMessage message, object value);
    }
}