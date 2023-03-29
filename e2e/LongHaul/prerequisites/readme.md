# Azure IoT .NET long haul test prerequisites

The long haul tests require some Azure resources to be set up and configured. Running the [LongHaulSetup.ps1](LongHaulSetup.ps1) powershell script is a convenient way of getting all the resources setup with the required configuration.

Note: The LongHaulSetup.ps1 script will setup all the resources necessary to run the app. Ensure to delete these resources when not required as they will cost money. If you want to specifically create some resources, you can take a look at the script for help.

- Navigate to LongHaulSetup.ps1.

- Open powershell and run the following command by replacing the variables in brackets with your own preferred values.

> If you need dependencies installed, run in an administrative PowerShell window and with `-InstallDependencies`.

  ```Shell
  .\LongHaulSetup.ps1 -SubscriptionId [yourSubscriptionId] -Region [YourRegion] -ResourceGroup [ResourceGroupName]
  ```

- LongHaulSetup.ps1 is a one-time script till you want to tear down the resources.
