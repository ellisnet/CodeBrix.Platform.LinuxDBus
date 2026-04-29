using System.Runtime.InteropServices;

namespace CodeBrix.Platform.LinuxDBus.Tests
{
    /// <summary>
    /// Helper used by xUnit v3 conditional <c>SkipUnless</c> attributes to gate
    /// tests that can only succeed on a Linux host (e.g. tests that require the
    /// <c>dbus-daemon</c> executable or the <c>libc</c> native library).
    /// </summary>
    internal static class PlatformGate
    {
        public static bool IsLinux { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    }
}
