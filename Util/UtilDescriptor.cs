using HCore.Modules.Base;

namespace HCore.Packages.HShellUtils.Util;

public class UtilDescriptor : IModuleDescriptor
{
    public string Name => "HCore.Packages.HShellUtils.Util";
    public string FriendlyName => "HCore Shell Utilities";
    public Type ImplementType => typeof(UtilImplement);
    public Type InterfaceType => typeof(IOneshotCommand);
}
