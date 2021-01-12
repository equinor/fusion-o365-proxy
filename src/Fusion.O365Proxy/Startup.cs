using System.Net;
using System.Net.Http;
using Fusion.O365Proxy.Authentication;
using Fusion.O365Proxy.Authorization;
using Fusion.O365Proxy.Resolvers;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.ReverseProxy.Service.Proxy;

namespace Fusion.O365Proxy
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMediatR(typeof(Startup));

            services.AddHttpProxy();
            services.AddAuthorization();
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(o =>
                {
                    o.Authority = Configuration["AzureAd:Authority"];

                    o.TokenValidationParameters.ValidAudiences = new[]
                    {
                        "https://o365-proxy.prod.fusion-dev.net",
                        "https://o365-proxy.test.fusion-dev.net",
                        $"api://{Configuration["AzureAd:ClientId"]}",
                        Configuration["AzureAd:ClientId"]
                    };
                });

            services.AddScoped<IAuthorizationHandler, MailboxAccessHandler>();
            services.AddSingleton<IApplicationMailboxResolver, ApplicationMailboxResolver>();
            services.AddSingleton<GraphCredentialsProvider>();
            services.AddMemoryCache();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHttpProxy httpProxy, GraphCredentialsProvider graphCredentials)
        {
            var httpClient = new HttpMessageInvoker(new SocketsHttpHandler()
            {
                UseProxy = false,
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.None,
                UseCookies = false
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.Map("/{version}/users/{mailbox}/{**catch-all}", async httpContext => await new Proxy.UserProxy(httpContext, httpClient).HandleAsync());
                endpoints.Map("/{version}/subscriptions", async httpContext => await new Proxy.SubscriptionProxy(httpContext, httpClient).HandleAsync());
            });
        }
    }

}
