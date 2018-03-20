# Prepare your development environment

This document describes how to prepare your development environment to build and use the **Microsoft Azure IoT device client SDK for .NET**

1.  [Prerequisites required to build the SDK](#min_setup)
2.  [Optional prerequisites required to test Xamarin, Windows IoT and .NET Micro Framework](#advanced)
3.  [Test prerequisites](#testprereq)

<a name="min_setup"/>

## Prerequisites required to build the SDK

The following prerequisites are the minimum required to build and produce IoT SDK binaries. Visual Studio is not required to build the SDK.
This may not be sufficient to run certain samples that require Visual Studio and other SDKs or frameworks installed on your machine.

### Windows + .NET Framework

- Install the latest .NET Core from https://dot.net
- Install .NET Framework 4.7 Developer Pack: https://support.microsoft.com/en-us/help/3186612/the-net-framework-4-7-developer-pack-and-language-packs
- Install .NET Framework 4.5.1 Developer Pack: https://www.microsoft.com/en-us/download/details.aspx?id=40772
- As admin (one-time setup):
    Enable Powershell script execution on your system. See http://go.microsoft.com/fwlink/?LinkID=135170 for more information.
    `Set-ExecutionPolicy -ExecutionPolicy RemoteSigned`

### Linux/OSX (tested on Ubuntu 16.04)

- Install Powershell https://github.com/PowerShell/PowerShell/blob/master/docs/installation/linux.md#ubuntu-1604
- Install the latest .NET Core SDK CLI https://www.microsoft.com/net/learn/get-started/linux/ubuntu16-04

<a name="advanced"/>

## Optional Setup required to test Xamarin, Windows IoT and .NET Micro Framework

### Installing Visual Studio 2017 for Xamarin applications

- Install [Visual Studio 2017][visual-studio]. You can use the **Visual Studio Community** Free download if you meet the licensing requirements.
- During the installation of Visual Studio 2017, we found that selecting the following workloads and components helps in running and debugging the SDK.

Workloads:
![](./workloads.png)

Components:
![](./components.png)

### Installing Windows IoT Core SDK

Install the Microsoft IoT Windows Core Project Templates for Visual Studio 2017 from the Extension Marketplace: 
	https://marketplace.visualstudio.com/items?itemName=MicrosoftIoT.WindowsIoTCoreProjectTemplatesforVS15

### Installing .NET Micro Framework

#### 1. Download and Install Visual Studio Community 2015 Update 3 (approx. size: 9GB)

NetMF is not supported on VS2017. We recommend a separate virtual machine with VS2015.
VS2015 Community is sufficient. Download from: https://www.visualstudio.com/vs/older-downloads/

Under Programming Languages, Visual C++ make sure that `Common Tools for Visual C++ 2015` is selected.

#### 2. Download and install .NET MicroFramework 4.4 and 4.3

1. SDK v 4.4: 
  * MicroFrameworkSDK.msi  (SDK v4.4) - Complete Installation
  * NetMFVS14.vsx
		
2. SDK 4.3 - The official site has been archived. *Either*:
  * Download the Codeplex archive (3.4GB) then unpack to find the MSI:
    * Under releases\13 unpack c4ad01a9-68f6-48cd-82e5-e6d154b0eb2f
    * Rename c4ad01a9-68f6-48cd-82e5-e6d154b0eb2f to netmf-v4.3.2-SDK-QFE2-RTM.zip and unzip
    * Install MicroFrameworkSDK.msi - Complete Installation
  * Download from http://developer.wildernesslabs.co/Netduino/About/Downloads/
  
#### 3. Install .NET Framework 3.5

See the following documentation on how to enable this Windows component: https://docs.microsoft.com/en-us/dotnet/framework/install/dotnet-35-windows-10

<a name="testprereq"/>

## Test prerequisites

Each test project that requires additional changes to your machine contains a `prerequisites` folder. Please follow the instructions within that folder on how to set-up your machine in order to successfully run our tests.

#### iothub/service/prerequisites one-time setup (Windows Only)
Run the following as admin: `iothub\service\tests\prerequisites\windows_install.cmd`

#### e2e/test/prerequisites

1. Create copies of the prerequisite scripts in a separate folder (optionally include this folder in your PATH environment variable for easy access. 

    * Windows: copy `e2e/test/prerequisites/iot_config.cmd_template` to a folder in your PATH `iot_config.cmd`
    * Linux/OSX: copy `e2e/test/prerequisites/iot_config.sh_template` to `iot_config.sh`

**Do not** check-in these files after you have edited them as they contain your account's secrets such as connection strings.

2. Add the variables to your environment

    * Windows: run `iot_config.cmd`
    * Linux: dot-source the config file: `. iot_config.sh` 

3. Run the e2e tests or launch Visual Studio from the same command prompt/terminal.

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