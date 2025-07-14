using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Base64Middleware
{
    public class Base64Middleware
    {
        private readonly RequestDelegate _next;

        public Base64Middleware(
            RequestDelegate next)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            _next = next;
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
            var compressionBody = new Base64Body(originalBodyFeature);
            context.Features.Set<IHttpResponseBodyFeature>(compressionBody);

            try
            {
                await _next(context);
                await compressionBody.FinishEncryptionAsync();
            }
            finally
            {
                context.Features.Set(originalBodyFeature);
            }
        }
    }
}