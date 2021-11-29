using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Shared
{
    /// <summary>
    /// A class that is used to handle an object that needs to be counted.
    /// </summary>
    /// <remarks>
    /// Use of this class should be a static member variable inside of another class. All of the public members will take in an object that will keep track of who has Created/Removed references
    /// </remarks>
    /// <example>
    /// <code> public class ContainerUsage
    /// {
    ///     private static ReferenceCounter&lt;object&gt; _refObject = new ReferenceCounter&lt;object&gt;();
    ///     
    ///     public ContainerUsage()
    ///     {
    ///         _refObject.Create(() => new object());
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <typeparam name="T">Any class based object type.</typeparam>
    public class ReferenceCounter<T> where T : class
    {
        private const int _mutextTimeout = Timeout.Infinite;

        private WeakReference<T> _objectToCount;

        private Mutex _lockObject = new Mutex();

        private volatile int _referenceCount;

        private IList<WeakReference> _weakRefList = new List<WeakReference>();

        /// <summary>
        /// The reference counted object.
        /// </summary>
        public T Value { get { _objectToCount.TryGetTarget(out T target); return target; } }

        /// <summary>
        /// The current count.
        /// </summary>
        public int Count => _referenceCount;

        /// <summary>
        /// Create the object or increment the counter.
        /// </summary>
        /// <remarks>
        /// If the object is already set this will not execute the code in <paramref name="objectToCreate"/>. Instead it will return the object that was previously created.
        /// 
        /// 
        /// 
        /// This will return a mutable object. It is possible to alter the object state which could cause unexpected behavior.
        /// </remarks>
        /// <example>
        /// <code> public class ContainerUsage
        /// {
        ///     private static ReferenceCounter&lt;object&gt; _refObject = new ReferenceCounter&lt;object&gt;();
        ///     
        ///     public ContainerUsage()
        ///     {
        ///         _refObject.Create(() => new object());
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <param name="objectToCreate">A function that generates the object to be counted. This must not return a null object.</param>
        /// 
        /// <exception cref="InvalidOperationException">This will be thrown if the reference object is somehow out of sync.</exception>
        /// <exception cref="ArgumentException">This will be thrown if the creation method supplied to this method returns a null object.</exception>
        /// <returns>The object created or the previously created object to be reference counted</returns>
        public T Create(T objectToCreate)
        {
            try
            {
                EnterLock();
                CreateWithoutLocking(objectToCreate);
                _referenceCount++;
            }
            finally
            {
                LeaveLock();
            }
            return Value;
        }

        /// <summary>
        /// Create the object or increment the counter.
        /// </summary>
        /// <remarks>
        /// If the object is already set this will not execute the code in <paramref name="objectToCreate"/>. Instead it will return the object that was previously created.
        /// 
        /// 
        /// 
        /// This will return a mutable object. It is possible to alter the object state which could cause unexpected behavior.
        /// </remarks>
        /// <example>
        /// <code> public class ContainerUsage
        /// {
        ///     private static ReferenceCounter&lt;object&gt; _refObject = new ReferenceCounter&lt;object&gt;();
        ///     
        ///     public ContainerUsage()
        ///     {
        ///         _refObject.Create(() => new object());
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <param name="objectToCreate">A function that generates the object to be counted. This must not return a null object.</param>
        /// 
        /// <exception cref="InvalidOperationException">This will be thrown if the reference object is somehow out of sync.</exception>
        /// <exception cref="ArgumentException">This will be thrown if the creation method supplied to this method returns a null object.</exception>
        /// <returns>The object created or the previously created object to be reference counted</returns>
        public T Create(Func<T> objectToCreate)
        {
            try
            {
                EnterLock();
                if (_objectToCount == null)
                {
                    CreateWithoutLocking(objectToCreate());
                }
                _referenceCount++;
            }
            finally
            {
                LeaveLock();
            }
            return Value;
        }

        private void LeaveLock()
        {
            _lockObject.ReleaseMutex();
        }

        private void EnterLock()
        {
            _lockObject.WaitOne(_mutextTimeout);
        }

        private void CreateWithoutLocking(T objectToCreate)
        {
            if (_objectToCount == null && _referenceCount == 0)
            {
                if (objectToCreate == null)
                {
                    throw new ArgumentException("The value returned from the object creation method was null. Ensure the method returns a valid object.");
                }
                _objectToCount = new WeakReference<T>(objectToCreate);

            }
            else if (Value == null && _referenceCount > 0)
            {
                throw new InvalidOperationException($"The reference count is {_referenceCount} but the object to count is null. The {nameof(ReferenceCounter<T>)} class should be ");
            }
        }

        /// <summary>
        /// Sets the object to null, clears the reference counter.
        /// </summary>
        /// <remarks>
        /// This will only execute if there is at least one reference count and will not throw an exception.
        /// </remarks>
        /// <example>
        /// <code> public class ContainerUsage
        /// {
        ///     private static ReferenceCounter&lt;Stream&gt; _refObject = new ReferenceCounter&lt;Stream&gt;();
        ///     
        ///     public ContainerUsage()
        ///     {
        ///         _refObject.Create(() => new FileStream(), this);
        ///         _refObject.Clear(this);
        ///     }
        /// }
        /// </code>
        /// </example>
        public T Clear()
        {
            try
            {
                EnterLock();
                return ClearInternal();
            }
            finally
            {
                LeaveLock();
            }
        }

        private T ClearInternal()
        {
            T objectToReturn = Value;
            _objectToCount = null;
            _referenceCount = 0;
            _weakRefList.Clear();
            return objectToReturn;
        }

        /// <summary>
        /// Remove the reference counted object or decrement the counter.
        /// </summary>
        /// <remarks>
        /// This will not dispose the underlying object and it is up to the caller to . Instead when the reference counter reaches zero it will execute <see cref="Clear"/>. If this method is called more times than there are references there will be no failure.
        /// </remarks>
        /// /// <example>
        /// <code> public class ContainerUsage
        /// {
        ///     private static ReferenceCounter&lt;object&gt; _refObject = new ReferenceCounter&lt;object&gt;();
        ///     
        ///     public ContainerUsage()
        ///     {
        ///         _refObject.Create(() => new object());
        ///         var objectToCheck = _refObject.Remove();
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <returns>If the counter reaches 0 when this method is called the reference counted object will be returned. Otherwise null</returns>
        public T Remove()
        {
            try
            {
                EnterLock();
                if (_referenceCount > 0 && --_referenceCount == 0)
                {
                    return ClearInternal();
                }
            }
            finally
            {
                LeaveLock();
            }
            return null;
        }
    }
}
