using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.Iot.DigitalTwin.Device.Model
{
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
            RespondVersion = respondVersion;
            StatusCode = statusCode;
            StatusDescription = statusDescription;
        }

        /// <summary>
        /// The respond version is used for server to disambiguate calls for given property.
        /// </summary>
        public int RespondVersion
        {
            get; private set;
        }

        /// <summary>
        /// The status code which maps to appropriate HTTP status code of the property updates.
        /// </summary>
        public int StatusCode
        {
            get; private set;
        }

        public string StatusDescription
        {
            get; private set;
        }
    }
}
