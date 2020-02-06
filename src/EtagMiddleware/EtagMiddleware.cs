using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace EtagMiddleware
{
    public class EtagMiddleware
    {
        private readonly RequestDelegate _next;
        private ILogger _logger;

        public EtagMiddleware(
            RequestDelegate next,
            ILogger<EtagMiddleware> logger)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Invoke the middleware.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task InvokeAsync(
            HttpContext context)
        {
            var originalBodyFeature = context.Features.Get<IHttpResponseBodyFeature>();
            var etagBody = new EtagBody(originalBodyFeature);
            context.Features.Set<IHttpResponseBodyFeature>(etagBody);

            try
            {
                await _next(context);
                await etagBody.FinishHashingAsync();

                var hash = ByteArrayToString(etagBody.GetHash());
                //context.Response.Headers["Etag"] = ByteArrayToString(hash);
                _logger.LogInformation($"Etag: {hash}");
            }
            finally
            {
                context.Features.Set(originalBodyFeature);
            }
        }

        public static string ByteArrayToString(
            byte[] ba)
        {
            var hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }
    }
}