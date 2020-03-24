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

#if NET451
using System.Runtime.ConstrainedExecution;
using System.Transactions;
using Microsoft.Win32;
#endif

namespace Microsoft.Azure.Devices.Common
{
    internal static class Fx
    {
        // This is only used for EventLog Source therefore matching EventLog source rather than ETL source
        private const string DefaultEventSource = "Microsoft.IotHub";

#if DEBUG
#if NET451
        private const string SBRegistryKey = @"SOFTWARE\Microsoft\IotHub\v2.0";
#endif
        private const string AssertsFailFastName = "AssertsFailFast";
        private const string BreakOnExceptionTypesName = "BreakOnExceptionTypes";
        private const string FastDebugName = "FastDebug";

        private static bool s_breakOnExceptionTypesRetrieved;
        private static Type[] s_breakOnExceptionTypesCache;
        private static bool s_fastDebugRetrieved;
        private static bool s_fastDebugCache;
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

        public static void AssertIsNotNull(object objectMayBeNull, string description)
        {
            if (objectMayBeNull == null)
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

        public static void AssertAndThrowFatal(bool condition, string description)
        {
            if (!condition)
            {
                AssertAndThrowFatal(description);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Exception AssertAndThrowFatal(string description)
        {
            Assert(description);
            throw Exception.AsError(new FatalException(description));
        }

#if NET451
        public static void AssertAndFailFastService(bool condition, string description)
        {
            if (!condition)
            {
                AssertAndFailFastService(description);
            }
        }

        // This never returns.  The Exception return type lets you write 'throw AssertAndFailFast()' which tells the compiler/tools that
        // execution stops.
        [Tag.SecurityNote(Critical = "Calls into critical method Environment.FailFast",
            Safe = "The side affect of the app crashing is actually intended here")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Exception AssertAndFailFastService(string description)
        {
            Assert(description);
            string failFastMessage = CommonResources.GetString(CommonResources.FailFastMessage, description);

            // The catch is here to force the finally to run, as finallys don't run until the stack walk gets to a catch.
            // The catch makes sure that the finally will run before the stack-walk leaves the frame, but the code inside is impossible to reach.
            try
            {
                try
                {
                    ////MessagingClientEtwProvider.Provider.EventWriteFailFastOccurred(description);
                    Exception.TraceFailFast(failFastMessage);

                    // Mark that a FailFast is in progress, so that we can take ourselves out of the NLB if for
                    // any reason we can't kill ourselves quickly.  Wait 15 seconds so this state gets picked up for sure.
                    FailFastInProgress = true;

                    Thread.Sleep(TimeSpan.FromSeconds(15));
                }
                finally
                {
                    // ########################## NOTE ###########################
                    // Environment.FailFast does not collect crash dumps when used in Azure services.
                    // Environment.FailFast(failFastMessage);

                    // ################## WORKAROUND #############################
                    // Workaround for the issue above. Throwing an unhandled exception on a separate thread to trigger process crash and crash dump collection
                    // Throwing FatalException since our service does not morph/eat up fatal exceptions
                    // We should find the tracking bug in Azure for this issue, and remove the workaround when fixed by Azure
                    Thread failFastWorkaroundThread = new Thread(delegate ()
                    {
                        throw new FatalException(failFastMessage);
                    });

                    failFastWorkaroundThread.Start();
                    failFastWorkaroundThread.Join();
                }
            }
            catch
            {
                throw;
            }

            return null; // we'll never get here since we've just fail-fasted
        }
#endif

        internal static bool FailFastInProgress { get; private set; }

        public static bool IsFatal(Exception exception)
        {
            while (exception != null)
            {
                // FYI, CallbackException is-a FatalException
                if (exception is FatalException || exception is OutOfMemoryException)
                {
#if !NET451
                    return true;
#else
                    if (!(exception is InsufficientMemoryException))
                    {
                        return true;
                    }
#endif
                }

#if NET451
                if (exception is ThreadAbortException
                    || exception is AccessViolationException
                    || exception is SEHException)
                {
                    return true;
                }
#endif

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

#if NET451
        // If the transaction has aborted then we switch over to a new transaction
        // which we will immediately abort after setting Transaction.Current
        public static TransactionScope CreateTransactionScope(Transaction transaction)
        {
            try
            {
                return transaction == null ? null : new TransactionScope(transaction);
            }
            catch (TransactionAbortedException)
            {
                var tempTransaction = new CommittableTransaction();
                try
                {
                    return new TransactionScope(tempTransaction.Clone());
                }
                finally
                {
                    tempTransaction.Rollback();
                }
            }
        }

        public static void CompleteTransactionScope(ref TransactionScope scope)
        {
            TransactionScope localScope = scope;
            if (localScope != null)
            {
                scope = null;
                try
                {
                    localScope.Complete();
                }
                finally
                {
                    localScope.Dispose();
                }
            }
        }
#endif

#if NET451
        [Tag.SecurityNote(Critical = "Construct the unsafe object IOCompletionThunk")]
        [SecurityCritical]
        public static IOCompletionCallback ThunkCallback(IOCompletionCallback callback)
        {
            return new IOCompletionThunk(callback).ThunkFrame;
        }
#endif

#if NET451
        public static TransactionCompletedEventHandler ThunkTransactionEventHandler(TransactionCompletedEventHandler handler)
        {
            return new TransactionEventHandlerThunk(handler).ThunkFrame;
        }
#endif
#if DEBUG

        internal static bool AssertsFailFast
        {
            get
            {
                object value;
                return TryGetDebugSwitch(AssertsFailFastName, out value) &&
#if !NET451
                        typeof(int).GetTypeInfo().IsAssignableFrom(value.GetType().GetTypeInfo())
#else
                        typeof(int).IsAssignableFrom(value.GetType())
#endif
                    && ((int)value) != 0;
            }
        }

        internal static Type[] BreakOnExceptionTypes
        {
            get
            {
                if (!s_breakOnExceptionTypesRetrieved)
                {
                    if (TryGetDebugSwitch(BreakOnExceptionTypesName, out object value))
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

        internal static bool FastDebug
        {
            get
            {
                if (!s_fastDebugRetrieved)
                {
                    if (TryGetDebugSwitch(FastDebugName, out object value))
                    {
#if !NET451
                        s_fastDebugCache = typeof(int).GetTypeInfo().IsAssignableFrom(value.GetType().GetTypeInfo())
#else
                        s_fastDebugCache = typeof(int).IsAssignableFrom(value.GetType())
#endif
                            && ((int)value) != 0;
                    }

                    s_fastDebugRetrieved = true;
                    ////MessagingClientEtwProvider.Provider.EventWriteLogAsWarning(string.Format(CultureInfo.InvariantCulture, "AppDomain({0}).FastDebug={1}", AppDomain.CurrentDomain.FriendlyName, fastDebugCache.ToString()));
                }

                return s_fastDebugCache;
            }
        }

        private static bool TryGetDebugSwitch(string name, out object value)
        {
            value = null;
#if NET451
            try
            {
                RegistryKey key = Registry.LocalMachine.OpenSubKey(SBRegistryKey);
                if (key != null)
                {
                    using (key)
                    {
                        value = key.GetValue(name);
                    }
                }
            }
            catch (SecurityException)
            {
                // This debug-only code shouldn't trace.
            }
#endif
            return value != null;
        }

#endif

        [SuppressMessage(FxCop.Category.Design, FxCop.Rule.DoNotCatchGeneralExceptionTypes,
            Justification = "Don't want to hide the exception which is about to crash the process.")]
#if NET451
        [Tag.SecurityNote(Miscellaneous = "Must not call into PT code as it is called within a CER.")]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
#endif
        private static void TraceExceptionNoThrow(Exception exception)
        {
            try
            {
                // This call exits the CER.  However, when still inside a catch, normal ThreadAbort is prevented.
                // Rude ThreadAbort will still be allowed to terminate processing.
                Exception.TraceUnhandled(exception);
            }
            catch
            {
                // This empty catch is only acceptable because we are a) in a CER and b) processing an exception
                // which is about to crash the process anyway.
            }
        }

        [SuppressMessage(FxCop.Category.Design, FxCop.Rule.DoNotCatchGeneralExceptionTypes,
            Justification = "Don't want to hide the exception which is about to crash the process.")]
        [SuppressMessage(FxCop.Category.ReliabilityBasic, FxCop.Rule.IsFatalRule,
            Justification = "Don't want to hide the exception which is about to crash the process.")]
        [Tag.SecurityNote(Miscellaneous = "Must not call into PT code as it is called within a CER.")]
#if NET451
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
#endif
        private static bool HandleAtThreadBase(Exception exception)
        {
            // This area is too sensitive to do anything but return.
            if (exception == null)
            {
                Assert("Null exception in HandleAtThreadBase.");
                return false;
            }

            TraceExceptionNoThrow(exception);

            return false;
        }

#if NET451
        // This can't derive from Thunk since T would be unsafe.
        [Tag.SecurityNote(Critical = "unsafe object")]
        [SecurityCritical]
        private sealed unsafe class IOCompletionThunk
        {
            [Tag.SecurityNote(Critical = "Make these safe to use in SecurityCritical contexts.")]
            private readonly IOCompletionCallback _callback;

            [Tag.SecurityNote(Critical = "Accesses critical field.", Safe = "Data provided by caller.")]
            public IOCompletionThunk(IOCompletionCallback callback)
            {
                _callback = callback;
            }

            public IOCompletionCallback ThunkFrame
            {
                [Tag.SecurityNote(Safe = "returns a delegate around the safe method UnhandledExceptionFrame")]
                get => new IOCompletionCallback(UnhandledExceptionFrame);
            }

            [Tag.SecurityNote(Critical = "Accesses critical field, calls PrepareConstrainedRegions which has a LinkDemand",
                Safe = "Delegates can be invoked, guaranteed not to call into PT user code from the finally.")]
            private void UnhandledExceptionFrame(uint error, uint bytesRead, NativeOverlapped* nativeOverlapped)
            {
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _callback(error, bytesRead, nativeOverlapped);
                }
                catch (Exception exception)
                {
                    if (!HandleAtThreadBase(exception))
                    {
                        throw;
                    }
                }
            }
        }
#endif

#if NET451
        private sealed class TransactionEventHandlerThunk
        {
            readonly TransactionCompletedEventHandler _callback;

            public TransactionEventHandlerThunk(TransactionCompletedEventHandler callback)
            {
                _callback = callback;
            }

            public TransactionCompletedEventHandler ThunkFrame => new TransactionCompletedEventHandler(UnhandledExceptionFrame);

            private void UnhandledExceptionFrame(object sender, TransactionEventArgs args)
            {
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _callback(sender, args);
                }
                catch (Exception exception)
                {
                    throw AssertAndFailFastService(exception.ToString());
                }
            }
        }
#endif

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
                    ElementType = elementType ?? throw Exception.ArgumentNull(nameof(elementType));
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

            [SuppressMessage(FxCop.Category.Performance, "CA1813:AvoidUnsealedAttributes",
                Justification = "This is intended to be an attribute heirarchy. It does not affect product perf.")]
            [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor,
                AllowMultiple = true, Inherited = false)]
            [Conditional("CODE_ANALYSIS")]
            public class ThrowsAttribute : Attribute
            {
                public ThrowsAttribute(Type exceptionType, string diagnosis)
                {
                    if (string.IsNullOrEmpty(diagnosis))
                    {
                        throw Exception.ArgumentNullOrEmpty(nameof(diagnosis));
                    }

                    ExceptionType = exceptionType ?? throw Exception.ArgumentNull(nameof(exceptionType));
                    Diagnosis = diagnosis;
                }

                public Type ExceptionType { get; private set; }

                public string Diagnosis { get; private set; }
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
