#### Autorest config file for generating Digital Twin Client models and protocol layer


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