using Volo.Abp.Modularity;

namespace OTM.DevLOG;

[DependsOn(
    typeof(DevLOGApplicationModule),
    typeof(DevLOGDomainTestModule)
    )]
public class DevLOGApplicationTestModule : AbpModule
{

}
