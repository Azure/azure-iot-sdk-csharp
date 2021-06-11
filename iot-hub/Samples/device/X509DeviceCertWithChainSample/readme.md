# x509 device certificate with chain sample

## How to run the sample

### Step 1

Create a device in your IoT hub using the x.509 CA signed option.

### Step 2

Create a secure string password to use in creation of self-signed certificates.

```powershell
    $password= ConvertTo-SecureString <your password> -AsPlainText -Force
```

### Step 3

Run the [AuthSetup.ps1](https://github.com/Azure-Samples/azure-iot-samples-csharp/blob/master/iot-hub/Samples/device/X509DeviceCertWithChainSample/AuthSetup.ps1) to setup the IoT hub and device with necessary certificates.

```powershell
    .\AuthSetup.ps1 `
    -certFolderPath <Path to root cert> `
    -rootCertPassword $password `
    -iotHubResourceGroup <IoT hub resource group> `
    -iotHubName <IoT hub name> `
    -deviceId <device Id> 
```

### Step 4

Run the sample with the following command line arguments.

```
    "commandLineArgs": "-h <hostname of IoT hub> 
                        -d <device Id> 
                        --devicePfxPath <path to device cert>
                        --devicePfxPassword <device cert password>  
                        --rootCertPath <Path to rootCACert>  
                        --intermediate1CertPath <Path to intermediateCert1> 
                        --intermediate2CertPath <Path to intermediateCert2>"
```

> Note: Use the same device Id created in step 1 as input to step 3 & 4.
