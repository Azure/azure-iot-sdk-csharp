// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Common
{
    internal static class CommonConstants
    {
        // Custom HTTP response contents
        public const string ErrorCode = "errorCode";

        public const string MediaTypeForDeviceManagementApis = "application/json";
        public const string AmqpsScheme = "amqps";

        public const string ContentTypeHeaderName = "Content-Type";
        public const string ContentEncodingHeaderName = "Content-Encoding";
        public const string BatchedMessageContentType = "application/vnd.microsoft.iothub.json";
        public const string BatchedFeedbackContentType = "application/vnd.microsoft.iothub.feedback.json";
        public const string FileNotificationContentType = "application/vnd.microsoft.iothub.filenotification.json";

        public const string IotHubErrorCode = "IotHubErrorCode";

        public const string HttpErrorCodeName = "iothub-errorcode";

        public static readonly string[] IotHubAadTokenScopes = new string[] { "https://iothubs.azure.net/.default" };
    }
}
