// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Globalization;
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
        // IoT hub event reservations: [20, 30)

        private const string MissingMember = "(?)";
        private const string NullInstance = "(null)";
        private const int MaxDumpSize = 1024;
#if !NET451
        private const string NoParameters = "";
#endif

        #endregion Metadata

        #region Events

        #region Enter

#if !NET451

        /// <summary>Logs entrance to a method.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="formattableString">A description of the entrance, including any arguments to the call.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Enter(object thisOrContextObject, FormattableString formattableString = null, [CallerMemberName] string memberName = null)
        {
            if (IsEnabled)
            {
                DebugValidateArg(thisOrContextObject);
                DebugValidateArg(formattableString);

                Log.Enter(IdOf(thisOrContextObject), memberName, formattableString != null ? Format(formattableString) : NoParameters);
            }
        }

#endif

        /// <summary>Logs entrance to a method.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="arg0">The object to log.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Enter(object thisOrContextObject, object arg0, [CallerMemberName] string memberName = null)
        {
            if (IsEnabled)
            {
                DebugValidateArg(thisOrContextObject);
                DebugValidateArg(arg0);

                Log.Enter(IdOf(thisOrContextObject), memberName, $"({Format(arg0)})");
            }
        }

        /// <summary>Logs entrance to a method.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="arg0">The first object to log.</param>
        /// <param name="arg1">The second object to log.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Enter(object thisOrContextObject, object arg0, object arg1, [CallerMemberName] string memberName = null)
        {
            if (IsEnabled)
            {
                DebugValidateArg(thisOrContextObject);
                DebugValidateArg(arg0);
                DebugValidateArg(arg1);

                Log.Enter(IdOf(thisOrContextObject), memberName, $"({Format(arg0)}, {Format(arg1)})");
            }
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
            if (IsEnabled)
            {
                DebugValidateArg(thisOrContextObject);
                DebugValidateArg(arg0);
                DebugValidateArg(arg1);
                DebugValidateArg(arg2);

                Log.Enter(IdOf(thisOrContextObject), memberName, $"({Format(arg0)}, {Format(arg1)}, {Format(arg2)})");
            }
        }

        [Event(EnterEventId, Level = EventLevel.Informational, Keywords = Keywords.EnterExit)]
        private void Enter(string thisOrContextObject, string memberName, string parameters) =>
            WriteEvent(EnterEventId, thisOrContextObject, memberName ?? MissingMember, parameters);

        #endregion Enter

        #region Exit

#if !NET451

        /// <summary>Logs exit from a method.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="formattableString">A description of the exit operation, including any return values.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Exit(object thisOrContextObject, FormattableString formattableString = null, [CallerMemberName] string memberName = null)
        {
            if (IsEnabled)
            {
                DebugValidateArg(thisOrContextObject);
                DebugValidateArg(formattableString);

                Log.Exit(IdOf(thisOrContextObject), memberName, formattableString != null ? Format(formattableString) : NoParameters);
            }
        }

#endif

        /// <summary>Logs exit from a method.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="arg0">A return value from the member.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Exit(object thisOrContextObject, object arg0, [CallerMemberName] string memberName = null)
        {
            if (IsEnabled)
            {
                DebugValidateArg(thisOrContextObject);
                DebugValidateArg(arg0);

                Log.Exit(IdOf(thisOrContextObject), memberName, Format(arg0).ToString());
            }
        }

        /// <summary>Logs exit from a method.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="arg0">A return value from the member.</param>
        /// <param name="arg1">A second return value from the member.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Exit(object thisOrContextObject, object arg0, object arg1, [CallerMemberName] string memberName = null)
        {
            if (IsEnabled)
            {
                DebugValidateArg(thisOrContextObject);
                DebugValidateArg(arg0);
                DebugValidateArg(arg1);

                Log.Exit(IdOf(thisOrContextObject), memberName, $"{Format(arg0)}, {Format(arg1)}");
            }
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
            if (IsEnabled)
            {
                DebugValidateArg(thisOrContextObject);
                DebugValidateArg(arg0);
                DebugValidateArg(arg1);
                DebugValidateArg(arg2);

                Log.Exit(IdOf(thisOrContextObject), memberName, $"({Format(arg0)}, {Format(arg1)}, {Format(arg2)})");
            }
        }

        [Event(ExitEventId, Level = EventLevel.Informational, Keywords = Keywords.EnterExit)]
        private void Exit(string thisOrContextObject, string memberName, string result) =>
            WriteEvent(ExitEventId, thisOrContextObject, memberName ?? MissingMember, result);

        #endregion Exit

        #region Info

