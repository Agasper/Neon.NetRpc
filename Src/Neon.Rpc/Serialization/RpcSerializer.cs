using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Neon.Logging;
using Neon.Networking.IO;
using Neon.Networking.Messages;
using Neon.Util.Pooling;

namespace Neon.Rpc.Serialization
{
    public class RpcSerializer
    {
        // static ILogger logger = LogManager.Default.GetLogger(nameof(RpcSerializer));
        //
        public enum PayloadType : byte
        {
            TypeCode,
            Protobuf
        }
        
        public Encoding Encoding { get; }

        protected Dictionary<string, MessageInfo> messageMap;
        protected Dictionary<uint, MessageInfo> messageMapId;
        protected Dictionary<Type, MessageInfo> messageMapReverse;
        
        protected IMemoryManager memoryManager;
        
        public RpcSerializer(IMemoryManager memoryManager)
            : this(memoryManager, Encoding.UTF8)
        {
            
        }

        public RpcSerializer(IMemoryManager memoryManager, Encoding encoding)
        {
            if (memoryManager == null)
                throw new ArgumentNullException(nameof(memoryManager));
            this.memoryManager = memoryManager;
            this.Encoding = encoding;
            this.messageMap = new Dictionary<string, MessageInfo>();
            this.messageMapId = new Dictionary<uint, MessageInfo>();
            this.messageMapReverse = new Dictionary<Type, MessageInfo>();
        }

        bool IsValidType(Type type)
        {
            return type.GetInterfaces().Contains(typeof(IMessage));
        }

        public bool RegisterType(Type type)
        {
            if (!IsValidType(type))
                throw new InvalidOperationException(
                    $"The provided type {type.FullName} must be inherited from {nameof(IMessage)}");

            MessageDescriptor descriptor = type.GetProperty("Descriptor",
                BindingFlags.Public | BindingFlags.Static)?.GetValue(null) as MessageDescriptor;

            if (descriptor == null)
                throw new NullReferenceException(
                    $"{nameof(MessageDescriptor)} not found for message {type.FullName}");

            ConstructorInfo constructorInfo = descriptor.ClrType.GetConstructor(Type.EmptyTypes);
            if (constructorInfo == null)
                throw new ArgumentException($"Message {descriptor.ClrType.Name} doesn't have public parameterless constructor");
            
            MessageParser parser = type.GetProperty("Parser",
                BindingFlags.Public | BindingFlags.Static)?.GetValue(null) as MessageParser;

            if (parser == null)
                throw new NullReferenceException(
                    $"{nameof(MessageParser)} not found for message {type.FullName}");

            uint? messageId = null;
            var extension = new Extension<MessageOptions, uint>(55217, null);
            var options = descriptor.GetOptions();
            if (options != null && options.HasExtension(extension))
                messageId = options.GetExtension(extension);

            MessageInfo messageInfo = new MessageInfo(descriptor, parser, messageId);
            try
            {
                messageMap.Add(messageInfo.MessageType, messageInfo);
            }
            catch (ArgumentException)
            {
                return false;
                // throw new ArgumentException($"Message with name {messageInfo.MessageType} already registered");
            }

            try
            {
                messageMapReverse.Add(type, messageInfo);
            }
            catch (ArgumentException)
            {
                messageMap.Remove(messageInfo.MessageType);
                return false;
                // throw new ArgumentException($"Message with CLR type {type} already registered");
            }
            
            if (messageId.HasValue)
            {
                try
                {
                    messageMapId.Add(messageId.Value, messageInfo);
                }
                catch (ArgumentException)
                {
                    messageMap.Remove(messageInfo.MessageType);
                    messageMapReverse.Remove(type);
                    return false;
                    // throw new ArgumentException($"Message with ID {messageId} already registered");
                }
            }

            OnTypeRegistered(type, messageInfo);
            return true;
        }

        protected virtual void OnTypeRegistered(Type type, MessageInfo messageInfo)
        {
            
        }

        public void RegisterTypesFromCurrentAssembly()
        {
            Assembly assembly = Assembly.GetEntryAssembly();
            if (assembly == null)
                throw new NullReferenceException("Couldn't get current assembly");
            this.RegisterTypesFromAssembly(assembly);
        }

