# Autorest config file for generating Model Repository Service Client models and protocol layer

# From command prompt, simpy run "autorest" from this folder and it will pick up this config file and output the results into this folder
# There is a powershell script that should be used instead. This is because there are a few other automated changes that should happen after running autorest
> see https://aka.ms/autorest

``` yaml 
input-file: ModelService.json

csharp:
  namespace: Azure.IoT.DigitalTwin.Model.Service.Generated
  output-folder: Generated
  add-credentials: true                
  use-internal-constructors: true
  sync-methods: none
```