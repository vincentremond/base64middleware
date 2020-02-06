using System.IO;
using System.IO.Pipelines;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Base64Middleware.StreamHelper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Base64Middleware
{
    public class Base64Body : IHttpResponseBodyFeature
    {
        private readonly IHttpResponseBodyFeature _originalBodyFeature;
        private readonly AutoFlushableStream _outputStream = null;
        private bool _complete = false;
        private PipeWriter _pipeAdapter = null;

        public Base64Body(
            IHttpResponseBodyFeature originalBodyFeature)
        {
            _outputStream = new AutoFlushableStream(CreateEncryptionStream(originalBodyFeature));
            _originalBodyFeature = originalBodyFeature;
        }

        private static CryptoStream CreateEncryptionStream(
            IHttpResponseBodyFeature originalBodyFeature)
        {
            return new CryptoStream(originalBodyFeature.Stream, new ToBase64Transform(), CryptoStreamMode.Write);
        }

        public async Task CompleteAsync()
        {
            if (!_complete)
            {
                await FinishEncryptionAsync();
            }

            await _originalBodyFeature.CompleteAsync();
        }

        public void DisableBuffering()
        {
            _outputStream.DisableBuffering();
            _originalBodyFeature.DisableBuffering();
        }

        public Task SendFileAsync(
            string path,
            long offset,
            long? count,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return SendFileFallback.SendFileAsync(_outputStream, path, offset, count, cancellationToken);
        }

        public Task StartAsync(
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _originalBodyFeature.StartAsync(cancellationToken);
        }

        public Stream Stream => _outputStream;

        public PipeWriter Writer
        {
            get
            {
                if (_pipeAdapter == null)
                {
                    _pipeAdapter = PipeWriter.Create(Stream, new StreamPipeWriterOptions(leaveOpen: true));
                }

                return _pipeAdapter;
            }
        }

        internal async Task FinishEncryptionAsync()
        {
            if (_complete)
            {
                return;
            }

            _complete = true;

            if (_pipeAdapter != null)
            {
                await _pipeAdapter.CompleteAsync();
            }

            if (_outputStream != null)
            {
                await _outputStream.DisposeAsync();
            }
        }
    }
}