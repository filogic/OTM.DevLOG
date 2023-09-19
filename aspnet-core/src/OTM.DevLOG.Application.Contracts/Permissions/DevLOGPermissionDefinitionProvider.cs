using OTM.DevLOG.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace OTM.DevLOG.Permissions;

public class DevLOGPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(DevLOGPermissions.GroupName);
        //Define your own permissions here. Example:
        //myGroup.AddPermission(DevLOGPermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<DevLOGResource>(name);
    }
}
