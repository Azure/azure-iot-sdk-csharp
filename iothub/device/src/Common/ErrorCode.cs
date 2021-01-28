// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Errors
{
    /// <summary>
    /// Unique code for each instance of DeviceGateway exception that identifies the error condition that caused the failure.
    /// </summary>
    /// <remarks>
    /// These error codes will allow us to do automatic analysis and aggregation of error responses sent from resource provider and frontend.
    /// </remarks>
    public enum ErrorCode
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        None = -1,
        OrchestrationStateInvalid = 100,
        OrchestrationRunningOnIotHub = 101,
        IotHubNotFoundInDatabase = 102,
        NoMatchingResourcePoolFound = 103,
        ResourcePoolNotFound = 104,
        NoMatchingResourceFound = 105,
        MultipleMatchingResourcesFound = 106,
        GarbageCollectionFailed = 107,
        IotHubUpdateFailed = 108,
        InvalidEventHubAccessRight = 109,

        /// <summary>
        /// Bad Request
        /// </summary>
        AuthorizationRulesExceededQuota = 200,

        InvalidIotHubName = 201,
        InvalidOperationId = 202,
        IotHubNameNotAvailable = 203,
        SystemPropertiesNotAllowed = 204,

        /// <summary>
        /// Internal Error
        /// </summary>
        IotHubActivationFailed = 300,

        IotHubDeletionFailed = 301,
        IotHubExportFailed = 302,
        IotHubsExportFailed = 303,
        IotHubImportFailed = 304,
        IotHubsImportFailed = 305,
        WinFabApplicationUpgradeFailed = 306,
        WinFabClusterUpgradeFailed = 307,
        IotHubInvalidStateTransition = 308,
        IotHubStateTransitionNotDefined = 309,
        IotHubInvalidProperties = 310,

        /// <summary>
        /// Not found
        /// </summary>
        KeyNameNotFound = 400,

        /// <summary>
        /// Internal Warning Range 1000-1299
        /// </summary>
        WinFabApplicationCleanupNotAttempted = 1000

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }
}
