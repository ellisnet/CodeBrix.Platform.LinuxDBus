using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nerdbank.Streams;

namespace CodeBrix.Platform.LinuxDBus; //was previously: Tmds.DBus.Protocol;
static class DBusEnvironment
{
    public static string UserId
    {
        get
        {
            if (PlatformDetection.IsWindows())
            {
                return System.Security.Principal.WindowsIdentity.GetCurrent().User?.Value;
            }
            else
            {
                return geteuid().ToString();
            }
        }
    }

    private static string _machineId;

    public static string MachineId
    {
        get
        {
            if (_machineId == null)
            {
                const string MachineUuidPath = @"/var/lib/dbus/machine-id";

                if (File.Exists(MachineUuidPath))
                {
                    _machineId = Guid.Parse(File.ReadAllText(MachineUuidPath).Substring(0, 32)).ToString("N");
                }
                else
                {
                    _machineId = Guid.Empty.ToString("N");
                }
            }

            return _machineId;
        }
    }

    [DllImport("libc")]
    internal static extern uint geteuid();
}