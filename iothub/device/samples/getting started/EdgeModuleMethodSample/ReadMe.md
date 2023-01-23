# Prerequisites for EdgeModuleMethodSample

Following instructions walk through steps to set up an Azure Edge device and an Azure Edge module.

## Setup and deploy custom Azure IoT Edge modules:

- Follow the steps in the [article](https://learn.microsoft.com/azure/iot-edge/how-to-visual-studio-develop-module) to set up Visual Studio 2022 to develop and debug modules for Azure IoT Edge.

- Within 'deployment.debug.template.json', value for 'createOptions' needs to be a string. By default, only edgeHub module has a createOptions value which needs to be updated as a string.  
    * This createOptions is particularly important as it opens specific ports to listen to for MQTT/AMQP/HTTP connection on edge device.

- For the new module(s) you create, update 'restartPolicy' to 'never'.
    * Within the article example, this applies to 'IotEdgeModule1' and 'IotEdgeModule2'. 
    * This is because you will manually connect to the module(s) from a console app in Visual Studio. If restartPolicy is set to 'always', the connection will switch between connected and disconnected as both console app and docker container will try to keep the module connected.

- Confirm that the specific module(s) were created within the Edge device on Azure portal.

## Create IoT Edge device as docker container on Linux: 

- Create a linux VM to host IoT Edge device within a docker container.
    * You can either host the VM locally or on Azure.

- Follow the steps in the [article](https://learn.microsoft.com/azure/iot-edge/how-to-provision-single-device-linux-symmetric) to register and provision a Linux IoT Edge device, including installing Azure IoT Edge.
    * You can follow this [article](https://learn.microsoft.com/azure/iot-edge/troubleshoot) for troubleshooting any issue. 

-  To be able to connect to the VM and use GatewayHostName within connection string, you need to add certificate from VM into trust store of the host/local machine. 
    * Execute the following command in the Linux VM terminal to access file manager as root user:
    ```
    ubuntu@ubuntu:~$ sudo nautilus
    ```
    *  Navigate to /var/lib/aziot/certd/certs/ and copy the file named 'aziotedgeca-xxxx'.
    * Install the certificate on your local machine. 
    * This allows your local machine to trust the VM and route the connection through the VM. 

    * Now execute the following to obtain value for GatewayHostName.
    ```
    ubuntu@ubuntu:~$ hostname
    ```     

- Execute the following to stop the module within VM as you will manually connect to it using console app in the next step. 
    * For the purpose of this sample, it applies to 'IotEdgeModule1' and 'IotEdgeModule2'.
    ```
    ubuntu@ubuntu:~$ docker stop <module Name>
    ```

## Create console app to connect to IoT Edge modules: 

- Within Visual Studio, create a console app to connect to IoT Edge modules. 

- Navigate to Azure Portal -> Iot Hub -> <*specific Iot Hub*> -> Devices -> <*specific Edge device*> -> <*specific Edge module*>, and copy the connection string. 

- Append GatewayHostName value to the connection string which you copied from last step and use this connection string as the parameter for the sample.