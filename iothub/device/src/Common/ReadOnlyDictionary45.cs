// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Threading;

namespace Microsoft.Azure.Devices.Client
{
#pragma warning disable CA1033 // Interface methods should be callable by child types - should have marked this class as sealed, but too late now

    /// <summary>
    /// Read-only wrapper for another generic dictionary.
    /// </summary>
    /// <typeparam name="TKey">Type to be used for keys.</typeparam>
    /// <typeparam name="TValue">Type to be used for values</typeparam>
    [Serializable]
    [DebuggerDisplay("Count = {Count}")]
#pragma warning disable CA1710 // Identifiers should have correct suffix
    public class ReadOnlyDictionary45<TKey, TValue> : IDictionary<TKey, TValue>, IDictionary
#pragma warning restore CA1710 // Identifiers should have correct suffix
    {
        [NonSerialized]
        private object _syncRoot;

        [NonSerialized]
        private KeyCollection _keys;

        [NonSerialized]
        private ValueCollection _values;

        [NonSerialized]
        private IReadOnlyIndicator _readOnlyIndicator;

        /// <summary>
        /// Creates an instance of ReadOnlyDictionary45 weeded with a supplied IDictionary
        /// </summary>
        /// <param name="dictionary"></param>
        public ReadOnlyDictionary45(IDictionary<TKey, TValue> dictionary)
            : this(dictionary, new AlwaysReadOnlyIndicator())
        {
        }

        internal ReadOnlyDictionary45(IDictionary<TKey, TValue> dictionary, IReadOnlyIndicator readOnlyIndicator)
        {
            Dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
            Contract.EndContractBlock();
            _readOnlyIndicator = readOnlyIndicator;
        }

        /// <summary>
        /// The dictionary
        /// </summary>
        protected IDictionary<TKey, TValue> Dictionary { get; private set; }

        /// <summary>
        /// They keys in the dictionary
        /// </summary>
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

        /// <summary>
        /// The values in the dictionary
        /// </summary>
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

        /// <summary>
        /// Reports whether a key exists in the dictionary
        /// </summary>
        /// <param name="key">The key to check</param>
        /// <returns>True if exists, othewise fals</returns>
        public bool ContainsKey(TKey key)
        {
            return Dictionary.ContainsKey(key);
        }

        ICollection<TKey> IDictionary<TKey, TValue>.Keys
        {
            get
            {
                return Keys;
            }
        }

        /// <summary>
        /// Gets the value of the specified key, if exists
        /// </summary>
        /// <param name="key">The desired key</param>
        /// <param name="value">The value found</param>
        /// <returns>True if key was found</returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            return Dictionary.TryGetValue(key, out value);
        }

        ICollection<TValue> IDictionary<TKey, TValue>.Values => Values;

        /// <summary>
        /// Enables accessing values by indexing with a key
        /// </summary>
        /// <param name="key">The desired key</param>
        /// <returns>The corresponding value</returns>
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

        /// <summary>
        /// The count of items in the dictionary
        /// </summary>
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

