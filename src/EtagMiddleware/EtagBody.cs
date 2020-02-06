using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using EtagMiddleware.StreamHelper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace EtagMiddleware
{
    public class EtagBody : IHttpResponseBodyFeature
    {
        private readonly IHttpResponseBodyFeature _originalBodyFeature;
        private readonly HashableStream _outputStream = null;
        private bool _complete = false;
        private PipeWriter _pipeAdapter = null;

        public EtagBody(
            IHttpResponseBodyFeature originalBodyFeature)
        {
            _outputStream = new HashableStream(originalBodyFeature.Stream);
            _originalBodyFeature = originalBodyFeature;
        }

        public async Task CompleteAsync()
        {
            if (!_complete)
            {
                await FinishHashingAsync();
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

        internal async Task FinishHashingAsync()
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

        public byte[] GetHash()
        {
            return _outputStream.GetHash();
        }
    }
}