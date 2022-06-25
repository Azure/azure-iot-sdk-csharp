// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Microsoft.Azure.Devices.Common
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
                AssertAndThrow(description);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Exception AssertAndThrow(string description)
        {
            Assert(description);
            throw Exception.AsError(new AssertionFailedException(description));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Exception AssertAndThrowFatal(string description)
        {
            Assert(description);
            throw Exception.AsError(new FatalException(description));
        }

        public static bool IsFatal(Exception exception)
        {
            while (exception != null)
            {
                // FYI, CallbackException is-a FatalException
                if (exception is FatalException || exception is OutOfMemoryException)
                {
                    return true;
                }

                if (exception is NullReferenceException)
                {
                    return true;
                }

                // These exceptions aren't themselves fatal, but since the CLR uses them to wrap other exceptions,
                // we want to check to see whether they've been used to wrap a fatal exception.  If so, then they
                // count as fatal.
                if (exception is TypeInitializationException
                    || exception is TargetInvocationException)
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
                if (!s_breakOnExceptionTypesRetrieved)
                {
                    if (TryGetDebugSwitch(out object value))
                    {
                        if (value is string[] typeNames && typeNames.Length > 0)
                        {
                            var types = new List<Type>(typeNames.Length);
                            for (int i = 0; i < typeNames.Length; i++)
                            {
                                types.Add(Type.GetType(typeNames[i], false));
                            }

                            if (types.Count != 0)
                            {
                                s_breakOnExceptionTypesCache = types.ToArray();
                            }
                        }
                    }
                    s_breakOnExceptionTypesRetrieved = true;
                }
                return s_breakOnExceptionTypesCache;
            }
        }

        private static bool TryGetDebugSwitch(out object value)
        {
            value = null;
            return value != null;
        }

#endif

        public static class Tag
        {
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

            [AttributeUsage(AttributeTargets.Field)]
            [Conditional("CODE_ANALYSIS")]
            public sealed class QueueAttribute : Attribute
            {
                public QueueAttribute(Type elementType)
                {
                    Scope = Strings.DeclaringInstance;
                    SizeLimit = Strings.Unbounded;
                    ElementType = elementType ?? throw Exception.ArgumentNull(nameof(elementType));
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
