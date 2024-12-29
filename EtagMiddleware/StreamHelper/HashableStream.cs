using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace EtagMiddleware.StreamHelper
{
    public class HashableStream : Stream
    {
        private readonly Stream _outputStream;
        private readonly HashAlgorithm _hasher;
        private bool _autoFlush;

        public HashableStream(
            Stream outputStream)
        {
            _outputStream = outputStream;
            _hasher = new MD5CryptoServiceProvider();
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
            throw new InvalidOperationException($"{nameof(HashableStream)} is only writable");
        }

        public override long Seek(
            long offset,
            SeekOrigin origin)
        {
            throw new InvalidOperationException($"{nameof(HashableStream)} is only writable");
        }

        public override void SetLength(
            long value)
        {
            throw new InvalidOperationException($"{nameof(HashableStream)} is only writable");
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

            _hasher.TransformBlock(buffer, 0, count, null, 0);
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

            _hasher.TransformBlock(buffer, 0, count, null, 0);
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => _outputStream.CanWrite;
        public override long Length => _outputStream.Length;

        public override long Position
        {
            get => throw new InvalidOperationException($"{nameof(HashableStream)} is only writable");
            set => throw new InvalidOperationException($"{nameof(HashableStream)} is only writable");
        }

        public new void Dispose()
        {
            _outputStream.Dispose();
        }

        public override ValueTask DisposeAsync()
        {
            _hasher.TransformFinalBlock(new byte[0], 0, 0);
            return _outputStream.DisposeAsync();
        }

        public void DisableBuffering()
        {
            _autoFlush = true;
        }

        public override void EndWrite(
            IAsyncResult asyncResult)
        {
            if (asyncResult == null)
            {
                throw new ArgumentNullException(nameof(asyncResult));
            }

            var task = (Task) asyncResult;
            task.GetAwaiter().GetResult();
        }

        public byte[] GetHash()
        {
            return _hasher.Hash;
        }
    }
}