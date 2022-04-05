// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using Microsoft.Azure.Devices.Client.Exceptions;

namespace Microsoft.Azure.Devices.Client
{
    internal static class Fx
    {
        // This is only used for EventLog Source therefore matching EventLog source rather than ETL source
        private const string DefaultEventSource = "Microsoft.IotHub";

#if DEBUG
        private static bool s_breakOnExceptionTypesRetrieved;
        private static Type[] s_breakOnExceptionTypesCache;
#endif

        private static ExceptionTrace s_exceptionTrace;

        public static ExceptionTrace Exception
        {
            get
            {
                if (s_exceptionTrace == null)
                {
                    //need not be a true singleton. No locking needed here.
                    s_exceptionTrace = new ExceptionTrace(DefaultEventSource);
                }

                return s_exceptionTrace;
            }
        }

        // Do not call the parameter "message" or else FxCop thinks it should be localized.
        [Conditional("DEBUG")]
        public static void Assert(bool condition, string description)
        {
            if (!condition)
            {
                Assert(description);
            }
        }

        [Conditional("DEBUG")]
        public static void Assert(string description)
        {
            Debug.Assert(false, description);
        }

        public static void AssertAndThrow(bool condition, string description)
        {
            if (!condition)
            {
                throw new InvalidOperationException(description);
            }
        }

