using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace OTM.DevLOG.Data;

/* This is used if database provider does't define
 * IDevLOGDbSchemaMigrator implementation.
 */
public class NullDevLOGDbSchemaMigrator : IDevLOGDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