        public void RegisterTypesFromAssembly(params Assembly[] assemblies)
        {
            if (assemblies == null)
                throw new ArgumentNullException(nameof(assemblies));

            for(int i = 0; i < assemblies.Length; i++)
            {
                var assembly = assemblies[i];
                foreach (var messageType in assembly.GetTypes())
                {
                    if (IsValidType(messageType))
                        RegisterType(messageType);
                }
            }
        }

        public object ParseBinary(IRawMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            int length = message.ReadVarInt32();
            if (length < 0)
                return null;

            byte serviceByte = message.ReadByte();
            PayloadType payloadType = (PayloadType)(serviceByte & 0b_0111_1111);
            if (payloadType == PayloadType.Protobuf)
            {
                bool hasId = (serviceByte & 0b_1000_0000) == 0b_1000_0000;
                MessageInfo messageInfo;
                if (!hasId)
                {
                    string messageType = message.ReadString();
                    if (!messageMap.TryGetValue(messageType, out messageInfo))
                        throw new InvalidOperationException($"Message type {messageType} not found in the registry");
                }
                else
                {
                    uint messageId = message.ReadVarUInt32();
                    if (!messageMapId.TryGetValue(messageId, out messageInfo))
                        throw new InvalidOperationException($"Message with ID {messageId} not found in the registry");
                }
                
                using (LimitedReadStream limitedReadStream = new LimitedReadStream(message.AsStream(), length, true))
                {
                    byte[] rentedArray = memoryManager.RentArray(memoryManager.DefaultBufferSize);
                    try
                    {
                        using (CodedInputStream cis = new CodedInputStream(limitedReadStream, rentedArray, true))
                            return messageInfo.Parser.ParseFrom(cis);
                    }
                    finally
                    {
                        memoryManager.ReturnArray(rentedArray);
                    }
                }
            }
            else if (payloadType == PayloadType.TypeCode)
            {
                TypeCode typeCode = (TypeCode)message.ReadVarInt32();
                return ReadPrimitive(message, typeCode, length);
            }
            
            throw new InvalidOperationException($"Unsupported payload type {(int)payloadType}");
        }

        public void WriteBinary(IRawMessage message, object value)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (value == null)
            {
                message.WriteVarInt(-1);
                return;
            }

            if (value is IMessage iMessage)
            {
                var type = value.GetType();
                if (!messageMapReverse.ContainsKey(type))
                {
                    throw new ArgumentException($"IMessage with clr type {type.FullName} not found in registry. Please register the type.");
                }
                var messageInfo = messageMapReverse[type];

                int len = iMessage.CalculateSize();
                message.WriteVarInt(len);
                byte serviceByte = (byte)(messageInfo.Id.HasValue ? 0b_1000_0000 : 0b_0000_0000);
                serviceByte |= (byte)PayloadType.Protobuf & 0b_0111_1111;
                message.Write(serviceByte);

                if (messageInfo.Id.HasValue)
                    message.WriteVarInt(messageInfo.Id.Value);
                else
                    message.Write(messageInfo.MessageType);

                if (len > 0)
                {
                    byte[] rentedArray = memoryManager.RentArray(memoryManager.DefaultBufferSize);
                    try
                    {
                        using (CodedOutputStream cos = new CodedOutputStream(message.AsStream(), rentedArray, true))
                        {
                            iMessage.WriteTo(cos);
                        }
                    }
                    finally
                    {
                        memoryManager.ReturnArray(rentedArray);
                    }
                }
            }
            else if (value is INeonMessage customMessageValue)
            {
                throw new NotImplementedException();
            }
            else if (IsPrimitive(value.GetType()))
            {
                WritePrimitive(message, value);
            }
            else
                throw new ArgumentException($"{nameof(value)} should be IMessage, IneonMessage or primitive type, but got {value.GetType().Name}");
        }
        
