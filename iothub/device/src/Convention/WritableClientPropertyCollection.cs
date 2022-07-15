// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The collection of writable property requests received from service.
    /// </summary>
    /// <remarks>
    /// See the <see href="https://docs.microsoft.com/azure/iot-pnp/concepts-convention#writable-properties">Writable properties</see> documentation for more information.
    /// </remarks>
    public class WritableClientPropertyCollection : IEnumerable<WritableClientProperty>
    {
        private const string VersionName = "$version";

        // TODO: Unit-testable and mockable

        internal WritableClientPropertyCollection(IDictionary<string, object> writableClientPropertyRequests, PayloadConvention payloadConvention)
        {
            Convention = payloadConvention;
            PopulateWritableClientProperties(writableClientPropertyRequests);
        }

        /// <summary>
        /// Gets the version of the writable property collection.
        /// </summary>
        /// <remarks>
        /// IoT Hub does not preserve writable property update notifications for disconnected devices/modules.
        /// On connecting, the client should retreive the full property document through <see cref="DeviceClient.GetClientPropertiesAsync(System.Threading.CancellationToken)"/>/
        /// <see cref="ModuleClient.GetClientPropertiesAsync(System.Threading.CancellationToken)"/> in addition to subscribing for update notifications
        /// through <see cref="DeviceClient.SubscribeToWritablePropertyUpdateRequestsAsync(Func{WritableClientPropertyCollection, Task}, System.Threading.CancellationToken)"/>/
        /// <see cref="ModuleClient.SubscribeToWritablePropertyUpdateRequestsAsync(Func{WritableClientPropertyCollection, Task}, System.Threading.CancellationToken)"/>.
        /// The client application can ignore all update notifications with version less that or equal to the version of the full document.
        /// </remarks>
        /// <value>A <see cref="long"/> that is used to identify the version of the writable property collection.</value>
        public long Version { get; private set; }

        internal IList<WritableClientProperty> WritableClientProperties { get; } = new List<WritableClientProperty>();

        internal PayloadConvention Convention { get; set; }

        /// <summary>
        /// Determines whether the specified root-level property is present in the received writable property update request.
        /// </summary>
        /// <param name="propertyName">The property to locate.</param>
        /// <returns><c>true</c> if the specified property is present; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="propertyName"/> is <c>null</c>.</exception>
        public bool Contains(string propertyName)
        {
            if (propertyName == null)
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            return GetMatches(null, propertyName).Any();
        }

        /// <summary>
        /// Determines whether the specified component-level property is present in the received writable property update request.
        /// </summary>
        /// <param name="propertyName">The property to locate.</param>
        /// <param name="componentName">The component which holds the required property.</param>
        /// <returns><c>true</c> if the specified property is present; otherwise, <c>false</c>.</returns>
        public bool Contains(string componentName, string propertyName)
        {
            if (componentName == null)
            {
                throw new ArgumentNullException(nameof(componentName));
            }

            if (propertyName == null)
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            return GetMatches(componentName, propertyName).Any();
        }

        /// <summary>
        /// Gets the value of a root-level property as a <see cref="WritableClientProperty"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="WritableClientProperty"/> has a convenience method <see cref="WritableClientProperty.AcknowledgeWith(int, string)"/>
        /// to help you build the writable property acknowledgement object that you can add to a <see cref="ClientPropertyCollection"/>
        /// using <see cref="ClientPropertyCollection.AddWritableClientPropertyAcknowledgement(WritableClientPropertyAcknowledgement)"/>
        /// and report it to service via <see cref="DeviceClient.UpdateClientPropertiesAsync(ClientPropertyCollection, CancellationToken)"/>
        /// or <see cref="ModuleClient.UpdateClientPropertiesAsync(ClientPropertyCollection, CancellationToken)"/>.
        /// <para>
        /// To retrieve the value of the root-level writable property update request see <see cref="TryGetValue{T}(string, out T)"/>
        /// or <see cref="WritableClientProperty.TryGetValue{T}(out T)"/>.
        /// </para>
        /// </remarks>
        /// <param name="propertyName">The property to get.</param>
        /// <param name="writableClientProperty">When this method returns true, this contains the value of the root-level property.
        /// When this method returns false, this contains an empty <see cref="WritableClientProperty"/>.</param>
        /// <returns><c>true</c> if a root-level property with the specified key was found; otherwise, <c>false</c>.</returns>
        public bool TryGetWritableClientProperty(string propertyName, out WritableClientProperty writableClientProperty)
        {
            writableClientProperty = default;

            // If the propertyName is null, empty or whitespace then return false with an empty WritableClientProperty.
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                return false;
            }

            if (Contains(propertyName))
            {
                IEnumerable<WritableClientProperty> matches = GetMatches(null, propertyName);

                // There will only be a single entry for a specific property name, so we can safely return the first element in the list.
                writableClientProperty = matches.First();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the value of a root-level property.
        /// </summary>
        /// <remarks>
        /// See <see cref="TryGetWritableClientProperty(string, out WritableClientProperty)"/> to get a <see cref="WritableClientProperty"/> object
        /// which has a convenience method <see cref="WritableClientProperty.AcknowledgeWith(int, string)"/> to help you build the writable property acknowledgement object
        /// that you can add to a <see cref="ClientPropertyCollection"/> using
        /// <see cref="ClientPropertyCollection.AddWritableClientPropertyAcknowledgement(WritableClientPropertyAcknowledgement)"/>
        /// and report it to service via <see cref="DeviceClient.UpdateClientPropertiesAsync(ClientPropertyCollection, CancellationToken)"/>
        /// or <see cref="ModuleClient.UpdateClientPropertiesAsync(ClientPropertyCollection, CancellationToken)"/>.
        /// </remarks>
        /// <typeparam name="T">The type to cast the <paramref name="propertyValue"/> to.</typeparam>
        /// <param name="propertyName">The property to get.</param>
        /// <param name="propertyValue">When this method returns true, this contains the value of the root-level property.
        /// When this method returns false, this contains the default value of the type <c>T</c> passed in.</param>
        /// <returns><c>true</c> if a root-level property of type <c>T</c> with the specified key was found; otherwise, <c>false</c>.</returns>
        public bool TryGetValue<T>(string propertyName, out T propertyValue)
        {
            propertyValue = default;

            if (TryGetWritableClientProperty(propertyName, out WritableClientProperty writableClientProperty))
            {
                // If the object is of type T or can be cast or converted to type T, go ahead and return it.
                if (writableClientProperty.TryGetValue(out propertyValue))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the value of a component-level property as a <see cref="WritableClientProperty"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="WritableClientProperty"/> has a convenience method <see cref="WritableClientProperty.AcknowledgeWith(int, string)"/>
        /// to help you build the writable property acknowledgement object that you can add to a <see cref="ClientPropertyCollection"/>
        /// using <see cref="ClientPropertyCollection.AddWritableClientPropertyAcknowledgement(WritableClientPropertyAcknowledgement)"/>
        /// and report it to service via <see cref="DeviceClient.UpdateClientPropertiesAsync(ClientPropertyCollection, CancellationToken)"/>
        /// or <see cref="ModuleClient.UpdateClientPropertiesAsync(ClientPropertyCollection, CancellationToken)"/>.
        /// <para>
        /// To retrieve the value of the component-level writable property update request see <see cref="WritableClientProperty.TryGetValue{T}(out T)"/>
        /// or <see cref="TryGetValue{T}(string, string, out T)"/>.
        /// </para>
        /// </remarks>
        /// <param name="componentName">The component which holds the required property.</param>
        /// <param name="propertyName">The property to get.</param>
        /// <param name="writableClientProperty">When this method returns true, this contains the value of the component-level property.
        /// When this method returns false, this contains an empty <see cref="WritableClientProperty"/>.</param>
        /// <returns><c>true</c> if a component-level property with the specified key was found; otherwise, <c>false</c>.</returns>
        public bool TryGetWritableClientProperty(string componentName, string propertyName, out WritableClientProperty writableClientProperty)
        {
            writableClientProperty = default;

            // If either the component name or the property name is null, empty or whitespace,
            // then return false with the default value of the type <T> passed in.
            if (string.IsNullOrWhiteSpace(componentName) || string.IsNullOrWhiteSpace(propertyName))
            {
                return false;
            }

            if (Contains(componentName, propertyName))
            {
                IEnumerable<WritableClientProperty> matches = GetMatches(componentName, propertyName);

                // There will only be a single entry for a specific property name, so we can safely return the first element in the list.
                writableClientProperty = matches.First();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the value of a component-level property.
        /// </summary>
        /// <remarks>
        /// See <see cref="TryGetWritableClientProperty(string, out WritableClientProperty)"/> to get a <see cref="WritableClientProperty"/> object
        /// which has a convenience method <see cref="WritableClientProperty.AcknowledgeWith(int, string)"/> to help you build the writable property acknowledgement object
        /// that you can add to a <see cref="ClientPropertyCollection"/> using
        /// <see cref="ClientPropertyCollection.AddWritableClientPropertyAcknowledgement(WritableClientPropertyAcknowledgement)"/>
        /// and report it to service via <see cref="DeviceClient.UpdateClientPropertiesAsync(ClientPropertyCollection, CancellationToken)"/>
        /// or <see cref="ModuleClient.UpdateClientPropertiesAsync(ClientPropertyCollection, CancellationToken)"/>.
        /// </remarks>
        /// <typeparam name="T">The type to cast the <paramref name="propertyValue"/> to.</typeparam>
        /// <param name="componentName">The component which holds the required property.</param>
        /// <param name="propertyName">The property to get.</param>
        /// <param name="propertyValue">When this method returns true, this contains the value of the component-level property.
        /// When this method returns false, this contains the default value of the type <c>T</c> passed in.</param>
        /// <returns><c>true</c> if a component-level property of type <c>T</c> with the specified key was found; otherwise, <c>false</c>.</returns>
        public bool TryGetValue<T>(string componentName, string propertyName, out T propertyValue)
        {
            propertyValue = default;

            if (TryGetWritableClientProperty(componentName, propertyName, out WritableClientProperty writableClientProperty))
            {
                // If the object is of type T or can be cast or converted to type T, go ahead and return it.
                if (writableClientProperty.TryGetValue(out propertyValue))
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc/>
        public IEnumerator<WritableClientProperty> GetEnumerator()
        {
            foreach (WritableClientProperty property in WritableClientProperties)
            {
                yield return property;
            }
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private void PopulateWritableClientProperties(IDictionary<string, object> writableClientPropertyRequests)
        {
            // The version information should not be a part of the enumerable ProperyCollection, but rather should be
            // accessible through its dedicated accessor.
            bool versionPresent = writableClientPropertyRequests.TryGetValue(VersionName, out object version);

            Version = versionPresent && ObjectConversionHelpers.TryCastNumericTo(version, out long longVersion)
                ? longVersion
                : throw new IotHubException("Properties document either missing version number or not formatted as expected. Contact service with logs.");

            foreach (KeyValuePair<string, object> property in writableClientPropertyRequests)
            {
                // Ignore the version entry since we've already saved it off.
                if (property.Key == VersionName)
                {
                    // no-op
                }
                else
                {
                    // Serialize the received property value. You can use the default serializer here as the response has previously been deserialized using the default serializer.
                    object propertyValueAsObject = property.Value;

                    // Check if the property value is for a root property or a component property.
                    // A component property be a JObject and will have the "__t": "c" identifiers.
                    // The component property collection will be a JObject because it has been deserailized into a dictionary using Newtonsoft.Json.
                    bool isComponentProperty = propertyValueAsObject is JObject propertyValueAsJObject
                        && NewtonsoftJsonPayloadSerializer.Instance.TryGetNestedJsonObjectValue(propertyValueAsJObject, ConventionBasedConstants.ComponentIdentifierKey, out string _);

                    if (isComponentProperty)
                    {
                        // If this is a component property then the collection is a JObject with each individual property as a writable property update request.
                        var componentPropertiesAsJObject = (JObject)propertyValueAsObject;

                        foreach (KeyValuePair<string, JToken> componentProperty in componentPropertiesAsJObject)
                        {
                            if (componentProperty.Key == ConventionBasedConstants.ComponentIdentifierKey)
                            {
                                // Ignore it. We won't be saving the component identifiers into the collection that we return to the user.
                            }
                            else
                            {
                                var individualPropertyValue = new WritableClientProperty(Version, Convention)
                                {
                                    ComponentName = property.Key,
                                    PropertyName = componentProperty.Key,
                                    Value = Convention.PayloadSerializer.DeserializeToType<object>(JsonConvert.SerializeObject(componentProperty.Value)),
                                };
                                WritableClientProperties.Add(individualPropertyValue);
                            }
                        }
                    }
                    else
                    {
                        var individualPropertyValue = new WritableClientProperty(Version, Convention)
                        {
                            PropertyName = property.Key,
                            Value = Convention.PayloadSerializer.DeserializeToType<object>(JsonConvert.SerializeObject(propertyValueAsObject)),
                        };
                        WritableClientProperties.Add(individualPropertyValue);
                    }
                }
            }
        }

        private IEnumerable<WritableClientProperty> GetMatches(string componentName, string propertyName)
        {
            return WritableClientProperties
                .Where(property =>
                    property.ComponentName == componentName
                    && property.PropertyName == propertyName);
        }
    }
}
