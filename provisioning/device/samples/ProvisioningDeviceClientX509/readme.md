# Provisioning Device Client Sample - X.509 Attestation

## Overview

This is a quick tutorial with the steps to register a device in the Microsoft Azure IoT Hub Device Provisioning Service using X.509 certificates locally generated.

## How to run the sample

Ensure that all prerequisite steps presented in [samples](../) have been performed.

The sample code is set up to use X.509 certificates stored within a password-protected PKCS12 formatted file (certificate.pfx). To generate a self-signed certificate run the following command:

`powershell .\GenerateTestCertificate.ps1`

The script will prompt for a PFX password. The same password must be used when running the sample.

In your Device Provisioning Service go to "Manage enrollments" and select "Individual Enrollments".
Select "Add" then fill in the following:
Mechanism: X.509
Certificate: Select the public key 'certificate.cer' file.
DeviceID: iothubx509device1

To run the sample, in a developer command prompt enter:
`dotnet run <IDScope>`

replacing `IDScope` with the value found within the Device Provisioning Service Overview tab. E.g. `dotnet run 0ne1234ABCD`

Continue by following the instructions presented by the sample.

## Using your certificates

The SDK requires an [X509Certificate2](https://msdn.microsoft.com/en-us/library/system.security.cryptography.x509certificates.x509certificate2(v=vs.110).aspx) object with private key ([HasPrivateKey](https://msdn.microsoft.com/en-us/library/system.security.cryptography.x509certificates.x509certificate2.hasprivatekey(v=vs.110).aspx)==true) and, optionally, the certificate chain within an [X509Certificate2Colection](https://msdn.microsoft.com/en-us/library/system.security.cryptography.x509certificates.x509certificate2collection(v=vs.110).aspx) object.

This can be achieved by changing the following line:

```C# 
    using (var security = new SecurityProviderX509Certificate(certificate))
```

to 

```C# 
    var myCertificate = new X509Certificate2("myCertificate.pfx", "mypassword");
    var myChain = new X509Certificate2Collection();
    
    // Comment out the below line if you do not have a .p7b file (e.g. if you generated certificates using the tool below)
    myChain.Import("myChain.p7b");
    
    using (var security = new SecurityProviderX509Certificate(myCertificate, myChain))
```

A tool for creating _test_ certificates is available at https://github.com/Azure/azure-iot-sdk-c/blob/master/tools/CACertificates/CACertificateOverview.md

If you generate _test_ certificates as an administrator using the above tool, please note you must run the sample as administrator as well.

If a Windows compatible Hardware Security Module is used, the certificate must be obtained by opening it from the Certificate Store using [X509Store](https://msdn.microsoft.com/en-us/library/system.security.cryptography.x509certificates.x509store(v=vs.110).aspx).

On Linux, .Net Core is using OpenSSL. Using [PInvoke](https://msdn.microsoft.com/en-us/library/55d3thsc.aspx) it is possible to configure the OpenSSL Engine to use a Hardware Security Module and create an X509Certificate2 object from the context.
