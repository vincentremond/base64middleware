using System;
using System.Threading.Tasks;
using Base64Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLipsum.Core;

namespace Base64Middleware.TestApp
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(
            IServiceCollection services)
        {
            services.AddLogging(builder => builder.AddConsole());
            services.AddResponseCaching();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(
            IApplicationBuilder app,
            IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMiddleware<Base64Middleware>();
            
            //app.UseMiddleware<EtagMiddleware.EtagMiddleware>();
            app.UseResponseCaching();
            app.UseRouting();
            app.UseEndpoints(
                endpoints =>
                {
                    endpoints.MapGet(
                        "/",
                        async context => { await context.Response.WriteAsync("Hello world !"); });
                    endpoints.MapGet(
                        "/now",
                        async context => { await context.Response.WriteAsync(DateTime.Now.ToString("u")); });
                    endpoints.MapGet(
                        "/lorem",
                        async context => { await LoremIpsum(context); });
                });
        }

        private static async Task LoremIpsum(
            HttpContext context)
        {
            for (int i = 0; i < 200000; i++)
            {
                var paragraph = LipsumGenerator.Generate(200000);
                await context.Response.WriteAsync(paragraph);   
            }
        }
    }
}