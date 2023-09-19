using OTM.DevLOG.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace OTM.DevLOG.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class DevLOGController : AbpControllerBase
{
    protected DevLOGController()
    {
        LocalizationResource = typeof(DevLOGResource);
    }
}
