using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.IoT.Thief.Device
{
    /// <summary>
    /// An interface for device code to interact with hub
    /// </summary>
    internal interface IIotHub
    {
        /// <summary>
        /// Sends the specified telemetry object as a message.
        /// </summary>
        /// <param name="telemetry">An object to be converted to an application/json payload to send.</param>
        /// <param name="extraProperties">Additional properties to send with the telemetry message.</param>
        void AddTelemetry(TelemetryBase telemetry, IDictionary<string, string> extraProperties = null);

        /// <summary>
        /// Sets the specified properties on the device twin.
        /// </summary>
        /// <param name="properties">Twin properties to set.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        Task SetPropertiesAsync(object properties, CancellationToken cancellationToken = default);
    }
}