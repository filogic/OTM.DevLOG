using Volo.Abp.Settings;

namespace OTM.DevLOG.Settings;

public class DevLOGSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(DevLOGSettings.MySetting1));
    }
}
