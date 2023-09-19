using Volo.Abp.Ui.Branding;
using Volo.Abp.DependencyInjection;

namespace OTM.DevLOG;

[Dependency(ReplaceServices = true)]
public class DevLOGBrandingProvider : DefaultBrandingProvider
{
    public override string AppName => "DevLOG";
}
