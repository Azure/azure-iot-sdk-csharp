// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Registry;
using Microsoft.Azure.Devices.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Api.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class DeviceAuthenticationTests
    {
        private const string IotHubName = "acme";

        [TestMethod]
        public async Task DeviceAuthenticationGoodAuthConfigTest1()
        {
            var deviceGoodAuthConfig = new Device("123")
            {
                ConnectionState = DeviceConnectionState.Connected,
                Authentication = new AuthenticationMechanism()
                {
                    SymmetricKey = new SymmetricKey()
                    {
                        PrimaryKey = CryptoKeyGenerator.GenerateKey(32),
                        SecondaryKey = CryptoKeyGenerator.GenerateKey(32)
                    },
                    X509Thumbprint = new X509Thumbprint()
                }
            };

            string responsePayload = JsonConvert.SerializeObject(deviceGoodAuthConfig);
            HttpContent mockContent = new StringContent(responsePayload);
            var responseMock = new Mock<HttpResponseMessage>();
            responseMock.Setup(responseMock => responseMock.StatusCode).Returns(HttpStatusCode.OK);
            responseMock.Setup(responseMock => responseMock.Content).Returns(mockContent);

            var restOpMock = new Mock<HttpClient>();
            restOpMock.Setup(
                restOp =>
                    restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(responseMock.Object);
            HttpTransportSettings2 settings = new HttpTransportSettings2();
            settings.HttpClient = restOpMock.Object;
            var registryClient = new RegistryClient(IotHubName, settings);
            await registryClient.AddDeviceAsync(deviceGoodAuthConfig).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceAuthenticationGoodAuthConfigTest2()
        {
            var deviceGoodAuthConfig = new Device("123")
            {
                ConnectionState = DeviceConnectionState.Connected,
                Authentication = new AuthenticationMechanism()
                {
                    SymmetricKey = null,
                    X509Thumbprint = new X509Thumbprint()
                    {
                        PrimaryThumbprint = "921BC9694ADEB8929D4F7FE4B9A3A6DE58B0790B",
                        SecondaryThumbprint = "921BC9694ADEB8929D4F7FE4B9A3A6DE58B0790B",
                    }
                }
            };

            string responsePayload = JsonConvert.SerializeObject(deviceGoodAuthConfig);
            HttpContent mockContent = new StringContent(responsePayload);
            var responseMock = new Mock<HttpResponseMessage>();
            responseMock.Setup(responseMock => responseMock.StatusCode).Returns(HttpStatusCode.OK);
            responseMock.Setup(responseMock => responseMock.Content).Returns(mockContent);

            var restOpMock = new Mock<HttpClient>();
            restOpMock.Setup(
                restOp =>
                    restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(responseMock.Object);
            HttpTransportSettings2 settings = new HttpTransportSettings2();
            settings.HttpClient = restOpMock.Object;
            var registryClient = new RegistryClient(IotHubName, settings);
            await registryClient.AddDeviceAsync(deviceGoodAuthConfig).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceAuthenticationGoodAuthConfigTest3()
        {
            var deviceGoodAuthConfig = new Device("123")
            {
                ConnectionState = DeviceConnectionState.Connected,
                Authentication = new AuthenticationMechanism()
                {
                    SymmetricKey = null,
                    X509Thumbprint = new X509Thumbprint()
                    {
                        PrimaryThumbprint = "921BC9694ADEB8929D4F7FE4B9A3A6DE58B0790B",
                        SecondaryThumbprint = null
                    }
                }
            };

            string responsePayload = JsonConvert.SerializeObject(deviceGoodAuthConfig);
            HttpContent mockContent = new StringContent(responsePayload);
            var responseMock = new Mock<HttpResponseMessage>();
            responseMock.Setup(responseMock => responseMock.StatusCode).Returns(HttpStatusCode.OK);
            responseMock.Setup(responseMock => responseMock.Content).Returns(mockContent);

            var restOpMock = new Mock<HttpClient>();
            restOpMock.Setup(
                restOp =>
                    restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(responseMock.Object);
            HttpTransportSettings2 settings = new HttpTransportSettings2();
            settings.HttpClient = restOpMock.Object;
            var registryClient = new RegistryClient(IotHubName, settings);
            await registryClient.AddDeviceAsync(deviceGoodAuthConfig).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceAuthenticationGoodAuthConfigTest4()
        {
            var deviceGoodAuthConfig = new Device("123")
            {
                ConnectionState = DeviceConnectionState.Connected,
                Authentication = new AuthenticationMechanism()
                {
                    SymmetricKey = null,
                    X509Thumbprint = new X509Thumbprint()
                    {
                        PrimaryThumbprint = null,
                        SecondaryThumbprint = "921BC9694ADEB8929D4F7FE4B9A3A6DE58B0790B"
                    }
                }
            };

            string responsePayload = JsonConvert.SerializeObject(deviceGoodAuthConfig);
            HttpContent mockContent = new StringContent(responsePayload);
            var responseMock = new Mock<HttpResponseMessage>();
            responseMock.Setup(responseMock => responseMock.StatusCode).Returns(HttpStatusCode.OK);
            responseMock.Setup(responseMock => responseMock.Content).Returns(mockContent);

            var restOpMock = new Mock<HttpClient>();
            restOpMock.Setup(
                restOp =>
                    restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(responseMock.Object);
            HttpTransportSettings2 settings = new HttpTransportSettings2();
            settings.HttpClient = restOpMock.Object;
            var registryClient = new RegistryClient(IotHubName, settings);
            await registryClient.AddDeviceAsync(deviceGoodAuthConfig).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceAuthenticationGoodAuthConfigTest5()
        {
            var deviceGoodAuthConfig = new Device("123")
            {
                ConnectionState = DeviceConnectionState.Connected,
                Authentication = new AuthenticationMechanism()
                {
                    SymmetricKey = new SymmetricKey()
                    {
                        PrimaryKey = CryptoKeyGenerator.GenerateKey(32),
                        SecondaryKey = CryptoKeyGenerator.GenerateKey(32)
                    },
                    X509Thumbprint = null
                }
            };

            string responsePayload = JsonConvert.SerializeObject(deviceGoodAuthConfig);
            HttpContent mockContent = new StringContent(responsePayload);
            var responseMock = new Mock<HttpResponseMessage>();
            responseMock.Setup(responseMock => responseMock.StatusCode).Returns(HttpStatusCode.OK);
            responseMock.Setup(responseMock => responseMock.Content).Returns(mockContent);

            var restOpMock = new Mock<HttpClient>();
            restOpMock.Setup(
                restOp =>
                    restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(responseMock.Object);
            HttpTransportSettings2 settings = new HttpTransportSettings2();
            settings.HttpClient = restOpMock.Object;
            var registryClient = new RegistryClient(IotHubName, settings);
            await registryClient.AddDeviceAsync(deviceGoodAuthConfig).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceAuthenticationGoodAuthConfigTest6()
        {
            var deviceBadAuthConfig = new Device("123")
            {
                ConnectionState = DeviceConnectionState.Connected,
                Authentication = new AuthenticationMechanism()
                {
                    SymmetricKey = null,
                    X509Thumbprint = new X509Thumbprint()
                    {
                        PrimaryThumbprint = "921BC9694ADEB8929D4F7FE4B9A3A6DE58B0790B",
                        SecondaryThumbprint = "781BC9694ADEB8929D4F7FE4B9A3A6DE58B07952"
                    }
                }
            };

            var restOpMock = new Mock<IHttpClientHelper>();
            restOpMock.Setup(
                restOp =>
                    restOp.PutAsync(It.IsAny<Uri>(), It.IsAny<Device>(), It.IsAny<PutOperationType>(),
                        It.IsAny<IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>>(),
                        It.IsAny<CancellationToken>())).ReturnsAsync(deviceBadAuthConfig);
            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.AddDeviceAsync(deviceBadAuthConfig).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceAuthenticationGoodAuthSHA256()
        {
            var deviceBadAuthConfig = new Device("123")
            {
                ConnectionState = DeviceConnectionState.Connected,
                Authentication = new AuthenticationMechanism()
                {
                    SymmetricKey = null,
                    X509Thumbprint = new X509Thumbprint()
                    {
                        PrimaryThumbprint = "921BC9694ADEB8929D4F7FE4B9A3A6DE58B0790B7FE4B9A3A6DE58B0790B790B",
                        SecondaryThumbprint = "781BC9694ADEB8929D4F7FE4B9A3A6DE58B079527FE4B9A3A6DE58B079527952"
                    }
                }
            };

            var restOpMock = new Mock<IHttpClientHelper>();
            restOpMock.Setup(
                restOp =>
                    restOp.PutAsync(It.IsAny<Uri>(), It.IsAny<Device>(), It.IsAny<PutOperationType>(),
                        It.IsAny<IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>>(),
                        It.IsAny<CancellationToken>())).ReturnsAsync(deviceBadAuthConfig);
            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.AddDeviceAsync(deviceBadAuthConfig).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task DeviceAuthenticationBadAuthConfigTest1()
        {
            var deviceBadAuthConfig = new Device("123")
            {
                ConnectionState = DeviceConnectionState.Connected,
                Authentication = new AuthenticationMechanism()
                {
                    SymmetricKey = new SymmetricKey()
                    {
                        PrimaryKey = CryptoKeyGenerator.GenerateKey(32),
                        SecondaryKey = CryptoKeyGenerator.GenerateKey(32)
                    },
                    X509Thumbprint = new X509Thumbprint()
                    {
                        PrimaryThumbprint = "921BC9694ADEB8929D4F7FE4B9A3A6DE58B0790B",
                        SecondaryThumbprint = "921BC9694ADEB8929D4F7FE4B9A3A6DE58B0790B"
                    }
                }
            };

            var restOpMock = new Mock<IHttpClientHelper>();
            restOpMock.Setup(
                restOp =>
                    restOp.PutAsync(It.IsAny<Uri>(), It.IsAny<Device>(), It.IsAny<PutOperationType>(),
                        It.IsAny<IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>>(),
                        It.IsAny<CancellationToken>())).ReturnsAsync(deviceBadAuthConfig);
            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.AddDeviceAsync(deviceBadAuthConfig).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task DeviceAuthenticationBadAuthConfigTest2()
        {
            var deviceBadAuthConfig = new Device("123")
            {
                ConnectionState = DeviceConnectionState.Connected,
                Authentication = new AuthenticationMechanism()
                {
                    SymmetricKey = new SymmetricKey()
                    {
                        PrimaryKey = CryptoKeyGenerator.GenerateKey(32),
                        SecondaryKey = CryptoKeyGenerator.GenerateKey(32)
                    },
                    X509Thumbprint = new X509Thumbprint()
                    {
                        PrimaryThumbprint = null,
                        SecondaryThumbprint = "921BC9694ADEB8929D4F7FE4B9A3A6DE58B0790B"
                    }
                }
            };

            var restOpMock = new Mock<IHttpClientHelper>();
            restOpMock.Setup(
                restOp =>
                    restOp.PutAsync(It.IsAny<Uri>(), It.IsAny<Device>(), It.IsAny<PutOperationType>(),
                        It.IsAny<IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>>(),
                        It.IsAny<CancellationToken>())).ReturnsAsync(deviceBadAuthConfig);
            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.AddDeviceAsync(deviceBadAuthConfig).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task DeviceAuthenticationBadAuthConfigTest3()
        {
            var deviceBadAuthConfig = new Device("123")
            {
                ConnectionState = DeviceConnectionState.Connected,
                Authentication = new AuthenticationMechanism()
                {
                    SymmetricKey = new SymmetricKey()
                    {
                        PrimaryKey = CryptoKeyGenerator.GenerateKey(32),
                        SecondaryKey = CryptoKeyGenerator.GenerateKey(32)
                    },
                    X509Thumbprint = new X509Thumbprint()
                    {
                        PrimaryThumbprint = "921BC9694ADEB8929D4F7FE4B9A3A6DE58B0790B",
                        SecondaryThumbprint = null
                    }
                }
            };

            var restOpMock = new Mock<IHttpClientHelper>();
            restOpMock.Setup(
                restOp =>
                    restOp.PutAsync(It.IsAny<Uri>(), It.IsAny<Device>(), It.IsAny<PutOperationType>(),
                        It.IsAny<IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>>(),
                        It.IsAny<CancellationToken>())).ReturnsAsync(deviceBadAuthConfig);
            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.AddDeviceAsync(deviceBadAuthConfig).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task DeviceAuthenticationBadAuthConfigTest4()
        {
            var deviceBadAuthConfig = new Device("123")
            {
                ConnectionState = DeviceConnectionState.Connected,
                Authentication = new AuthenticationMechanism()
                {
                    SymmetricKey = new SymmetricKey()
                    {
                        PrimaryKey = CryptoKeyGenerator.GenerateKey(32),
                        SecondaryKey = null
                    },
                    X509Thumbprint = new X509Thumbprint()
                    {
                        PrimaryThumbprint = "921BC9694ADEB8929D4F7FE4B9A3A6DE58B0790B",
                        SecondaryThumbprint = "921BC9694ADEB8929D4F7FE4B9A3A6DE58B0790B"
                    }
                }
            };

            var restOpMock = new Mock<IHttpClientHelper>();
            restOpMock.Setup(
                restOp =>
                    restOp.PutAsync(It.IsAny<Uri>(), It.IsAny<Device>(), It.IsAny<PutOperationType>(),
                        It.IsAny<IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>>(),
                        It.IsAny<CancellationToken>())).ReturnsAsync(deviceBadAuthConfig);
            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.AddDeviceAsync(deviceBadAuthConfig).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task DeviceAuthenticationBadAuthConfigTest5()
        {
            var deviceBadAuthConfig = new Device("123")
            {
                ConnectionState = DeviceConnectionState.Connected,
                Authentication = new AuthenticationMechanism()
                {
                    SymmetricKey = new SymmetricKey()
                    {
                        PrimaryKey = null,
                        SecondaryKey = CryptoKeyGenerator.GenerateKey(32)
                    },
                    X509Thumbprint = new X509Thumbprint()
                    {
                        PrimaryThumbprint = "921BC9694ADEB8929D4F7FE4B9A3A6DE58B0790B",
                        SecondaryThumbprint = "921BC9694ADEB8929D4F7FE4B9A3A6DE58B0790B"
                    }
                }
            };

            var restOpMock = new Mock<IHttpClientHelper>();
            restOpMock.Setup(
                restOp =>
                    restOp.PutAsync(It.IsAny<Uri>(), It.IsAny<Device>(), It.IsAny<PutOperationType>(),
                        It.IsAny<IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>>(),
                        It.IsAny<CancellationToken>())).ReturnsAsync(deviceBadAuthConfig);
            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.AddDeviceAsync(deviceBadAuthConfig).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task DeviceAuthenticationBadAuthConfigTest6()
        {
            var deviceBadAuthConfig = new Device("123")
            {
                ConnectionState = DeviceConnectionState.Connected,
                Authentication = new AuthenticationMechanism()
                {
                    SymmetricKey = new SymmetricKey()
                    {
                        PrimaryKey = null,
                        SecondaryKey = CryptoKeyGenerator.GenerateKey(32)
                    },
                    X509Thumbprint = null
                }
            };

            var restOpMock = new Mock<IHttpClientHelper>();
            restOpMock.Setup(
                restOp =>
                    restOp.PutAsync(It.IsAny<Uri>(), It.IsAny<Device>(), It.IsAny<PutOperationType>(),
                        It.IsAny<IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>>(),
                        It.IsAny<CancellationToken>())).ReturnsAsync(deviceBadAuthConfig);
            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.AddDeviceAsync(deviceBadAuthConfig).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task DeviceAuthenticationBadAuthConfigTest7()
        {
            var deviceBadAuthConfig = new Device("123")
            {
                ConnectionState = DeviceConnectionState.Connected,
                Authentication = new AuthenticationMechanism()
                {
                    SymmetricKey = new SymmetricKey()
                    {
                        PrimaryKey = CryptoKeyGenerator.GenerateKey(32),
                        SecondaryKey = null
                    },
                    X509Thumbprint = null
                }
            };

            var restOpMock = new Mock<IHttpClientHelper>();
            restOpMock.Setup(
                restOp =>
                    restOp.PutAsync(It.IsAny<Uri>(), It.IsAny<Device>(), It.IsAny<PutOperationType>(),
                        It.IsAny<IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>>(),
                        It.IsAny<CancellationToken>())).ReturnsAsync(deviceBadAuthConfig);
            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.AddDeviceAsync(deviceBadAuthConfig).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task DeviceAuthenticationBadThumbprintTest1()
        {
            var deviceBadThumbprint = new Device("123")
            {
                ConnectionState = DeviceConnectionState.Connected,
                Authentication = new AuthenticationMechanism()
                {
                    SymmetricKey = null,
                    X509Thumbprint = new X509Thumbprint()
                    {
                        PrimaryThumbprint = null,
                        SecondaryThumbprint = "921BC9694ADEB8929D4F7FE4B9A3A6DE58B079"
                    }
                }
            };

            var restOpMock = new Mock<IHttpClientHelper>();
            restOpMock.Setup(
                restOp =>
                    restOp.PutAsync(It.IsAny<Uri>(), It.IsAny<Device>(), It.IsAny<PutOperationType>(),
                        It.IsAny<IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>>(),
                        It.IsAny<CancellationToken>())).ReturnsAsync(deviceBadThumbprint);
            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.AddDeviceAsync(deviceBadThumbprint).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task DeviceAuthenticationBadThumbprintTest2()
        {
            var deviceBadThumbprint = new Device("123")
            {
                ConnectionState = DeviceConnectionState.Connected,
                Authentication = new AuthenticationMechanism()
                {
                    SymmetricKey = null,
                    X509Thumbprint = new X509Thumbprint()
                    {
                        PrimaryThumbprint = "921BC9694ADEB8929D4F74B9A3A6DE58B0790B",
                        SecondaryThumbprint = null
                    }
                }
            };

            var restOpMock = new Mock<IHttpClientHelper>();
            restOpMock.Setup(
                restOp =>
                    restOp.PutAsync(It.IsAny<Uri>(), It.IsAny<Device>(), It.IsAny<PutOperationType>(),
                        It.IsAny<IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>>(),
                        It.IsAny<CancellationToken>())).ReturnsAsync(deviceBadThumbprint);
            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.AddDeviceAsync(deviceBadThumbprint).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task DeviceAuthenticationBadThumbprintTest3()
        {
            var deviceBadThumbprint = new Device("123")
            {
                ConnectionState = DeviceConnectionState.Connected,
                Authentication = new AuthenticationMechanism()
                {
                    SymmetricKey = null,
                    X509Thumbprint = new X509Thumbprint()
                    {
                        PrimaryThumbprint = "921BC9694ADEB8929D4F74B9A3A",
                        SecondaryThumbprint = "8929D4F74B9A3A6DE58B0790B"
                    }
                }
            };

            var restOpMock = new Mock<IHttpClientHelper>();
            restOpMock.Setup(
                restOp =>
                    restOp.PutAsync(It.IsAny<Uri>(), It.IsAny<Device>(), It.IsAny<PutOperationType>(),
                        It.IsAny<IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>>(),
                        It.IsAny<CancellationToken>())).ReturnsAsync(deviceBadThumbprint);
            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.AddDeviceAsync(deviceBadThumbprint).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task DeviceAuthenticationBadThumbprintTest4()
        {
            var deviceBadThumbprint = new Device("123")
            {
                ConnectionState = DeviceConnectionState.Connected,
                Authentication = new AuthenticationMechanism()
                {
                    SymmetricKey = null,
                    X509Thumbprint = new X509Thumbprint()
                    {
                        PrimaryThumbprint = "921BC9694ADEB8929D4F74B9A3A",
                        SecondaryThumbprint = null
                    }
                }
            };

            var restOpMock = new Mock<IHttpClientHelper>();
            restOpMock.Setup(
                restOp =>
                    restOp.PutAsync(It.IsAny<Uri>(), It.IsAny<Device>(), It.IsAny<PutOperationType>(),
                        It.IsAny<IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>>(),
                        It.IsAny<CancellationToken>())).ReturnsAsync(deviceBadThumbprint);
            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.AddDeviceAsync(deviceBadThumbprint).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task DeviceAuthenticationBadThumbprintTest5()
        {
            var deviceBadThumbprint = new Device("123")
            {
                ConnectionState = DeviceConnectionState.Connected,
                Authentication = new AuthenticationMechanism()
                {
                    SymmetricKey = null,
                    X509Thumbprint = new X509Thumbprint()
                    {
                        PrimaryThumbprint = null,
                        SecondaryThumbprint = "8929D4F74B9A3A6DE58B0790B"
                    }
                }
            };

            var restOpMock = new Mock<IHttpClientHelper>();
            restOpMock.Setup(
                restOp =>
                    restOp.PutAsync(It.IsAny<Uri>(), It.IsAny<Device>(), It.IsAny<PutOperationType>(),
                        It.IsAny<IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>>(),
                        It.IsAny<CancellationToken>())).ReturnsAsync(deviceBadThumbprint);
            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.AddDeviceAsync(deviceBadThumbprint).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task DeviceAuthenticationBadThumbprintTest6()
        {
            var deviceBadThumbprint = new Device("123")
            {
                ConnectionState = DeviceConnectionState.Connected,
                Authentication = new AuthenticationMechanism()
                {
                    SymmetricKey = null,
                    X509Thumbprint = new X509Thumbprint()
                    {
                        PrimaryThumbprint = "921BC9694ADEB8929D4F7FE4B9A3A6DE58B0790B",
                        SecondaryThumbprint = "8929D4F74B9A3A6DE58B0790B"
                    }
                }
            };

            var restOpMock = new Mock<IHttpClientHelper>();
            restOpMock.Setup(
                restOp =>
                    restOp.PutAsync(It.IsAny<Uri>(), It.IsAny<Device>(), It.IsAny<PutOperationType>(),
                        It.IsAny<IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>>(),
                        It.IsAny<CancellationToken>())).ReturnsAsync(deviceBadThumbprint);
            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.AddDeviceAsync(deviceBadThumbprint).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task DeviceAuthenticationBadThumbprintTest7()
        {
            var deviceBadThumbprint = new Device("123")
            {
                ConnectionState = DeviceConnectionState.Connected,
                Authentication = new AuthenticationMechanism()
                {
                    SymmetricKey = null,
                    X509Thumbprint = new X509Thumbprint()
                    {
                        PrimaryThumbprint = "921BC9694ADEB8929D4F74B9A3A",
                        SecondaryThumbprint = "921BC9694ADEB8929D4F7FE4B9A3A6DE58B0790B"
                    }
                }
            };

            var restOpMock = new Mock<IHttpClientHelper>();
            restOpMock.Setup(
                restOp =>
                    restOp.PutAsync(It.IsAny<Uri>(), It.IsAny<Device>(), It.IsAny<PutOperationType>(),
                        It.IsAny<IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>>(),
                        It.IsAny<CancellationToken>())).ReturnsAsync(deviceBadThumbprint);
            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.AddDeviceAsync(deviceBadThumbprint).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task DeviceAuthenticationBadThumbprintSHA256Test()
        {
            var deviceBadAuthConfig = new Device("123")
            {
                ConnectionState = DeviceConnectionState.Connected,
                Authentication = new AuthenticationMechanism()
                {
                    SymmetricKey = null,
                    X509Thumbprint = new X509Thumbprint()
                    {
                        PrimaryThumbprint = "921BC9694ADEB8929D4F7FE4B9A3A6DE58B0790B7FE4B9A3A6DE58B0790B790B",
                        SecondaryThumbprint = "781BC9694ADEB8929D4F7FE4B9A3A6DE58B07952"
                    }
                }
            };

            var restOpMock = new Mock<IHttpClientHelper>();
            restOpMock.Setup(
                restOp =>
                    restOp.PutAsync(It.IsAny<Uri>(), It.IsAny<Device>(), It.IsAny<PutOperationType>(),
                        It.IsAny<IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>>(),
                        It.IsAny<CancellationToken>())).ReturnsAsync(deviceBadAuthConfig);
            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.AddDeviceAsync(deviceBadAuthConfig).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceAuthenticationIsCertificateAuthority()
        {
            var deviceBadThumbprint = new Device("123")
            {
                ConnectionState = DeviceConnectionState.Connected,
                Authentication = new AuthenticationMechanism
                {
                    Type = AuthenticationType.CertificateAuthority,
                    SymmetricKey = null,
                    X509Thumbprint = null
                }
            };

            var restOpMock = new Mock<IHttpClientHelper>();
            restOpMock.Setup(
                restOp =>
                    restOp.PutAsync(It.IsAny<Uri>(), It.IsAny<Device>(), It.IsAny<PutOperationType>(),
                        It.IsAny<IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>>(),
                        It.IsAny<CancellationToken>())).ReturnsAsync(deviceBadThumbprint);
            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.AddDeviceAsync(deviceBadThumbprint).ConfigureAwait(false);
        }
    }
}
