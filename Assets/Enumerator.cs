using System;
using System.Collections;
using System.Collections.Generic;

public struct Enumerator<TKey,TValue> : IEnumerator<KeyValuePair<TKey,TValue>>
{
    private readonly SerializebleDictionary<TKey, TValue> _dictionary;
    private int _version;
    private int _index;
    KeyValuePair<TKey, TValue> _current;
    public KeyValuePair<TKey, TValue> Current => _current;

    object IEnumerator.Current => _current;

    public Enumerator(SerializebleDictionary<TKey, TValue> dictionary)
    {
        _dictionary = dictionary;
        _version = dictionary.Version;
        _current = default(KeyValuePair<TKey, TValue>);
        _index = 0;
    }

    public void Dispose()
    {
    }

    public bool MoveNext()
    {
        CheckVersion();
        while (_index < _dictionary.Length)
        {
            if(_dictionary.HashCodes[_index] >= 0)
            {
                _current = new KeyValuePair<TKey, TValue>(_dictionary.ArraiedKeys[_index], _dictionary.ArraiedValue[_index]);
                _index++;
                return true;
            }
            _index++;
        }
        _index = _dictionary.Length + 1;
        _current = default(KeyValuePair<TKey, TValue>);
        return false;
    }

    public void Reset()
    {
        CheckVersion();
        _index = 0;
        _current = default(KeyValuePair<TKey, TValue>);
    }

    private void CheckVersion()
    {
        if (_version != _dictionary.Version)
            throw new InvalidOperationException(string.Format("Enumerator version {0} != Dictionary version {1}", _version, _dictionary.Version));
    }
}
