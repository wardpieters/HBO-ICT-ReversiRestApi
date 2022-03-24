using Audit.Core;
using Audit.WebApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using ReversiRestApi.DAL;
using ReversiRestApi.Repository;
using ReversiRestApi.Hubs;

namespace ReversiRestApi
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
            services.AddMvc(mvc =>
            {
                mvc.AddAuditFilter(config => config
                    .LogAllActions()
                    .WithEventType("{verb}.{controller}.{action}")
                    .IncludeHeaders(ctx => !ctx.ModelState.IsValid)
                    .IncludeRequestBody()
                    .IncludeModelState()
                );
            });
            
            Audit.Core.Configuration.Setup()
                .UseSqlServer(config => config
                    .ConnectionString(Configuration.GetConnectionString("ReversiApiAudit"))
                    .Schema("dbo")
                    .TableName("Event")
                    .IdColumnName("EventId")
                    .JsonColumnName("JsonData")
                    .LastUpdatedColumnName("LastUpdatedDate")
                    .CustomColumn("EventType", ev => ev.EventType)
                    .CustomColumn("User", ev => ev.Environment.UserName));
            
            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "ReversiApi", Version = "v1" });
            });

            services.AddControllers().AddNewtonsoftJson(options => options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore);

            services.AddTransient<ISpelRepository, SpelAccessLayer>();
            services.AddDbContext<ReversiContext>(options => options.UseSqlServer(Configuration.GetConnectionString("ReversiApi")));

            services.AddSignalR();

            services.AddCors();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Reversi REST API v1"));
            }

            app.UseHttpsRedirection();
            
            app.UseCors(builder => builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader()
            );

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<DefaultHub>("/api/hub");
            });
        }
    }
}
