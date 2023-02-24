using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AutoMapper;
using Microservice.API.Extensions;
using Microservice.Business.Automapper;
using Microservice.Business.BackgroundWorker;
using Microservice.Business.Business;
using Microservice.Business.Repositories;
using Microservice.Database;
using Microservice.Database.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Serilog;

namespace Microservice.API
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            InitialiseLogger();

            Log.Information("Microservice starting up");
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
            services.AddControllersWithViews();
            services.AddRazorPages();

            services.AddCors(options =>
            {
                options.AddPolicy("cors-policy",
                    builder =>
                    {
                        var allowedCorsUrls = Configuration
                            .GetSection("Security:AllowedCorsUrls")
                            .Get<string[]>();

                        if (allowedCorsUrls is not null)
                            builder.WithOrigins(allowedCorsUrls)
                            .AllowCredentials();
                        else
                            builder.AllowAnyOrigin();

                        builder.AllowAnyHeader()
                        .AllowAnyMethod();
                    });
            });

            // Database Setup
            var ConnectionString = Configuration.GetConnectionString("DefaultConnection");
            var dbContext = new DatabaseContext.DataContextFactory(Configuration).CreateDbContext(null);
            dbContext.Seed();

            services.AddDbContext<DatabaseContext>(options =>
            {
                options.UseSqlServer(ConnectionString,
                sqlServerOptionsAction: sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(10, TimeSpan.FromSeconds(60), null);
                    sqlOptions.CommandTimeout(300);
                });
            },
            ServiceLifetime.Transient,
            ServiceLifetime.Transient
            );

            // Add Unit of Work
            services.AddScoped<IExportManagement, Business.Business.Concrete.ExportManagement>();
            services.AddScoped(typeof(IDatabaseRepository<>), typeof(Business.Repositories.Concrete.DatabaseRepository<>));

            // Add Timed Tasks
            if (Configuration.GetValue<bool>("EnableExportService"))
                services.AddHostedService<ProcessRequestWorker>();

            // Add Swagger
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Microservice API", Version = "v1" });
                c.DocInclusionPredicate((_, api) => !string.IsNullOrWhiteSpace(api.GroupName));
                c.TagActionsBy(api => new[] { api.GroupName });
            });

            // EntityFramework AutoMapper
            services.AddAutoMapper(cfg =>
            {
                cfg.AddProfile(new AutoMapperProfileConfiguration());
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Microservice");
            });

            if (env.EnvironmentName.Equals("development", StringComparison.OrdinalIgnoreCase))
                app.UseDeveloperExceptionPage();
            else
                app.UseHsts();

            app.ConfigureExceptionHandler();

            if (Configuration.GetValue<bool>("Security:ForceHttpsRedirect"))
                app.UseHttpsRedirection();

            app.UseRouting();

            app.UseCors("cors-policy");

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
                endpoints.MapRazorPages();
                endpoints.MapSwagger();
            });
        }

        private void InitialiseLogger()
        {
            var executableLocation = Assembly.GetEntryAssembly().Location;
            var executablePath = Path.GetDirectoryName(executableLocation);

            var logTemplate =
                "[{MachineName}:{Environment}] {Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}";
            var logFileName = "microservice_" + Configuration.GetValue<string>("Hosting:Environment") + "_log.txt";

            var filepath = Configuration.GetValue<string>("Logging:Path");

            var logger = new LoggerConfiguration()
                .Enrich.WithMachineName()
                .Enrich.WithProperty("Environment", Configuration.GetValue<string>("Hosting:Environment"))
                .WriteTo.File(
     
                    string.IsNullOrEmpty(filepath) || filepath.Contains("<") || filepath.Contains(">") ? Path.Combine(executablePath, "logs", logFileName) : Path.Combine(filepath, logFileName),
                    rollOnFileSizeLimit: true,
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: logTemplate
                );

            if (Configuration.GetValue<bool>("Logging:EnableDebugLogging"))
            {
                logger.MinimumLevel.Verbose();
                Serilog.Debugging.SelfLog.Enable(Console.Error);
            }
            else
                logger.MinimumLevel.Information();

            Log.Logger = logger.CreateLogger();

        }
    }
}