        public static void AssertAndThrowFatal(bool condition, string description)
        {
            Debug.Assert(condition, description);

            if (!condition)
            {
                AssertAndThrowFatal(description);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Exception AssertAndThrowFatal(string description)
        {
            Fx.Assert(description);
            throw Fx.Exception.AsError(new FatalException(description));
        }

        public static bool IsFatal(Exception exception)
        {
            while (exception != null)
            {
                // FYI, CallbackException is-a FatalException
                if (exception is FatalException ||
                    exception is OutOfMemoryException ||
                    exception is SEHException)
                {
                    return true;
                }

                // These exceptions aren't themselves fatal, but since the CLR uses them to wrap other exceptions,
                // we want to check to see whether they've been used to wrap a fatal exception.  If so, then they
                // count as fatal.
                if (exception is TypeInitializationException ||
                    exception is TargetInvocationException)
                {
                    exception = exception.InnerException;
                }
                else if (exception is AggregateException)
                {
                    // AggregateExceptions have a collection of inner exceptions, which may themselves be other
                    // wrapping exceptions (including nested AggregateExceptions).  Recursively walk this
                    // hierarchy.  The (singular) InnerException is included in the collection.
                    ReadOnlyCollection<Exception> innerExceptions = ((AggregateException)exception).InnerExceptions;
                    foreach (Exception innerException in innerExceptions)
                    {
                        if (IsFatal(innerException))
                        {
                            return true;
                        }
                    }

                    break;
                }
                else if (exception is NullReferenceException)
                {
                    ////MessagingClientEtwProvider.Provider.EventWriteNullReferenceErrorOccurred(exception.ToString());
                    break;
                }
                else
                {
                    break;
                }
            }

            return false;
        }

#if DEBUG
        internal static Type[] BreakOnExceptionTypes
        {
            get
            {
                if (!Fx.s_breakOnExceptionTypesRetrieved)
                {
                    if (TryGetDebugSwitch(out object value))
                    {
                        string[] typeNames = value as string[];
                        if (typeNames != null && typeNames.Length > 0)
                        {
                            var types = new List<Type>(typeNames.Length);
                            for (int i = 0; i < typeNames.Length; i++)
                            {
                                types.Add(Type.GetType(typeNames[i], false));
                            }
                            if (types.Count != 0)
                            {
                                Fx.s_breakOnExceptionTypesCache = types.ToArray();
                            }
                        }
                    }
                    Fx.s_breakOnExceptionTypesRetrieved = true;
                }
                return Fx.s_breakOnExceptionTypesCache;
            }
        }

        [SuppressMessage("Usage", "CA1801:Review unused parameters", Justification = "Unused parameters are inside of the DEBUG compilation flag.")]
        private static bool TryGetDebugSwitch(out object value)
        {
            // No registry access in UWP
            value = null;
            return false;
        }

#endif // DEBUG
        public static class Tag
        {
            public enum CacheAttrition
            {
                None,
                ElementOnTimer,

                // A finalizer/WeakReference based cache, where the elements are held by WeakReferences (or hold an
                // inner object by a WeakReference), and the weakly-referenced object has a finalizer which cleans the
                // item from the cache.
                ElementOnGC,

                // A cache that provides a per-element token, delegate, interface, or other piece of context that can
                // be used to remove the element (such as IDisposable).
                ElementOnCallback,

                FullPurgeOnTimer,
                FullPurgeOnEachAccess,
                PartialPurgeOnTimer,
                PartialPurgeOnEachAccess,
            }

            public enum Location
            {
                InProcess,
                OutOfProcess,
                LocalSystem,
                LocalOrRemoteSystem, // as in a file that might live on a share
                RemoteSystem,
            }

            public enum SynchronizationKind
            {
                LockStatement,
                MonitorWait,
                MonitorExplicit,
                InterlockedNoSpin,
                InterlockedWithSpin,

                // Same as LockStatement if the field type is object.
                FromFieldType,
            }

            [Flags]
            public enum BlocksUsing
            {
                MonitorEnter,
                MonitorWait,
                ManualResetEvent,
                AutoResetEvent,
                AsyncResult,
                IAsyncResult,
                PInvoke,
                InputQueue,
                ThreadNeutralSemaphore,
                PrivatePrimitive,
                OtherInternalPrimitive,
                OtherFrameworkPrimitive,
                OtherInterop,
                Other,

                NonBlocking, // For use by non-blocking SynchronizationPrimitives such as IOThreadScheduler
            }

            public static class Strings
            {
                internal const string ExternallyManaged = "externally managed";
                internal const string AppDomain = "AppDomain";
                internal const string DeclaringInstance = "instance of declaring class";
                internal const string Unbounded = "unbounded";
                internal const string Infinite = "infinite";
            }

            [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Constructor,
                AllowMultiple = true, Inherited = false)]
            [Conditional("CODE_ANALYSIS")]
            public sealed class ExternalResourceAttribute : Attribute
            {
                public ExternalResourceAttribute(Location location, string description)
                {
                    Location = location;
                    Description = description;
                }

                public Location Location { get; private set; }

                public string Description { get; private set; }
            }

            [AttributeUsage(AttributeTargets.Field)]
            [Conditional("CODE_ANALYSIS")]
            public sealed class CacheAttribute : Attribute
            {
                public CacheAttribute(Type elementType, CacheAttrition cacheAttrition)
                {
                    Scope = Strings.DeclaringInstance;
                    SizeLimit = Strings.Unbounded;
                    Timeout = Strings.Infinite;

                    if (elementType == null)
                    {
                        throw Fx.Exception.ArgumentNull(nameof(elementType));
                    }

                    ElementType = elementType;
                    CacheAttrition = cacheAttrition;
                }

                public Type ElementType { get; private set; }

                public CacheAttrition CacheAttrition { get; private set; }

                public string Scope { get; set; }
                public string SizeLimit { get; set; }
                public string Timeout { get; set; }
            }

            [AttributeUsage(AttributeTargets.Field)]
            [Conditional("CODE_ANALYSIS")]
            public sealed class QueueAttribute : Attribute
            {
                public QueueAttribute(Type elementType)
                {
                    Scope = Strings.DeclaringInstance;
                    SizeLimit = Strings.Unbounded;
                    ElementType = elementType ?? throw Fx.Exception.ArgumentNull(nameof(elementType));
                }

                public Type ElementType { get; private set; }

                public string Scope { get; set; }
                public string SizeLimit { get; set; }
                public bool StaleElementsRemovedImmediately { get; set; }
                public bool EnqueueThrowsIfFull { get; set; }
            }

            // Set on a class when that class uses lock (this) - acts as though it were on a field
            //     private object this;
            [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class, Inherited = false)]
            [Conditional("CODE_ANALYSIS")]
            public sealed class SynchronizationObjectAttribute : Attribute
            {
                public SynchronizationObjectAttribute()
                {
                    Blocking = true;
                    Scope = Strings.DeclaringInstance;
                    Kind = SynchronizationKind.FromFieldType;
                }

                public bool Blocking { get; set; }

                public string Scope { get; set; }

                public SynchronizationKind Kind { get; set; }
            }

            [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
            [Conditional("CODE_ANALYSIS")]
            public sealed class SynchronizationPrimitiveAttribute : Attribute
            {
                public SynchronizationPrimitiveAttribute(BlocksUsing blocksUsing)
                {
                    BlocksUsing = blocksUsing;
                }

                public BlocksUsing BlocksUsing { get; private set; }

                public bool SupportsAsync { get; set; }

                public bool Spins { get; set; }

                public string ReleaseMethod { get; set; }
            }

            [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
            [Conditional("CODE_ANALYSIS")]
            public sealed class BlockingAttribute : Attribute
            {
                public BlockingAttribute()
                {
                }

                public string CancelMethod { get; set; }

                public Type CancelDeclaringType { get; set; }

                public string Conditional { get; set; }
            }

            // Sometime a method will call a conditionally-blocking method in such a way that it is guaranteed
            // not to block (i.e. the condition can be Asserted false).  Such a method can be marked as
            // GuaranteeNonBlocking as an assertion that the method doesn't block despite calling a blocking method.
            //
            // Methods that don't call blocking methods and aren't marked as Blocking are assumed not to block, so
            // they do not require this attribute.
            [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
            [Conditional("CODE_ANALYSIS")]
            public sealed class GuaranteeNonBlockingAttribute : Attribute
            {
                public GuaranteeNonBlockingAttribute()
                {
                }
            }

            [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
            [Conditional("CODE_ANALYSIS")]
            public sealed class NonThrowingAttribute : Attribute
            {
                public NonThrowingAttribute()
                {
                }
            }

            [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
            [Conditional("CODE_ANALYSIS")]
            public sealed class InheritThrowsAttribute : Attribute
            {
                public InheritThrowsAttribute()
                {
                }

                public Type FromDeclaringType { get; set; }

                public string From { get; set; }
            }

            [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Module | AttributeTargets.Class |
                AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Constructor | AttributeTargets.Method |
                AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Interface |
                AttributeTargets.Delegate, AllowMultiple = false, Inherited = false)]
            [Conditional("CODE_ANALYSIS")]
            public sealed class SecurityNoteAttribute : Attribute
            {
                public SecurityNoteAttribute()
                {
                }

                public string Critical { get; set; }

                public string Safe { get; set; }

                public string Miscellaneous { get; set; }
            }
        }
    }
}
