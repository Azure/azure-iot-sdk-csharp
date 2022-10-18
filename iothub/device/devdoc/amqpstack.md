# IoT Hub AMQP Stack architecture

## Abstract

This document describes the internal architecture for the Azure IoT Hub Device SDK AMQP transport layer as of v1.20.0.
The design is based on the https://github.com/azure/azure-amqp library.

## Architecture diagram

<!--amqpstack.puml-->
![csharpDeviceClientAmqpArchitecture](https://www.plantuml.com/plantuml/png/0/dLPHRzem47v7uZzOx8LkAxIL9hIgGcMXT4Y5TeLfawPfCibHh2PsPZiKrEs_xvo494x2gCeBrlc---xEtzcvL9fA7GK-STMAod08Q-Q2upFWUX3y3WVINJ4DheuaD1lD1bqeBQchYUjJfSXSKgv28VLdoZqV96asdfiDWhztv9Pdkuul57ZN28HbvFlR6fSXKwxW79d0QpR-6gw4mcHHfibOEft1vCiTcMcgeTD9RCVa5qNCMCnde1GJl6oUe9a-m0ouTyFuuvxjUt21hI7tC6mTqWRj7fdLBe4iDpiBB2u4Fb1RxkuCSHSiopVAz4Z8Kfyibx7wgkZ2XnrTSa142nCkyaXxUEOSvQIcgAkrERmyIAUJk28cHRUTis41b9PgyGuMs6fHAa04eGyQhDWJuKL-d52wQa_E9eNWLzoa7-TKvK4uw9NmLDa_DLlqfhR4VW_OBgxiIFCdnkr66Cirw4eGOx-ZsbrPEmbnUQ-an5mV0u-66XL_SK7Qi_TlHgDUx-JaYE0yQUQIVZ_MQ1LzjN4XrAZAL-AnzCZNhmGdOgxTocRbEPi7fkw4yBDn3AdxY7MJba6GT5WHGaBq8gWM4c1c31MoOVoXbKMgendX5IBaTfT_hpSQcVnd24I12MugCDZdtUZ5yfE00sO7Ya-fxSMo7dkeCQQtHmaeJjeiIdTM6i392Z1Lrj2PigOqWAADHdfbI5mQ0wLOCm_aV1jQNWha6cGP6z0D2w9W6WKBaBVBd5yLlQwfN62N7U7xk5jmAcZrnPLPLMMeFSxEfrcb2wW2BHw1tyDIWbg1L3z-5hETcTTZNl2u4I9CCtqPw_SF9n_hQ3TeTq1gjZJDXjfWHU9AZ6vtAZISazid3XvPRCcOuyHmwYUFMxURXOoZLbwoYp_7ntiUdpehQPsso5k4c7zRe-ISY2UkIFyGCg1ysohEwY6aI5Eit4g7_Sltq664TVEObjMFT7qrzhILwghk7mholm3voLNb_LBdH9LrCUBKH9wQhS5cWwFeaYioRqsBK4Mcf_iwXbyLPdDwYgj-vJvkDYxnoS8lj7y0 "csharpDeviceClientAmqpArchitecture")

## AMQPTransportHandler

The `AmqpTransportHandler` is always the last element of the transport pipeline, providing protocol adaptation from `DeviceClient` IoT APIs to the [AMQP IoT Hub specification](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-protocols). 

### Network connectivity

By default, each `DeviceClient`/`ModuleClient` will use a separate TCP/WSS non-pooled connection.

The Azure IoT Device SDK for .NET supports high performance scenarios such as [Azure IoT Protocol Gateway](https://github.com/Azure/azure-iot-protocol-gateway) and [Azure IoT Edge](https://github.com/Azure/iotedge). For this, the following features are available:
- Multiplexing allows multiple IoT identities (devices or modules) to share a single TCP/TLS connection.
- Connection pooling allows the SDK to automatically load-balance multiple clients on a small number of TCP/TLS connections.

For examples of how to configure the AMQP stack for high-performance, see our End-to-end and stress tests.

### Security

Three authentication methods are supported (see [here](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-security#access-control-and-permissions) for details):
1. Shared Access Signature (SAS) Token derived from the device/module's Shared Access Key.
1. SAS Token derived from any of the IoT Hub Shared Access Policies.
1. X509 client certificate (TLS mutual authentication).

SAS Token authentication is performed by following the [AMQP claims-based-security v1.0](https://www.oasis-open.org/committees/download.php/50506/amqp-cbs-v1%200-wd02%202013-08-12.doc) protocol extension (Draft).

### Functional requirements

1. `OpenAsync` will request an `AmqpIoTSession` from `AmqpIoTSessionFactory`. After obtaining the session, the handler assumes ownership of the session object. 
1. `OpenAsync` will always create and connect the `SendEvent` Amqp Link. (This is required by the IoT Hub service even if `SendMessageAsync` is not used by the application.) 
1. All other links will be created on demand. The handler has ownership on all links and needs to manage their lifetime.
1. The handler manages all disconnect notifications received from the underlying AMQP library (`AmqpObject.Closed`). These need to call `OnTransportDisconnected`.
1. All operations are performed through the adaptation layer formed by `AmqpIoTLink`, `AmqpIoTSession`, etc. wrappers/adapters to ensure that exceptions are translated into correct `IoTException`s and that transport the current state is reported to the pipeline.
1. `CloseAsync` will perform graceful shutdown of the `AmqpIoTLink` and `AmqpIoTSession`.
1. `Dispose` will immediately `Abort` the owned `AmqpIoTSession` (which also aborts all links) and dispose all other owned objects.

### Ownership

1. Owns the AMQP `session` (`AmqpIoTSession`).
1. Owns all AMQP `link` associated with the Amqp Session (`AmqpIoTLink`).
1. Authentication information is passed through and _not_ owned (i.e. `TokenRefresher` or `X509Certificate`).

## Connection Pooling

The AMQP protocol specifies the following constructs: A `connection` is a container of `session`s. A `session` is a container of `link`s. Note that `connection` is an AMQP protocol construct and can be layered on top of other protocols such as TLS, TCP or WebSockets. Pooling is performed for AMQP `connection`s. 

### Pool selection algorithm

`AmqpIoTConnectionPool`s are partitioning the connections by the following criteria:
1. Pool name (configurable by the application). A generated name will be used if the application doesn't specify one.
1. Server name
1. Authentication provider type:
    1. CBS connections
    1. X509 connections

<!--amqppoolconnectionselection.puml-->
![csharpDeviceClientAmqpConnectionSelection](https://www.plantuml.com/plantuml/png/0/dPD1JyCm38Nla_OVV9-gt2iq3HKDkA5jTnGbHgkKsftZ3l7lQIbGkhMhoYrA_BuylpYRg5QqAWAuUBHIFk0vTvY574dlYrEPCH4wpPds6Dh5Sj4S7CHIB5dqoP8FACcwsNtcmnvZhAksrYdBHmexF5KO5Opf5nZJWguaewHjJXvO81EqYg171CSYjQiEtlbzOGxJT_JhKtZ4GTWVK5yn2ffdTff_6SPjo6ItwOyn2uJljnwdqmnYOYjuTLoHhbOZ3kwI0UaEjHBwYcRcS5mEtozhZ3CyhTUO4kMDfG2k5xPn6UVflzC05pYYDpwFhcqO_Ry2JPBbOeFawm_w2G00 "csharpDeviceClientAmqpConnectionSelection")

## Multiplexing

Multiplexing allows more than one identity (`Device` or `Module`) to operate on the same AMQP `connection`. This is reducing the number of TLS and TCP connections when a high number of identities need to be created by the same host and avoids TCP ephemeral port exhaustion in gateway scenarios.

Multiplexing is always enabled when pooling is enabled. The number of multiplexed clients on the same TCP connection is not limited by the client library. At the time of writing, there is a limit of 1000 devices authenticated on the same CBS link.

### Connection selection algorithm
1. The connection selection algorithm will allocate one client per connection until `maximumNumberOfConnections` is reached.
1. Clients must be associated with the same connection throughout the lifetime of the application. This is accomplished by consistent hashing based on `deviceId` or `deviceId/moduleId` (a perfect distribution is not guaranteed).

### Connection close algorithm

#### Non-pooled connections
1. `AmqpIoTSession` will keep a reference to the `AmqpConnection` when created.
1. `AmqpIoTSession.CloseAsync` will directly call `AmqpConnection.CloseAsync`.
1. `client.Dispose` will call `AmqpIoTSession.Abort` which will also abort the `AmqpConnection`.

#### Pooled Connections
1. Connections should be closed no sooner than the specified `AmqpConnectionPoolSettings.ConnectionIdleTimeout` (default 2 minutes, minimum value accepted: 5 seconds).
1. Connections will register to `AmqpSession.Closed` to count active AMQP sessions.
1. When the count reaches 0, the `AmqpIoTConnection` will start a delay task to measure the `ConnectionIdleTimeout` time. If the count increases, the delay task will be cancelled, otherwise the connection will be closed gracefully.
1. If the graceful close causes any exceptions, the AMQP connection will be `Abort`ed.
1. Changing the active state of the connection needs to be performed under a lock so that the `AmqpIoTConnectionPool` can be sure of the reported state.

_Note:_ The `AmqpIoTConnection` will not manage the session lifetime although it will register for `AmqpSession.Closed` events.

### X509 Authentication
Clients using X509 authentication cannot use multiplexing. Internally, a default X509 connection pool will generate these connections.

### CBS Authentication
Clients using SAS token will need to create and maintain a `TokenRefresher` object. `InternalClient` will maintain ownership of this object. A disposed `TokenRefresher` will indicate that the client no longer needs to be authenticated. 

The `TokenRefresher` list will queried `AmqpIoTConnection` object to obtain all that need to refresh within a hard-coded time span (e.g. 5 seconds). Refresh will be performed on the resulted list of refreshers such that no more than one authentication per second occurs (the requests are pipelined).

__Important:__ Due to CBS implementation, the client remains authenticated on the connection for the remainder of the SAS token lifetime. This, combined with the fact that connections are not immediately terminated adds security risks if the code making requests is not trusted. If threat boundaries are needed we recommend running untrusted code in separate processes and TCP connections.

If the Shared Access Key (SAK) is not available, the associated `TokenRefresher` should simply return the same SAS token and rely on the IoT Hub to shutdown the connection due to invalid credentials after expiration.

## Azure AMQP protocol reference

- http://docs.oasis-open.org/amqp/core/v1.0/os/amqp-core-overview-v1.0-os.html
- https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-amqp-overview
- https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-amqp-protocol-guide
