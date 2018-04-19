using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NinMemApi.Data;
using NinMemApi.Data.Interfaces;
using NinMemApi.Data.Models;
using NinMemApi.Data.Stores.Azure;
using NinMemApi.GraphDb;
using NinMemApi.Middleware;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.IO;
using System.Net;

namespace NinMemApi
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
            services.AddMvc()
                    .AddJsonOptions(options =>
                    {
                        options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                        options.SerializerSettings.DefaultValueHandling = DefaultValueHandling.Ignore;
                    });
            services.AddCors();

            var artsdbStorageConnectionString = new ArtsdbStorageConnectionString { ConnectionString = Configuration.GetConnectionString("artsdbstorage") };

            services.AddSingleton(artsdbStorageConnectionString);
            services.AddSingleton<IStorage, AzureStorage>();

            var graphInputGetter = new GraphInputGetter(new AzureStorage(artsdbStorageConnectionString/*, Configuration["CacheFolder"]*/));
            var graphInput = graphInputGetter.Get().GetAwaiter().GetResult();

            G g = new G();
            GraphBuilder.Build(g, graphInput);

            services.AddSingleton(g);

            var kdTree = KdTreeBuilder.Build(graphInput.Taxons);
            var stRtree = STRTteeBuilder.Build(graphInput.NatureAreas);

            var codeSearch = new CodeSearch(g, kdTree, stRtree);
            services.AddSingleton(codeSearch);
            services.AddSingleton(new StatTreeBuilder(g, codeSearch));

            services.AddSingleton<ICosmosGraphClient>(new CosmosGraphClient(Configuration["CosmosHost"], Configuration["CosmosAuthKey"], Configuration["CosmosDatabase"], Configuration["CosmosCollection"]));

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

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            var rewriteOptions = new RewriteOptions()
                .AddRedirect(@"^$", "swagger", (int)HttpStatusCode.Redirect);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                rewriteOptions = rewriteOptions.AddRedirectToHttps();
            }

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