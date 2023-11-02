# Discovery + Provisioning Device Client Sample

## Overview

This is a quick tutorial with the steps to register an ARC enabled device.

## How to run the sample

1. Ensure that all prerequisite steps presented in [samples](../) have been performed.
1. In order to access the Hardware Security Module (HSM), the application must be run in administrative mode, so open VS as an admin, or open a console window as an admin.
1. You'll need the endorsement key (EK) of your TPM device to create an enrollment. This sample has a parameter `--GetTpmEndorsementKey` that can be used to get it and print it to the console.
1. If using a console, enter: `dotnet run -r <RegistrationId>`.
1. If using VS, edit project properties | debug | application arguments and add the parameters: `-r <RegistrationId>`

> Replace `RegistrationId` with the individual enrollment registration Id.
> To see a full list of parameters, run `dotnet run -?`.

Continue by following the instructions presented by the sample.