#if !NET451

        /// <summary>Logs an information message.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="formattableString">The message to be logged.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Info(object thisOrContextObject, FormattableString formattableString = null, [CallerMemberName] string memberName = null)
        {
            if (IsEnabled)
            {
                DebugValidateArg(thisOrContextObject);
                DebugValidateArg(formattableString);

                Log.Info(IdOf(thisOrContextObject), memberName, formattableString != null ? Format(formattableString) : NoParameters);
            }
        }

#endif

        /// <summary>Logs an information message.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="message">The message to be logged.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Info(object thisOrContextObject, object message, [CallerMemberName] string memberName = null)
        {
            if (IsEnabled)
            {
                DebugValidateArg(thisOrContextObject);
                DebugValidateArg(message);

                Log.Info(IdOf(thisOrContextObject), memberName, Format(message).ToString());
            }
        }

        [Event(InfoEventId, Level = EventLevel.Informational, Keywords = Keywords.Default)]
        private void Info(string thisOrContextObject, string memberName, string message) =>
            WriteEvent(InfoEventId, thisOrContextObject, memberName ?? MissingMember, message);

        #endregion Info

        #region Error

#if !NET451

        /// <summary>Logs an error message.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="formattableString">The message to be logged.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Error(object thisOrContextObject, FormattableString formattableString, [CallerMemberName] string memberName = null)
        {
            if (IsEnabled)
            {
                DebugValidateArg(thisOrContextObject);
                DebugValidateArg(formattableString);

                Log.ErrorMessage(IdOf(thisOrContextObject), memberName, Format(formattableString));
            }
        }

#endif

        /// <summary>Logs an error message.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="message">The message to be logged.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Error(object thisOrContextObject, object message, [CallerMemberName] string memberName = null)
        {
            if (IsEnabled)
            {
                DebugValidateArg(thisOrContextObject);
                DebugValidateArg(message);

                Log.ErrorMessage(IdOf(thisOrContextObject), memberName, Format(message).ToString());
            }
        }

        [Event(ErrorEventId, Level = EventLevel.Warning, Keywords = Keywords.Default)]
        private void ErrorMessage(string thisOrContextObject, string memberName, string message) =>
            WriteEvent(ErrorEventId, thisOrContextObject, memberName ?? MissingMember, message);

        #endregion Error

        #region Fail

#if !NET451

        /// <summary>Logs a fatal error and raises an assert.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="formattableString">The message to be logged.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Fail(object thisOrContextObject, FormattableString formattableString, [CallerMemberName] string memberName = null)
        {
            // Don't call DebugValidateArg on args, as we expect Fail to be used in assert/failure situations
            // that should never happen in production, and thus we don't care about extra costs.

            if (IsEnabled)
            {
                Log.CriticalFailure(IdOf(thisOrContextObject), memberName, Format(formattableString));
            }

            Debug.Fail(Format(formattableString), $"{IdOf(thisOrContextObject)}.{memberName}");
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

            if (IsEnabled)
            {
                Log.CriticalFailure(IdOf(thisOrContextObject), memberName, Format(message).ToString());
            }

            Debug.Fail(Format(message).ToString(), $"{IdOf(thisOrContextObject)}.{memberName}");
        }

        [Event(CriticalFailureEventId, Level = EventLevel.Critical, Keywords = Keywords.Debug)]
        private void CriticalFailure(string thisOrContextObject, string memberName, string message) =>
            WriteEvent(CriticalFailureEventId, thisOrContextObject, memberName ?? MissingMember, message);

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
        private unsafe void DumpBuffer(string thisOrContextObject, string memberName, byte[] buffer) =>
            WriteEvent(DumpArrayEventId, thisOrContextObject, memberName ?? MissingMember, buffer);

        #endregion DumpBuffer

        #region Associate

        /// <summary>Logs a relationship between two objects.</summary>
        /// <param name="first">The first object.</param>
        /// <param name="second">The second object.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Associate(object first, object second, [CallerMemberName] string memberName = null)
        {
            if (IsEnabled)
            {
                DebugValidateArg(first);
                DebugValidateArg(second);

                Log.Associate(IdOf(first), memberName, IdOf(first), IdOf(second));
            }
        }

        /// <summary>Logs a relationship between two objects.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="first">The first object.</param>
        /// <param name="second">The second object.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Associate(object thisOrContextObject, object first, object second, [CallerMemberName] string memberName = null)
        {
            if (IsEnabled)
            {
                DebugValidateArg(thisOrContextObject);
                DebugValidateArg(first);
                DebugValidateArg(second);

                Log.Associate(IdOf(thisOrContextObject), memberName, IdOf(first), IdOf(second));
            }
        }

        [Event(AssociateEventId, Level = EventLevel.Informational, Keywords = Keywords.Default, Message = "[{2}]<-->[{3}]")]
        private void Associate(string thisOrContextObject, string memberName, string first, string second) =>
            WriteEvent(AssociateEventId, thisOrContextObject, memberName ?? MissingMember, first, second);

        #endregion Associate

        #endregion Events

        #region Helpers

        private static void DebugValidateArg(object arg)
        {
            if (!IsEnabled)
            {
                Debug.Assert(!(arg is ValueType), $"Should not be passing value type {arg?.GetType()} to logging without IsEnabled check");
#if !NET451
                Debug.Assert(!(arg is FormattableString), $"Should not be formatting FormattableString \"{arg}\" if tracing isn't enabled");
#endif
            }
        }

