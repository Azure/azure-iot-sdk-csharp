using System;
using System.Threading;

namespace Microsoft.Azure.Devices.Shared
{
    /// <summary>
    /// A class that is used to handle an object that needs to be counted.
    /// </summary>
    /// <remarks>
    /// Use of this class should be a static member variable inside of another class.
    /// </remarks>
    /// <example>
    /// <code> public class ContainerUsage
    /// {
    ///     private ReferenceCounter&lt;object&gt; _refObject = new ReferenceCounter&lt;object&gt;();
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
        private T _obejctToCount;

        private object _lockObject = new object();

        private Action<T> _executeOnDispose;

        private volatile int _referenceCount;

        /// <summary>
        /// The reference counted object.
        /// </summary>
        public T Value => _obejctToCount;

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
        /// <param name="objectToCreate">A function that generates the object to be counted. This must not return a null object.</param>
        /// <exception cref="InvalidOperationException">This will be thrown if the reference object is somehow out of sync.</exception>
        /// <exception cref="ArgumentException">This will be thrown if the creation method supplied to this method returns a null object.</exception>
        /// <returns>The object created</returns>
        public T Create(Func<T> objectToCreate)
        {
            lock (_lockObject)
            {
                CreateWithoutLocking(objectToCreate);
                Interlocked.Increment(ref _referenceCount);
                return _obejctToCount;
            }
        }

        private void CreateWithoutLocking(Func<T> objectToCreate)
        {
            if (_obejctToCount == null && _referenceCount <= 0)
            {
                _obejctToCount = objectToCreate();
                if (_obejctToCount == null)
                {
                    throw new ArgumentException("The value returned from the object creation method was null. Ensure the method returns a valid object.");
                }
            }
            else if (_obejctToCount == null && _referenceCount > 0)
            {
                throw new InvalidOperationException($"The reference count is {_referenceCount} but the object to count is null. The {nameof(ReferenceCounter<T>)} class should be ");
            }
        }

        /// <summary>
        /// Create the object or increment the counter.
        /// </summary>
        /// <param name="objectToCreate">A function that generates the object to be counted. This must not return a null object.</param>
        /// <param name="executeOnLastRemoval">An action that will receive the reference counted object so you can properly dispose of the object if needed. This method can be null.</param>
        /// <returns>The object created</returns>
        /// <remarks>
        /// If the object is already set this will not execute the code in <paramref name="objectToCreate"/>. Instead it will return the object that was previously created.
        /// 
        /// 
        /// 
        /// This will return a mutable object. It is possible to alter the object state which could cause unexpected behavior.
        /// </remarks>
        /// <exception cref="InvalidOperationException">This will be thrown if the reference object is somehow out of sync.</exception>
        /// <exception cref="ArgumentException">This will be thrown if the creation method supplied to this method returns a null object.</exception>
        public T CreateWithRemoveAction(Func<T> objectToCreate, Action<T> executeOnLastRemoval)
        {
            lock (_lockObject)
            {
                if (_executeOnDispose == null && _referenceCount <= 0)
                {
                    _executeOnDispose = executeOnLastRemoval;
                }
                else if (_executeOnDispose == null && _referenceCount > 0)
                {
                    throw new InvalidOperationException($"The reference count is {_referenceCount} but the object to count is null. The {nameof(ReferenceCounter<T>)} class should be cleared.");
                }
                CreateWithoutLocking(objectToCreate);
                Interlocked.Increment(ref _referenceCount);
                return _obejctToCount;
            }
        }

        /// <summary>
        /// Sets the internal object to null, clears the reference counter, and if <see cref="CreateWithRemoveAction"/> was used it will execute the removal function defined.
        /// </summary>
        /// <remarks>
        /// This will only execute if there is atleast one reference count.
        </remarks>
        /// Take care to properly tear down your object. It is best to use the <see cref="CreateWithRemoveAction(Func{T}, Action{T})"/> method to create your reference counted object.
        /// <example>
        /// <code> public class ContainerUsage
        /// {
        ///     private ReferenceCounter&lt;Stream&gt; _refObject = new ReferenceCounter&lt;Stream&gt;();
        ///     
        ///     public ContainerUsage()
        ///     {
        ///         _refObject.Create(() => new FileStream(), (teardown) => teardown.Dispose());
        ///         _refObject.Clear();
        ///     }
        /// }
        /// </code>
        /// </example>
        public void Clear()
        {
            ClearInternal(false);
        }

        private void ClearInternal(bool calledFromDispose)
        {
            lock (_lockObject)
            {
                if (calledFromDispose || _referenceCount > 0)
                {
                    if (_executeOnDispose != null)
                    {
                        _executeOnDispose(_obejctToCount);
                    }
                    _obejctToCount = null;
                    _referenceCount = 0;
                }
            }
        }

        /// <summary>
        /// Remove the refernce counted object or decrement the counter.
        /// </summary>
        /// <remarks>
        /// This will not dispose the underlying object. Instead when the reference counter reaches zero it will execute <see cref="Clear"/>. If this method is called more times than there are references there will be no failure.
        /// </remarks>
        public void Remove()
        {
            if (_referenceCount > 0 && Interlocked.Decrement(ref _referenceCount) == 0)
            {
                ClearInternal(true);
            }
        }
    }
}
