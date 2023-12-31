using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Medallion.Threading;
using Medallion.Threading.Redis;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OTM.DevLOG.EntityFrameworkCore;
using OTM.DevLOG.MultiTenancy;
using StackExchange.Redis;
using Microsoft.OpenApi.Models;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.MultiTenancy;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.Caching;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.Swashbuckle;
using Volo.Abp.VirtualFileSystem;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.AspNetCore.Http.Features;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.BackgroundWorkers;

namespace OTM.DevLOG;

[DependsOn(
    typeof(DevLOGHttpApiModule),
    typeof(AbpAutofacModule),
    typeof(AbpCachingStackExchangeRedisModule),
    //typeof(AbpDistributedLockingModule),
    typeof(AbpAspNetCoreMvcUiMultiTenancyModule),
    typeof(DevLOGApplicationModule),
    typeof(DevLOGEntityFrameworkCoreModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpSwashbuckleModule)
)]
[DependsOn(typeof(AbpBackgroundWorkersModule))]
    public class DevLOGHttpApiHostModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        var hostingEnvironment = context.Services.GetHostingEnvironment();
        if (hostingEnvironment.IsDevelopment())
            Console.WriteLine("Running in development environment");
        else
            Console.WriteLine("Running in non-development environment");


        Console.WriteLine("Sql configuration: " + configuration["ConnectionStrings:Default"]);
        Console.WriteLine("Redis configuration: " + configuration["Redis:Configuration"]);

        // Check whether the environment variable exists.
        var googleApplicationCredentials = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
        // If necessary, create it.
        if (googleApplicationCredentials == null)
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", "devlog-prod-gcp.json");

            // Now retrieve it.
            googleApplicationCredentials = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
        }
        // Display the value.
        Console.WriteLine($"GOOGLE_APPLICATION_CREDENTIALS: {googleApplicationCredentials}");




        ConfigureConventionalControllers();
        ConfigureAuthentication(context, configuration);
        ConfigureCache(configuration);
        ConfigureVirtualFileSystem(context);
        ConfigureDataProtection(context, configuration, hostingEnvironment);
        //ConfigureDistributedLocking(context, configuration);
        ConfigureCors(context, configuration);
        ConfigureSwaggerServices(context, configuration);
    }

    private void ConfigureCache(IConfiguration configuration)
    {
        Configure<AbpDistributedCacheOptions>(options => { options.KeyPrefix = "DevLOG:"; });
    }

    private void ConfigureVirtualFileSystem(ServiceConfigurationContext context)
    {
        var hostingEnvironment = context.Services.GetHostingEnvironment();

        if (hostingEnvironment.IsDevelopment())
        {
            Configure<AbpVirtualFileSystemOptions>(options =>
            {
                options.FileSets.ReplaceEmbeddedByPhysical<DevLOGDomainSharedModule>(
                    Path.Combine(hostingEnvironment.ContentRootPath,
                        $"..{Path.DirectorySeparatorChar}OTM.DevLOG.Domain.Shared"));
                options.FileSets.ReplaceEmbeddedByPhysical<DevLOGDomainModule>(
                    Path.Combine(hostingEnvironment.ContentRootPath,
                        $"..{Path.DirectorySeparatorChar}OTM.DevLOG.Domain"));
                options.FileSets.ReplaceEmbeddedByPhysical<DevLOGApplicationContractsModule>(
                    Path.Combine(hostingEnvironment.ContentRootPath,
                        $"..{Path.DirectorySeparatorChar}OTM.DevLOG.Application.Contracts"));
                options.FileSets.ReplaceEmbeddedByPhysical<DevLOGApplicationModule>(
                    Path.Combine(hostingEnvironment.ContentRootPath,
                        $"..{Path.DirectorySeparatorChar}OTM.DevLOG.Application"));
            });
        }
    }

    private void ConfigureConventionalControllers()
    {
        Configure<AbpAspNetCoreMvcOptions>(options =>
        {
            options.ConventionalControllers.Create(typeof(DevLOGApplicationModule).Assembly);
        });
    }

    private void ConfigureAuthentication(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = configuration["AuthServer:Authority"];
                options.RequireHttpsMetadata = Convert.ToBoolean(configuration["AuthServer:RequireHttpsMetadata"]);
                options.Audience = "DevLOG";
            });
    }

    private static void ConfigureSwaggerServices(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddSwaggerGen(
            options =>
            {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "DevLOG API", Version = "v1" });
            options.DocInclusionPredicate((docName, description) => true);
            options.CustomSchemaIds(type => type.FullName);
            });

        /*
        context.Services.AddAbpSwaggerGenWithOAuth(
            configuration["AuthServer:Authority"],
            new Dictionary<string, string>
            {
                    {"DevLOG", "DevLOG API"}
            },
            options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "DevLOG API", Version = "v1" });
                options.DocInclusionPredicate((docName, description) => true);
                options.CustomSchemaIds(type => type.FullName);
            });
        */
    }

    private void ConfigureDataProtection(
        ServiceConfigurationContext context,
        IConfiguration configuration,
        IWebHostEnvironment hostingEnvironment)
    {
        var dataProtectionBuilder = context.Services.AddDataProtection().SetApplicationName("DevLOG");
       // if (!hostingEnvironment.IsDevelopment())
       // {
         //   var redis = ConnectionMultiplexer.Connect(configuration["Redis:Configuration"]);
         //   dataProtectionBuilder.PersistKeysToStackExchangeRedis(redis, "DevLOG-Protection-Keys");
        //}
    }

    private void ConfigureDistributedLocking(
        ServiceConfigurationContext context,
        IConfiguration configuration )
    {
        context.Services.AddSingleton<IDistributedLockProvider>(sp =>
        {
            var connection = ConnectionMultiplexer
                .Connect(configuration["Redis:Configuration"]);
            return new RedisDistributedSynchronizationProvider(connection.GetDatabase());
        });
    }

    private void ConfigureCors(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder
                    .WithOrigins(configuration["App:CorsOrigins"]?
                        .Split(",", StringSplitOptions.RemoveEmptyEntries)
                        .Select(o => o.RemovePostFix("/"))
                        .ToArray() ?? Array.Empty<string>())
                    .WithAbpExposedHeaders()
                    .SetIsOriginAllowedToAllowWildcardSubdomains()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        var env = context.GetEnvironment();

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseAbpRequestLocalization();
        app.UseCorrelationId();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseCors();
        app.UseAuthentication();

        if (MultiTenancyConsts.IsEnabled)
        {
            app.UseMultiTenancy();
        }

        app.UseAuthorization();

        app.UseSwagger();
        app.UseAbpSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "DevLOG API");

            var configuration = context.GetConfiguration();
            options.OAuthClientId(configuration["AuthServer:SwaggerClientId"]);
            options.OAuthScopes("DevLOG");
        });

        app.UseAuditing();
        app.UseAbpSerilogEnrichers();
        app.UseUnitOfWork();
        app.UseConfiguredEndpoints();
    }
}
