# Prerequisites for EdgeModuleMessageSample

Following instructions walk through steps to set-up Azure Edge device and module.

## 1. Setup and deploy custom Azure IoT Edge modules:

    1. Follow the article: 
        - https://learn.microsoft.com/azure/iot-edge/how-to-visual-studio-develop-module?view=iotedge-2020-11&pivots=iotedge-dev-cli

    2. Within 'deployment.debug.template.json',  'createOptions' needs to be a string value. By default, only edgeHub module has a createOptions value which needs to be updated as a string.
        - This createOptions is particularly important as it opens specific ports to listen to for MQTT/AMQP/HTTP connection on edge device.

    3. For the new module(s) you create, update 'restartPolicy' to 'never'.
        - Within the article example, this applies to 'IotEdgeModule1' and 'IotEdgeModule2'. 
        - This is because you will manually connect to the module(s) from a console app in Visual Studio. If restartPolicy is set to 'always', the connection will switch between connected and disconnected as both console app and docker container will try to keep the module connected.

    4. Confirm that the specific module(s) were created within the Edge device on Azure portal.

## 2. Create IoT Edge device as docker container on Linux: 

    1. Create a linux VM to host IoT Edge device within a docker container.
        - You can either host the VM locally or on Azure.

    2. Follow the article:
        - https://learn.microsoft.com/azure/iot-edge/how-to-provision-single-device-linux-symmetric?view=iotedge-1.4
        - This allows us to start the IoT edge device and edgeHub/edgeAgent modules.
        - Troubleshooting: 
            - https://learn.microsoft.com/azure/iot-edge/troubleshoot?view=iotedge-1.4

    3.  To be able to connect to the VM and use GatewayHostName within connection string, you need to add certificate from VM into trust store of the host/local machine. 
        - Within the VM, run 'sudo nautilus'
        - Go to /var/lib/aziot/certd/certs/ and copy the file named 'aziotedgeca-<xxxx>'.
        - Install the certificate on your local windows machine. 
        - This allows your local machine to trust the VM and route the connection through the VM. 

    4. Within the VM, execute 'hostname' to obtain value to be used for GatewayHostName in the connection string.     

    5. Execute 'docker stop <moduleName>' to stop the module within VM as you will manually connect to it using console app in the next step. 
        - For the purpose of this sample, it applies to 'IotEdgeModule1' and 'IotEdgeModule2'.

## 3. Create console app to connect to IoT Edge modules: 

    1. Within visual studio, create a console app to connect to IoT Edge modules. 

    2. Navigate to Azure Portal => IotHub => Devices => *Edge Device* => *specific Edge module* and copy the connection string. 

    3. Append GatewayHostName value to the connection string which you copied from last step.

    4. Navigate to Portal => IotHub => Devices => <Edge Device> => Set Modules => Routes  and add the following route:   
        - FROM /messages/modules/<name of sender IotEdgeModule>/outputs/* INTO BrokeredEndpoint("/modules/<name of receiver IotEdgeModule>/inputs/*")
        - This creates routes to be used when Edge device receives a message from the module. You also can send a message to the module itself.