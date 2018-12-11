using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NinMemApi.Data;
using NinMemApi.Data.Models;
using NinMemApi.Data.Stores.Azure;
using NinMemApi.GraphDb;
using NinMemApi.Middleware;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.IO;
using System.Net;
using NinMemApi.Data.Interfaces;
using NinMemApi.Data.Stores.Local;
using NinMemApi.Data.Stores.Web;

namespace NinMemApi
{
    /// <summary>
    /// 
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="configuration"></param>
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        /// <summary>
        /// 
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc()
                    .AddJsonOptions(options =>
                    {
                        options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                        options.SerializerSettings.DefaultValueHandling = DefaultValueHandling.Ignore;
                    });
            services.AddCors();

            var graphInput = GetGraphInput();

            G g = new G();
            GraphBuilder.Build(g, graphInput);

            services.AddSingleton(g);

            var kdTree = KdTreeBuilder.Build(graphInput.Taxons);
            var stRtree = STRTteeBuilder.Build(graphInput.NatureAreas);

            var codeSearch = new CodeSearch(g, kdTree, stRtree);
            services.AddSingleton(codeSearch);
            services.AddSingleton(new StatTreeBuilder(g, codeSearch));

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info
                {
                    Version = "v1",
                    Title = "API for Økologisk grunnkart",
                    Description = "API for Økologisk grunnkart",
                    TermsOfService = "None",
                    Contact = new Contact { Name = "Bjørn Reppen", Email = "bjorn.reppen@artsdatabanken.no", Url = "https://twitter.com/breppen" }
                });

                var basePath = AppContext.BaseDirectory;
                var xmlPath = Path.Combine(basePath, "NinMemApi.xml");
                c.IncludeXmlComments(xmlPath);
            });
        }

        private GraphInput GetGraphInput()
        {
            var ninMemApiData = Configuration["NinMemApiData"];

            IStorage storage;

            if (ninMemApiData.StartsWith("http"))
                storage = new WebStorage(ninMemApiData);
            else
                storage = new LocalStorage(ninMemApiData);

            var graphInputGetter = new GraphInputGetter(storage);

            return graphInputGetter.Get().GetAwaiter().GetResult();
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            var rewriteOptions = new RewriteOptions()
                .AddRedirect(@"^$", "swagger", (int)HttpStatusCode.Redirect);

            //if (env.IsDevelopment())
            //{
            //    app.UseDeveloperExceptionPage();
            //}
            //else
            //{
            //    rewriteOptions = rewriteOptions.AddRedirectToHttps();
            //}

            app.UseRewriter(rewriteOptions);

            app.UseCors(
                options => options.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()
            );

            app.UseMiddleware(typeof(ErrorHandlingMiddleware));

            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "API for Økologisk grunnkart - v1");
            });

            app.UseMvc();
        }
    }
}