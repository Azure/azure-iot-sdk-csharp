1.	Setup and deploy custom Azure IoT Edge modules: 
	
	a. Follow the article: 
		https://learn.microsoft.com/en-us/azure/iot-edge/how-to-visual-studio-develop-module?view=iotedge-2020-11&pivots=iotedge-dev-cli

	b. Within 'deployment.debug.template.json',  'createOptions' needs to be a string value. By default, only edgeHub module has a createOptions value which needs to be updated as a string.
		This createOptions is particularly important as it opens specific ports to listen to for MQTT/AMQP/HTTP connection on edge device. 

	c. For the new module(s) you create, update 'restartPolicy' to 'never'.
		Within the article example, this applies to 'IotEdgeModule1' and 'myIotEdgeModule2'. 
		This is because we will manually connect to the module(s) from a console app in Visual Studio. If restartPolicy is set to 'always', the connection will switch between connected and disconnected as both console app and docker container will try to keep the module connected. 

    d. Confirm that the specific module(s) were created within the Edge device on Azure portal. 

2. Create IoT Edge device as docker container on Linux: 

	a. Create a linux VM to host IoT Edge device within a docker container.
		You can use VMWare to host a VM locally. Creating a Azure VM should work fine as well.

	b. Follow the article:
		https://learn.microsoft.com/en-us/azure/iot-edge/how-to-provision-single-device-linux-symmetric?view=iotedge-1.4&tabs=azure-portal%2Cubuntu
		This allows us to start the IoT edge device and edgeHub/edgeAgent modules.  

    c. To be able to connect to the VM and use GatewayHostName within connection string, we need to add certificate from VM into trust store of the host/local machine. 
		Within VM, run 'sudo nautilus'
		Go to /var/lib/aziot/certd/certs/ and copy file 'aziotedgeca-xxxx'. 
		Install the certificate on your local windows machine. 
		This allows the windows machine to trust the VM and route the connection through the VM. 

    d. Within VM, execute 'hostname' to obtain value to be used for GatewayHostName in the connection string. 

	e. Execute 'docker stop <moduleName>' to stop the module within VM as we will manually connect to it using console app in Windows in the next step. 

3. Create console app to connect to IoT Edge modules: 

    a. Within visual studio, create a console app to connect to IoT Edge modules. 

	b. Go to Portal -> IotHub -> Devices -> *Edge Device* -> *specific Edge module*  and copy the connection string. 

	c. Append GatewayHostName value to the connection string which we copied from last step. 

	d. Go to Portal -> IotHub -> Devices -> *Edge Device* -> Set Modules -> Routes  and add the following route: 
		FROM /messages/modules/<name of sender IotEdgeModule>/outputs/outputChannel INTO BrokeredEndpoint("/modules/<name of receiver IotEdgeModule>/inputs/inputChannel") 
		(This creates routes to be used when Edge device receives a message from the module. We also can send a message to the module itself.) 
