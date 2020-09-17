#### Autorest config file for generating Digital Twin Service Client models and protocol layer

#### Protocol layer generation:
- For generating the protocol layer from the swagger file, from command prompt simply run "autorest" from this folder and it will pick up this config file and output the results into this folder.

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