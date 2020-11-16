using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using GitProxy.Aggregation;
using GitProxy.Cache;
using GitProxy.Proxy;
using GitProxy.RequestHandler;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GitProxy
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);//.AddViewOptions(options =>
            //options.HtmlHelperOptions.ClientValidationEnabled = false);

            var gitToken = Configuration.GetValue<string>("GITHUB_API_TOKEN");

            // TODO: read from config

            // DI for components
            var netflixCachedApis = new HashSet<string> { "", "orgs/netflix", "orgs/netflix/members", "orgs/netflix/repos" };
            var gitApiViewManager = new GitApiViewManager("Netflix");
            var endpointCacheManager = new EndpointCacheManager(netflixCachedApis, gitApiViewManager);
            var gitRequestHandler = new GitRequestHandler(gitToken);
            var endpointProxy = new EndpointProxy(gitRequestHandler, endpointCacheManager, TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(1));
            services.AddScoped(typeof(IEndpointCacheManager), serviceProvider => endpointCacheManager);
            services.AddScoped(typeof(IRequestHandler), serviceProvider => gitRequestHandler);
            services.AddScoped(typeof(IEndpointProxy), serviceProvider => endpointProxy);
            services.AddScoped(typeof(IApiViewManager), serviceProvider => gitApiViewManager);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }


            //app.UseHttpsRedirection();
            app.UseMvc(routes =>
            {
                routes.MapRoute("Probe",
                               "healthcheck",
                               new { controller = "Probe", action = "Probe" });
                routes.MapRoute("View",
                               "view/top/{count}/{dimension}",
                               new { controller = "View", action = "ViewTop" });
                routes.MapRoute("Default",
                                "{*catchall}",
                                new { controller = "GitProxy", action = "CatchAll" });
               
            });
        }
    }
}
