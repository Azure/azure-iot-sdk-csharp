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

        /// <summary>Logs entrance to a method.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="formattableString">A description of the entrance, including any arguments to the call.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Enter(object thisOrContextObject, string formattableString = null, [CallerMemberName] string memberName = null)
        {
        }

        /// <summary>Logs entrance to a method.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="arg0">The object to log.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Enter(object thisOrContextObject, object arg0, [CallerMemberName] string memberName = null)
        {
        }

        /// <summary>Logs entrance to a method.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="arg0">The first object to log.</param>
        /// <param name="arg1">The second object to log.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Enter(object thisOrContextObject, object arg0, object arg1, [CallerMemberName] string memberName = null)
        {
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
        }


        /// <summary>Logs exit from a method.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="formattableString">A description of the exit operation, including any return values.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Exit(object thisOrContextObject, string formattableString = null, [CallerMemberName] string memberName = null)
        {
        }

        /// <summary>Logs exit from a method.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="arg0">A return value from the member.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Exit(object thisOrContextObject, object arg0, [CallerMemberName] string memberName = null)
        {
        }

        /// <summary>Logs exit from a method.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="arg0">A return value from the member.</param>
        /// <param name="arg1">A second return value from the member.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Exit(object thisOrContextObject, object arg0, object arg1, [CallerMemberName] string memberName = null)
        {
        }

        /// <summary>Logs an information message.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="formattableString">The message to be logged.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Info(object thisOrContextObject, string formattableString = null, [CallerMemberName] string memberName = null)
        {
        }

        /// <summary>Logs an information message.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="message">The message to be logged.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Info(object thisOrContextObject, object message, [CallerMemberName] string memberName = null)
        {
        }

        [Event(InfoEventId, Level = EventLevel.Informational, Keywords = Keywords.Default)]
        private void Info(string thisOrContextObject, string memberName, string message) =>
            WriteEvent(InfoEventId, thisOrContextObject, memberName ?? MissingMember, message);

        /// <summary>Logs an error message.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="formattableString">The message to be logged.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Error(object thisOrContextObject, string formattableString, [CallerMemberName] string memberName = null)
        {
        }

        /// <summary>Logs an error message.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="message">The message to be logged.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Error(object thisOrContextObject, object message, [CallerMemberName] string memberName = null)
        {
        }

        /// <summary>Logs a fatal error and raises an assert.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="formattableString">The message to be logged.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Fail(object thisOrContextObject, string formattableString, [CallerMemberName] string memberName = null)
        {
            Debug.Fail(formattableString, $"{IdOf(thisOrContextObject)}.{memberName}");
        }

        /// <summary>Logs a fatal error and raises an assert.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="message">The message to be logged.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Fail(object thisOrContextObject, object message, [CallerMemberName] string memberName = null)
        {
            Debug.Fail(message.ToString(), $"{IdOf(thisOrContextObject)}.{memberName}");
        }

        [Event(CriticalFailureEventId, Level = EventLevel.Critical, Keywords = Keywords.Debug)]
        private void CriticalFailure(string thisOrContextObject, string memberName, string message) =>
            WriteEvent(CriticalFailureEventId, thisOrContextObject, memberName ?? MissingMember, message);


        /// <summary>Logs the contents of a buffer.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="bufferPtr">The starting location of the buffer to be logged.</param>
        /// <param name="count">The number of bytes to log.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static unsafe void DumpBuffer(object thisOrContextObject, IntPtr bufferPtr, int count, [CallerMemberName] string memberName = null)
        {
        }

        /// <summary>Logs a relationship between two objects.</summary>
        /// <param name="first">The first object.</param>
        /// <param name="second">The second object.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Associate(object first, object second, [CallerMemberName] string memberName = null)
        {
            Debug.WriteLine($"Associate: {memberName}, {IdOf(first)}, {IdOf(second)}");
        }

        /// <summary>Logs a relationship between two objects.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="first">The first object.</param>
        /// <param name="second">The second object.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Associate(object thisOrContextObject, object first, object second, [CallerMemberName] string memberName = null)
        {
            Debug.WriteLine($"Associate: {IdOf(thisOrContextObject)}, {memberName}, {IdOf(first)}, {IdOf(second)}");
        }


        public static new bool IsEnabled => 
#if DEBUG
            true;
#else
            false;
#endif

        [NonEvent]
        public static string IdOf(object value) => value != null ? value.GetType().Name + "#" + GetHashCode(value) : NullInstance;

        [NonEvent]
        public static int GetHashCode(object value) => value?.GetHashCode() ?? 0;
	}
}
