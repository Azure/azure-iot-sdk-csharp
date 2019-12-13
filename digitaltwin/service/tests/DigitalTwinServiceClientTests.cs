using System;
using System.Threading;
using Microsoft.Azure.IoT.DigitalTwin.Service.models;
using NSubstitute;
using Xunit;

namespace Microsoft.Azure.IoT.DigitalTwin.Service.ServiceClient.Test
{
    public class DigitalTwinServiceClientTests
    {
        [Fact]
        public async void GetDigitalTwinCallsGetDigitalTwinAsync()
        {
            //arrange
            string digitalTwinId = "someDigitalTwinId";
            CancellationToken cancellationToken = CancellationToken.None;
            string mockedDigitalTwin = "someDigitalTwinString";
            Response mockedResponse = Substitute.For<Response>();
            Response<string> expectedResponse = new Response<string>(mockedResponse, mockedDigitalTwin);
            DigitalTwinServiceClient digitalTwinServiceClient = Substitute.ForPartsOf<DigitalTwinServiceClient>();
            digitalTwinServiceClient.When(client => client.GetDigitalTwinAsync(digitalTwinId, cancellationToken)).DoNotCallBase();
            digitalTwinServiceClient.GetDigitalTwinAsync(digitalTwinId, cancellationToken).Returns(expectedResponse);

            //act
            var actualResponse = digitalTwinServiceClient.GetDigitalTwin(digitalTwinId, cancellationToken);

            //assert
            Assert.Equal(expectedResponse, actualResponse);
            await digitalTwinServiceClient.Received().GetDigitalTwinAsync(digitalTwinId, cancellationToken).ConfigureAwait(false);
        }

        [Fact]
        public async void UpdateDigitalTwinPropertiesWithoutEtagCallsUpdateDigitalTwinPropertiesAsyncWithoutEtag()
        {
            //arrange
            string digitalTwinId = "someDigitalTwinId";
            CancellationToken cancellationToken = CancellationToken.None;
            string patch = "some json patch";
            string interfaceInstanceName = "someInterfaceInstanceName";

            string mockedDigitalTwin = "someDigitalTwinString";
            Response mockedResponse = Substitute.For<Response>();
            Response<string> expectedResponse = new Response<string>(mockedResponse, mockedDigitalTwin);
            DigitalTwinServiceClient digitalTwinServiceClient = Substitute.ForPartsOf<DigitalTwinServiceClient>();
            digitalTwinServiceClient.When(client => client.UpdateDigitalTwinPropertiesAsync(digitalTwinId, interfaceInstanceName, patch, cancellationToken)).DoNotCallBase();
            digitalTwinServiceClient.UpdateDigitalTwinPropertiesAsync(digitalTwinId, interfaceInstanceName, patch, cancellationToken).Returns(expectedResponse);

            //act
            var actualResponse = digitalTwinServiceClient.UpdateDigitalTwinProperties(digitalTwinId, interfaceInstanceName, patch, cancellationToken);

            //assert
            Assert.Equal(expectedResponse, actualResponse);
            await digitalTwinServiceClient.Received().UpdateDigitalTwinPropertiesAsync(digitalTwinId, interfaceInstanceName, patch, cancellationToken).ConfigureAwait(false);
        }

        [Fact]
        public async void UpdateDigitalTwinPropertiesWithEtagCallsUpdateDigitalTwinPropertiesAsyncWithEtag()
        {
            //arrange
            string digitalTwinId = "someDigitalTwinId";
            CancellationToken cancellationToken = CancellationToken.None;
            string patch = "some json patch";
            string etag = "some etag";

            string mockedDigitalTwin = "someDigitalTwinString";
            Response mockedResponse = Substitute.For<Response>();
            Response<string> expectedResponse = new Response<string>(mockedResponse, mockedDigitalTwin);
            DigitalTwinServiceClient digitalTwinServiceClient = Substitute.ForPartsOf<DigitalTwinServiceClient>();
            digitalTwinServiceClient.When(client => client.UpdateDigitalTwinPropertiesAsync(digitalTwinId, patch, etag, cancellationToken)).DoNotCallBase();
            digitalTwinServiceClient.UpdateDigitalTwinPropertiesAsync(digitalTwinId, patch, etag, cancellationToken).Returns(expectedResponse);

            //act
            var actualResponse = digitalTwinServiceClient.UpdateDigitalTwinProperties(digitalTwinId, patch, etag, cancellationToken);

            //assert
            Assert.Equal(expectedResponse, actualResponse);
            await digitalTwinServiceClient.Received().UpdateDigitalTwinPropertiesAsync(digitalTwinId, patch, etag, cancellationToken).ConfigureAwait(false);
        }

