# Auto start log traces and ship to Azure Storage
Instructions on how to create a trace session that is started on boot and will automatically upload the files using a scheduled task.

## Prerequisites
* [Azure Storage Explorer](https://docs.microsoft.com/en-us/azure/vs-azure-tools-storage-manage-with-storage-explorer?tabs=windows)
* Azure Storage Account
* SAS Token for Storage Account
* [azcopy](https://github.com/Azure/azure-storage-azcopy/releases/latest) from the GitHub page
* Elevated command prompt

## Steps to complete

1. Create a new Azure Storage account, or use an existing one
2. Create a container for log files
3. Generate a SAS Token
   * Make sure it has READ, WRITE, and CREATE permissions
   * Make sure the SAS token expiry is long enough to capture the failure scenario
4. Copy azcopy to the **remote machine** (c:\azcopy in this example)
5. Copy the [iot_providers.txt](iot_providers.txt) and [IotTraceScheduledTask.ps1](IotTraceScheduledTask.ps1) files to the **remote machine** (c:\azcopy in this example)
6. Edit the IotTraceScheduledTask.ps1 variables on the **remote machine**
    * There will be instructions in the file
7. Execute the following logman commands on the **remote machine**
    * `logman create trace IotTrace -pf c:\azcopy\iot_providers.txt -o c:\azcopy\iotlogs\iot.etl -cnf 01:00:00 -v mmddhhmm`
    * `logman start IotTrace`
8. Execute the folloing schtasks command on the **remote machine**
    * This command runs daily see [this page](https://docs.microsoft.com/en-us/windows-server/administration/windows-commands/schtasks-create) for more examples
    * Change `/st 22:00` to be a proper time to upload
    * `schtasks /create /tn StartLogMan /tr "logman start IotTrace" /sc onstart /ru system`
    * `schtasks /create /sc DAILY /tn IotTraceUpload /tr "powershell.exe -ExecutionPolicy Bypass -File c:\azcopy\IotTraceScheduledTask.ps1" /ru system /st 22:00`


> NOTE The `IotTraceScheduledTask.ps1` file was designed with a daily upload in mind. You can review the file for instructions on how to modify the commands to handle a different scheduling type.

## Create Azure Storage Account

See [Create a storage account](https://docs.microsoft.com/en-us/azure/storage/common/storage-account-create?tabs=azure-portal) for instructions.

## Install Azure Storage Explorer

See [Get started with Storage Explorer](https://docs.microsoft.com/en-us/azure/vs-azure-tools-storage-manage-with-storage-explorer?tabs=windows) for download link and instructions.

## Create the Azure Storage Container

Use the Azure Storage Explorer to [create a container](https://docs.microsoft.com/en-us/azure/storage/blobs/storage-quickstart-blobs-storage-explorer)

## Get the Azure Storage SAS token

Follow [these instructions](https://docs.microsoft.com/en-us/azure/cognitive-services/translator/document-translation/create-sas-tokens?tabs=Containers) on how to generate the SAS token. You will need this to replace the variable in the script.

> **NOTE** You MUST generate the token with READ and WRITE permissions so we can use the overwrite feature.

## Install azcopy on remote machine

Unzip the [azcopy](https://github.com/Azure/azure-storage-azcopy/releases/latest) zip file obtained from the GitHub page to a location that can be accessed on the remote machine.

## Create a logman trace session (elevated command prompt)

These commands will create and start the logman session. The `autosession\` identifier will ensure that this trace session is started on boot. The `-cnf ... -v mmddhhmm` options will ensure that we're creating a new file every hour with the specified format.

```
logman create trace IotTrace -pf c:\azcopy\iot_providers.txt -o c:\azcopy\iotlogs\iot.etl -cnf 01:00:00 -v mmddhhmm
logman start IotTrace
```

## Create a Scheduled Task to start trace  (elevated command prompt)

This command creates a [scheduled task](https://docs.microsoft.com/en-us/windows-server/administration/windows-commands/schtasks) that starts the specified trace on system boot.

`schtasks /create /tn StartLogMan /tr "logman start IotTrace" /sc onstart /ru system`

## Edit the powershell script to use the SAS token

Edit the [IotTraceScheduledTask.ps1](IotTraceScheduledTask.ps1) script with the correct variables. If you have a pre-combined URI and SAS token you can just replace `combinedURI`

Copy this script to a location that can be accessed on the remote machine. It would make sense to copy it to the same place you have `azcopy`.

```powershell
$ETLLogs = "c:\perflogs\iot"
$AZCopyLocation = "c:\azcopy\azcopy.exe"
$SASToken = "<<your SAS token>>"
$StorageContainerURI = "https://[account].blob.core.windows.net/[container]"

# Optionally you can replace this variable with a pre combined SAS token URL
$combinedURI = "$StorageContainerURI`?$SASToken"
# $combinedURI = "https://[account].blob.core.windows.net/[container]?[SAS]"
```

## Create a Scheduled Task for upload  (elevated command prompt)

This command creates a [scheduled task](https://docs.microsoft.com/en-us/windows-server/administration/windows-commands/schtasks) that runs the upload powershell script every day at 10pm. This also runs as SYSTEM so it will run regardless of a logged in user.

### Every day at 10:00pm
```
schtasks /create /sc DAILY /tn IotTraceUpload /tr "powershell.exe -ExecutionPolicy Bypass -File c:\azcopy\IotTraceScheduledTask.ps1" /ru system /st 22:00
```

### Every 3 hours
```
schtasks /create /sc HOURLY /mo 3 /tn IotTraceUpload /tr "powershell.exe -ExecutionPolicy Bypass -File c:\azcopy\IotTraceScheduledTask.ps1" /ru system
```

## IotTraceScheduledTask.ps1

This powershell will do two things. First it tries to sync the files in the ETL trace folder. It will only overwrite if the source is newer. It will then attempt to delete all but the last 24 hours of log files so there is not a lot of file space wasted.

**You should immediately test this script once you've replaced the variables to ensure you have all of the folders and files setup before committing this to the scheduled task.**