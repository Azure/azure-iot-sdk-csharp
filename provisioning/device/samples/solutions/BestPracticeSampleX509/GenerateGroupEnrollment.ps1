# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

param(
    # Intermediate certificate file name including the path
    [Parameter(Mandatory=$true)]
    [string] $intermediateCertName,

    # Resource group name of the IoT dps.
    [Parameter(Mandatory=$true)]
    [string] $resourceGroup,

    # IoT dps name used for running the sample.
    [Parameter(Mandatory=$true)]
    [string] $dpsName
)
# GroupEnrollment Id created to use in sample.
$groupEnrollmentId = "x509GroupEnrollment"

# Check certificate file extension.
if (($intermediateCertName.EndsWith('.pem') -ne $true) -and ($intermediateCertName.EndsWith('.cer') -ne $true)){
    Write-Host "Certificate file type must be either '.pem' or '.cer'"
    exit
}

# Check if the resource group exists. If not, exit.
$resourceGroupExists = az group exists -n $resourceGroup
if ($resourceGroupExists -ne $true){
    Write-Host "Resource Group '$resourceGroup' does not exist. Exiting..."
    exit
}

# Check if the dps instance exists. If not, exit.
$dpsExists = az iot dps show --name $dpsName -g $resourceGroup 2>nul
if ($dpsExists -eq $null){
    Write-Host "Dps '$dpsName' does not exist under '$resourceGroup'. Exiting..."
    exit
}

# Check if the enrollment group already exists in dps instance. If it does, delete and regenerate the group enrollment.
Write-Host "`Checking if '$groupEnrollmentId' enrollment group already exists in '$dpsName'..."
$groupEnrollmentExists = az iot dps enrollment-group show --dps-name $dpsName -g $resourceGroup --enrollment-id $groupEnrollmentId 2>nul
if ($groupEnrollmentExists)
{
    Write-Host "Deleting existing enrollment group '$groupEnrollmentId' in '$dpsName'..."
    az iot dps enrollment-group delete -g $resourceGroup --eid $groupEnrollmentId --dps-name $dpsName
    Write-Host "Enrollment group '$groupEnrollmentId' is deleted in '$dpsName'."
}
else {
    Write-Host "$groupEnrollmentId enrollment group does not exist in $dpsName"
}

Write-Host "Creating an enrollment group '$groupEnrollmentId' in '$dpsName'..."
az iot dps enrollment-group create -g $resourceGroup --dps-name $dpsName --enrollment-id $groupEnrollmentId --certificate-path $intermediateCertName
Write-Host "enrollment group '$groupEnrollmentId' is created in '$dpsName'."