        [Fact]
        public async void UpdateDigitalTwinPropertiesAsyncWithoutEtagCallsUpdateDigitalTwinPropertiesAsyncWithEtag()
        {
            //arrange
            string digitalTwinId = "someDigitalTwinId";
            CancellationToken cancellationToken = CancellationToken.None;
            string patch = "some json patch";
            string etag = null;
            string interfaceInstanceName = "someInterfaceInstanceName";

            string mockedDigitalTwin = "someDigitalTwinString";
            Response mockedResponse = Substitute.For<Response>();
            Response<string> expectedResponse = new Response<string>(mockedResponse, mockedDigitalTwin);
            DigitalTwinServiceClient digitalTwinServiceClient = Substitute.ForPartsOf<DigitalTwinServiceClient>();
            digitalTwinServiceClient.When(client => client.UpdateDigitalTwinPropertiesAsync(digitalTwinId, interfaceInstanceName, patch, etag, cancellationToken)).DoNotCallBase();
            digitalTwinServiceClient.UpdateDigitalTwinPropertiesAsync(digitalTwinId, interfaceInstanceName, patch, etag, cancellationToken).Returns(expectedResponse);

            //act
            var actualResponse = await digitalTwinServiceClient.UpdateDigitalTwinPropertiesAsync(digitalTwinId, interfaceInstanceName, patch, cancellationToken);

            //assert
            Assert.Equal(expectedResponse, actualResponse);
            await digitalTwinServiceClient.Received().UpdateDigitalTwinPropertiesAsync(digitalTwinId, interfaceInstanceName, patch, etag, cancellationToken).ConfigureAwait(false);
        }

        [Fact]
        public async void GetModelCallsGetModelAsync()
        {
            //arrange
            string modelId = "someModelId";
            bool expand = false;
            CancellationToken cancellationToken = CancellationToken.None;
            Response mockedResponse = Substitute.For<Response>();
            Response<string> expectedResponse = new Response<string>(mockedResponse, "some model");
            DigitalTwinServiceClient digitalTwinServiceClient = Substitute.ForPartsOf<DigitalTwinServiceClient>();
            digitalTwinServiceClient.When(client => client.GetModelAsync(modelId, expand, cancellationToken)).DoNotCallBase();
            digitalTwinServiceClient.GetModelAsync(modelId, expand, cancellationToken).Returns(expectedResponse);

            //act
            var actualResponse = digitalTwinServiceClient.GetModel(modelId, expand, cancellationToken);

            //assert
            Assert.Equal(expectedResponse, actualResponse);
            await digitalTwinServiceClient.Received().GetModelAsync(modelId, expand, cancellationToken).ConfigureAwait(false);
        }

        [Fact]
        public async void InvokeCommandCallsInvokeCommandAsync()
        {
            //arrange
            string digitalTwinId = "someDigitalTwinId";
            string interfaceInstanceName = "someInterfaceInstanceName";
            string commandName = "someCommandName";
            string payload = "some payload";
            string requestId = "someRequestId";
            int? status = 200;
            CancellationToken cancellationToken = CancellationToken.None;
            Response mockedResponse = Substitute.For<Response>();
            DigitalTwinCommandResponse mockedCommandResponse = Substitute.For<DigitalTwinCommandResponse>(requestId, status, payload);
            Response<DigitalTwinCommandResponse> expectedResponse = new Response<DigitalTwinCommandResponse>(mockedResponse, mockedCommandResponse);
            DigitalTwinServiceClient digitalTwinServiceClient = Substitute.ForPartsOf<DigitalTwinServiceClient>();
            digitalTwinServiceClient.When(client => client.InvokeCommandAsync(digitalTwinId, interfaceInstanceName, commandName, payload, cancellationToken)).DoNotCallBase();
            digitalTwinServiceClient.InvokeCommandAsync(digitalTwinId, interfaceInstanceName, commandName, payload, cancellationToken).Returns(expectedResponse);

            //act
            var actualResponse = digitalTwinServiceClient.InvokeCommand(digitalTwinId, interfaceInstanceName, commandName, payload, cancellationToken);

            //assert
            Assert.NotNull(actualResponse.Value);
            Assert.Equal(expectedResponse, actualResponse.Value);
            await digitalTwinServiceClient.Received().InvokeCommandAsync(digitalTwinId, interfaceInstanceName, commandName, payload, cancellationToken).ConfigureAwait(false);
        }

        [Fact]
        public async void ConstructorThrowsForNullConnectionString()
        {
            Assert.Throws<ArgumentNullException>(() => new DigitalTwinServiceClient(null));
        }

        [Fact]
        public async void ConstructorThrowsForNullUri()
        {
            IoTServiceClientCredentials mockCredentials = Substitute.For<IoTServiceClientCredentials>();
            Assert.Throws<ArgumentNullException>(() => new DigitalTwinServiceClient(null, mockCredentials));
        }

        [Fact]
        public async void ConstructorThrowsForNullCredentials()
        {
            Uri mockUri = new Uri("https://www.microsoft.com");
            Assert.Throws<ArgumentNullException>(() => new DigitalTwinServiceClient(mockUri, (IoTServiceClientCredentials) null));
        }
    }
}