// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: rpc_message_header_data_rpc_request.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021, 8981
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace Neon.Rpc.Messages.Proto {

  /// <summary>Holder for reflection information generated from rpc_message_header_data_rpc_request.proto</summary>
  internal static partial class RpcMessageHeaderDataRpcRequestReflection {

    #region Descriptor
    /// <summary>File descriptor for rpc_message_header_data_rpc_request.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static RpcMessageHeaderDataRpcRequestReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "CilycGNfbWVzc2FnZV9oZWFkZXJfZGF0YV9ycGNfcmVxdWVzdC5wcm90bxII",
            "bmVvbi5ycGMiTAojUnBjTWVzc2FnZUhlYWRlckRhdGFScGNSZXF1ZXN0UHJv",
            "dG8SDAoEcGF0aBgBIAEoCRIXCg9leHBlY3RfcmVzcG9uc2UYAiABKAhCGqoC",
            "F05lb24uUnBjLk1lc3NhZ2VzLlByb3RvYgZwcm90bzM="));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { },
          new pbr::GeneratedClrTypeInfo(null, null, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::Neon.Rpc.Messages.Proto.RpcMessageHeaderDataRpcRequestProto), global::Neon.Rpc.Messages.Proto.RpcMessageHeaderDataRpcRequestProto.Parser, new[]{ "Path", "ExpectResponse" }, null, null, null, null)
          }));
    }
    #endregion

  }
  #region Messages
  internal sealed partial class RpcMessageHeaderDataRpcRequestProto : pb::IMessage<RpcMessageHeaderDataRpcRequestProto>
  #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      , pb::IBufferMessage
  #endif
  {
    private static readonly pb::MessageParser<RpcMessageHeaderDataRpcRequestProto> _parser = new pb::MessageParser<RpcMessageHeaderDataRpcRequestProto>(() => new RpcMessageHeaderDataRpcRequestProto());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pb::MessageParser<RpcMessageHeaderDataRpcRequestProto> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::Neon.Rpc.Messages.Proto.RpcMessageHeaderDataRpcRequestReflection.Descriptor.MessageTypes[0]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public RpcMessageHeaderDataRpcRequestProto() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public RpcMessageHeaderDataRpcRequestProto(RpcMessageHeaderDataRpcRequestProto other) : this() {
      path_ = other.path_;
      expectResponse_ = other.expectResponse_;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public RpcMessageHeaderDataRpcRequestProto Clone() {
      return new RpcMessageHeaderDataRpcRequestProto(this);
    }

    /// <summary>Field number for the "path" field.</summary>
    public const int PathFieldNumber = 1;
    private string path_ = "";
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public string Path {
      get { return path_; }
      set {
        path_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    /// <summary>Field number for the "expect_response" field.</summary>
    public const int ExpectResponseFieldNumber = 2;
    private bool expectResponse_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public bool ExpectResponse {
      get { return expectResponse_; }
      set {
        expectResponse_ = value;
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override bool Equals(object other) {
      return Equals(other as RpcMessageHeaderDataRpcRequestProto);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public bool Equals(RpcMessageHeaderDataRpcRequestProto other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (Path != other.Path) return false;
      if (ExpectResponse != other.ExpectResponse) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override int GetHashCode() {
      int hash = 1;
      if (Path.Length != 0) hash ^= Path.GetHashCode();
      if (ExpectResponse != false) hash ^= ExpectResponse.GetHashCode();
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void WriteTo(pb::CodedOutputStream output) {
    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      output.WriteRawMessage(this);
    #else
      if (Path.Length != 0) {
        output.WriteRawTag(10);
        output.WriteString(Path);
      }
      if (ExpectResponse != false) {
        output.WriteRawTag(16);
        output.WriteBool(ExpectResponse);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    #endif
    }

    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    void pb::IBufferMessage.InternalWriteTo(ref pb::WriteContext output) {
      if (Path.Length != 0) {
        output.WriteRawTag(10);
        output.WriteString(Path);
      }
      if (ExpectResponse != false) {
        output.WriteRawTag(16);
        output.WriteBool(ExpectResponse);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(ref output);
      }
    }
    #endif

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public int CalculateSize() {
      int size = 0;
      if (Path.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(Path);
      }
      if (ExpectResponse != false) {
        size += 1 + 1;
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void MergeFrom(RpcMessageHeaderDataRpcRequestProto other) {
      if (other == null) {
        return;
      }
      if (other.Path.Length != 0) {
        Path = other.Path;
      }
      if (other.ExpectResponse != false) {
        ExpectResponse = other.ExpectResponse;
      }
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void MergeFrom(pb::CodedInputStream input) {
    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      input.ReadRawMessage(this);
    #else
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 10: {
            Path = input.ReadString();
            break;
          }
          case 16: {
            ExpectResponse = input.ReadBool();
            break;
          }
        }
      }
    #endif
    }

    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    void pb::IBufferMessage.InternalMergeFrom(ref pb::ParseContext input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, ref input);
            break;
          case 10: {
            Path = input.ReadString();
            break;
          }
          case 16: {
            ExpectResponse = input.ReadBool();
            break;
          }
        }
      }
    }
    #endif

  }

  #endregion

}

#endregion Designer generated code