using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Devices.DigitalTwin.Client.Model
{
    public class DigitalTwinPropertyUpdate
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalTwinPropertyUpdate"/> class.
        /// </summary>
        /// <param name="respondVersion">The response version.</param>
        /// <param name="statusCode">The status code which maps to appropriate HTTP status code of the property updates.</param>
        /// <param name="statusDescription">Friendly description string of current status of update.</param>
        internal DigitalTwinPropertyUpdate(string propertyName, int desiredVersion, Memory<byte> propertyDesired, Memory<byte> propertyReported)
        {
            PropertyName = propertyName;
            DesiredVersion = desiredVersion;
            PropertyDesired = propertyDesired;
            PropertyReported = propertyReported;
        }

        /// <summary>
        /// Name of the property being update.
        /// </summary>
        public string PropertyName
        {
            get; private set;
        }

        /// <summary>
        /// Value that the device application had previously reported for this property.  
        /// This value may be NULL if the application never reported a property.  It will also
        /// be NULL when an update arrives to the given property after the initial callback.
        /// </summary>
        public Memory<byte> PropertyReported
        {
            get; private set;
        }

        /// <summary>
        /// Value the service requests the given property to be set to.
        /// </summary>
        public Memory<byte> PropertyDesired
        {
            get; private set;
        }

        /// <summary>
        /// Version (from the service, NOT the C structure) of this property.
        /// </summary>
        public int DesiredVersion
        {
            get; private set;
        }

    }
}
