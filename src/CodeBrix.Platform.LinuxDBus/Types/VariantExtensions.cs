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
// This type is for writing so we don't need to add
// DynamicallyAccessedMemberTypes.PublicParameterlessConstructor.
#pragma warning disable IL2091

/// <summary>Variant Extensions.</summary>
public static class VariantExtensions
{
    /// <summary>As Variant.</summary>
    public static Variant AsVariant(this byte value)
        => new Variant(value);

    /// <summary>As Variant.</summary>
    public static Variant AsVariant(this bool value)
        => new Variant(value);

    /// <summary>As Variant.</summary>
    public static Variant AsVariant(this short value)
        => new Variant(value);

    /// <summary>As Variant.</summary>
    public static Variant AsVariant(this ushort value)
        => new Variant(value);

    /// <summary>As Variant.</summary>
    public static Variant AsVariant(this int value)
        => new Variant(value);

    /// <summary>As Variant.</summary>
    public static Variant AsVariant(this uint value)
        => new Variant(value);

    /// <summary>As Variant.</summary>
    public static Variant AsVariant(this long value)
        => new Variant(value);

    /// <summary>As Variant.</summary>
    public static Variant AsVariant(this ulong value)
        => new Variant(value);

    /// <summary>As Variant.</summary>
    public static Variant AsVariant(this double value)
        => new Variant(value);

    /// <summary>As Variant.</summary>
    public static Variant AsVariant(this string value)
        => new Variant(value);

    /// <summary>As Variant.</summary>
    public static Variant AsVariant(this SafeHandle value)
        => new Variant(value);
}