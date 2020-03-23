// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;

namespace Microsoft.Azure.Devices.Common
{
    /*============================================================
    **
    ** Class:  ReadOnlyDictionary<TKey, TValue>
    **
    ** <OWNER>gpaperin</OWNER>
    **
    ** Purpose: Read-only wrapper for another generic dictionary.
    **
    ===========================================================*/

    [Serializable]
    [DebuggerDisplay("Count = {Count}")]
    public sealed class ReadOnlyDictionary45<TKey, TValue> : IDictionary<TKey, TValue>, IDictionary //, IReadOnlyDictionary<TKey, TValue>
    {
        [NonSerialized]
        private object _syncRoot;

        [NonSerialized]
        private KeyCollection _keys;

        [NonSerialized]
        private ValueCollection _values;

        [NonSerialized]
        private IReadOnlyIndicator _readOnlyIndicator;

        public ReadOnlyDictionary45(IDictionary<TKey, TValue> dictionary)
            : this(dictionary, new AlwaysReadOnlyIndicator())
        {
        }

        internal ReadOnlyDictionary45(IDictionary<TKey, TValue> dictionary, IReadOnlyIndicator readOnlyIndicator)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }
            Contract.EndContractBlock();
            Dictionary = dictionary;
            _readOnlyIndicator = readOnlyIndicator;
        }

        protected IDictionary<TKey, TValue> Dictionary { get; private set; }

        public KeyCollection Keys
        {
            get
            {
                Contract.Ensures(Contract.Result<KeyCollection>() != null);
                if (_keys == null)
                {
                    _keys = new KeyCollection(Dictionary.Keys, _readOnlyIndicator);
                }
                return _keys;
            }
        }

        public ValueCollection Values
        {
            get
            {
                Contract.Ensures(Contract.Result<ValueCollection>() != null);
                if (_values == null)
                {
                    _values = new ValueCollection(Dictionary.Values, _readOnlyIndicator);
                }
                return _values;
            }
        }

        #region IDictionary<TKey, TValue> Members

        public bool ContainsKey(TKey key)
        {
            return Dictionary.ContainsKey(key);
        }

        ICollection<TKey> IDictionary<TKey, TValue>.Keys => Keys;

        public bool TryGetValue(TKey key, out TValue value)
        {
            return Dictionary.TryGetValue(key, out value);
        }

        ICollection<TValue> IDictionary<TKey, TValue>.Values => Values;

        public TValue this[TKey key]
        {
            get
            {
                return Dictionary[key];
            }
        }

        void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
        {
            if (_readOnlyIndicator.IsReadOnly)
            {
                throw Fx.Exception.AsError(new NotSupportedException(Resources.ObjectIsReadOnly));
            }

            Dictionary.Add(key, value);
        }

        bool IDictionary<TKey, TValue>.Remove(TKey key)
        {
            if (_readOnlyIndicator.IsReadOnly)
            {
                throw Fx.Exception.AsError(new NotSupportedException(Resources.ObjectIsReadOnly));
            }

            return Dictionary.Remove(key);
        }

        TValue IDictionary<TKey, TValue>.this[TKey key]
        {
            get => Dictionary[key];
            set
            {
                if (_readOnlyIndicator.IsReadOnly)
                {
                    throw Fx.Exception.AsError(new NotSupportedException(Resources.ObjectIsReadOnly));
                }

                Dictionary[key] = value;
            }
        }

        #endregion IDictionary<TKey, TValue> Members

        #region ICollection<KeyValuePair<TKey, TValue>> Members

        public int Count => Dictionary.Count;

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            return Dictionary.Contains(item);
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            Dictionary.CopyTo(array, arrayIndex);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => true;

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            if (_readOnlyIndicator.IsReadOnly)
            {
                throw Fx.Exception.AsError(new NotSupportedException(Resources.ObjectIsReadOnly));
            }

            Dictionary.Add(item);
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Clear()
        {
            if (_readOnlyIndicator.IsReadOnly)
            {
                throw Fx.Exception.AsError(new NotSupportedException(Resources.ObjectIsReadOnly));
            }

            Dictionary.Clear();
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            if (_readOnlyIndicator.IsReadOnly)
            {
                throw Fx.Exception.AsError(new NotSupportedException(Resources.ObjectIsReadOnly));
            }

            return Dictionary.Remove(item);
        }

        #endregion ICollection<KeyValuePair<TKey, TValue>> Members

        #region IEnumerable<KeyValuePair<TKey, TValue>> Members

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return Dictionary.GetEnumerator();
        }

        #endregion IEnumerable<KeyValuePair<TKey, TValue>> Members

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)Dictionary).GetEnumerator();
        }

        #endregion IEnumerable Members

        #region IDictionary Members

        private static bool IsCompatibleKey(object key)
        {
            if (key == null)
            {
                throw Fx.Exception.ArgumentNull(nameof(key));
            }

            return key is TKey;
        }

        void IDictionary.Add(object key, object value)
        {
            if (_readOnlyIndicator.IsReadOnly)
            {
                throw Fx.Exception.AsError(new NotSupportedException(Resources.ObjectIsReadOnly));
            }

            Dictionary.Add((TKey)key, (TValue)value);
        }

        void IDictionary.Clear()
        {
            if (_readOnlyIndicator.IsReadOnly)
            {
                throw Fx.Exception.AsError(new NotSupportedException(Resources.ObjectIsReadOnly));
            }

            Dictionary.Clear();
        }

        bool IDictionary.Contains(object key)
        {
            return IsCompatibleKey(key) && ContainsKey((TKey)key);
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            var d = Dictionary as IDictionary;
            return d != null
                ? d.GetEnumerator()
                : new DictionaryEnumerator(Dictionary);
        }

        bool IDictionary.IsFixedSize => true;

        bool IDictionary.IsReadOnly => true;

        ICollection IDictionary.Keys => Keys;

        void IDictionary.Remove(object key)
        {
            if (_readOnlyIndicator.IsReadOnly)
            {
                throw Fx.Exception.AsError(new NotSupportedException(Resources.ObjectIsReadOnly));
            }

            Dictionary.Remove((TKey)key);
        }

        ICollection IDictionary.Values => Values;

        object IDictionary.this[object key]
        {
            get
            {
                return IsCompatibleKey(key)
                    ? this[(TKey)key]
                    : (object)null;
            }
            set
            {
                if (_readOnlyIndicator.IsReadOnly)
                {
                    throw Fx.Exception.AsError(new NotSupportedException(Resources.ObjectIsReadOnly));
                }

                Dictionary[(TKey)key] = (TValue)value;
            }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
            {
                Fx.Exception.ArgumentNull(nameof(array));
            }

            if (array.Rank != 1 || array.GetLowerBound(0) != 0)
            {
                throw Fx.Exception.Argument(nameof(array), Resources.InvalidBufferSize);
            }

            if (index < 0 || index > array.Length)
            {
                throw Fx.Exception.ArgumentOutOfRange(nameof(index), index, Resources.ValueMustBeNonNegative);
            }

            if (array.Length - index < Count)
            {
                throw Fx.Exception.Argument(nameof(array), Resources.InvalidBufferSize);
            }

            KeyValuePair<TKey, TValue>[] pairs = array as KeyValuePair<TKey, TValue>[];
            if (pairs != null)
            {
                Dictionary.CopyTo(pairs, index);
            }
            else
            {
                DictionaryEntry[] dictEntryArray = array as DictionaryEntry[];
                if (dictEntryArray != null)
                {
                    foreach (KeyValuePair<TKey, TValue> item in Dictionary)
                    {
                        dictEntryArray[index++] = new DictionaryEntry(item.Key, item.Value);
                    }
                }
                else
                {
                    if (!(array is object[] objects))
                    {
                        throw Fx.Exception.Argument(nameof(array), Resources.InvalidBufferSize);
                    }

                    try
                    {
                        foreach (var item in Dictionary)
                        {
                            objects[index++] = new KeyValuePair<TKey, TValue>(item.Key, item.Value);
                        }
                    }
                    catch (ArrayTypeMismatchException)
                    {
                        throw Fx.Exception.Argument(nameof(array), Resources.InvalidBufferSize);
                    }
                }
            }
        }

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot
        {
            get
            {
                if (_syncRoot == null)
                {
                    var c = Dictionary as ICollection;
                    if (c != null)
                    {
                        _syncRoot = c.SyncRoot;
                    }
                    else
                    {
                        System.Threading.Interlocked.CompareExchange<Object>(ref _syncRoot, new Object(), null);
                    }
                }

                return _syncRoot;
            }
        }

        [Serializable]
        private struct DictionaryEnumerator : IDictionaryEnumerator
        {
            private readonly IDictionary<TKey, TValue> m_dictionary;
            private IEnumerator<KeyValuePair<TKey, TValue>> m_enumerator;

            public DictionaryEnumerator(IDictionary<TKey, TValue> dictionary)
            {
                m_dictionary = dictionary;
                m_enumerator = m_dictionary.GetEnumerator();
            }

            public DictionaryEntry Entry => new DictionaryEntry(m_enumerator.Current.Key, m_enumerator.Current.Value);

            public object Key => m_enumerator.Current.Key;

            public object Value => m_enumerator.Current.Value;

            public object Current => Entry;

            public bool MoveNext()
            {
                return m_enumerator.MoveNext();
            }

            public void Reset()
            {
                m_enumerator.Reset();
            }
        }

        #endregion IDictionary Members

        [DebuggerDisplay("Count = {Count}")]
        [Serializable]
        public sealed class KeyCollection : ICollection<TKey>, ICollection
        {
            private readonly ICollection<TKey> m_collection;

            [NonSerialized]
            private object m_syncRoot;

            [NonSerialized]
            private readonly IReadOnlyIndicator m_readOnlyIndicator;

            internal KeyCollection(ICollection<TKey> collection, IReadOnlyIndicator readOnlyIndicator)
            {
                m_collection = collection ?? throw Fx.Exception.ArgumentNull(nameof(collection));
                m_readOnlyIndicator = readOnlyIndicator;
            }

            #region ICollection<T> Members

            void ICollection<TKey>.Add(TKey item)
            {
                if (m_readOnlyIndicator.IsReadOnly)
                {
                    throw Fx.Exception.AsError(new NotSupportedException(Resources.ObjectIsReadOnly));
                }

                m_collection.Add(item);
            }

            void ICollection<TKey>.Clear()
            {
                if (m_readOnlyIndicator.IsReadOnly)
                {
                    throw Fx.Exception.AsError(new NotSupportedException(Resources.ObjectIsReadOnly));
                }

                m_collection.Clear();
            }

            bool ICollection<TKey>.Contains(TKey item)
            {
                return m_collection.Contains(item);
            }

            public void CopyTo(TKey[] array, int arrayIndex)
            {
                m_collection.CopyTo(array, arrayIndex);
            }

            public int Count => m_collection.Count;

            bool ICollection<TKey>.IsReadOnly => true;

            bool ICollection<TKey>.Remove(TKey item)
            {
                if (m_readOnlyIndicator.IsReadOnly)
                {
                    throw Fx.Exception.AsError(new NotSupportedException(Resources.ObjectIsReadOnly));
                }

                return m_collection.Remove(item);
            }

            #endregion ICollection<T> Members

            #region IEnumerable<T> Members

            public IEnumerator<TKey> GetEnumerator()
            {
                return m_collection.GetEnumerator();
            }

            #endregion IEnumerable<T> Members

            #region IEnumerable Members

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)m_collection).GetEnumerator();
            }

            #endregion IEnumerable Members

            #region ICollection Members

            void ICollection.CopyTo(Array array, int index)
            {
                throw Fx.Exception.AsError(new NotImplementedException());
            }

            bool ICollection.IsSynchronized => false;

            object ICollection.SyncRoot
            {
                get
                {
                    if (m_syncRoot == null)
                    {
                        var c = m_collection as ICollection;
                        if (c != null)
                        {
                            m_syncRoot = c.SyncRoot;
                        }
                        else
                        {
                            System.Threading.Interlocked.CompareExchange<Object>(ref m_syncRoot, new Object(), null);
                        }
                    }
                    return m_syncRoot;
                }
            }

            #endregion ICollection Members
        }

        [DebuggerDisplay("Count = {Count}")]
        [Serializable]
        public sealed class ValueCollection : ICollection<TValue>, ICollection
        {
            private readonly ICollection<TValue> m_collection;

            [NonSerialized]
            private object m_syncRoot;

            [NonSerialized]
            private readonly IReadOnlyIndicator m_readOnlyIndicator;

            internal ValueCollection(ICollection<TValue> collection, IReadOnlyIndicator readOnlyIndicator)
            {
                m_collection = collection ?? throw Fx.Exception.ArgumentNull(nameof(collection));
                m_readOnlyIndicator = readOnlyIndicator;
            }

            #region ICollection<T> Members

            void ICollection<TValue>.Add(TValue item)
            {
                if (m_readOnlyIndicator.IsReadOnly)
                {
                    throw Fx.Exception.AsError(new NotSupportedException(Resources.ObjectIsReadOnly));
                }

                m_collection.Add(item);
            }

            void ICollection<TValue>.Clear()
            {
                if (m_readOnlyIndicator.IsReadOnly)
                {
                    throw Fx.Exception.AsError(new NotSupportedException(Resources.ObjectIsReadOnly));
                }

                m_collection.Clear();
            }

            bool ICollection<TValue>.Contains(TValue item)
            {
                return m_collection.Contains(item);
            }

            public void CopyTo(TValue[] array, int arrayIndex)
            {
                m_collection.CopyTo(array, arrayIndex);
            }

            public int Count => m_collection.Count;

            bool ICollection<TValue>.IsReadOnly => true;

            bool ICollection<TValue>.Remove(TValue item)
            {
                if (m_readOnlyIndicator.IsReadOnly)
                {
                    throw Fx.Exception.AsError(new NotSupportedException(Resources.ObjectIsReadOnly));
                }

                return m_collection.Remove(item);
            }

            #endregion ICollection<T> Members

            #region IEnumerable<T> Members

            public IEnumerator<TValue> GetEnumerator()
            {
                return m_collection.GetEnumerator();
            }

            #endregion IEnumerable<T> Members

            #region IEnumerable Members

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)m_collection).GetEnumerator();
            }

            #endregion IEnumerable Members

            #region ICollection Members

            void ICollection.CopyTo(Array array, int index)
            {
                throw Fx.Exception.AsError(new NotImplementedException());
            }

            bool ICollection.IsSynchronized => false;

            object ICollection.SyncRoot
            {
                get
                {
                    if (m_syncRoot == null)
                    {
                        var c = m_collection as ICollection;
                        if (c != null)
                        {
                            m_syncRoot = c.SyncRoot;
                        }
                        else
                        {
                            System.Threading.Interlocked.CompareExchange<object>(ref m_syncRoot, new object(), null);
                        }
                    }
                    return m_syncRoot;
                }
            }

            #endregion ICollection Members
        }

        private class AlwaysReadOnlyIndicator : IReadOnlyIndicator
        {
            public bool IsReadOnly => true;
        }
    }

    internal interface IReadOnlyIndicator
    {
        bool IsReadOnly { get; }
    }
}
