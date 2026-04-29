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
/// <summary>Reader.</summary>
public ref partial struct Reader
{
    /// <summary>Read Array Of Byte.</summary>
    public byte[] ReadArrayOfByte()
        => ReadArrayOfNumeric<byte>();

    /// <summary>Read Array Of Bool.</summary>
    public bool[] ReadArrayOfBool()
        => ReadArrayOfT<bool>();

    /// <summary>Read Array Of Int16.</summary>
    public short[] ReadArrayOfInt16()
        => ReadArrayOfNumeric<short>();

    /// <summary>Read Array Of U Int16.</summary>
    public ushort[] ReadArrayOfUInt16()
        => ReadArrayOfNumeric<ushort>();

    /// <summary>Read Array Of Int32.</summary>
    public int[] ReadArrayOfInt32()
        => ReadArrayOfNumeric<int>();

    /// <summary>Read Array Of U Int32.</summary>
    public uint[] ReadArrayOfUInt32()
        => ReadArrayOfNumeric<uint>();

    /// <summary>Read Array Of Int64.</summary>
    public long[] ReadArrayOfInt64()
        => ReadArrayOfNumeric<long>();

    /// <summary>Read Array Of U Int64.</summary>
    public ulong[] ReadArrayOfUInt64()
        => ReadArrayOfNumeric<ulong>();

    /// <summary>Read Array Of Double.</summary>
    public double[] ReadArrayOfDouble()
        => ReadArrayOfNumeric<double>();

    /// <summary>Read Array Of String.</summary>
    public string[] ReadArrayOfString()
        => ReadArrayOfT<string>();

    /// <summary>Read Array Of Object Path.</summary>
    public ObjectPath[] ReadArrayOfObjectPath()
        => ReadArrayOfT<ObjectPath>();

    /// <summary>Read Array Of Signature.</summary>
    public Signature[] ReadArrayOfSignature()
        => ReadArrayOfT<Signature>();

    /// <summary>Read Array Of Variant Value.</summary>
    public VariantValue[] ReadArrayOfVariantValue()
        => ReadArrayOfT<VariantValue>();

    /// <summary>Read Array Of Handle.</summary>
    public T[] ReadArrayOfHandle<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T>() where T : SafeHandle, new()
        => ReadArrayOfT<T>();

    private T[] ReadArrayOfT<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T>()
    {
        List<T> items = new();
        ArrayEnd arrayEnd = ReadArrayStart(TypeModel.GetTypeAlignment<T>());
        while (HasNext(arrayEnd))
        {
            items.Add(Read<T>());
        }
        return items.ToArray();
    }

    private unsafe T[] ReadArrayOfNumeric<T>() where T : unmanaged
    {
        int length = ReadInt32();
        if (sizeof(T) > 4)
        {
            AlignReader(sizeof(T));
        }
        T[] array = new T[length / sizeof(T)];
        bool dataRead = _reader.TryCopyTo(MemoryMarshal.AsBytes(array.AsSpan()));
        if (!dataRead)
        {
            ThrowHelper.ThrowIndexOutOfRange();
        }
        _reader.Advance(sizeof(T) * array.Length);
        if (sizeof(T) > 1 && ReverseEndianness)
        {
            if (sizeof(T) == 2)
            {
                var span = MemoryMarshal.Cast<T, short>(array.AsSpan());
                BinaryPrimitives.ReverseEndianness(span, span);
            }
            else if (sizeof(T) == 4)
            {
                var span = MemoryMarshal.Cast<T, int>(array.AsSpan());
                BinaryPrimitives.ReverseEndianness(span, span);
            }
            else if (sizeof(T) == 8)
            {
                Span<long> span = MemoryMarshal.Cast<T, long>(array.AsSpan());
                BinaryPrimitives.ReverseEndianness(span, span);
            }
        }
        return array;

    }
}
