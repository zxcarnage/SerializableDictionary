using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

[Serializable]
public class SerializebleDictionary<TKey, TValue> : IDictionary<TKey, TValue>
{
    [SerializeField, HideInInspector] private int[] _buckets;
    [SerializeField, HideInInspector] private int[] _hashCodes;
    [SerializeField, HideInInspector] private int[] _next;
    [SerializeField, HideInInspector] private int _count;
    [SerializeField, HideInInspector] private int _version;
    [SerializeField, HideInInspector] private int _freeList;
    [SerializeField, HideInInspector] private int _freeCount;
    [SerializeField, HideInInspector] private TKey[] _keys;
    [SerializeField, HideInInspector] private TValue[] _values;

    readonly IEqualityComparer<TKey> _comparer;

    public int Version => _version;
    public int Length => _count;
    public int[] HashCodes => _hashCodes;
    public TKey[] ArraiedKeys => _keys;
    public TValue[] ArraiedValue => _values;

    
    public Dictionary<TKey, TValue> AsDictionary
    {
        get { return new Dictionary<TKey, TValue>(this); }
    }

    public SerializebleDictionary() : this(0) { }

    public SerializebleDictionary(int capacity)
    {
        if (capacity < 0)
            throw new ArgumentOutOfRangeException("capacity");

        Initialize(capacity);

        _comparer = EqualityComparer<TKey>.Default;
    }

    public SerializebleDictionary(IDictionary<TKey, TValue> dictionary) 
    {
        if (dictionary == null)
            throw new ArgumentNullException("dictionary");

        foreach (KeyValuePair<TKey, TValue> current in dictionary)
            Add(current.Key, current.Value);
        _comparer = EqualityComparer<TKey>.Default;
    }
    public TValue this[TKey key]
    {
        get
        {
            int index = FindIndex(key);
            if (index >= 0)
                return _values[index];
            throw new KeyNotFoundException(key.ToString());
        }
        set
        {
            Insert(key, value, false);
        }
    }

    public TValue this[TKey key, TValue defaultValue]
    {
        get
        {
            int index = FindIndex(key);
            if (index >= 0)
                return _values[index];
            return defaultValue;
        }
    }

    public ICollection<TKey> Keys
    {
        get
        {
            return _keys.Take(Count).ToArray();
        }
    }

    public ICollection<TValue> Values
    {
        get
        {
            return _values.Take(Count).ToArray();
        }
    }

    public int Count => _count - _freeCount;

    public bool IsReadOnly => false;

    public void Add(TKey key, TValue value)
    {
        Insert(key, value, true);
    }

    public void Add(KeyValuePair<TKey, TValue> item)
    {
        Add(item.Key, item.Value);
    }

