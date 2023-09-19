using System.Threading.Tasks;

namespace OTM.DevLOG.Data;

public interface IDevLOGDbSchemaMigrator
{
    Task MigrateAsync();
}
