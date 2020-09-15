#### Autorest config file for generating Digital Twin Service Client models and protocol layer

#### From command prompt, simpy run "autorest" from this folder and it will pick up this config file and output the results into this folder

> see https://aka.ms/autorest

``` yaml 
input-file: plugAndPlayOnly.json

csharp:
  namespace: Microsoft.Azure.Devices.Generated
  output-folder: Generated
  add-credentials: true                
  use-internal-constructors: true
  sync-methods: none
```