using Azure.IoT.DigitalTwin.Service.Models;
using Microsoft.Rest;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.IoT.DigitalTwin.Service
{
    /// <summary>
    /// Service client for getting digital twin interfaces, invoking interface commands, and updating digital twin state
    /// </summary>
    public partial class DigitalTwinServiceClient
    {
        private DigitalTwin digitalTwin;
        private const string apiVersion = "2019-07-01-preview";

        #region Constructors
        public DigitalTwinServiceClient(string connectionString) : this(connectionString, new DigitalTwinServiceClientOptions())
        {
        }

        public DigitalTwinServiceClient(string connectionString, DigitalTwinServiceClientOptions options)
        {
            var iothubConnectionStringParser = ServiceConnectionStringParser.Create(connectionString);
            ServiceConnectionString iothubServiceConnectionString = new ServiceConnectionString(iothubConnectionStringParser);
            IoTServiceClientCredentials serviceClientCredentials = new SharedAccessKeyCredentials(iothubServiceConnectionString);
            SetupDigitalTwinServiceClient(iothubServiceConnectionString.HttpsEndpoint, serviceClientCredentials, options);
        }
        
        public DigitalTwinServiceClient(Uri uri, IoTServiceClientCredentials credentials, DigitalTwinServiceClientOptions options)
        {
            SetupDigitalTwinServiceClient(uri, credentials, options);
        }

        protected DigitalTwinServiceClient()
        {
            //for mocking purposes only
        }

        private void SetupDigitalTwinServiceClient(Uri uri, IoTServiceClientCredentials credentials, DigitalTwinServiceClientOptions options)
        {
            DelegatingHandler[] handlers = new DelegatingHandler[1] { new AuthorizationDelegatingHandler(credentials) };
            var protocolLayer = new IotHubGatewayServiceAPIs20190701Preview(credentials, handlers)
            {
                ApiVersion = options.ApiVersion,
                BaseUri = uri
            };
            this.digitalTwin = new DigitalTwin(protocolLayer);
        }
        #endregion

        public virtual Models.DigitalTwin GetDigitalTwin(string digitalTwinId)
        {
            //todo make other PL layer models private?
            DigitalTwinInterfaces digitalTwinInterfaces = digitalTwin.GetAllInterfaces(digitalTwinId);
            return new Models.DigitalTwin(digitalTwinInterfaces.Interfaces, digitalTwinInterfaces.Version);
        }

        public virtual async Task<Models.DigitalTwin> GetDigitalTwinAsync(string digitalTwinId, CancellationToken cancellationToken = default(CancellationToken))
        {
            DigitalTwinInterfaces digitalTwinInterfaces = await digitalTwin.GetAllInterfacesAsync(digitalTwinId, cancellationToken).ConfigureAwait(false);
            return new Models.DigitalTwin(digitalTwinInterfaces.Interfaces, digitalTwinInterfaces.Version);
        }

        public virtual Models.DigitalTwin GetDigitalTwinComponent(string digitalTwinId, string componentName, CancellationToken cancellationToken = default(CancellationToken))
        {
            DigitalTwinInterfaces digitalTwinInterfaces = digitalTwin.GetSingleInterface(digitalTwinId, componentName);
            return new Models.DigitalTwin(digitalTwinInterfaces.Interfaces, digitalTwinInterfaces.Version);
        }

        public virtual async Task<Models.DigitalTwin> GetDigitalTwinComponentAsync(string digitalTwinId, string componentName, CancellationToken cancellationToken = default(CancellationToken))
        {
            DigitalTwinInterfaces digitalTwinInterfaces = await digitalTwin.GetSingleInterfaceAsync(digitalTwinId, componentName, cancellationToken).ConfigureAwait(false);
            return new Models.DigitalTwin(digitalTwinInterfaces.Interfaces, digitalTwinInterfaces.Version);
        }

        public virtual Models.DigitalTwin UpdateDigitalTwin(string digitalTwinId, DigitalTwinInterfacesPatch patch, string ETag = default(string))
        {
            DigitalTwinInterfaces digitalTwinInterfaces = digitalTwin.UpdateMultipleInterfaces(digitalTwinId, patch, ETag);
            return new Models.DigitalTwin(digitalTwinInterfaces.Interfaces, digitalTwinInterfaces.Version);
        }

        public virtual async Task<Models.DigitalTwin> UpdateDigitalTwinAsync(string digitalTwinId, DigitalTwinInterfacesPatch patch, string ETag = default(string), CancellationToken cancellationToken = default(CancellationToken))
        {
            DigitalTwinInterfaces digitalTwinInterfaces = await digitalTwin.UpdateMultipleInterfacesAsync(digitalTwinId, patch, ETag, cancellationToken).ConfigureAwait(false);
            return new Models.DigitalTwin(digitalTwinInterfaces.Interfaces, digitalTwinInterfaces.Version);
        }

        public virtual Models.DigitalTwin UpdateDigitalTwinProperty(string digitalTwinId, string componentName, string propertyName, string propertyValue, string ETag = default(string))
        {
            var value = new DigitalTwinInterfacesPatchInterfacesValuePropertiesValueDesired(propertyValue);
            DigitalTwinInterfacesPatch patch = new DigitalTwinInterfacesPatch()
            {
                Interfaces = new Dictionary<string, DigitalTwinInterfacesPatchInterfacesValue>
                    {
                        {
                            componentName, new DigitalTwinInterfacesPatchInterfacesValue()
                            {
                                Properties = new Dictionary<string, DigitalTwinInterfacesPatchInterfacesValuePropertiesValue>()
                                {
                                    {propertyName, new DigitalTwinInterfacesPatchInterfacesValuePropertiesValue(value)}
                                }
                            }
                        }
                    }
            };

            DigitalTwinInterfaces digitalTwinInterfaces = digitalTwin.UpdateMultipleInterfaces(digitalTwinId, patch, ETag);
            return new Models.DigitalTwin(digitalTwinInterfaces.Interfaces, digitalTwinInterfaces.Version);
        }

        public virtual async Task<Models.DigitalTwin> UpdateDigitalTwinPropertyAsync(string digitalTwinId, string componentName, string propertyName, string propertyValue, string ETag = default(string), CancellationToken cancellationToken = default(CancellationToken))
        {
            var value = new DigitalTwinInterfacesPatchInterfacesValuePropertiesValueDesired(propertyValue);
            DigitalTwinInterfacesPatch patch = new DigitalTwinInterfacesPatch()
            {
                Interfaces = new Dictionary<string, DigitalTwinInterfacesPatchInterfacesValue>
                    {
                        {
                            componentName, new DigitalTwinInterfacesPatchInterfacesValue()
                            {
                                Properties = new Dictionary<string, DigitalTwinInterfacesPatchInterfacesValuePropertiesValue>()
                                {
                                    {propertyName, new DigitalTwinInterfacesPatchInterfacesValuePropertiesValue(value)}
                                }
                            }
                        }
                    }
            };
            DigitalTwinInterfaces digitalTwinInterfaces = await digitalTwin.UpdateMultipleInterfacesAsync(digitalTwinId, patch, ETag, cancellationToken).ConfigureAwait(false);
            return new Models.DigitalTwin(digitalTwinInterfaces.Interfaces, digitalTwinInterfaces.Version);
        }

        public virtual object InvokeCommand(string digitalTwinId, string componentName, string commandName, byte[] argument)
        {
            //Response<T> where T containst status code and result and other headers
            //payload is currently "object", should be byte[]. Fix the PL if necessary to accomodate this
            //TODO if we do byte[], others sdks have to do the same! Currently, the default is to parse to json which is inconssitent
            return digitalTwin.InvokeInterfaceCommand(digitalTwinId, componentName, commandName, System.Text.Encoding.UTF8.GetString(argument));
        }

        /// <summary>
        /// Invoke a command on a digital twin component
        /// </summary>
        /// <param name='digitalTwinId'>
        /// Digital Twin ID. Format of digitalTwinId is DeviceId[~ModuleId]. ModuleId
        /// is optional.
        /// Example 1: "myDevice"
        /// Example 2: "myDevice~module1"
        /// </param>
        /// <param name="componentName">
        /// Component name, for example &lt;example&gt;myThermostat&lt;/example&gt;.
        /// </param>
        /// <param name="commandName"></param>
        /// <param name="argument"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<object> InvokeCommandAsync(string digitalTwinId, string componentName, string commandName, byte[] argument, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await digitalTwin.InvokeInterfaceCommandAsync(digitalTwinId, componentName, commandName, System.Text.Encoding.UTF8.GetString(argument), null, null, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieve a model 
        /// </summary>
        /// <param name="modelId">The URN identifier for the model to be retrieved. For Example: "urn:contoso:TemperatureSensor:1"</param>
        /// <param name='expand'>
        /// Indicates whether to expand the device capability model's interface
        /// definitions inline or not.
        /// This query parameter ONLY applies to Capability model.
        /// </param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The retrieved model</returns>
        public virtual object GetModel(ModelId modelId, bool? expand = false)
        {
            return this.digitalTwin.GetDigitalTwinModel(modelId.ModelIdString, expand);
        }

        /// <summary>
        /// Retrieve a model 
        /// </summary>
        /// <param name="modelId">The URN identifier for the model to be retrieved. For Example: "urn:contoso:TemperatureSensor:1"</param>
        /// <param name='expand'>
        /// Indicates whether to expand the device capability model's interface
        /// definitions inline or not.
        /// This query parameter ONLY applies to Capability model.
        /// </param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The retrieved model</returns>
        public virtual async Task<object> GetModelAsync(ModelId modelId, bool? expand = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await this.digitalTwin.GetDigitalTwinModelAsync(modelId.ModelIdString, expand).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Represents the modelId string. For Example: "urn:contoso:TemperatureSensor:1"
    /// </summary>
    public struct ModelId
    {
        public string ModelIdString { get; private set; }

        public ModelId(string modelId)
        {
            this.ModelIdString = modelId;
        }
    }
    
    /// <summary>
    /// The configurable options for a digital twin service client
    /// </summary>
    public class DigitalTwinServiceClientOptions //:httppipelineoptions 
    {
        public enum ServiceVersion
        {
            V2019_07_01_preview = 0
        }

        public string ApiVersion { get; private set; }

        public DigitalTwinServiceClientOptions(ServiceVersion version = ServiceVersion.V2019_07_01_preview)
        {
            this.ApiVersion = ServiceVersionToString(version);
        }

        private string ServiceVersionToString(ServiceVersion serviceVersion)
        {
            if (serviceVersion == ServiceVersion.V2019_07_01_preview)
            {
                return "2019-07-01-preview";
            }

            throw new ArgumentException("Unrecognized api version");
        }
    }
}
