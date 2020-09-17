# Digital Twin API Design Doc

## Client initialization
```csharp
public static DigitalTwinClient CreateFromConnectionString(string connectionString) {}
```

To be discussed:
- Existing track 1 clients (`ServiceClient`, `RegistryManager`, `JobClient`) only have the `CreateFromConnectionString` factory method. These client create sas tokens with a time to live of 1 hour [(non-customizable)](https://github.com/Azure/azure-iot-sdk-csharp/blob/master/common/src/service/IotHubConnectionString.cs#L16).

If we want to have the ttl customizable, we can take it is as an optional param:
```csharp
public static DigitalTwinClient CreateFromConnectionString(string connectionString, TimeSpan sasTokenTimeToLive = default) {}
```
Internally we provide an implementation of `Microsoft.Rest.ServiceClientCredentials`, which is the credential provider class for the protocol layer. This implementation class will handle injection of SAS token per Http request. (this http client doesn't have any retry logic built in, nor is it configurable via the client library).

## APIs

Items to discuss:
- Should we also have sync APIs? Other track 1 clients don't have sync APIs - only async APIs.
- Shoud we return both `string` and `Response<string>` in the response? - only `Response<string>`.
- Shoud we merge `InvokeCommandAsync()` and `InvokeComponentCommandAsync()` into a single API (with `componentName` optional) - they call different PL APIs -> keep them separate.
- Should we put the optional params `connectTimeoutInSeconds` and `responseTimeoutInSeconds` in invoke command APIs into an `Options` type - yes

```csharp
/// <summary>
/// Gets a strongly-typed digital twin asynchronously.
/// </summary>
/// <param name="digitalTwinId">The Id of the digital twin.</param>
/// <param name="cancellationToken">The cancellation token.</param>
/// <returns>The application/json digital twin and the http response.</returns>
public async Task<HttpOperationResponse<T, DigitalTwinGetHeaders>> GetAsync<T>(string digitalTwinId, CancellationToken cancellationToken = default) {}

/// <summary>
/// Updates a digital twin asynchronously.
/// </summary>
/// <param name="digitalTwinId">The Id of the digital twin to update.</param>
/// <param name="digitalTwinUpdateOperations">The application/json-patch+json operations to be performed on the specified digital twin.</param>
/// <param name="requestOptions">The optional settings for this request.</param>
/// <param name="cancellationToken">The cancellationToken.</param>
/// <returns>The http response.</returns>
public Task<HttpOperationHeaderResponse<DigitalTwinUpdateHeaders>> UpdateAsync(string digitalTwinId, string digitalTwinUpdateOperations, DigitalTwinUpdateRequestOptions requestOptions = default, CancellationToken cancellationToken = default) {}

/// <summary>
/// Invoke a command on a digital twin asynchronously.
/// </summary>
/// <param name="digitalTwinId">The Id of the digital twin.</param>
/// <param name="commandName">The command to be invoked.</param>
/// <param name="payload">The command payload.</param>
/// <param name="requestOptions">The optional settings for this request.</param>
/// <param name="cancellationToken">The cancellationToken.</param>
/// <returns>The application/json command invocation response and the http response. </returns>
public async Task<HttpOperationResponse<string, DigitalTwinInvokeCommandHeaders>> InvokeCommandAsync(string digitalTwinId, string commandName, string payload, DigitalTwinInvokeCommandRequestOptions requestOptions, CancellationToken cancellationToken = default) {}

/// <summary>
/// Invoke a command on a digital twin asynchronously.
/// </summary>
/// <param name="digitalTwinId">The Id of the digital twin.</param>
/// <param name="componentName">The component name under which the command is defined.</param>
/// <param name="commandName">The command to be invoked.</param>
/// <param name="payload">The command payload.</param>
/// <param name="requestOptions">The optional settings for this request.</param>
/// <param name="cancellationToken">The cancellationToken.</param>
/// <returns>The application/json command invocation response and the http response. </returns>
public async Task<HttpOperationResponse<string, DigitalTwinInvokeCommandHeaders>> InvokeComponentCommandAsync(string digitalTwinId, string componentName, string commandName, string payload, DigitalTwinInvokeCommandRequestOptions requestOptions, CancellationToken cancellationToken = default) {}
```

*NOTE:
- The response header class names will need to be updated - `DigitalTwinGetDigitalTwinHeaders` -> `DigitalTwinGetHeaders`, `DigitalTwinUpdateDigitalTwinHeaders` -> `DigitalTwinUpdateHeaders` (they have different json properties).
- We will provide a utility to create the json-patch for update operation - similar to what we have on ADT.
- We cannot provide a basic digital twin type for the `GetAsync<T>` because the returned twin is almost completely defined by the model Id (the metadata field is embedded inside each property).
```json
{
  "$dtId": "tc",
  "serialNumber": "SR-123456",
  "thermostat1": {
    "maxTempSinceLastReboot": 19.9,
    "targetTemperature": "10.0",
    "$metadata": {
      "targetTemperature": {
        "desiredValue": 10,
        "desiredVersion": 2,
        "ackVersion": 2,
        "ackCode": 200,
        "ackDescription": "\"Successfully updated target temperature\"",
        "lastUpdateTime": "2020-09-17T06:05:51.4582256Z"
      },
      "maxTempSinceLastReboot": {
        "lastUpdateTime": "2020-09-17T07:36:47.4529523Z"
      }
    }
  },
  "thermostat2": {
    "maxTempSinceLastReboot": 16.0,
    "targetTemperature": 50.0,
    "$metadata": {
      "targetTemperature": {
        "desiredValue": 50,
        "desiredVersion": 4,
        "ackVersion": 4,
        "ackCode": 200,
        "ackDescription": "Successfully updated target temperature",
        "lastUpdateTime": "2020-09-17T06:19:15.7068524Z"
      },
      "maxTempSinceLastReboot": {
        "lastUpdateTime": "2020-09-17T07:36:47.9645314Z"
      }
    }
  },
  "$metadata": {
    "$model": "dtmi:com:example:TemperatureController;1",
    "serialNumber": {
      "lastUpdateTime": "2020-09-17T07:36:47.0104998Z"
    }
  }
}
```
