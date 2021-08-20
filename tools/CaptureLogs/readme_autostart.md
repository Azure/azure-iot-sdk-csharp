# Auto start log traces and ship to Azure Storage
Instructions on how to create a trace session that is started on boot and will automatically upload the files using a scheduled task.

## Prerequisites
* [Azure Storage Explorer](https://docs.microsoft.com/en-us/azure/vs-azure-tools-storage-manage-with-storage-explorer?tabs=windows)
* Azure Storage Account
* SAS Token for Storage Account
* [azcopy](https://github.com/Azure/azure-storage-azcopy/releases/latest) from the GitHub page
* Elevated command prompt


## Install Azure Storage Explorer

See [Get started with Storage Explorer](https://docs.microsoft.com/en-us/azure/vs-azure-tools-storage-manage-with-storage-explorer?tabs=windows) for download link and instructions.

## Create the Azure Storage Container

Use the Azure Storage Explorer to [create a container](https://docs.microsoft.com/en-us/azure/storage/blobs/storage-quickstart-blobs-storage-explorer)

## Get the Azure Storage SAS token

Follow [these instructions](https://docs.microsoft.com/en-us/azure/cognitive-services/translator/document-translation/create-sas-tokens?tabs=Containers) on how to generate the SAS token. You will need this to replace the variable in the script.

## Install azcopy on remote machine

Unzip the [azcopy](https://github.com/Azure/azure-storage-azcopy/releases/latest) zip file obtained from the GitHub page to a location that can be accessed on the remote machine.

## Create a logman autosession (elevated command prompt)

These commands will create and start the logman session. The `autosession\` identifier will ensure that this trace session is started on boot. The `-cnf ... -v mmddhhmm` options will ensure that we're creating a new file every hour with the specified format.

```
logman create trace autosession\IotTrace -pf .\iot_providers.txt -o c:\perflogs\iot\iot.etl -cnf 01:00:00 -v mmddhhmm
logman start IotTrace -ets
```

## Edit the powershell script to use the SAS token

Edit the [IotTraceScheduledTask.ps1](IotTraceScheduledTask.ps1) script with the correct variables

Copy this script to a location that can be accessed on the remote machine. It would make sense to copy it to the same place you have `azcopy`.

```powershell
$ETLLogs = "c:\perflogs\iot"
$AZCopyLocation = "<<PATH TO AZ COPY>>"
$SASToken = "<<YOUR SAS TOKEN>>"
$StorageContainerURI = "https://[account].blob.core.windows.net/[container]/[path/to/directory]"
```

## Create a Scheduled Task (elevated command prompt)

This command creates a [scheduled task](https://docs.microsoft.com/en-us/windows-server/administration/windows-commands/schtasks) that runs the upload powershell script every day at 10pm. This also runs as SYSTEM so it will run regardless of a logged in user.

```
schtasks /create /sc DAILY /tn IotTraceUpload /tr c:\azcopy\IotTraceScheduledTask.ps1 /ru system /st 22:00 /ENABLE
```

## IotTraceScheduledTask.ps1

This powershell will do two things. First it tries to upload all of the files in the ETL trace folder. It will only overwrite if the source is newer. It will then attempt to delete all but the last 24 hours of log files so there is not a lot of file space wasted.