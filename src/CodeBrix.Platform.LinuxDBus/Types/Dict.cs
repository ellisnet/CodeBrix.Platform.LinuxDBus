using System.Collections;
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

/// <summary>Dict.</summary>
public sealed class Dict<TKey, TValue> : IDBusWritable, IDictionary<TKey, TValue>, IVariantValueConvertable
    where TKey : notnull
    where TValue : notnull
{
    private readonly Dictionary<TKey, TValue> _dict;

    /// <summary>Dict.</summary>
    public Dict() :
        this(new Dictionary<TKey, TValue>())
    { }

    /// <summary>Dict.</summary>
    public Dict(IDictionary<TKey, TValue> dictionary) :
        this(new Dictionary<TKey, TValue>(dictionary))
    { }

    private Dict(Dictionary<TKey, TValue> value)
    {
        TypeModel.EnsureSupportedVariantType<TKey>();
        TypeModel.EnsureSupportedVariantType<TValue>();
        _dict = value;
    }

    /// <summary>As Variant.</summary>
    public Variant AsVariant() => Variant.FromDict(this);

    /// <summary>implicit operator Variant.</summary>
    public static implicit operator Variant(Dict<TKey, TValue> value)
        => value.AsVariant();

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026")] // this is a supported variant type.
    void IDBusWritable.WriteTo(ref MessageWriter writer)
        => writer.WriteDictionary<TKey, TValue>(_dict);

    ICollection<TKey> IDictionary<TKey, TValue>.Keys => _dict.Keys;

    ICollection<TValue> IDictionary<TKey, TValue>.Values => _dict.Values;

    /// <summary>Count.</summary>
    public int Count => _dict.Count;

    /// <summary>this[T Key].</summary>
    public TValue this[TKey key]
    {
        get => _dict[key];
        set => _dict[key] = value;
    }

    /// <summary>Add.</summary>
    public void Add(TKey key, TValue value)
        => _dict.Add(key, value);

    /// <summary>Contains Key.</summary>
    public bool ContainsKey(TKey key)
        => _dict.ContainsKey(key);

    /// <summary>Remove.</summary>
    public bool Remove(TKey key)
        => _dict.Remove(key);

    /// <summary>Try Get Value.</summary>
    public bool TryGetValue(TKey key,
                            [MaybeNullWhen(false)]
                            out TValue value)
        => _dict.TryGetValue(key, out value);

    /// <summary>Clear.</summary>
    public void Clear()
        => _dict.Clear();

    void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        => ((ICollection<KeyValuePair<TKey, TValue>>)_dict).Add(item);

    bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        => ((ICollection<KeyValuePair<TKey, TValue>>)_dict).Contains(item);

    void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        => ((ICollection<KeyValuePair<TKey, TValue>>)_dict).CopyTo(array, arrayIndex);

    bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        => ((ICollection<KeyValuePair<TKey, TValue>>)_dict).Remove(item);

    bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        => _dict.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => _dict.GetEnumerator();

    /// <summary>implicit operator Variant Value.</summary>
    public static implicit operator VariantValue(Dict<TKey, TValue> value)
        => value.AsVariantValue();

    /// <summary>As Variant Value.</summary>
    public VariantValue AsVariantValue()
    {
        Span<byte> buffer = stackalloc byte[ProtocolConstants.MaxSignatureLength];
        ReadOnlySpan<byte> sig = TypeModel.GetSignature<TKey>(buffer);
        DBusType keyType = (DBusType)sig[0];
        KeyValuePair<VariantValue, VariantValue>[] pairs = _dict.Select(pair => new KeyValuePair<VariantValue, VariantValue>(VariantValueConverter.ToVariantValue(pair.Key), VariantValueConverter.ToVariantValue(pair.Value))).ToArray();
        return VariantValue.Dictionary(keyType, TypeModel.GetSignature<TValue>(buffer), pairs);
    }
}