        /// <summary>
        /// Returns an enumerator that iterates through the dictionary.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the dictionary.</returns>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return Dictionary.GetEnumerator();
        }

        #endregion IEnumerable<KeyValuePair<TKey, TValue>> Members

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
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
            if (d != null)
            {
                return d.GetEnumerator();
            }
            return new DictionaryEnumerator(Dictionary);
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
                if (IsCompatibleKey(key))
                {
                    return this[(TKey)key];
                }
                return null;
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

            var pairs = array as KeyValuePair<TKey, TValue>[];
            if (pairs != null)
            {
                Dictionary.CopyTo(pairs, index);
            }
            else
            {
                var dictEntryArray = array as DictionaryEntry[];
                if (dictEntryArray != null)
                {
                    foreach (KeyValuePair<TKey, TValue> item in Dictionary)
                    {
                        dictEntryArray[index++] = new DictionaryEntry(item.Key, item.Value);
                    }
                }
                else
                {
                    object[] objects = array as object[];
                    if (objects == null)
                    {
                        throw Fx.Exception.Argument(nameof(array), Resources.InvalidBufferSize);
                    }

                    try
                    {
                        foreach (KeyValuePair<TKey, TValue> item in Dictionary)
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

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

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

        /// <summary>
        /// A collection of dictionary keys
        /// </summary>
        [DebuggerDisplay("Count = {Count}")]
        [Serializable]
#pragma warning disable CA1034 // Nested types should not be visible
        public sealed class KeyCollection : ICollection<TKey>, ICollection
#pragma warning restore CA1034 // Nested types should not be visible
        {
            private readonly ICollection<TKey> _collection;

            [NonSerialized]
            private object _syncRoot;

            [NonSerialized]
            private readonly IReadOnlyIndicator _readOnlyIndicator;

            internal KeyCollection(ICollection<TKey> collection, IReadOnlyIndicator readOnlyIndicator)
            {
                if (collection == null)
                {
                    throw Fx.Exception.ArgumentNull(nameof(collection));
                }
                _collection = collection;
                _readOnlyIndicator = readOnlyIndicator;
            }

            #region ICollection<T> Members

            void ICollection<TKey>.Add(TKey item)
            {
                if (_readOnlyIndicator.IsReadOnly)
                {
                    throw Fx.Exception.AsError(new NotSupportedException(Resources.ObjectIsReadOnly));
                }

                _collection.Add(item);
            }

            void ICollection<TKey>.Clear()
            {
                if (_readOnlyIndicator.IsReadOnly)
                {
                    throw Fx.Exception.AsError(new NotSupportedException(Resources.ObjectIsReadOnly));
                }

                _collection.Clear();
            }

            bool ICollection<TKey>.Contains(TKey item)
            {
                return _collection.Contains(item);
            }

            /// <summary>
            /// Copies the key collection to the specified array
            /// </summary>
            /// <param name="array">Destination array</param>
            /// <param name="arrayIndex">Starting index to copy to</param>
            public void CopyTo(TKey[] array, int arrayIndex)
            {
                _collection.CopyTo(array, arrayIndex);
            }

            /// <summary>
            /// The count of keys
            /// </summary>
            public int Count => _collection.Count;

            bool ICollection<TKey>.IsReadOnly => true;

            bool ICollection<TKey>.Remove(TKey item)
            {
                if (_readOnlyIndicator.IsReadOnly)
                {
                    throw Fx.Exception.AsError(new NotSupportedException(Resources.ObjectIsReadOnly));
                }

                return _collection.Remove(item);
            }

            #endregion ICollection<T> Members

            #region IEnumerable<T> Members

            /// <summary>
            /// Gets an enumerator
            /// </summary>
            /// <returns>The enumerator</returns>
            public IEnumerator<TKey> GetEnumerator()
            {
                return _collection.GetEnumerator();
            }

            #endregion IEnumerable<T> Members

            #region IEnumerable Members

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)_collection).GetEnumerator();
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
                    if (_syncRoot == null)
                    {
                        var c = _collection as ICollection;
                        if (c != null)
                        {
                            _syncRoot = c.SyncRoot;
                        }
                        else
                        {
                            Interlocked.CompareExchange<object>(ref _syncRoot, new object(), null);
                        }
                    }
                    return _syncRoot;
                }
            }

            #endregion ICollection Members
        }

        /// <summary>
        /// A collection of dictionary values
        /// </summary>
        [DebuggerDisplay("Count = {Count}")]
        [Serializable]
#pragma warning disable CA1034 // Nested types should not be visible
        public sealed class ValueCollection : ICollection<TValue>, ICollection
#pragma warning restore CA1034 // Nested types should not be visible
        {
            private readonly ICollection<TValue> m_collection;

            [NonSerialized]
            private object m_syncRoot;

            [NonSerialized]
            private readonly IReadOnlyIndicator m_readOnlyIndicator;

            internal ValueCollection(ICollection<TValue> collection, IReadOnlyIndicator readOnlyIndicator)
            {
                if (collection == null)
                {
                    throw Fx.Exception.ArgumentNull(nameof(collection));
                }

                m_collection = collection;
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

            /// <summary>
            /// Copies the values to the specified array
            /// </summary>
            /// <param name="array">The destination array</param>
            /// <param name="arrayIndex">The starting index</param>
            public void CopyTo(TValue[] array, int arrayIndex)
            {
                m_collection.CopyTo(array, arrayIndex);
            }

            /// <summary>
            /// The count of values in the collection
            /// </summary>
            public int Count
            {
                get { return m_collection.Count; }
            }

            bool ICollection<TValue>.IsReadOnly
            {
                get { return true; }
            }

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

            /// <summary>
            /// Gets an enumerator
            /// </summary>
            /// <returns>The enumerator</returns>
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

            bool ICollection.IsSynchronized
            {
                get { return false; }
            }

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
                            Interlocked.CompareExchange<Object>(ref m_syncRoot, new Object(), null);
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

    /// <summary>
    /// Indicates if a class is read-only
    /// </summary>
    public interface IReadOnlyIndicator
    {
        /// <summary>
        /// Indicates if the entity is read-only
        /// </summary>
        bool IsReadOnly { get; }
    }

#pragma warning restore CA1033 // Interface methods should be callable by child types
}
