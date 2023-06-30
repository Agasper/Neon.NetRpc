using System;
using System.IO;

namespace Neon.Util.Io
{
    /// <summary>
    ///     Stream with read limits
    /// </summary>
    public class LimitedReadOnlyStream : Stream
    {
        public override bool CanRead => _stream.CanRead;

        public override bool CanSeek => _stream.CanSeek;

        public override bool CanWrite => false;

        public override long Length => _length;

        public override long Position
        {
            get => _stream.Position - _offset;
            set
            {
                if (value < 0 || value > _length)
                    throw new ArgumentOutOfRangeException(nameof(value));
                _stream.Position = value + _offset;
            }
        }

        readonly int _length;
        readonly long _offset;
        readonly Stream _stream;

        public LimitedReadOnlyStream(Stream stream, int offset, int length)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _offset = offset;
            _length = length;
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            try
            {
                if (Position + count > Length)
                    count = (int) (Length - Position);
                int result = _stream.Read(buffer, offset, count);
                return result;
            }
            catch (ArgumentException ex)
            {
                if (ex.Message == "buffer length must be at least offset + count")
                    throw new ArgumentException(
                        $"buffer length must be at least offset + count. buffer len: {buffer.Length}, offset: {offset}, count: {count}, Position: {Position}, Length: {Length}, base position: {_stream.Position}, base length: {_stream.Length}");
                throw;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}