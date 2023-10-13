using System.Threading.Tasks;
using OTM.DevLOG.BackgroundWorkers;
using Volo.Abp;
using Volo.Abp.Account;
using Volo.Abp.AutoMapper;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;

namespace OTM.DevLOG;

[DependsOn(
    typeof(DevLOGDomainModule),
    typeof(AbpAccountApplicationModule),
    typeof(DevLOGApplicationContractsModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpPermissionManagementApplicationModule),
    typeof(AbpTenantManagementApplicationModule),
    typeof(AbpFeatureManagementApplicationModule),
    typeof(AbpSettingManagementApplicationModule),
    typeof(AbpBackgroundWorkersModule)
    )]
public class DevLOGApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<DevLOGApplicationModule>();
        });
    }

    public async override Task OnApplicationInitializationAsync(ApplicationInitializationContext context)
    {
        await context.AddBackgroundWorkerAsync<OTM.DevLOG.BackgroundWorkers.DatexII2JsonTranslatorBackgroundWorker>();
        await context.AddBackgroundWorkerAsync<OTM.DevLOG.BackgroundWorkers.NdwOpenDataMeasurementSiteReferenceDownloadBackgroundWorker>();
        await context.AddBackgroundWorkerAsync<OTM.DevLOG.BackgroundWorkers.DatexIIJsonOtmPublisherBackgroundWorker>();
        
    }
}