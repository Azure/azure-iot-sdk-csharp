# Provisioning Device Client Sample - TPM Attestation

## Overview

This is a quick tutorial with the steps to register a device in the Microsoft Azure IoT Hub Device Provisioning Service using the Trusted Platform Module (TPM) attestation.

## How to run the sample

1. Ensure that all prerequisite steps presented in [samples](../) have been performed.
1. The sample must be run in administrative mode, so open VS as an admin, or open a console window as an admin.
1. You'll need the endorsement key (EK) of your TPM device to create an individual enrollment. This sample has a parameter `--GetTpmEndorsementKey` that can be used to get it and print it to the console.
1. If using a console, enter: `dotnet run -s <IdScope> -r <RegistrationId>`.
1. If using VS, edit project properties | debug | application arguments and add the parameters: `-s <IdScope> -r <RegistrationId>`

> Replace `IdScope` with the value found within the Device Provisioning Service Overview tab, and `RegistrationId` with the individual enrollment registration Id.
> To see a full list of parameters, run `dotnet run -?`.

Continue by following the instructions presented by the sample.

## Notes

For convenience, this sample uses a TPM simulator (SecurityProviderTpmSimulator) which is not supported on Linux.

To run against a real TPM2.0 device, replace this with `SecurityProviderTpmHsm`.
