// <auto-generated>
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Microsoft.Azure.Devices.Generated
{
    using Microsoft.Rest;
    using Models;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// DigitalTwin operations.
    /// </summary>
    internal partial interface IDigitalTwin
    {
        /// <summary>
        /// Gets a digital twin.
        /// </summary>
        /// <param name='id'>
        /// Digital Twin ID.
        /// </param>
        /// <param name='customHeaders'>
        /// The headers that will be added to request.
        /// </param>
        /// <param name='cancellationToken'>
        /// The cancellation token.
        /// </param>
        /// <exception cref="Microsoft.Rest.HttpOperationException">
        /// Thrown when the operation returned an invalid status code
        /// </exception>
        /// <exception cref="Microsoft.Rest.SerializationException">
        /// Thrown when unable to deserialize the response
        /// </exception>
        /// <exception cref="Microsoft.Rest.ValidationException">
        /// Thrown when a required parameter is null
        /// </exception>
        Task<HttpOperationResponse<string,DigitalTwinGetHeaders>> GetDigitalTwinWithHttpMessagesAsync(string id, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken));
        /// <summary>
        /// Updates a digital twin.
        /// </summary>
        /// <param name='id'>
        /// Digital Twin ID.
        /// </param>
        /// <param name='digitalTwinPatch'>
        /// json-patch contents to update.
        /// </param>
        /// <param name='ifMatch'>
        /// </param>
        /// <param name='customHeaders'>
        /// The headers that will be added to request.
        /// </param>
        /// <param name='cancellationToken'>
        /// The cancellation token.
        /// </param>
        /// <exception cref="Microsoft.Rest.HttpOperationException">
        /// Thrown when the operation returned an invalid status code
        /// </exception>
        /// <exception cref="Microsoft.Rest.ValidationException">
        /// Thrown when a required parameter is null
        /// </exception>
        Task<HttpOperationHeaderResponse<DigitalTwinUpdateHeaders>> UpdateDigitalTwinWithHttpMessagesAsync(string id, string digitalTwinPatch, string ifMatch = default(string), Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken));
        /// <summary>
        /// Invoke a digital twin root level command.
        /// </summary>
        /// <remarks>
        /// Invoke a digital twin root level command.
        /// </remarks>
        /// <param name='id'>
        /// </param>
        /// <param name='commandName'>
        /// </param>
        /// <param name='payload'>
        /// </param>
        /// <param name='connectTimeoutInSeconds'>
        /// Maximum interval of time, in seconds, that the digital twin command
        /// will wait for the answer.
        /// </param>
        /// <param name='responseTimeoutInSeconds'>
        /// Maximum interval of time, in seconds, that the digital twin command
        /// will wait for the answer.
        /// </param>
        /// <param name='customHeaders'>
        /// The headers that will be added to request.
        /// </param>
        /// <param name='cancellationToken'>
        /// The cancellation token.
        /// </param>
        /// <exception cref="Microsoft.Rest.HttpOperationException">
        /// Thrown when the operation returned an invalid status code
        /// </exception>
        /// <exception cref="Microsoft.Rest.SerializationException">
        /// Thrown when unable to deserialize the response
        /// </exception>
        /// <exception cref="Microsoft.Rest.ValidationException">
        /// Thrown when a required parameter is null
        /// </exception>
        Task<HttpOperationResponse<object,DigitalTwinInvokeRootLevelCommandHeaders>> InvokeRootLevelCommandWithHttpMessagesAsync(string id, string commandName, object payload, int? connectTimeoutInSeconds = default(int?), int? responseTimeoutInSeconds = default(int?), Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken));
        /// <summary>
        /// Invoke a digital twin command.
        /// </summary>
        /// <remarks>
        /// Invoke a digital twin command.
        /// </remarks>
        /// <param name='id'>
        /// </param>
        /// <param name='componentPath'>
        /// </param>
        /// <param name='commandName'>
        /// </param>
        /// <param name='payload'>
        /// </param>
        /// <param name='connectTimeoutInSeconds'>
        /// Maximum interval of time, in seconds, that the digital twin command
        /// will wait for the answer.
        /// </param>
        /// <param name='responseTimeoutInSeconds'>
        /// Maximum interval of time, in seconds, that the digital twin command
        /// will wait for the answer.
        /// </param>
        /// <param name='customHeaders'>
        /// The headers that will be added to request.
        /// </param>
        /// <param name='cancellationToken'>
        /// The cancellation token.
        /// </param>
        /// <exception cref="Microsoft.Rest.HttpOperationException">
        /// Thrown when the operation returned an invalid status code
        /// </exception>
        /// <exception cref="Microsoft.Rest.SerializationException">
        /// Thrown when unable to deserialize the response
        /// </exception>
        /// <exception cref="Microsoft.Rest.ValidationException">
        /// Thrown when a required parameter is null
        /// </exception>
        Task<HttpOperationResponse<object,DigitalTwinInvokeComponentCommandHeaders>> InvokeComponentCommandWithHttpMessagesAsync(string id, string componentPath, string commandName, object payload, int? connectTimeoutInSeconds = default(int?), int? responseTimeoutInSeconds = default(int?), Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken));
    }
}