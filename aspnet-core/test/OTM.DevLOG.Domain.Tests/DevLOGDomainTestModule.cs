using OTM.DevLOG.EntityFrameworkCore;
using Volo.Abp.Modularity;

namespace OTM.DevLOG;

[DependsOn(
    typeof(DevLOGEntityFrameworkCoreTestModule)
    )]
public class DevLOGDomainTestModule : AbpModule
{

}
