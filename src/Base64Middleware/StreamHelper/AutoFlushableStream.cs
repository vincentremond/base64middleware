using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Base64Middleware.StreamHelper
{
    /// <summary>
    /// This is an encapsulation for a stream to become auto-flushable after being written into.
    /// </summary>
    public class AutoFlushableStream : Stream
    {
        private readonly Stream _outputStream;
        private bool _autoFlush;

        public AutoFlushableStream(
            Stream outputStream)
        {
            _outputStream = outputStream;
        }

        public override void Flush()
        {
            _outputStream.Flush();
        }

        public override int Read(
            byte[] buffer,
            int offset,
            int count)
        {
            return _outputStream.Read(buffer, offset, count);
        }

        public override long Seek(
            long offset,
            SeekOrigin origin)
        {
            return _outputStream.Seek(offset, origin);
        }

        public override void SetLength(
            long value)
        {
            _outputStream.SetLength(value);
        }

        public override void Write(
            byte[] buffer,
            int offset,
            int count)
        {
            _outputStream.Write(buffer, offset, count);
            if (_autoFlush)
            {
                _outputStream.Flush();
            }
        }

        public override async Task WriteAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            await _outputStream.WriteAsync(buffer, offset, count, cancellationToken);
            if (_autoFlush)
            {
                await _outputStream.FlushAsync(cancellationToken);
            }
        }

        public override bool CanRead => _outputStream.CanRead;
        public override bool CanSeek => _outputStream.CanSeek;
        public override bool CanWrite => _outputStream.CanWrite;
        public override long Length => _outputStream.Length;

        public override long Position
        {
            get => _outputStream.Position;
            set => _outputStream.Position = value;
        }

        public new void Dispose()
        {
            _outputStream.Dispose();
        }

        public override ValueTask DisposeAsync()
        {
            return _outputStream.DisposeAsync();
        }

        public void DisableBuffering()
        {
            _autoFlush = true;
        }
        
        public override void EndWrite(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
            {
                throw new ArgumentNullException(nameof(asyncResult));
            }

            var task = (Task)asyncResult;
            task.GetAwaiter().GetResult();
        }
    }
}