# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

try {
	# Remove the manually renamed (model) files, since `autorest` command will only delete and regenerate the autorest created files.
	Get-ChildItem ./Generated/Models *.cs |
		Foreach-Object {
			Remove-Item $_.FullName
		}

	#Generate the base code from the swagger file that is defined in this folder's README
	autorest
	
	#Edit the protocol layer to make all classes and interfaces internal
	Get-ChildItem ./Generated *.cs |
		Foreach-Object {
			$protocolLayerClassCode = ($_ | Get-Content)
			$protocolLayerClassCode = $protocolLayerClassCode -replace 'public partial class', 'internal partial class'
			$protocolLayerClassCode = $protocolLayerClassCode -replace 'public partial interface', 'internal partial interface'
			$protocolLayerClassCode = $protocolLayerClassCode -replace 'public static partial class', 'internal static partial class'
			[IO.File]::WriteAllText($_.FullName, ($protocolLayerClassCode -join "`r`n"))
		}

	#Update the response header class names
	Get-ChildItem ./Generated/Models *.cs |
		Foreach-Object {
			$protocolLayerModelsClassCode = ($_ | Get-Content)
			$protocolLayerModelsClassCode = $protocolLayerModelsClassCode -replace 'DigitalTwinGetDigitalTwinHeaders', 'DigitalTwinGetHeaders'
			$protocolLayerModelsClassCode = $protocolLayerModelsClassCode -replace 'DigitalTwinUpdateDigitalTwinHeaders', 'DigitalTwinUpdateHeaders'
			[IO.File]::WriteAllText($_.FullName, ($protocolLayerModelsClassCode -join "`r`n"))
		}

	# Rename the class file names as well
	Rename-Item ./Generated/Models/DigitalTwinGetDigitalTwinHeaders.cs DigitalTwinGetHeaders.cs
	Rename-Item ./Generated/Models/DigitalTwinUpdateDigitalTwinHeaders.cs DigitalTwinUpdateHeaders.cs

	#Edit the protocol layer interface to return the correct response types
	Get-ChildItem . Generated/IDigitalTwin.cs |
		Foreach-Object {
			$IDigitalTwinInterfaceClassCode = ($_ | Get-Content) 
			$IDigitalTwinInterfaceClassCode = $IDigitalTwinInterfaceClassCode -replace 'Task<HttpOperationResponse<object,DigitalTwinGetDigitalTwinHeaders>>', 'Task<HttpOperationResponse<string,DigitalTwinGetHeaders>>'
			$IDigitalTwinInterfaceClassCode = $IDigitalTwinInterfaceClassCode -replace 'Task<HttpOperationHeaderResponse<DigitalTwinUpdateDigitalTwinHeaders>>', 'Task<HttpOperationHeaderResponse<DigitalTwinUpdateHeaders>>'
			[IO.File]::WriteAllText($_.FullName, ($IDigitalTwinInterfaceClassCode -join "`r`n"))
		}

	#Edit the protocol layer http requests to take string without json escaping anything, and return the correct response types
	Get-ChildItem . Generated/DigitalTwin.cs |
		Foreach-Object {
			$DigitalTwinClassCode = ($_ | Get-Content)
			$DigitalTwinClassCode = $DigitalTwinClassCode -replace '_responseContent = string.Empty;' , '_responseContent = null;'
			$DigitalTwinClassCode = $DigitalTwinClassCode -replace 'Rest.Serialization.SafeJsonConvert.DeserializeObject<object>\(_responseContent, Client.DeserializationSettings\);', '_responseContent;'
			$DigitalTwinClassCode = $DigitalTwinClassCode -replace 'Task<HttpOperationResponse<object,DigitalTwinGetDigitalTwinHeaders>>', 'Task<HttpOperationResponse<string,DigitalTwinGetHeaders>>'
			$DigitalTwinClassCode = $DigitalTwinClassCode -replace 'new HttpOperationResponse<object,DigitalTwinGetDigitalTwinHeaders>', 'new HttpOperationResponse<string,DigitalTwinGetHeaders>'
			$DigitalTwinClassCode = $DigitalTwinClassCode -replace '_httpResponse.GetHeadersAsJson\(\).ToObject<DigitalTwinGetDigitalTwinHeaders>', '_httpResponse.GetHeadersAsJson().ToObject<DigitalTwinGetHeaders>'
			$DigitalTwinClassCode = $DigitalTwinClassCode -replace '_requestContent = Rest.Serialization.SafeJsonConvert.SerializeObject(digitalTwinPatch, Client.SerializationSettings);', '_requestContent = digitalTwinPatch;'
			$DigitalTwinClassCode = $DigitalTwinClassCode -replace 'Task<HttpOperationHeaderResponse<DigitalTwinUpdateDigitalTwinHeaders>>' , 'Task<HttpOperationHeaderResponse<DigitalTwinUpdateHeaders>>'
			$DigitalTwinClassCode = $DigitalTwinClassCode -replace 'new HttpOperationHeaderResponse<DigitalTwinUpdateDigitalTwinHeaders>', 'new HttpOperationHeaderResponse<DigitalTwinUpdateHeaders>'
			$DigitalTwinClassCode = $DigitalTwinClassCode -replace '_httpResponse.GetHeadersAsJson\(\).ToObject<DigitalTwinUpdateDigitalTwinHeaders>', '_httpResponse.GetHeadersAsJson().ToObject<DigitalTwinUpdateHeaders>'
			[IO.File]::WriteAllText($_.FullName, ($DigitalTwinClassCode -join "`r`n"))
		}

	#Edit the protocol layer extensions class to return the correct response types
	Get-ChildItem . Generated/DigitalTwinExtensions.cs |
		Foreach-Object {
			$DigitalTwinExtensionsClassCode = ($_ | Get-Content) 
			$DigitalTwinExtensionsClassCode = $DigitalTwinExtensionsClassCode -replace 'Task<DigitalTwinUpdateDigitalTwinHeaders>' , 'Task<DigitalTwinUpdateHeaders> '
			[IO.File]::WriteAllText($_.FullName, ($DigitalTwinExtensionsClassCode -join "`r`n"))
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