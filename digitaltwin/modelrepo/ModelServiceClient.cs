using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Azure;
using Azure.Core.Http;
using Azure.IoT.DigitalTwin.Model.Service.Generated;
using Azure.IoT.DigitalTwin.Model.Service.Generated.Models;
using Microsoft.Azure.Devices.Common.Authorization;
using Microsoft.Rest;

namespace Microsoft.Azure.DigitalTwin.Model.Service
{
    public class ModelServiceClient
    {
        private DigitalTwinRepositoryService digitalTwinRepositoryService;

        private const string _apiVersion = "v1";
        private string repositoryId = "";

        /// <summary> Initializes a new instance of the <see cref="ModelServiceClient"/> class.</summary>
        protected ModelServiceClient()
        {

        } 

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelServiceClient"/> class.</summary>
        /// <param name="connectionString">Your Model Repo's connection string.</param>
        public ModelServiceClient(string connectionString)
        {
            GuardHelper.ThrowIfNullOrWhiteSpace(connectionString, nameof(connectionString));

            var modelConnectionStringParser = ModelServiceConnectionStringParser.CreateForModel(connectionString);
            this.repositoryId = modelConnectionStringParser.RespositoryId;
            ModelServiceConnectionString modelServiceConnectionString = new ModelServiceConnectionString(modelConnectionStringParser);
            IoTServiceClientCredentials serviceClientCredentials = new ModelSharedAccessKeyCredentials(modelServiceConnectionString);
            // parse repository Id
            this.SetupModelServiceClient(modelServiceConnectionString.HttpsEndpoint, serviceClientCredentials);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelServiceClient"/> class.
        /// The client will use the provided endpoint and will generate credentials for each request using the provided <paramref name="credentials"/>.
        /// </summary>
        /// <param name="endpoint">The endpoint to connect to.</param>
        /// <param name="credentials">The SAS token provider to use for authorization.</param>
        /// <param name="options">The options for the client instance to use.</param>
        public ModelServiceClient(Uri endpoint, IoTServiceClientCredentials credentials, string repositoryId)
        {
            GuardHelper.ThrowIfNull(endpoint, nameof(endpoint));
            GuardHelper.ThrowIfNull(credentials, nameof(credentials));
            GuardHelper.ThrowIfNull(repositoryId, nameof(repositoryId));
            this.repositoryId = repositoryId;
            this.SetupModelServiceClient(endpoint, credentials);
        }

        private void SetupModelServiceClient(Uri endpoint, IoTServiceClientCredentials credentials)
        {
            DelegatingHandler[] handlers = new DelegatingHandler[1] { new AuthorizationDelegatingHandler(credentials) };
            this.digitalTwinRepositoryService = new DigitalTwinRepositoryService(credentials, handlers)
            {
                BaseUri = endpoint,
            };
        }

        /// <summary>
        /// Gets a DigitalTwin model object for the given digital twin model id.
        /// </summary>
        /// <param name='modelId'>
        /// Digital twin model id Ex:
        /// &lt;example&gt;urn:contoso:com:temperaturesensor:1&lt;/example&gt;
        /// <param name='expand'>
        /// Indicates whether to expand the capability model's interface definitions
        /// inline or not. This query parameter ONLY applies to Capability model.
        /// </param>
        /// <param name='clientRequestId'>
        /// Optional. Provides a client-generated opaque value that is recorded in the
        /// logs. Using this header is highly recommended for correlating client-side
        /// activities with requests received by the server.
        /// </param>
        /// <param name='cancellationToken'>
        /// The cancellation token.
        /// </param>
        /// <return>
        /// A response object containing the model definition.
        /// </return>
        public virtual Response<GetModelResponse> GetModel(string modelId, bool expand = false, string clientRequestId = default(string), CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.GetModelAsync(modelId, expand, clientRequestId, cancellationToken).Result;
        }

        public virtual async Task<Response<GetModelResponse>> GetModelAsync(string modelId,  bool expand = false, string clientRequestId = default, CancellationToken cancellationToken = default)
        {
            var config = new MapperConfiguration(cfg => {
                cfg.CreateMap<GetModelHeaders, GetModelResponse>();
            });
            IMapper iMapper = config.CreateMapper();

            var result = await digitalTwinRepositoryService.GetModelWithHttpMessagesAsync(modelId, _apiVersion, this.repositoryId, clientRequestId, expand, null, cancellationToken).ConfigureAwait(false);

            var getModelResponse = iMapper.Map<GetModelHeaders, GetModelResponse>(result.Headers);
            getModelResponse.StatusCode = result.Response.StatusCode;
            getModelResponse.Payload = result.Body.ToString();

            return new Response<GetModelResponse>(null, getModelResponse);
        }

        /// <summary>
        /// Creates a DigitalTwin Model in a repository.
        /// </summary>
        /// <param name='modelId'>
        /// Digital twin model id Ex:
        /// &lt;example&gt;urn:contoso:TemparatureSensor:1&lt;/example&gt;
        /// </param>
        /// <param name='content'>
        /// Model definition in Digital Twin Definition Language format.
        /// </param>
        /// <param name='clientRequestId'>
        /// Optional. Provides a client-generated opaque value that is recorded in the
        /// logs. Using this header is highly recommended for correlating client-side
        /// activities with requests received by the server.
        /// </param>
        /// <param name='cancellationToken'>
        /// The cancellation token.
        /// </param>
        /// <return>
        /// A response with request Id and Status of the request.
        /// </return>
        public virtual Response<CreateOrUpdateModelResponse> CreateModel(string modelId, string content, string clientRequestId = default(string), CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.CreateModelAsync(modelId, content, clientRequestId, cancellationToken).Result;
        }

        public virtual async Task<Response<CreateOrUpdateModelResponse>> CreateModelAsync(string modelId, string content, string clientRequestId = default(string), CancellationToken cancellationToken = default(CancellationToken))
        {
            var config = new MapperConfiguration(cfg => {
                cfg.CreateMap<CreateOrUpdateModelHeaders, CreateOrUpdateModelResponse>();
            });
            IMapper iMapper = config.CreateMapper();

            var _result = new HttpOperationHeaderResponse<CreateOrUpdateModelHeaders>();
            _result = await digitalTwinRepositoryService.CreateOrUpdateModelWithHttpMessagesAsync(modelId, _apiVersion, (object) content, this.repositoryId, clientRequestId, null, null, cancellationToken).ConfigureAwait(false);

            var createModelResponse = iMapper.Map<CreateOrUpdateModelHeaders, CreateOrUpdateModelResponse>(_result.Headers);
            createModelResponse.StatusCode = _result.Response.StatusCode;

            return new Response<CreateOrUpdateModelResponse>(null, createModelResponse);
        }

        /// <summary>
        /// Update a DigitalTwin Model in a repository.
        /// </summary>
        /// <param name='modelId'>
        /// Digital twin model id Ex:
        /// &lt;example&gt;urn:contoso:TemparatureSensor:1&lt;/example&gt;
        /// </param>
        /// <param name='content'>
        /// Model definition in Digital Twin Definition Language format.
        /// </param>
        /// <param name='clientRequestId'>
        /// Optional. Provides a client-generated opaque value that is recorded in the
        /// logs. Using this header is highly recommended for correlating client-side
        /// activities with requests received by the server.
        /// </param>
        /// <param name='ifMatchEtag'>
        /// Used to make operation conditional for optimistic concurrency. That is, the
        /// document is updated only if the specified etag matches the current version
        /// in the database. The value should be set to the etag value of the resource.
        /// </param>
        /// <param name='cancellationToken'>
        /// The cancellation token.
        /// </param>
        /// <return>
        /// A response with request Id and Status of the request.
        /// </return>
        public virtual Response<CreateOrUpdateModelResponse> UpdateModel(string modelId, string content, string ifMatchEtag, string clientRequestId = default(string), CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.UpdateModelAsync(modelId, content, ifMatchEtag, clientRequestId, cancellationToken).Result;
        }

        public virtual async Task<Response<CreateOrUpdateModelResponse>> UpdateModelAsync(string modelId, string content, string ifMatchEtag, string clientRequestId = default(string), CancellationToken cancellationToken = default(CancellationToken))
        {
            var config = new MapperConfiguration(cfg => {
                cfg.CreateMap<CreateOrUpdateModelHeaders, CreateOrUpdateModelResponse>();
            });
            IMapper iMapper = config.CreateMapper();

            var _result = new HttpOperationHeaderResponse<CreateOrUpdateModelHeaders>();
            _result = await digitalTwinRepositoryService.CreateOrUpdateModelWithHttpMessagesAsync(modelId, _apiVersion, (object)content, this.repositoryId, clientRequestId, ifMatchEtag, null, cancellationToken).ConfigureAwait(false);

            var updateModelResponse = iMapper.Map<CreateOrUpdateModelHeaders, CreateOrUpdateModelResponse>(_result.Headers);
            updateModelResponse.StatusCode = _result.Response.StatusCode;

            return new Response<CreateOrUpdateModelResponse>(null, updateModelResponse);
        }



        /// <summary>
        /// Searches repository for Digital twin models matching supplied search
        /// options.
        /// </summary>
        /// <param name='searchOptions'>
        /// searchKeyword: To search models with the keyword.
        /// modelFilterType: To filter a type of Digital twin models (Ex: Interface or
        /// CapabilityModel).
        /// pageSize: Page size per request.
        /// continuationToken: When there are more results than a page size, server
        /// responds with a continuation token. Supply this token to retrieve next page
        /// results.
        /// <param name='clientRequestId'>
        /// Optional. Provides a client-generated opaque value that is recorded in the
        /// logs. Using this header is highly recommended for correlating client-side
        /// activities with requests received by the server..
        /// </param>
        /// <param name='cancellationToken'>
        /// The cancellation token.
        /// </param>
        /// <return>
        /// A response List containing search results.
        /// </return>
        public virtual Response<SearchModelResponse> SearchModel(SearchModelOptions searchModelOptions, string clientRequestId = default(string), CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.SearchModelAsync(searchModelOptions, clientRequestId, cancellationToken).Result;
        }

        public virtual async Task<Response<SearchModelResponse>> SearchModelAsync(SearchModelOptions searchModelOptions, string clientRequestId = default(string), CancellationToken cancellationToken = default(CancellationToken))
        {
            var config = new MapperConfiguration(cfg => {
                cfg.CreateMap<SearchModelHeaders, SearchModelResponse>();
            });
            IMapper iMapperResponse = config.CreateMapper();

            var configInput = new MapperConfiguration(cfg => {
                cfg.CreateMap<SearchModelOptions, SearchOptions>();
            });
            IMapper iMapperInput = configInput.CreateMapper();

            var searchOptions = iMapperInput.Map<SearchModelOptions, SearchOptions>(searchModelOptions);

            var _result = new HttpOperationResponse<SearchResponse, SearchModelHeaders>();
            _result = await digitalTwinRepositoryService.SearchModelWithHttpMessagesAsync(searchOptions, _apiVersion, this.repositoryId, clientRequestId, null, cancellationToken).ConfigureAwait(false);

            var searchModelResponse = iMapperResponse.Map<SearchResponse, SearchModelResponse>(_result.Body);
            searchModelResponse.StatusCode = _result.Response.StatusCode;

            return new Response<SearchModelResponse>(null, searchModelResponse);
        }


        /// <summary>
        /// Deletes a Digital twin model from the repository.
        /// </summary>
        /// <param name='modelId'>
        /// Model id Ex:
        /// &lt;example&gt;urn:contoso:com:temparaturesensor:1&lt;/example&gt;
        /// </param>
        /// <param name='ClientRequestId'>
        /// Optional. Provides a client-generated opaque value that is recorded in the
        /// logs. Using this header is highly recommended for correlating client-side
        /// activities with requests received by the server.
        /// </param>
        /// <param name='cancellationToken'>
        /// The cancellation token.
        /// </param>
        /// <return>
        /// A response with request Id and Status of the request.
        /// </return>

        public virtual Response<DeleteModelResponse> DeleteModel(string modelId, string clientRequestId = default(string), CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.DeleteModelAsync(modelId, clientRequestId, cancellationToken).Result;
        }

        public virtual async Task<Response<DeleteModelResponse>> DeleteModelAsync(string modelId, string clientRequestId = default(string), CancellationToken cancellationToken = default(CancellationToken))
        {
            var config = new MapperConfiguration(cfg => {
                cfg.CreateMap<DeleteModelHeaders, DeleteModelResponse>();
            });
            IMapper iMapper = config.CreateMapper();

            var _result = await digitalTwinRepositoryService.DeleteModelWithHttpMessagesAsync(modelId, this.repositoryId, _apiVersion, clientRequestId, null, cancellationToken).ConfigureAwait(false);

            var deleteModelResponse = iMapper.Map<DeleteModelHeaders, DeleteModelResponse>(_result.Headers);
            deleteModelResponse.StatusCode = _result.Response.StatusCode;

            return new Response<DeleteModelResponse>(null, deleteModelResponse);
        }
    }
}
