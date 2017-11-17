# Prepare your development environment

This document describes how to prepare your development environment to build and use the **Microsoft Azure IoT device client SDK for .NET**

1.  [Setting up a Windows development environment](#windows)
2.  [Directly using IoT Device Client SDK using NuGet packages](#directly_using_sdk)
3.  [Building the Azure IoT Device Client .NET SDK locally](#building_sdk)
4.  [Sample Applications](#samplecode)

<a name="windows"/>

## Setting up a Windows development environment

- Install [Visual Studio 2017][visual-studio]. You can use the **Visual Studio Community** Free download if you meet the licensing requirements.

Be sure to include [NuGet Package Manager][NuGet-Package-Manager].

<a name="directly_using_sdk"/>

## Directly using Azure IoT Device Client SDK using NuGet packages

Go to VS 2015 Solution Explorer and right click on the solution or project and click Manage NuGet Packages. There are **2** different **NuGet** packages to choose from

### 1.) Microsoft.Azure.Devices.Client

For building classic desktop [.NET] [.NET] application, use NuGet Package Manager to install latest version of **Microsoft.Azure.Devices.Client** Device SDK for Azure IoT Devices NuGet package to your project.

Two versions of Microsoft.Azure.Devices.Client.dll are built -- one for the classic .NET desktop application and
another for building [UWP] [UWP] applications.

The package is located at [Azure IoT Device Client SDK NuGet Package] [lnk-NuGet-package]

### 2.) Microsoft.Azure.Devices.Client.PCL

[PCL] [PCL] (Portable Class Library) version of .NET library helps you to build cross-platform apps.
For example, for bulding iOS and Android application in C# in VS 2015 using [Xamarin] [Xamarin], use NuGet Package Manager to install latest version of **Microsoft.Azure.Devices.Client.PCL** Device SDK for Azure IoT Devices NuGet package to your project.

The package is located at [Azure IoT Device Client PCL SDK NuGet Package] [lnk-NuGet-package_pcl]

> Currently, **Microsoft.Azure.Devices.Client.PCL** only supports HTTPS protocol.

### Building [UWP][UWP] Apps

Just like classic desktop app, install [Azure IoT Device Client SDK NuGet Package] [lnk-NuGet-package].
A [UWP] [UWP]-compatible assembly Microsoft.Azure.Devices.Client.dll will get installed via NuGet Package manager when you select **Universal** app in VS 2015. This is the version you need to build UWP apps.

#### Building C# and Visual Basic UWP apps

For building UWP, follow the same steps as you would follow if building classic .NET desktop application.

For example, for building in **Visual Basic** , the calls using the SDK client library would be something like this

```
Dim deviceClient As Microsoft.Azure.Devices.Client.DeviceClient = Microsoft.Azure.Devices.Client.DeviceClient.CreateFromConnectionString("{My device connection string}", Microsoft.Azure.Devices.Client.TransportType.Amqp)
Dim myMessage = "Hello!" + DateTime.Now.ToString()
Dim message = New Microsoft.Azure.Devices.Client.Message(Encoding.ASCII.GetBytes(myMessage))
Dim Async = deviceClient.SendEventAsync(message)
```

where you would replace {My device connection string} with your own valid device connection string.

### Building iOS and Android apps using Xamarin

For building iOS and Android app in C# , you will use PCL NuGet package  located at [Azure IoT Device Client PCL SDK NuGet Package] [lnk-NuGet-package_pcl]

<a name="building_sdk"/>

## Building the Azure IoT Device Client .NET SDK locally

In some cases, you may want to build the .NET SDK libraries **locally** for development and testing purpose. For example, you may want to build the **latest** code available on the **develop** branch which follows the Continuous Integration (CI) approach and you may also want to step through the client library code while debugging.

To pull latest code on **develop** branch you can use following command

`git clone -b develop --recursive https://github.com/Azure/azure-iot-sdks.git azure-iot-develop`

To clone the **master** use following command or simply download the **.zip** from [Azure IoT SDK][lnk-azure-iot] 

`git clone https://github.com/Azure/azure-iot-sdks.git`

There are ways you can build the C# SDK (along with samples). You can either build using command line or you can build using VS 2015 IDE

### Option 1:  Build using command line

This requires Powershell script execution enabled on your system. See http://go.microsoft.com/fwlink/?LinkID=135170 for more information.

1. Open a Developer Command Prompt for VS2015.
2. Go to scripts folder located at **csharp\\device\\build**. Run the build script `build` which will build the SDK and the samples using default option.

Use 'build --options' to list various build options.
For example to build for x64 as debug build, you can use

`build --platform x64 --config Debug`

You will be prompted to download and run nuget.exe for downloading packages that Azure IoT SDK has dependency on.
Once build completes, it will create

1. Microsoft.Azure.Devices.Client.dll (Client SDK Assembly)
2. Microsoft.Azure.Devices.Client.PCL.dll (PCL library)
3. Microsoft.Azure.Devices.Client.dll (UWP version that you will need to UWP application)
4. Sample executables 

### Option 2: Build using VS 2015 IDE

1. Open iothub_csharp_deviceclient.sln file in VS 2015 IDE.
2. Select the configuration that you want and press Build->Build Solution command.
Just like command line option it will build Device Client SDK along with the samples.

### Building NuGet Package locally

In case, you want to build NuGet package **locally**, you can use following steps

a.) Build Release_Delay_Sign build using VS 2015 IDE

b.) Go to csharp\device\nuget folder in Windows PowerShell command prompt and run `./make_nuget_package.ps1` command. This will result is Microsoft.Azure.Devices.Client NuGet Package in \csharp\device\NuGet folder.You can then add this package locally in your build system by going though NuGet Package Manager.


<a name="samplecode"/>

## Sample Applications

This repository contains various .NET sample applications that illustrate how to use the Microsoft Azure IoT device SDK for .NET. For more information, see the [readme][readme].

You can build the **samples** in VS 2015 by opening csharp\device\\**iothub_csharp_deviceclient.sln** file which has various projects for samples. To build particular sample, right click on that sample in Solution Explorer and set that project as StartUp Project and build the project. Before running the sample, set the connecting string to a valid device connection and then re-build the sample before running it.

### Building for iOS and Android using C#

For building for iOS and Android using C#, you need to install [Xamarin][lnk-visualstudio-xamarin] for VS 2015

For building iOS sample, open **csharp\device\samples\DeviceClientSampleiOS** project file in VS 2015. You will be prompted to install Xamarin to build native iOS apps in C# if tool is not installed. Install the tool. You can test the sample on iPhone simulator running on your Mac.

For building Android sample open **csharp\device\samples\DeviceClientSampleAndroid** VS project file in VS 2015. You will be prompted to install Xamarin to build native Android apps in C# if tool is not installed. Install the tool. You can test the sample on Android emulator running on you host PC.

To check for any latest Xamarin update for Visual Studio check Tools->Options->Xamarin->Other.

### Building and running CppUWPSample (Universal Windows) C++ sample application

Select CppUWPSample as as StartUp Project. Replace the connection string with a valid connection string in `MainPage.xaml.cpp` file
On running the applciation, you will see "Could not load file or assembly Microsoft.Azure.Amqp.Uwp" error.

> To workaround this error copy 3 assemblies that application has dependencies on. Copy **Microsoft.Azure.Amqp.Uwp.dll**, PCLCrypto.dll and Validation.dll into **CppUWPSample AppX** folder from **UWPSample** folder.

For example for building debug version for x64 copy these 3 files from device\samples\UWPSample\bin\x64\Debug into device\x64\Debug\CppUWPSample\AppX folder.

After this redeploy and re-run the application.

### Building and running JSSample (Universal Windows) JavaScript sample application

Open **JSSample.sln** file from samples\JSSample in VS 2015 IDE. Replace the connecting string with a valid connection string in `default.js` file. Deploy and run the application. The application will throw System.IO.FileNotFoundException : "Could not load file or assembly Microsoft.Azure.Amqp.Uwp".

> To workaround this error copy assemblies that application has dependencies on. Copy **Microsoft.Azure.Amqp.Uwp.dll**, PCLCrypto.dll and Validation.dll into **JSSample AppX** folder from **UWPSample** folder.

For example for building debug version for x64 copy these 3 files from device\samples\UWPSample\bin\x64\Debug into JSSample\bin\x64\Debug\AppX folder.

After this redeploy and re-run the application.



[visual-studio]: https://www.visualstudio.com/
[readme]: ../readme.md
[lnk-sdk-vs2015]: http://go.microsoft.com/fwlink/?LinkId=518003
[lnk-sdk-vs2013]: http://go.microsoft.com/fwlink/?LinkId=323510
[lnk-sdk-vs2012]: http://go.microsoft.com/fwlink/?LinkId=323511
[lnk-visualstudio-xamarin]: https://msdn.microsoft.com/en-us/library/mt299001.aspx
[lnk-NuGet-package]:https://www.nuget.org/packages/Microsoft.Azure.Devices.Client
[lnk-NuGet-package_pcl]:https://www.nuget.org/packages/Microsoft.Azure.Devices.Client.PCL
[lnk-azure-iot]:https://github.com/Azure/azure-iot-sdks
[NuGet-Package-Manager]:https://visualstudiogallery.msdn.microsoft.com/5d345edc-2e2d-4a9c-b73b-d53956dc458d
[NuGet]:https://www.nuget.org/
[PCL]:https://msdn.microsoft.com/en-us/library/gg597391(v=vs.110).aspx
[UWP]:https://msdn.microsoft.com/en-us/windows/uwp/get-started/universal-application-platform-guide
[.NET]:https://www.microsoft.com/net
[UWP]:https://msdn.microsoft.com/en-us/windows/uwp/winrt-components/index
[Xamarin]:https://www.xamarin.com/

