// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.LongHaul.Module
{
    internal static class LoggingConstants
    {
        // Metrics

        public const string ModuleDisconnectedDurationSeconds = "ModuleDisconnectedDurationSeconds";
        public const string TotalTelemetryMessagesToModuleSent = "TotalTelemetryMessagesToModuleSent";
        public const string TelemetryMessageToModuleDelaySeconds = "TelemetryMessageToModuleDelaySeconds";
        public const string ModuleMessageBacklog = "ModuleMessageBacklog";
        public const string C2mDirectMethodDelaySeconds = "C2mDirectMethodDelaySeconds";
        public const string TotalTwinUpdatesToModuleReported = "TotalTwinUpdatesToModuleReported";
        public const string TotalTwinCallbacksToModuleHandled = "TotalTwinCallbacksToModuleHandled";
        public const string TotalDesiredPropertiesToModuleHandled = "TotalDesiredPropertiesToModuleHandled";
        public const string TotalM2mMessagesCompleted = "TotalM2mMessagesCompleted";
        public const string TotalM2mMessagesRejected = "TotalM2mMessagesRejected";
        public const string M2mMessageOperationSeconds = "M2mMessageOperationSeconds";
        public const string ReportedTwinUpdateToModuleOperationSeconds = "ReportedTwinUpdateToModuleOperationSeconds";
        public const string DirectMethodToModuleRoundTripSeconds = "DirectMethodToModuleRoundTripSeconds";
        public const string TotalDirectMethodCallsToModuleCount = "TotalDirectMethodCallsToModuleCount";
        public const string ModuleClientCloseDelaySeconds = "ModuleClientCloseDelaySeconds";
        public const string ModuleClientOpenDelaySeconds = "ModuleClientOpenDelaySeconds";

        // Events

        public const string StartingRun = "StartingRun";
        public const string ConnectedEvent = "Connected";
        public const string DiscconnectedEvent = "Disconnected";
        public const string EndingRun = "EndingRun";

        // Logging properties

        public const string TestClient = "testClient";
        public const string RunId = "runId";
        public const string SdkLanguage = "sdkLanguage";
        public const string SdkVersion = "sdkVersion";
        public const string OperationName = "operationName";

        public const string Hub = "hub";
        public const string DeviceId = "deviceId";
        public const string ModuleId = "moduleId";
        public const string Transport = "transport";

        public const string ConnectionReason = "connectionReason";
        public const string ConnectionRecommendedAction = "connectionRecommendedAction";

        public const string DisconnectedStatus = "disconnectedStatus";
        public const string DisconnectedReason = "disconnectedReason";
        public const string DisconnectedRecommendedAction = "disconnectedRecommendedAction";
        public const string ConnectionStatusChangeCount = "connectionStatusChangeCount";

        // Operations

        public const string TelemetryMessage = "TelemetryMessage";
        public const string ReportTwinProperties = "ReportTwinProperties";
        public const string DirectMethod = "DirectMethod";
    }
}