    public void Clear()
    {
        if (_count <= 0)
            return;
        for (int i = 0; i < _buckets.Length; i++)
        {
            _buckets[i] = -1;
        }
        Array.Clear(_keys, 0, _count);
        Array.Clear(_values, 0, _count);
        Array.Clear(_hashCodes, 0, _count);
        Array.Clear(_next, 0, _count);

        _freeList = -1;
        _count = 0;
        _freeCount = 0;
        _version++;
    }

    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        int index = FindIndex(item.Key);
        return index >= 0 &&
            EqualityComparer<TValue>.Default.Equals(_values[index], item.Value);
    }

    public bool ContainsKey(TKey key)
    {
        return FindIndex(key) >= 0;
    }

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        if (array == null)
            throw new ArgumentNullException("array");
        if (arrayIndex < 0 || arrayIndex > array.Length)
            throw new ArgumentOutOfRangeException(string.Format("index = {0} array.Length = {1}", arrayIndex, array.Length));
        if(array.Length - arrayIndex < Count)
            throw new ArgumentException(string.Format("The number of elements in the dictionary ({0}) is greater than the available space from index to the end of the destination array {1}.", Count, array.Length));
        for (int i = 0; i < _count; i++)
        {
            if (_hashCodes[i] >= 0)
                array[arrayIndex++] = new KeyValuePair<TKey, TValue>(_keys[i], _values[i]);
        }
    }

    public Enumerator<TKey,TValue> GetEnumerator()
    {
        return new Enumerator<TKey,TValue>(this);
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
    {
        return GetEnumerator();
    }

    public bool Remove(TKey key)
    {
        if (key == null)
            throw new ArgumentNullException("key");

        int hash = _comparer.GetHashCode(key) & int.MaxValue;

        int index = hash % _buckets.Length;
        int num = -1;
        for (int i = _buckets[index]; i >= 0; i = _next[i])
        {
            if (_hashCodes[i] == hash && _comparer.Equals(_keys[i], key))
            {
                if (num < 0)
                    _buckets[index] = _next[i];
                else
                    _next[num] = _next[i];

                _hashCodes[i] = -1;
                _next[i] = _freeList;
                _keys[i] = default(TKey);
                _values[i] = default(TValue);
                _freeList = i;
                _freeCount++;
                _version++;
                return true;
            }
            num = i;
        }
        return false;
    }

    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        return Remove(item.Key);
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        int index = FindIndex(key);
        if (index >= 0) 
        {
            value = _values[index];
            return true;
        }
        value = default(TValue);
        return false;
    }

    private int FindIndex(TKey key)
    {
        if (key == null)
            throw new ArgumentNullException("key");
        if (_buckets == null)
            return - 1;
        int hash = _comparer.GetHashCode(key) & int.MaxValue;

        for (int i = _buckets[hash % _buckets.Length]; i >= 0; i = _next[i]) 
        {
            if (_hashCodes[i] == hash && _comparer.Equals(_keys[i], key))
                return i;
        }
        return -1;
    }

    private void Insert(TKey key, TValue value, bool add)
    {

        if (key == null)
            throw new ArgumentNullException("key");
        if (_buckets == null || _buckets.Length == 0)
            Initialize(0);

        int hash = _comparer.GetHashCode(key) & int.MaxValue;
        if (_buckets.Length == 0)
            throw new ArgumentException("DEBUG 0");
        int index = hash % _buckets.Length;
        int num1 = 0;
        for (int i = _buckets[index]; i >= 0; i = _next[i])
        {
            if (_hashCodes[i] == hash && _comparer.Equals(_keys[i], key))
            {
                if (add)
                    throw new ArgumentException("Key already exists: " + key);

                _values[i] = value;
                _version++;
                return;
            }
            num1++;
        }
        int num2;
        if (_freeCount > 0)
        {
            num2 = _freeList;
            _freeList = _next[num2];
            _freeCount--;
        }
        else
        {
            if (_count == _keys.Length)
            {
                Resize();
                index = hash % _buckets.Length;
            }
            num2 = _count;
            _count++;
        }
        _hashCodes[num2] = hash;
        _next[num2] = _buckets[index];
        _keys[num2] = key;
        _values[num2] = value;
        _buckets[index] = num2;
        _version++;
    }

    private void Initialize(int capacity)
    {
        int prime = PrimeHelper.GetPrime(capacity);

        _buckets = new int[prime];
        for (int i = 0; i < _buckets.Length; i++)
        {
            _buckets[i] = -1;
        }

        _keys = new TKey[prime];
        _values = new TValue[prime];
        _hashCodes = new int[prime];
        _next = new int[prime];

        _freeList = -1;
    }

    private void Resize()
    {
        Resize(PrimeHelper.ExpandPrime(_count), false);
    }

    private void Resize(int newSize, bool forceNewHashCodes)
    {
        int[] bucketsCopy = new int[newSize];
        for (int i = 0; i < bucketsCopy.Length; i++)
            bucketsCopy[i] = -1;
        var keysCopy = new TKey[newSize];
        var valuesCopy = new TValue[newSize];
        var hashCodesCopy = new int[newSize];
        var nextCopy = new int[newSize];

        Array.Copy(_values, 0, valuesCopy, 0, _count);
        Array.Copy(_keys, 0, keysCopy, 0, _count);
        Array.Copy(_hashCodes, 0, hashCodesCopy, 0, _count);
        Array.Copy(_next, 0, nextCopy, 0, _count);
        
        if(forceNewHashCodes)
        {
            for (int i = 0; i < _count; i++)
            {
                if (hashCodesCopy[i] != -1)
                    hashCodesCopy[i] = (_comparer.GetHashCode(keysCopy[i]) & int.MaxValue);
            }
        }

        for (int i = 0; i < _count; i++)
        {
            int index = hashCodesCopy[i] % newSize;
            nextCopy[i] = bucketsCopy[index];
            bucketsCopy[index] = i;
        }
        _buckets = bucketsCopy;
        _hashCodes = hashCodesCopy;
        _keys = keysCopy;
        _values = valuesCopy;
        _next = nextCopy;
    }

}
