# Microsoft Azure IoT device SDK Xamarin Samples for .NET

**Xamarin is not an officially supported platform!  There are known issues on Xamarin and progress is tracked on ["Xamarin stabilization"](https://github.com/Azure/azure-iot-sdk-csharp/projects/1) project.**

We currently have the following working samples on Xamarin:

### Send and Receive Telemetry
Platform | HTTP | MQTT | AMQP
-------- | ---- | ---- | ---
__Android__ | Yes | No | Yes
__iOS__ | Yes | No | Yes
__UWP__ | Yes | Yes | No

#### Current Versions used:
* Visual Studio v15.7.4
* Xcode v9.4.1
* Xamarin v4.10.10.2
* Xamarin.Android SDK v8.3.3.2
* Xamarin.iOS and Xamarin.Mac SDK v11.12.0.4

## Running the Xamarin Samples:
* Include the following .csproj projects in your workspace to run the samples:
    * XamarinSample.Android, XamarinSample.iOS, XamarinSample.UWP (based on your requirement)
* Set either XamarinSample.Android, XamarinSample.iOS or XamarinSample.UWP as the startup project (based on your requirement).
* Set the values for "DeviceConnectionString" and "deviceId" in XamarinSample.MainPage.xaml.cs.
* Run the sample on either a simulator or a device.

## Known issues with build:
#### __Android__: 
* For the latest Visual Studio and Target Framework of MonoAndroid 8.1, make sure you have support for Android API Level 27
    * To verify: 
      * Go To Tools -> Android -> Android SDK Manager -> Platforms
      * Make sure the following is checked: Android 8.1 - Oreo -> Android SDK Platform 27

#### __iOS__:
* If prompted, update to iOS version 11.4, and also update Xcode.