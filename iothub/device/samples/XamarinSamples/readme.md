# Microsoft Azure IoT device SDK Xamarin Samples for .NET

This folder contains the following samples:

### Device client samples for UWP
* Send and Receive Messages through HTTP, MQTT

### Device client samples for Android
* Send and Receive Messages through HTTP, AMQP

## Running the Xamarin Samples:
* Include the following projects in your workspace to run the samples:
    * Xamarin
    * Xamarin.Android &/or Xamarin.UWP (based on your requirement)
* Set either Xamarin.Android or Xamarin.UWP as the startup project (based on your requirement).
* Set the device connection string "DeviceConnectionString", and the device ID "deviceId".
* Run the sample on either a simulator or a device.

## Known issues with Android build:
* While building Android App, if it throws the following error:
    ```Could not load assembly 'System.Runtime.CompilerServices.Unsafe' during startup registration. This might be due to an invalid debug installation. A common cause is to 'adb install' the app directly instead of doing from the ID```

    * Set the Linking to "SDK and User Libraries in Xamarin.Android Properties --> Android Options.
    * This will link all libraries, including user defined ones. User defined code can get removed if there is no static reference. If this happens, user code needs to be preserved. Follow https://docs.microsoft.com/en-us/xamarin/android/deploy-test/linker#preserving-code

    __OR__

    * Set the Linking to "SDK Libraries only" in Xamarin.Android Properties --> Android Options
    * Go to C:\Users\%user%\.nuget\system.runtime.compilerservices.unsafe\4.4.0, and delete "ref"       folder then make a copy of "lib" folder and rename the copy back to "ref". 
    * Cleanup all the "bin" and "obj" folders in the projects.
    * Rebuild and run..
    * Repeat for any other assembly that throws that error

    __OR__

    * Use packages.config file for nuget packages instead of PackageReference

    This is due to a known Xamarin issue: [System.Runtime.CompilerServices.Unsafe linked away in release leading to crashes][xamarin-link]
    
    [xamarin-link]: https://github.com/xamarin/xamarin-android/issues/1196