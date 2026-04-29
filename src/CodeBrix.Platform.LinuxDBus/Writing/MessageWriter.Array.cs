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
/// <summary>Message Writer.</summary>
public ref partial struct MessageWriter
{
    /// <summary>Write Array.</summary>
    public void WriteArray(byte[] value)
        => WriteArray(value.AsSpan());

    /// <summary>Write Array.</summary>
    public void WriteArray(ReadOnlySpan<byte> value)
        => WriteArrayOfNumeric(value);

    /// <summary>Write Array.</summary>
    public void WriteArray(IEnumerable<byte> value)
        => WriteArrayOfT(value);

    /// <summary>Write Array.</summary>
    public void WriteArray(short[] value)
        => WriteArray(value.AsSpan());

    /// <summary>Write Array.</summary>
    public void WriteArray(ReadOnlySpan<short> value)
        => WriteArrayOfNumeric(value);

    /// <summary>Write Array.</summary>
    public void WriteArray(IEnumerable<short> value)
        => WriteArrayOfT(value);

    /// <summary>Write Array.</summary>
    public void WriteArray(ushort[] value)
        => WriteArray(value.AsSpan());

    /// <summary>Write Array.</summary>
    public void WriteArray(ReadOnlySpan<ushort> value)
        => WriteArrayOfNumeric(value);

    /// <summary>Write Array.</summary>
    public void WriteArray(IEnumerable<ushort> value)
        => WriteArrayOfT(value);

    /// <summary>Write Array.</summary>
    public void WriteArray(int[] value)
        => WriteArray(value.AsSpan());

    /// <summary>Write Array.</summary>
    public void WriteArray(ReadOnlySpan<int> value)
        => WriteArrayOfNumeric(value);

    /// <summary>Write Array.</summary>
    public void WriteArray(IEnumerable<int> value)
        => WriteArrayOfT(value);

    /// <summary>Write Array.</summary>
    public void WriteArray(uint[] value)
        => WriteArray(value.AsSpan());

    /// <summary>Write Array.</summary>
    public void WriteArray(ReadOnlySpan<uint> value)
        => WriteArrayOfNumeric(value);

    /// <summary>Write Array.</summary>
    public void WriteArray(IEnumerable<uint> value)
        => WriteArrayOfT(value);

    /// <summary>Write Array.</summary>
    public void WriteArray(long[] value)
        => WriteArray(value.AsSpan());

    /// <summary>Write Array.</summary>
    public void WriteArray(ReadOnlySpan<long> value)
        => WriteArrayOfNumeric(value);

    /// <summary>Write Array.</summary>
    public void WriteArray(IEnumerable<long> value)
        => WriteArrayOfT(value);

    /// <summary>Write Array.</summary>
    public void WriteArray(ulong[] value)
        => WriteArray(value.AsSpan());

    /// <summary>Write Array.</summary>
    public void WriteArray(ReadOnlySpan<ulong> value)
        => WriteArrayOfNumeric(value);

    /// <summary>Write Array.</summary>
    public void WriteArray(IEnumerable<ulong> value)
        => WriteArrayOfT(value);

    /// <summary>Write Array.</summary>
    public void WriteArray(double[] value)
        => WriteArray(value.AsSpan());

    /// <summary>Write Array.</summary>
    public void WriteArray(ReadOnlySpan<double> value)
        => WriteArrayOfNumeric(value);

    /// <summary>Write Array.</summary>
    public void WriteArray(IEnumerable<double> value)
        => WriteArrayOfT(value);

    /// <summary>Write Array.</summary>
    public void WriteArray(string[] value)
        => WriteArray(value.AsSpan());

    /// <summary>Write Array.</summary>
    public void WriteArray(ReadOnlySpan<string> value)
        => WriteArrayOfT(value);

    /// <summary>Write Array.</summary>
    public void WriteArray(IEnumerable<string> value)
        => WriteArrayOfT(value);

    /// <summary>Write Array.</summary>
    public void WriteArray(Signature[] value)
        => WriteArray(value.AsSpan());

    /// <summary>Write Array.</summary>
    public void WriteArray(ReadOnlySpan<Signature> value)
        => WriteArrayOfT(value);

    /// <summary>Write Array.</summary>
    public void WriteArray(IEnumerable<Signature> value)
        => WriteArrayOfT(value);

    /// <summary>Write Array.</summary>
    public void WriteArray(ObjectPath[] value)
        => WriteArray(value.AsSpan());

    /// <summary>Write Array.</summary>
    public void WriteArray(ReadOnlySpan<ObjectPath> value)
        => WriteArrayOfT(value);

    /// <summary>Write Array.</summary>
    public void WriteArray(IEnumerable<ObjectPath> value)
        => WriteArrayOfT(value);

    /// <summary>Write Array.</summary>
    public void WriteArray(VariantValue[] value)
        => WriteArray(value.AsSpan());

    /// <summary>Write Array.</summary>
    public void WriteArray(ReadOnlySpan<VariantValue> value)
        => WriteArrayOfT(value);

    /// <summary>Write Array.</summary>
    public void WriteArray(IEnumerable<VariantValue> value)
        => WriteArrayOfT(value);

    /// <summary>Write Array.</summary>
    public void WriteArray(Variant[] value)
        => WriteArray(value.AsSpan());

    /// <summary>Write Array.</summary>
    public void WriteArray(ReadOnlySpan<Variant> value)
        => WriteArrayOfT(value);

    /// <summary>Write Array.</summary>
    public void WriteArray(IEnumerable<Variant> value)
        => WriteArrayOfT(value);

    /// <summary>Write Array.</summary>
    public void WriteArray(SafeHandle[] value)
        => WriteArray(value.AsSpan());

    /// <summary>Write Array.</summary>
    public void WriteArray(ReadOnlySpan<SafeHandle> value)
        => WriteArrayOfT(value);

    /// <summary>Write Array.</summary>
    public void WriteArray(IEnumerable<SafeHandle> value)
        => WriteArrayOfT(value);


    internal void WriteArray<T>(ReadOnlySpan<T> value)
        where T : notnull
    {
        if (typeof(T) == typeof(byte))
        {
            ReadOnlySpan<byte> span = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(value)), value.Length);
            WriteArray(span);
        }
        else if (typeof(T) == typeof(short))
        {
            ReadOnlySpan<short> span = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, short>(ref MemoryMarshal.GetReference(value)), value.Length);
            WriteArray(span);
        }
        else if (typeof(T) == typeof(ushort))
        {
            ReadOnlySpan<ushort> span = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, ushort>(ref MemoryMarshal.GetReference(value)), value.Length);
            WriteArray(span);
        }
        else if (typeof(T) == typeof(int))
        {
            ReadOnlySpan<int> span = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, int>(ref MemoryMarshal.GetReference(value)), value.Length);
            WriteArray(span);
        }
        else if (typeof(T) == typeof(uint))
        {
            ReadOnlySpan<uint> span = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, uint>(ref MemoryMarshal.GetReference(value)), value.Length);
            WriteArray(span);
        }
        else if (typeof(T) == typeof(long))
        {
            ReadOnlySpan<long> span = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, long>(ref MemoryMarshal.GetReference(value)), value.Length);
            WriteArray(span);
        }
        else if (typeof(T) == typeof(ulong))
        {
            ReadOnlySpan<ulong> span = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, ulong>(ref MemoryMarshal.GetReference(value)), value.Length);
            WriteArray(span);
        }
        else if (typeof(T) == typeof(double))
        {
            ReadOnlySpan<double> span = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, double>(ref MemoryMarshal.GetReference(value)), value.Length);
            WriteArray(span);
        }
        else
        {
            WriteArrayOfT<T>(value);
        }
    }

    private unsafe void WriteArrayOfNumeric<T>(ReadOnlySpan<T> value) where T : unmanaged
    {
        WriteInt32(value.Length * sizeof(T));
        if (sizeof(T) > 4)
        {
            WritePadding(sizeof(T));
        }
        WriteRaw(MemoryMarshal.AsBytes(value));
    }

    private void WriteArrayOfT<T>(ReadOnlySpan<T> value)
        where T : notnull
    {
        ArrayStart arrayStart = WriteArrayStart(TypeModel.GetTypeAlignment<T>());
        foreach (var item in value)
        {
            Write<T>(item);
        }
        WriteArrayEnd(arrayStart);
    }

    private void WriteArrayOfT<T>(IEnumerable<T> value)
        where T : notnull
    {
        if (value is T[] array)
        {
            WriteArrayOfT<T>(array.AsSpan());
            return;
        }
        ArrayStart arrayStart = WriteArrayStart(TypeModel.GetTypeAlignment<T>());
        foreach (var item in value)
        {
            Write<T>(item);
        }
        WriteArrayEnd(arrayStart);
    }

    private static void WriteArraySignature<T>(ref MessageWriter writer) where T : notnull
    {
        Span<byte> buffer = stackalloc byte[ProtocolConstants.MaxSignatureLength];
        writer.WriteSignature(TypeModel.GetSignature<Array<T>>(buffer));
    }
}
