#### Autorest config file for generating Digital Twin Service Client models and protocol layer

#### Protocol layer generation:
- Run the powershell script `generateCode.ps`. It will pick up the config below and output the results into this folder. It will also make a few automated changes to the generated protocol layer, that are required for this client library.

*NOTE: The `DigitalTwinClient` is available only on .NET framework 4.7.2 and .NET Standard 2.0+.


> see https://aka.ms/autorest

``` yaml 
input-file: DigitalTwin.json

csharp:
  namespace: Microsoft.Azure.Devices.Generated
  output-folder: Generated
  add-credentials: true
  use-internal-constructors: true
  sync-methods: none
```