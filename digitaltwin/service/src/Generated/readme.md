# Autorest config file for generating Digital Twin Service Client models and protocol layer

# From command prompt, simpy run "autorest" from this folder and it will pick up this config file and output the results into this folder

> see https://aka.ms/autorest

``` yaml 
input-file: service.json # TODO need to add service.json swagger file to this directory

csharp:
  namespace: Microsoft.Azure.IoT.DigitalTwin.Service
  output-folder: .
  add-credentials: true                # enable experimental XML serialization support
```