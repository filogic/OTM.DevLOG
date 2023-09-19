using System;
using System.Collections.Generic;
using System.Text;
using OTM.DevLOG.Localization;
using Volo.Abp.Application.Services;

namespace OTM.DevLOG;

/* Inherit your application services from this class.
 */
public abstract class DevLOGAppService : ApplicationService
{
    protected DevLOGAppService()
    {
        LocalizationResource = typeof(DevLOGResource);
    }
}
