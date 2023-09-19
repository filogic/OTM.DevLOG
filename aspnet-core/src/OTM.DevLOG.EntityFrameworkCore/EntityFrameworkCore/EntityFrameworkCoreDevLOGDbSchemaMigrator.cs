using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OTM.DevLOG.Data;
using Volo.Abp.DependencyInjection;

namespace OTM.DevLOG.EntityFrameworkCore;

public class EntityFrameworkCoreDevLOGDbSchemaMigrator
    : IDevLOGDbSchemaMigrator, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public EntityFrameworkCoreDevLOGDbSchemaMigrator(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        /* We intentionally resolving the DevLOGDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<DevLOGDbContext>()
            .Database
            .MigrateAsync();
    }
}
