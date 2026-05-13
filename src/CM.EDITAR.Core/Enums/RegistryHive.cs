namespace CM.EDITAR.Core;

/// <summary>
/// Logical registry hive used by CM.EDITAR+. Runtime writes are constrained to <see cref="HKCU"/>.
/// HKCR is read-only and used as a discovery fallback only.
/// </summary>
public enum RegistryHive
{
    HKCU = 0,
    HKCR = 1,
}
