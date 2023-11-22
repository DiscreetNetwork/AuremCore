using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremBlockAuthority.Core
{
    public sealed record KVKey
    {
        public int ShortId { get; init; }
        public ReadOnlyMemory<byte> Key { get; init; }

        private byte[] cache = null;

        public static implicit operator byte[](KVKey v) { return v.Key.ToArray(); }

        public static implicit operator ReadOnlySpan<byte>(KVKey v) => v.Key.Span;

        public static implicit operator ReadOnlyMemory<byte>(KVKey v) => v.Key;

        public static implicit operator KVKey(byte[] b) { return new KVKey(b); }

        public static implicit operator KVKey(ReadOnlySpan<byte> bytes) { return new KVKey(bytes.ToArray()); }

        public KVKey(byte[] data)
        {
            cache = data;
            Key = data.AsMemory();
            ShortId = BinaryPrimitives.ReadInt32LittleEndian(data);
        }

        public override int GetHashCode()
        {
            return ShortId;
        }

        public bool KVEquals(KVKey other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other == null) return false;

            if (ShortId != other.ShortId) return false;

            return Key.Span.SequenceCompareTo(other.Key.Span) == 0;
        }
    }

    public sealed class KVValue
    {
        private readonly ReadOnlyMemory<byte> value;

        public static implicit operator byte[](KVValue v) { return v.value.ToArray(); }

        public static implicit operator ReadOnlySpan<byte>(KVValue v) => v.value.Span;

        public static implicit operator ReadOnlyMemory<byte>(KVValue v) => v.value;

        public ReadOnlySpan<byte> Value => value.Span;

        public KVValue(ReadOnlyMemory<byte> value) { this.value = value; }

        public KVValue(byte[] data) { this.value = data; }

        public KVValue(ReadOnlySpan<byte> bytes) { this.value = bytes.ToArray(); }
    }

    /// <summary>
    /// Implements a super simple key-value store in-memory
    /// </summary>
    public class KVStore
    {
        private class KVKeyEqualityComparer : IEqualityComparer<KVKey>
        {
            public bool Equals(KVKey? x, KVKey? y)
            {
                if (x == null || y == null) return false;
                return x.KVEquals(y);
            }

            public int GetHashCode([DisallowNull] KVKey obj)
            {
                return obj.GetHashCode();
            }
        }

        private ConcurrentDictionary<KVKey, KVValue> _store;
        private bool _throwErrorOnNotFound;

        public KVStore(bool throwErrorOnNotFound = true)
        {
            _store = new(new KVKeyEqualityComparer());
            _throwErrorOnNotFound = throwErrorOnNotFound;
        }

        public KVValue this[KVKey key]
        {
            get
            {
                if (_throwErrorOnNotFound)
                {
                    return _store[key];
                }
                else
                {
                    var success = _store.TryGetValue(key, out var value);
                    return value;
                }
            }
        }

        public KVValue GetOrAdd(KVKey key, Func<KVKey, KVValue> factory)
        {
            return _store.GetOrAdd(key, factory);
        }

        public bool TryGetValue(KVKey key, out KVValue value) => _store.TryGetValue(key, out value);

        public bool TryRemove(KVKey key, out KVValue value) => _store.TryRemove(key, out value);

        public bool Add(KVKey key, KVValue value)
        {
            return _store.TryAdd(key, value);
        }

        public void Clear()
        {
            _store.Clear();
        }
    }
}
