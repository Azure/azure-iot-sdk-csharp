// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Threading;

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

    /// <summary>
    /// Read-only wrapper for another generic dictionary.
    /// </summary>
    /// <typeparam name="TKey">Type to be used for keys.</typeparam>
    /// <typeparam name="TValue">Type to be used for values</typeparam>
    [Serializable]
    [DebuggerDisplay("Count = {Count}")]
#pragma warning disable CA1710 // Identifiers should have correct suffix
    public sealed class ReadOnlyDictionary45<TKey, TValue> : IDictionary<TKey, TValue>, IDictionary
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
        /// Creates a readonly dictionary from a specified IDictionary
        /// </summary>
        /// <param name="dictionary">The source dictionary</param>
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
        /// The source dictionary
        /// </summary>
#pragma warning disable CS0628 // New protected member declared in sealed class
        protected IDictionary<TKey, TValue> Dictionary { get; private set; }
#pragma warning restore CS0628 // New protected member declared in sealed class

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

        ICollection<TKey> IDictionary<TKey, TValue>.Keys => Keys;

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
        /// Returns an enumerator
        /// </summary>
        /// <returns>The enumerator</returns>
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
            get => IsCompatibleKey(key)
                    ? this[(TKey)key]
                    : (object)null;
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
                    if (!(array is object[] objects))
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
            private readonly IDictionary<TKey, TValue> _dictionary;
            private IEnumerator<KeyValuePair<TKey, TValue>> _enumerator;

            public DictionaryEnumerator(IDictionary<TKey, TValue> dictionary)
            {
                _dictionary = dictionary;
                _enumerator = _dictionary.GetEnumerator();
            }

            public DictionaryEntry Entry => new DictionaryEntry(_enumerator.Current.Key, _enumerator.Current.Value);

            public object Key => _enumerator.Current.Key;

            public object Value => _enumerator.Current.Value;

            public object Current => Entry;

            public bool MoveNext()
            {
                return _enumerator.MoveNext();
            }

            public void Reset()
            {
                _enumerator.Reset();
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
                _collection = collection ?? throw Fx.Exception.ArgumentNull(nameof(collection));
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
            private readonly ICollection<TValue> _collection;

            [NonSerialized]
            private object _syncRoot;

            [NonSerialized]
            private readonly IReadOnlyIndicator _readOnlyIndicator;

            internal ValueCollection(ICollection<TValue> collection, IReadOnlyIndicator readOnlyIndicator)
            {
                _collection = collection ?? throw Fx.Exception.ArgumentNull(nameof(collection));
                _readOnlyIndicator = readOnlyIndicator;
            }

            #region ICollection<T> Members

            void ICollection<TValue>.Add(TValue item)
            {
                if (_readOnlyIndicator.IsReadOnly)
                {
                    throw Fx.Exception.AsError(new NotSupportedException(Resources.ObjectIsReadOnly));
                }

                _collection.Add(item);
            }

            void ICollection<TValue>.Clear()
            {
                if (_readOnlyIndicator.IsReadOnly)
                {
                    throw Fx.Exception.AsError(new NotSupportedException(Resources.ObjectIsReadOnly));
                }

                _collection.Clear();
            }

            bool ICollection<TValue>.Contains(TValue item)
            {
                return _collection.Contains(item);
            }

            /// <summary>
            /// Copies the values to the specified array
            /// </summary>
            /// <param name="array">The destination array</param>
            /// <param name="arrayIndex">The starting index</param>
            public void CopyTo(TValue[] array, int arrayIndex)
            {
                _collection.CopyTo(array, arrayIndex);
            }

            /// <summary>
            /// The count of values in the collection
            /// </summary>
            public int Count => _collection.Count;

            bool ICollection<TValue>.IsReadOnly => true;

            bool ICollection<TValue>.Remove(TValue item)
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
            public IEnumerator<TValue> GetEnumerator()
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
