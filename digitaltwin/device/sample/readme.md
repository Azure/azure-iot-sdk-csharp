# Azure IoT Digital Twins Device SDK Sample

**PREVIEW - WILL LIKELY HAVE BREAKING CHANGES**

The [EnvironmentalSensorSample](EnvironmentalSensorSample) folder contains a sample implementation of a simulated environmental sensor. It shows how to:

  * Implement the environmental sensor interface
  * Create an interfaceInstance for this interface
  * Use the digital twin device client to register this interfaceInstance and interact with the Digital Twins services.

Note that the DigitalTwinClient depends on the DeviceClient class from the Azure.IoT.Devices.Client library to communicate with the hub. The sample shows how to compose these two together.


The [EnvironmentSensorLib](EnvironmentSensorLib) folder contains the code that demonstrates how to implement a Digital Twins interface. In this case, it implements 
an environmental sensor interface that contains several properties (such as "brightness" and "name") and has several commands that 
can be invoked on it (such as "turnon", "turnoff", "blink"). The interface also has the ability to send telemetry (such as "humidity" 
and "temperature")
