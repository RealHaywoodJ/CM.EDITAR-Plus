using System.Runtime.InteropServices;

namespace CM.EDITAR.Registry;

/// <summary>P/Invoke surface for Windows shell APIs CM.EDITAR+ relies on.</summary>
internal static class NativeMethods
{
    public const uint SHCNE_ASSOCCHANGED = 0x08000000;
    public const uint SHCNF_IDLIST = 0x0000;
    public const uint SHCNF_FLUSH = 0x1000;

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    public static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
}
