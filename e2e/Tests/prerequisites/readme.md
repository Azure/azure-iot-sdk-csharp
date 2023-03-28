
# Azure IoT .NET end-to-end test prerequisites

The E2E tests require some Azure resources to be set up and configured. Running the [e2eTestsSetup.ps1](e2eTestsSetup.ps1) powershell script is a convenient way of getting all the resources setup with the required configuration.

Note: The e2eTestsSetup.ps1 script will setup all the resources necessary to run the full test suite. Ensure to delete these resources when not required as they will cost money. If you want to specifically create some resources, you can take a look at the script for help.

- Navigate to e2eTestsSetup.ps1.

- Open powershell in Administrator mode and run the following command by replacing the variables in brackets with your own preferred values.
  
  ```Shell
  .\e2eTestsSetup -SubscriptionId [yourSubscriptionId] -Region [YourRegion] -ResourceGroup [ResourceGroupName] -GroupCertificatePassword [Password] -InstallDependencies
  ```

- e2eTestsSetup is a one-time script till you want to tear down the resources. Once that is run successfully, run `.\[load-<keyVaultName>]].ps1` each time before you run your E2E tests. The exact name of the file to run and its location will be displayed as soon the e2eTestsSetup script completes.

- `GroupCertificatePassword` is the password that will be used by the group certs. The tests require the cert to be password protected. This can be anything you want (It will be stored in the KeyVault so you don't need to remember the password you choose)

- If everything is already installed and you are running the script again, don't add `-InstallDependencies` in order to skip installation.

- This script will install docker and pull the required IoT images for your E2E tests and it will provision IoT hub and all other required resources in your subscription.

- The script will automatically upload all the values to a KeyVault and set the Environment Variables in the open powershell session.

## Common script issues

- The script may fail saying something is already installed. In that case, set -InstallDependencies $false and rerun the script.

