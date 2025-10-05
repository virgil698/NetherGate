namespace NetherGate.Core.Permissions;

/// <summary>
/// 内部权限组（运行时使用）
/// </summary>
internal class PermissionGroupInternal
{
    public string Name { get; set; } = string.Empty;
    public int Priority { get; set; }
    public HashSet<string> Permissions { get; set; } = new();
    public HashSet<string> InheritFrom { get; set; } = new();
    public bool IsDefault { get; set; }
}
