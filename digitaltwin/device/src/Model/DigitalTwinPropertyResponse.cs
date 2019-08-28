// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.Iot.DigitalTwin.Device.Helper;

namespace Azure.Iot.DigitalTwin.Device.Model
{
    /// <summary>
    /// Contains response of the property update request passed from the Digital Twin Client to Digital Twin
    /// Interface Client for further processing.
    /// </summary>
    public class DigitalTwinPropertyResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalTwinPropertyResponse"/> class.
        /// </summary>
        /// <param name="respondVersion">The response version.</param>
        /// <param name="statusCode">The status code which maps to appropriate HTTP status code of the property updates.</param>
        /// <param name="statusDescription">Friendly description string of current status of update.</param>
        public DigitalTwinPropertyResponse(int respondVersion, int statusCode, string statusDescription)
        {
            GuardHelper.ThrowIfNullOrWhiteSpace(statusDescription, nameof(statusDescription));

            this.RespondVersion = respondVersion;
            this.StatusCode = statusCode;
            this.StatusDescription = statusDescription;
        }

        /// <summary>
        /// Gets the version which is used for server to disambiguate calls for given property.
        /// </summary>
        public int RespondVersion
        {
            get; private set;
        }

        /// <summary>
        /// Gets the status code associated with the respond.
        /// </summary>
        public int StatusCode
        {
            get; private set;
        }

        /// <summary>
        /// Gets the status description associated with the respond.
        /// </summary>
        public string StatusDescription
        {
            get; private set;
        }
    }
}