        public static bool IsPrimitive(Type type)
        {
            TypeCode typeCode = Type.GetTypeCode(type);
            if (typeCode == TypeCode.Empty || 
                typeCode == TypeCode.Object || 
                typeCode == TypeCode.DBNull)
                return false;

            return true;
        }

        void WritePrimitiveSetup(IRawMessage message, int length, TypeCode typeCode)
        {
            message.WriteVarInt(length);
            message.Write((byte)PayloadType.TypeCode);
            message.WriteVarInt((int)typeCode);
        }

        void WritePrimitive(IRawMessage message, object value)
        {
            TypeCode typeCode = Type.GetTypeCode(value.GetType());
            switch (typeCode)
            {
                case TypeCode.Boolean:
                    WritePrimitiveSetup(message, 1, typeCode); message.Write((bool)value); break;
                case TypeCode.Single:
                    WritePrimitiveSetup(message, 4, typeCode); message.Write((float)value); break;
                case TypeCode.Double: 
                    WritePrimitiveSetup(message, 8, typeCode); message.Write((double)value); break;
                case TypeCode.Byte: 
                    WritePrimitiveSetup(message, 1, typeCode); message.Write((byte)value); break;
                case TypeCode.SByte: 
                    WritePrimitiveSetup(message, 1, typeCode); message.Write((sbyte)value); break;
                case TypeCode.Int16: 
                    WritePrimitiveSetup(message, 2, typeCode); message.Write((short)value); break;
                case TypeCode.UInt16: 
                    WritePrimitiveSetup(message, 2, typeCode); message.Write((ushort)value); break;
                case TypeCode.Int32: 
                    WritePrimitiveSetup(message, 4, typeCode); message.Write((int)value); break;
                case TypeCode.UInt32: 
                    WritePrimitiveSetup(message, 4, typeCode); message.Write((uint)value); break;
                case TypeCode.Int64: 
                    WritePrimitiveSetup(message, 8, typeCode); message.Write((long)value); break;
                case TypeCode.UInt64: 
                    WritePrimitiveSetup(message, 8, typeCode); message.Write((ulong)value); break;
                case TypeCode.Decimal:
                    WritePrimitiveSetup(message, 16, typeCode); message.Write((decimal)value); break;
                case TypeCode.Char:
                    byte[] bytes = BitConverter.GetBytes((char) value);
                    WritePrimitiveSetup(message, bytes.Length, typeCode); 
                    message.Write(bytes);
                    break;
                case TypeCode.String:
                    string s = (string) value;
                    int bytesLen = this.Encoding.GetByteCount(s);
                    WritePrimitiveSetup(message, bytesLen, typeCode); 
                    message.Write(this.Encoding.GetBytes(s)); 
                    break;
                case TypeCode.DateTime:
                    WritePrimitiveSetup(message, 8, typeCode); message.Write(((DateTime)value).Ticks); break;
                default: throw new ArgumentException($"Invalid primitive type `{value.GetType().FullName}`");
            }
        }

        object ReadPrimitive(IRawMessage message, TypeCode typeCode, int length)
        {
            switch (typeCode)
            {
                case TypeCode.Boolean: return message.ReadBoolean();
                case TypeCode.Single: return message.ReadSingle();
                case TypeCode.Double: return message.ReadDouble();
                case TypeCode.Byte: return message.ReadByte();
                case TypeCode.SByte: return message.ReadSByte();
                case TypeCode.Int16: return message.ReadInt16();
                case TypeCode.UInt16: return message.ReadUInt16();
                case TypeCode.Int32: return message.ReadInt32();
                case TypeCode.UInt32: return message.ReadUInt32();
                case TypeCode.Int64: return message.ReadInt64();
                case TypeCode.UInt64: return message.ReadUInt64();
                case TypeCode.String: return Encoding.UTF8.GetString(message.ReadBytes(length));
                case TypeCode.Char: return BitConverter.ToChar(message.ReadBytes(length), 0);
                case TypeCode.DateTime: return new DateTime(message.ReadInt64());
                case TypeCode.Decimal: return message.ReadDecimal();
                case TypeCode.DBNull: return null;
                default: throw new ArgumentException($"Unsupported type code `{typeCode}`");
            }
        }
    }
}
