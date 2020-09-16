# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

try {
	#Generate the base code from the swagger file that is defined in this folder's README
	autorest
	
	#Edit the protocol layer to make all classes and interfaces internal
	Get-ChildItem ./Generated *.cs |
		Foreach-Object {
			$protocolLayerClassCode = ($_ | Get-Content) 
			$protocolLayerClassCode = $protocolLayerClassCode -replace 'public partial class', 'internal partial class'
			$protocolLayerClassCode = $protocolLayerClassCode -replace 'public partial interface', 'internal partial interface'
			[IO.File]::WriteAllText($_.FullName, ($protocolLayerClassCode -join "`r`n"))
		}

	#Edit the Protocol layer http requests to take string without json escaping anything
	Get-ChildItem . Generated/DigitalTwin.cs |
		Foreach-Object {
			$DigitalTwinClassCode = ($_ | Get-Content) 
			$DigitalTwinClassCode = $DigitalTwinClassCode -replace 'string _responseContent = null;' , 'string _responseContent = null;'
			$DigitalTwinClassCode = $DigitalTwinClassCode -replace '_responseContent = string.Empty;' , '_responseContent = null;'
			$DigitalTwinClassCode = $DigitalTwinClassCode -replace 'Microsoft.Rest.Serialization.SafeJsonConvert.DeserializeObject<object>\(_responseContent, Client.DeserializationSettings\);' , '_responseContent;'
			$DigitalTwinClassCode = $DigitalTwinClassCode -replace '_result.Body = Microsoft.Rest.Serialization.SafeJsonConvert.DeserializeObject<DigitalTwinInterfaces>(_responseContent, Client.DeserializationSettings);' , '_result.Body = Microsoft.Rest.Serialization.SafeJsonConvert.DeserializeObject<DigitalTwinInterfaces>(System.Text.Encoding.UTF8.GetString(_responseContent, 0, _responseContent.Length), Client.DeserializationSettings);'
			$DigitalTwinClassCode = $DigitalTwinClassCode -replace '_requestContent = Microsoft.Rest.Serialization.SafeJsonConvert.SerializeObject\(interfacesPatchInfo, Client.SerializationSettings\);' , '_requestContent = patch;'
			$DigitalTwinClassCode = $DigitalTwinClassCode -replace 'interfacesPatchInfo' , 'patch'
			$DigitalTwinClassCode = $DigitalTwinClassCode -replace 'DigitalTwinInterfacesPatch patch' , 'string patch'
			$DigitalTwinClassCode = $DigitalTwinClassCode -replace '_requestContent = Microsoft.Rest.Serialization.SafeJsonConvert.SerializeObject\(payload, Client.SerializationSettings\);' , '_requestContent = payload.ToString();'
			$DigitalTwinClassCode = $DigitalTwinClassCode -replace 'new HttpOperationResponse<DigitalTwinInterfaces,DigitalTwinGetInterfaceHeaders>();' , 'new HttpOperationResponse<string,DigitalTwinGetInterfaceHeaders>();'
			$DigitalTwinClassCode = $DigitalTwinClassCode -replace '_result.Body = Microsoft.Rest.Serialization.SafeJsonConvert.DeserializeObject<DigitalTwinInterfaces>\(_responseContent, Client.DeserializationSettings\);' , '_result.Body = _responseContent;'
			$DigitalTwinClassCode = $DigitalTwinClassCode -replace 'Task<HttpOperationResponse<DigitalTwinInterfaces,' , 'Task<HttpOperationResponse<string,'
			$DigitalTwinClassCode = $DigitalTwinClassCode -replace 'new HttpOperationResponse<DigitalTwinInterfaces,' , 'new HttpOperationResponse<string,'
			                
			[IO.File]::WriteAllText($_.FullName, ($DigitalTwinClassCode -join "`r`n"))
		}

	#Edit the Protocol layer to take string without json escaping anything
	Get-ChildItem . Generated/DigitalTwinExtensions.cs |
		Foreach-Object {
			$DigitalTwinExtensionsClassCode = ($_ | Get-Content) 
			$DigitalTwinExtensionsClassCode = $DigitalTwinExtensionsClassCode -replace 'public ' , 'internal '
			$DigitalTwinExtensionsClassCode = $DigitalTwinExtensionsClassCode -replace 'DigitalTwinInterfacesPatch' , 'string'
			$DigitalTwinExtensionsClassCode = $DigitalTwinExtensionsClassCode -replace 'interfacesPatchInfo' , 'patch'
			$DigitalTwinExtensionsClassCode = $DigitalTwinExtensionsClassCode -replace 'Task<DigitalTwinInterfaces>' , 'Task<string>'
			[IO.File]::WriteAllText($_.FullName, ($DigitalTwinExtensionsClassCode -join "`r`n"))
		}

	#Edit the Protocol layer to take string without json escaping anything
	Get-ChildItem . Generated/IDigitalTwin.cs |
		Foreach-Object {
			$IDigitalTwinClassCode = ($_ | Get-Content) 
			$IDigitalTwinClassCode = $IDigitalTwinClassCode -replace 'DigitalTwinInterfacesPatch interfacesPatchInfo,' , 'string patch,'
			$IDigitalTwinClassCode = $IDigitalTwinClassCode -replace 'Task<HttpOperationResponse<DigitalTwinInterfaces,' , 'Task<HttpOperationResponse<string,'
			[IO.File]::WriteAllText($_.FullName, ($IDigitalTwinClassCode -join "`r`n"))
		}

	
}
catch [Exception]{
    $generatorFailed = $true
    $errorMessage = $Error[0]
}

if ($generatorFailed) {
    Write-Host -ForegroundColor Red "Generating code failed ($errorMessage)"
    exit 1
}
else {
    Write-Host -ForegroundColor Green "Generating code succeeded."
    exit 0
}