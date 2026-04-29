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
// Using obsolete generic read members
#pragma warning disable CS0618

public ref partial struct Reader
{
    /// <summary>Read Dictionary Start.</summary>
    public ArrayEnd ReadDictionaryStart()
        => ReadArrayStart(ProtocolConstants.StructAlignment);

    // Read method for the common 'a{sv}' type.
    /// <summary>Read Dictionary Of String To Variant Value.</summary>
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026")] // It's safe to call ReadDictionary with these types.
    public Dictionary<string, VariantValue> ReadDictionaryOfStringToVariantValue()
        => ReadDictionary<string, VariantValue>();

    private Dictionary<TKey, TValue> ReadDictionary
        <
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]TKey,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]TValue
        >
        ()
        where TKey : notnull
        where TValue : notnull
            => ReadDictionary<TKey, TValue>(new Dictionary<TKey, TValue>());

    internal Dictionary<TKey, TValue> ReadDictionary
        <
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]TKey,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]TValue
        >
        (Dictionary<TKey, TValue> dictionary)
        where TKey : notnull
        where TValue : notnull
    {
        ArrayEnd dictEnd = ReadDictionaryStart();
        while (HasNext(dictEnd))
        {
            var key = Read<TKey>();
            var value = Read<TValue>();
            // Use the indexer to avoid throwing if the key is present multiple times.
            dictionary[key] = value;
        }
        return dictionary;
    }
}
