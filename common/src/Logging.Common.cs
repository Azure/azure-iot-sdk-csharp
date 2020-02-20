// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Azure.Devices.Shared
{
    [SuppressMessage("Microsoft.Naming", "CA1724:TypeNamesShouldNotMatchNamespaces", Justification = "Conflicts with DotNetty.Common.Internal.Logging")]
    internal sealed partial class Logging : EventSource
    {
        /// <summary>The single event source instance to use for all logging.</summary>
        public static readonly Logging Log = new Logging();

        #region Metadata

        public static class Keywords
        {
            public const EventKeywords Default = (EventKeywords)0x0001;
            public const EventKeywords Debug = (EventKeywords)0x0002;
            public const EventKeywords EnterExit = (EventKeywords)0x0004;
        }

        // Common event reservations: [1, 10)
        private const int EnterEventId = 1;

        private const int ExitEventId = 2;
        private const int AssociateEventId = 3;
        private const int InfoEventId = 4;
        private const int ErrorEventId = 5;
        private const int CriticalFailureEventId = 6;
        private const int DumpArrayEventId = 7;

        // Provisioning event reservations: [10, 20)
        // IoT Hub event reservations: [20, 30)

        private const string MissingMember = "(?)";
        private const string NullInstance = "(null)";
        private const string NoParameters = "";
        private const int MaxDumpSize = 1024;

        #endregion Metadata

        #region Events

        #region Enter

#if !NET451

        /// <summary>Logs entrance to a method.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="message">A description of the entrance, including any arguments to the call.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Enter(object thisOrContextObject, string message = null, [CallerMemberName] string memberName = null)
        {
            DebugValidateArg(thisOrContextObject);
            DebugValidateArg(message);
            if (IsEnabled) Log.Enter(IdOf(thisOrContextObject), memberName, message ?? NoParameters);
        }

#endif

        /// <summary>Logs entrance to a method.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="arg0">The object to log.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Enter(object thisOrContextObject, object arg0, [CallerMemberName] string memberName = null)
        {
            DebugValidateArg(thisOrContextObject);
            DebugValidateArg(arg0);
            if (IsEnabled) Log.Enter(IdOf(thisOrContextObject), memberName, $"({Format(arg0)})");
        }

        /// <summary>Logs entrance to a method.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="arg0">The first object to log.</param>
        /// <param name="arg1">The second object to log.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Enter(object thisOrContextObject, object arg0, object arg1, [CallerMemberName] string memberName = null)
        {
            DebugValidateArg(thisOrContextObject);
            DebugValidateArg(arg0);
            DebugValidateArg(arg1);
            if (IsEnabled) Log.Enter(IdOf(thisOrContextObject), memberName, $"({Format(arg0)}, {Format(arg1)})");
        }

        /// <summary>Logs entrance to a method.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="arg0">The first object to log.</param>
        /// <param name="arg1">The second object to log.</param>
        /// <param name="arg2">The third object to log.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Enter(object thisOrContextObject, object arg0, object arg1, object arg2, [CallerMemberName] string memberName = null)
        {
            DebugValidateArg(thisOrContextObject);
            DebugValidateArg(arg0);
            DebugValidateArg(arg1);
            DebugValidateArg(arg2);
            if (IsEnabled) Log.Enter(IdOf(thisOrContextObject), memberName, $"({Format(arg0)}, {Format(arg1)}, {Format(arg2)})");
        }

        [Event(EnterEventId, Level = EventLevel.Informational, Keywords = Keywords.EnterExit)]
        private void Enter(string thisOrContextObject, string memberName, string parameters)
        {
            try
            {
                WriteEvent(EnterEventId, thisOrContextObject, memberName ?? MissingMember, TruncateForEtw(parameters));
            }
#if NETSTANDARD1_3 // bug in System.Diagnostics.Tracing
            catch (ArgumentNullException) { }
#endif
            catch (Exception)
            {
#if DEBUG
                throw;
#endif
            }
        }

        #endregion Enter

        #region Exit

#if !NET451

        /// <summary>Logs exit from a method.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="message">A description of the exit operation, including any return values.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Exit(object thisOrContextObject, string message = null, [CallerMemberName] string memberName = null)
        {
            DebugValidateArg(thisOrContextObject);
            DebugValidateArg(message);
            if (IsEnabled) Log.Exit(IdOf(thisOrContextObject), memberName, message ?? NoParameters);
        }

#endif

        /// <summary>Logs exit from a method.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="arg0">A return value from the member.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Exit(object thisOrContextObject, object arg0, [CallerMemberName] string memberName = null)
        {
            DebugValidateArg(thisOrContextObject);
            DebugValidateArg(arg0);
            if (IsEnabled) Log.Exit(IdOf(thisOrContextObject), memberName, Format(arg0).ToString());
        }

        /// <summary>Logs exit from a method.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="arg0">A return value from the member.</param>
        /// <param name="arg1">A second return value from the member.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Exit(object thisOrContextObject, object arg0, object arg1, [CallerMemberName] string memberName = null)
        {
            DebugValidateArg(thisOrContextObject);
            DebugValidateArg(arg0);
            DebugValidateArg(arg1);
            if (IsEnabled) Log.Exit(IdOf(thisOrContextObject), memberName, $"{Format(arg0)}, {Format(arg1)}");
        }

        /// <summary>Logs exit to a method.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="arg0">The first object to log.</param>
        /// <param name="arg1">The second object to log.</param>
        /// <param name="arg2">The third object to log.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Exit(object thisOrContextObject, object arg0, object arg1, object arg2, [CallerMemberName] string memberName = null)
        {
            DebugValidateArg(thisOrContextObject);
            DebugValidateArg(arg0);
            DebugValidateArg(arg1);
            DebugValidateArg(arg2);
            if (IsEnabled) Log.Exit(IdOf(thisOrContextObject), memberName, $"({Format(arg0)}, {Format(arg1)}, {Format(arg2)})");
        }

        [Event(ExitEventId, Level = EventLevel.Informational, Keywords = Keywords.EnterExit)]
        private void Exit(string thisOrContextObject, string memberName, string result)
        {
            try
            {
                WriteEvent(ExitEventId, thisOrContextObject, memberName ?? MissingMember, TruncateForEtw(result));
            }
#if NETSTANDARD1_3 // bug in System.Diagnostics.Tracing
            catch (ArgumentNullException) { }
#endif
            catch (Exception)
            {
#if DEBUG
                throw;
#endif
            }
        }

        #endregion Exit

        #region Info

#if !NET451

        /// <summary>Logs an information message.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="message">The message to be logged.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Info(object thisOrContextObject, string message = null, [CallerMemberName] string memberName = null)
        {
            DebugValidateArg(thisOrContextObject);
            DebugValidateArg(message);
            if (IsEnabled) Log.Info(IdOf(thisOrContextObject), memberName, message ?? NoParameters);
        }

#endif

        /// <summary>Logs an information message.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="message">The message to be logged.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Info(object thisOrContextObject, object message, [CallerMemberName] string memberName = null)
        {
            DebugValidateArg(thisOrContextObject);
            DebugValidateArg(message);
            if (IsEnabled) Log.Info(IdOf(thisOrContextObject), memberName, Format(message).ToString());
        }

        [Event(InfoEventId, Level = EventLevel.Informational, Keywords = Keywords.Default)]
        private void Info(string thisOrContextObject, string memberName, string message)
        {
            try
            {
                WriteEvent(InfoEventId, thisOrContextObject, memberName ?? MissingMember, TruncateForEtw(message));
            }
#if NETSTANDARD1_3 // bug in System.Diagnostics.Tracing
            catch (ArgumentNullException) { }
#endif
            catch (Exception)
            {
#if DEBUG
                throw;
#endif
            }
        }

        #endregion Info

        #region Error

#if !NET451

        /// <summary>Logs an error message.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="message">The message to be logged.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Error(object thisOrContextObject, string message, [CallerMemberName] string memberName = null)
        {
            DebugValidateArg(thisOrContextObject);
            DebugValidateArg(message);
            if (IsEnabled) Log.ErrorMessage(IdOf(thisOrContextObject), memberName, message);
        }

#endif

        /// <summary>Logs an error message.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="message">The message to be logged.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Error(object thisOrContextObject, object message, [CallerMemberName] string memberName = null)
        {
            DebugValidateArg(thisOrContextObject);
            DebugValidateArg(message);
            if (IsEnabled) Log.ErrorMessage(IdOf(thisOrContextObject), memberName, Format(message).ToString());
        }

        [Event(ErrorEventId, Level = EventLevel.Warning, Keywords = Keywords.Default)]
        private void ErrorMessage(string thisOrContextObject, string memberName, string message)
        {
            try
            {
                WriteEvent(ErrorEventId, thisOrContextObject, memberName ?? MissingMember, TruncateForEtw(message));
            }
#if NETSTANDARD1_3 // bug in System.Diagnostics.Tracing
            catch (ArgumentNullException) { }
#endif
            catch (Exception)
            {
#if DEBUG
                throw;
#endif
            }
        }

        #endregion Error

        #region Fail

#if !NET451

        /// <summary>Logs a fatal error and raises an assert.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="message">The message to be logged.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Fail(object thisOrContextObject, string message, [CallerMemberName] string memberName = null)
        {
            // Don't call DebugValidateArg on args, as we expect Fail to be used in assert/failure situations
            // that should never happen in production, and thus we don't care about extra costs.

            if (IsEnabled) Log.CriticalFailure(IdOf(thisOrContextObject), memberName, message);
            Debug.Fail(message, $"{IdOf(thisOrContextObject)}.{memberName}");
        }

#endif

        /// <summary>Logs a fatal error and raises an assert.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="message">The message to be logged.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Fail(object thisOrContextObject, object message, [CallerMemberName] string memberName = null)
        {
            // Don't call DebugValidateArg on args, as we expect Fail to be used in assert/failure situations
            // that should never happen in production, and thus we don't care about extra costs.

            if (IsEnabled) Log.CriticalFailure(IdOf(thisOrContextObject), memberName, Format(message).ToString());
            Debug.Fail(Format(message).ToString(), $"{IdOf(thisOrContextObject)}.{memberName}");
        }

        [Event(CriticalFailureEventId, Level = EventLevel.Critical, Keywords = Keywords.Debug)]
        private void CriticalFailure(string thisOrContextObject, string memberName, string message)
        {
            try
            {
                WriteEvent(CriticalFailureEventId, thisOrContextObject, memberName ?? MissingMember, TruncateForEtw(message));
            }
#if NETSTANDARD1_3 // bug in System.Diagnostics.Tracing
            catch (ArgumentNullException) { }
#endif
            catch (Exception)
            {
#if DEBUG
                throw;
#endif
            }
        }

        #endregion Fail

        #region DumpBuffer

        /// <summary>Logs the contents of a buffer.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="buffer">The buffer to be logged.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void DumpBuffer(object thisOrContextObject, byte[] buffer, [CallerMemberName] string memberName = null)
        {
            DumpBuffer(thisOrContextObject, buffer, 0, buffer.Length, memberName);
        }

        /// <summary>Logs the contents of a buffer.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="buffer">The buffer to be logged.</param>
        /// <param name="offset">The starting offset from which to log.</param>
        /// <param name="count">The number of bytes to log.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void DumpBuffer(object thisOrContextObject, byte[] buffer, int offset, int count, [CallerMemberName] string memberName = null)
        {
            if (IsEnabled)
            {
                if (offset < 0 || offset > buffer.Length - count)
                {
                    Fail(thisOrContextObject, $"Invalid {nameof(DumpBuffer)} Args. Length={buffer.Length}, Offset={offset}, Count={count}", memberName);
                    return;
                }

                count = Math.Min(count, MaxDumpSize);

                byte[] slice = buffer;
                if (offset != 0 || count != buffer.Length)
                {
                    slice = new byte[count];
                    Buffer.BlockCopy(buffer, offset, slice, 0, count);
                }

                Log.DumpBuffer(IdOf(thisOrContextObject), memberName, slice);
            }
        }

        /// <summary>Logs the contents of a buffer.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="bufferPtr">The starting location of the buffer to be logged.</param>
        /// <param name="count">The number of bytes to log.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static unsafe void DumpBuffer(object thisOrContextObject, IntPtr bufferPtr, int count, [CallerMemberName] string memberName = null)
        {
            Debug.Assert(bufferPtr != IntPtr.Zero);
            Debug.Assert(count >= 0);

#if !NET451
            if (IsEnabled)
            {
                var buffer = new byte[Math.Min(count, MaxDumpSize)];
                fixed (byte* targetPtr = buffer)
                {
                    Buffer.MemoryCopy((byte*)bufferPtr, targetPtr, buffer.Length, buffer.Length);
                }
                Log.DumpBuffer(IdOf(thisOrContextObject), memberName, buffer);
            }
#endif
        }

        [Event(DumpArrayEventId, Level = EventLevel.Verbose, Keywords = Keywords.Debug)]
        private unsafe void DumpBuffer(string thisOrContextObject, string memberName, byte[] buffer)
        {
            try
            {
                WriteEvent(DumpArrayEventId, thisOrContextObject, memberName ?? MissingMember, buffer);
            }
#if NETSTANDARD1_3 // bug in System.Diagnostics.Tracing
            catch (ArgumentNullException) { }
#endif
            catch (Exception)
            {
#if DEBUG
                throw;
#endif
            }
        }

        #endregion DumpBuffer

        #region Associate

        /// <summary>Logs a relationship between two objects.</summary>
        /// <param name="first">The first object.</param>
        /// <param name="second">The second object.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Associate(object first, object second, [CallerMemberName] string memberName = null)
        {
            DebugValidateArg(first);
            DebugValidateArg(second);
            if (IsEnabled) Log.Associate(IdOf(first), memberName, IdOf(first), IdOf(second));
        }

        /// <summary>Logs a relationship between two objects.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="first">The first object.</param>
        /// <param name="second">The second object.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Associate(object thisOrContextObject, object first, object second, [CallerMemberName] string memberName = null)
        {
            DebugValidateArg(thisOrContextObject);
            DebugValidateArg(first);
            DebugValidateArg(second);
            if (IsEnabled) Log.Associate(IdOf(thisOrContextObject), memberName, IdOf(first), IdOf(second));
        }

        [Event(AssociateEventId, Level = EventLevel.Informational, Keywords = Keywords.Default, Message = "[{2}]<-->[{3}]")]
        private void Associate(string thisOrContextObject, string memberName, string first, string second)
        {
            try
            {
                WriteEvent(AssociateEventId, thisOrContextObject, memberName ?? MissingMember, first, second);
            }
#if NETSTANDARD1_3 // bug in System.Diagnostics.Tracing
            catch (ArgumentNullException) { }
#endif
            catch (Exception)
            {
#if DEBUG
                throw;
#endif
            }
        }

        #endregion Associate

        #endregion Events

        #region Helpers

        private static void DebugValidateArg(object arg)
        {
            if (!IsEnabled)
            {
                Debug.Assert(!(arg is ValueType), $"Should not be passing value type {arg?.GetType()} to logging without IsEnabled check");
            }
        }

        public static new bool IsEnabled => Log.IsEnabled();

        [NonEvent]
        public static string IdOf(object value) => value != null ? value.GetType().Name + "#" + GetHashCode(value) : NullInstance;

        [NonEvent]
        public static int GetHashCode(object value) => value?.GetHashCode() ?? 0;

        [NonEvent]
        public static object Format(object value)
        {
            // If it's null, return a known string for null values
            if (value == null)
            {
                return NullInstance;
            }

            // Give another partial implementation a chance to provide its own string representation
            string result = null;
            AdditionalCustomizedToString(value, ref result);
            if (result != null)
            {
                return result;
            }

            // Format arrays with their element type name and length
            var arr = value as Array;
            if (arr != null)
            {
                return $"{arr.GetType().GetElementType()}[{((Array)value).Length}]";
            }

            // Format ICollections as the name and count
            var c = value as ICollection;
            if (c != null)
            {
                return $"{c.GetType().Name}({c.Count})";
            }

            // Format SafeHandles as their type, hash code, and pointer value
            var handle = value as SafeHandle;
            if (handle != null)
            {
                return $"{handle.GetType().Name}:{handle.GetHashCode()}(0x{handle.DangerousGetHandle():X})";
            }

            // Format IntPtrs as hex
            if (value is IntPtr)
            {
                return $"0x{value:X}";
            }

            // If the string representation of the instance would just be its type name,
            // use its id instead.
            string toString = value.ToString();
            if (toString == null || toString == value.GetType().FullName)
            {
                return IdOf(value);
            }

            // Otherwise, return the original object so that the caller does default formatting.
            return value;
        }

        static partial void AdditionalCustomizedToString<T>(T value, ref string result);

        #endregion Helpers

        #region Custom WriteEvent overloads

        [NonEvent]
        private unsafe void WriteEvent(int eventId, string arg1, string arg2, string arg3, string arg4)
        {
            if (IsEnabled())
            {
                if (arg1 == null) arg1 = "";
                if (arg2 == null) arg2 = "";
                if (arg3 == null) arg3 = "";
                if (arg4 == null) arg4 = "";

                fixed (char* string1Bytes = arg1)
                fixed (char* string2Bytes = arg2)
                fixed (char* string3Bytes = arg3)
                fixed (char* string4Bytes = arg4)
                {
                    const int numEventDatas = 4;
                    var descrs = stackalloc EventData[numEventDatas];

                    descrs[0].DataPointer = (IntPtr)string1Bytes;
                    descrs[0].Size = (arg1.Length + 1) * 2;

                    descrs[1].DataPointer = (IntPtr)string2Bytes;
                    descrs[1].Size = (arg2.Length + 1) * 2;

                    descrs[2].DataPointer = (IntPtr)string3Bytes;
                    descrs[2].Size = (arg3.Length + 1) * 2;

                    descrs[3].DataPointer = (IntPtr)string4Bytes;
                    descrs[3].Size = (arg4.Length + 1) * 2;

                    WriteEventCore(eventId, numEventDatas, descrs);
                }
            }
        }

        [NonEvent]
        private unsafe void WriteEvent(int eventId, string arg1, string arg2, byte[] arg3)
        {
            if (IsEnabled())
            {
                if (arg1 == null) arg1 = "";
                if (arg2 == null) arg2 = "";
#if !NET451
                if (arg3 == null) arg3 = Array.Empty<byte>();
#else
                if (arg3 == null) arg3 = new byte[0];
#endif

                fixed (char* arg1Ptr = arg1)
                fixed (char* arg2Ptr = arg2)
                fixed (byte* arg3Ptr = arg3)
                {
                    int bufferLength = arg3.Length;
                    const int numEventDatas = 4;
                    var descrs = stackalloc EventData[numEventDatas];

                    descrs[0].DataPointer = (IntPtr)arg1Ptr;
                    descrs[0].Size = (arg1.Length + 1) * sizeof(char);

                    descrs[1].DataPointer = (IntPtr)arg2Ptr;
                    descrs[1].Size = (arg2.Length + 1) * sizeof(char);

                    descrs[2].DataPointer = (IntPtr)(&bufferLength);
                    descrs[2].Size = 4;

                    descrs[3].DataPointer = (IntPtr)arg3Ptr;
                    descrs[3].Size = bufferLength;

                    WriteEventCore(eventId, numEventDatas, descrs);
                }
            }
        }

        [NonEvent]
        private unsafe void WriteEvent(int eventId, string arg1, int arg2, int arg3, int arg4)
        {
            if (IsEnabled())
            {
                if (arg1 == null) arg1 = "";

                fixed (char* arg1Ptr = arg1)
                {
                    const int numEventDatas = 4;
                    var descrs = stackalloc EventData[numEventDatas];

                    descrs[0].DataPointer = (IntPtr)(arg1Ptr);
                    descrs[0].Size = (arg1.Length + 1) * sizeof(char);

                    descrs[1].DataPointer = (IntPtr)(&arg2);
                    descrs[1].Size = sizeof(int);

                    descrs[2].DataPointer = (IntPtr)(&arg3);
                    descrs[2].Size = sizeof(int);

                    descrs[3].DataPointer = (IntPtr)(&arg4);
                    descrs[3].Size = sizeof(int);

                    WriteEventCore(eventId, numEventDatas, descrs);
                }
            }
        }

        [NonEvent]
        private unsafe void WriteEvent(int eventId, string arg1, int arg2, string arg3)
        {
            if (IsEnabled())
            {
                if (arg1 == null) arg1 = "";
                if (arg3 == null) arg3 = "";

                fixed (char* arg1Ptr = arg1)
                fixed (char* arg3Ptr = arg3)
                {
                    const int numEventDatas = 3;
                    var descrs = stackalloc EventData[numEventDatas];

                    descrs[0].DataPointer = (IntPtr)arg1Ptr;
                    descrs[0].Size = (arg1.Length + 1) * sizeof(char);

                    descrs[1].DataPointer = (IntPtr)(&arg2);
                    descrs[1].Size = sizeof(int);

                    descrs[2].DataPointer = (IntPtr)arg3Ptr;
                    descrs[2].Size = (arg3.Length + 1) * sizeof(char);

                    WriteEventCore(eventId, numEventDatas, descrs);
                }
            }
        }

        [NonEvent]
        private unsafe void WriteEvent(int eventId, string arg1, string arg2, int arg3)
        {
            if (IsEnabled())
            {
                if (arg1 == null) arg1 = "";
                if (arg2 == null) arg2 = "";

                fixed (char* arg1Ptr = arg1)
                fixed (char* arg2Ptr = arg2)
                {
                    const int numEventDatas = 3;
                    var descrs = stackalloc EventData[numEventDatas];

                    descrs[0].DataPointer = (IntPtr)arg1Ptr;
                    descrs[0].Size = (arg1.Length + 1) * sizeof(char);

                    descrs[1].DataPointer = (IntPtr)arg2Ptr;
                    descrs[1].Size = (arg2.Length + 1) * sizeof(char);

                    descrs[2].DataPointer = (IntPtr)(&arg3);
                    descrs[2].Size = sizeof(int);

                    WriteEventCore(eventId, numEventDatas, descrs);
                }
            }
        }

        [NonEvent]
        private unsafe void WriteEvent(int eventId, string arg1, string arg2, string arg3, int arg4)
        {
            if (IsEnabled())
            {
                if (arg1 == null) arg1 = "";
                if (arg2 == null) arg2 = "";
                if (arg3 == null) arg3 = "";

                fixed (char* arg1Ptr = arg1)
                fixed (char* arg2Ptr = arg2)
                fixed (char* arg3Ptr = arg3)
                {
                    const int numEventDatas = 4;
                    var descrs = stackalloc EventData[numEventDatas];

                    descrs[0].DataPointer = (IntPtr)arg1Ptr;
                    descrs[0].Size = (arg1.Length + 1) * sizeof(char);

                    descrs[1].DataPointer = (IntPtr)arg2Ptr;
                    descrs[1].Size = (arg2.Length + 1) * sizeof(char);

                    descrs[2].DataPointer = (IntPtr)arg3Ptr;
                    descrs[2].Size = (arg3.Length + 1) * sizeof(char);

                    descrs[3].DataPointer = (IntPtr)(&arg4);
                    descrs[3].Size = sizeof(int);

                    WriteEventCore(eventId, numEventDatas, descrs);
                }
            }
        }

        #endregion Custom WriteEvent overloads

        private static string TruncateForEtw(string value)
        {
            // https://github.com/dotnet/runtime/issues/16844
            // Max size is 64K, but includes all info, so limit message to less
            const int MaxEtwMessageSize = 60 * 1024;

            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= MaxEtwMessageSize
                ? value
                : value.Substring(0, MaxEtwMessageSize);
        }
    }
}