#if !NET451

        private static void DebugValidateArg(FormattableString arg)
        {
            Debug.Assert(IsEnabled || arg == null, $"Should not be formatting FormattableString \"{arg}\" if tracing isn't enabled");
        }

#endif

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
            if (value is Array arr)
            {
                return $"{arr.GetType().GetElementType()}[{((Array)value).Length}]";
            }

            // Format ICollections as the name and count
            if (value is ICollection c)
            {
                return $"{c.GetType().Name}({c.Count})";
            }

            // Format SafeHandles as their type, hash code, and pointer value
            if (value is SafeHandle handle)
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

#if !NET451

        [NonEvent]
        private static string Format(FormattableString s)
        {
            switch (s.ArgumentCount)
            {
                case 0: return s.Format;
                case 1: return string.Format(CultureInfo.InvariantCulture, s.Format, Format(s.GetArgument(0)));
                case 2: return string.Format(CultureInfo.InvariantCulture, s.Format, Format(s.GetArgument(0)), Format(s.GetArgument(1)));
                case 3: return string.Format(CultureInfo.InvariantCulture, s.Format, Format(s.GetArgument(0)), Format(s.GetArgument(1)), Format(s.GetArgument(2)));
                default:
                    object[] args = s.GetArguments();
                    object[] formattedArgs = new object[args.Length];
                    for (int i = 0; i < args.Length; i++)
                    {
                        formattedArgs[i] = Format(args[i]);
                    }
                    return string.Format(CultureInfo.InvariantCulture, s.Format, formattedArgs);
            }
        }

#endif

        static partial void AdditionalCustomizedToString<T>(T value, ref string result);

        #endregion Helpers

        #region Custom WriteEvent overloads

        [NonEvent]
        private unsafe void WriteEvent(int eventId, string arg1, string arg2, string arg3, string arg4)
        {
            if (IsEnabled())
            {
                arg1 ??= "";
                arg2 ??= "";
                arg3 ??= "";
                arg4 ??= "";

                fixed (char* string1Bytes = arg1)
                fixed (char* string2Bytes = arg2)
                fixed (char* string3Bytes = arg3)
                fixed (char* string4Bytes = arg4)
                {
                    const int numEventDatas = 4;
                    EventData* descrs = stackalloc EventData[numEventDatas];

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
                arg1 ??= "";
                arg2 ??= "";

#if !NET451
                arg3 ??= Array.Empty<byte>();
#else
                arg3 ??= new byte[0];
#endif

                fixed (char* arg1Ptr = arg1)
                fixed (char* arg2Ptr = arg2)
                fixed (byte* arg3Ptr = arg3)
                {
                    int bufferLength = arg3.Length;
                    const int numEventDatas = 4;
                    EventData* descrs = stackalloc EventData[numEventDatas];

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
                arg1 ??= "";

                fixed (char* arg1Ptr = arg1)
                {
                    const int numEventDatas = 4;
                    EventData* descrs = stackalloc EventData[numEventDatas];

                    descrs[0].DataPointer = (IntPtr)arg1Ptr;
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
                arg1 ??= "";
                arg3 ??= "";

                fixed (char* arg1Ptr = arg1)
                fixed (char* arg3Ptr = arg3)
                {
                    const int numEventDatas = 3;
                    EventData* descrs = stackalloc EventData[numEventDatas];

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
                arg1 ??= "";
                arg2 ??= "";

                fixed (char* arg1Ptr = arg1)
                fixed (char* arg2Ptr = arg2)
                {
                    const int numEventDatas = 3;
                    EventData* descrs = stackalloc EventData[numEventDatas];

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
                arg1 ??= "";
                arg2 ??= "";
                arg3 ??= "";

                fixed (char* arg1Ptr = arg1)
                fixed (char* arg2Ptr = arg2)
                fixed (char* arg3Ptr = arg3)
                {
                    const int numEventDatas = 4;
                    EventData* descrs = stackalloc EventData[numEventDatas];

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
    }
}
