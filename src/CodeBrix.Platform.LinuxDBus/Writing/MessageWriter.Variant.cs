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

#pragma warning disable CS0618 // Variant is internally used despite being [Obsolete] in Release
namespace CodeBrix.Platform.LinuxDBus; //was previously: Tmds.DBus.Protocol;
public ref partial struct MessageWriter
{
    /// <summary>Write Variant.</summary>
    public void WriteVariant(Variant value)
    {
        value.WriteTo(ref this);
    }

    /// <summary>Write Variant.</summary>
    public void WriteVariant(VariantValue value)
    {
        value.WriteVariantTo(ref this);
    }
}
