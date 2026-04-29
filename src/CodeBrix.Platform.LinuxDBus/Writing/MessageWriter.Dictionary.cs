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
// Using obsolete generic write members
#pragma warning disable CS0618

public ref partial struct MessageWriter
{
    /// <summary>Write Dictionary Start.</summary>
    public ArrayStart WriteDictionaryStart()
        => WriteArrayStart(ProtocolConstants.StructAlignment);

    /// <summary>Write Dictionary End.</summary>
    public void WriteDictionaryEnd(ArrayStart start)
        => WriteArrayEnd(start);

    /// <summary>Write Dictionary Entry Start.</summary>
    public void WriteDictionaryEntryStart()
        => WriteStructureStart();

    // Write method for the common 'a{sv}' type.
    /// <summary>Write Dictionary.</summary>
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026")] // It's safe to call WriteDictionary with these types.
    public void WriteDictionary(IEnumerable<KeyValuePair<string, VariantValue>> value)
        => WriteDictionary<string, VariantValue>(value);

    // Write method for the common 'a{sv}' type.
    /// <summary>Write Dictionary.</summary>
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026")] // It's safe to call WriteDictionary with these types.
    public void WriteDictionary(KeyValuePair<string, VariantValue>[] value)
        => WriteDictionary<string, VariantValue>(value);

    // Write method for the common 'a{sv}' type.
    /// <summary>Write Dictionary.</summary>
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026")] // It's safe to call WriteDictionary with these types.
    public void WriteDictionary(Dictionary<string, VariantValue> value)
        => WriteDictionary<string, VariantValue>(value);

    // Write method for the common 'a{sv}' type.
    /// <summary>Write Dictionary.</summary>
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026")] // It's safe to call WriteDictionary with these types.
    public void WriteDictionary(IEnumerable<KeyValuePair<string, Variant>> value)
        => WriteDictionary<string, Variant>(value);

    // Write method for the common 'a{sv}' type.
    /// <summary>Write Dictionary.</summary>
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026")] // It's safe to call WriteDictionary with these types.
    public void WriteDictionary(KeyValuePair<string, Variant>[] value)
        => WriteDictionary<string, Variant>(value);

    // Write method for the common 'a{sv}' type.
    /// <summary>Write Dictionary.</summary>
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026")] // It's safe to call WriteDictionary with these types.
    public void WriteDictionary(Dictionary<string, Variant> value)
        => WriteDictionary<string, Variant>(value);

    private void WriteDictionary<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> value)
        where TKey : notnull
        where TValue : notnull
    {
        ArrayStart arrayStart = WriteDictionaryStart();
        foreach (var item in value)
        {
            WriteDictionaryEntryStart();
            Write<TKey>(item.Key);
            Write<TValue>(item.Value);
        }
        WriteDictionaryEnd(arrayStart);
    }

    private void WriteDictionary<TKey, TValue>(KeyValuePair<TKey, TValue>[] value)
        where TKey : notnull
        where TValue : notnull
    {
        ArrayStart arrayStart = WriteDictionaryStart();
        foreach (var item in value)
        {
            WriteDictionaryEntryStart();
            Write<TKey>(item.Key);
            Write<TValue>(item.Value);
        }
        WriteDictionaryEnd(arrayStart);
    }

    internal void WriteDictionary<TKey, TValue>(Dictionary<TKey, TValue> value)
        where TKey : notnull
        where TValue : notnull
    {
        ArrayStart arrayStart = WriteDictionaryStart();
        foreach (var item in value)
        {
            WriteDictionaryEntryStart();
            Write<TKey>(item.Key);
            Write<TValue>(item.Value);
        }
        WriteDictionaryEnd(arrayStart);
    }
}
