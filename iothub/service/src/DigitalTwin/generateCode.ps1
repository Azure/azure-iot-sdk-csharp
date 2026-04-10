# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

try {
	# Remove the manually renamed (model) files, since `autorest` command will only delete and regenerate the autorest created files.
	Get-ChildItem ./Generated/Models *.cs |
		Foreach-Object {
			Remove-Item $_.FullName
		}

	# TEMPORARY: Until https://msazure.visualstudio.com/One/_workitems/edit/8354260 is resolved.
    # Update the swagger json file to accept payload as "optional" for invoke command APIs.
    $swaggerJson = Get-Content .\DigitalTwin.json -raw | ConvertFrom-Json
    $commandRestPath = '/digitaltwins/{id}/commands/{commandName}'
    $commandParameters = $swaggerJson.paths.$commandRestPath.post.parameters
    Foreach ($parameter in $commandParameters) {
        if ($parameter.name -eq 'payload') {
            $parameter.required = $false
        }
    }
	$componentCommandRestPath = '/digitaltwins/{id}/components/{componentPath}/commands/{commandName}'
    $componentCommandParameters = $swaggerJson.paths.$componentCommandRestPath.post.parameters
    Foreach ($parameter in $componentCommandParameters) {
        if ($parameter.name -eq 'payload') {
            $parameter.required = $false
        }
    }
    $swaggerJson | ConvertTo-Json -Depth 10 | Set-Content .\DigitalTwin.json

	# Generate the base code from the swagger file that is defined in this folder's README
	autorest ./autorest.md

	# Update the response header class names, comments, internal setters, edit the duplicate invoke component command header class to be internal,
	# update the namespace to be outside Generated (they are referenced publicly)
	Get-ChildItem ./Generated/Models *.cs |
		Foreach-Object {
			$protocolLayerModelsClassCode = ($_ | Get-Content)
			$protocolLayerModelsClassCode = $protocolLayerModelsClassCode -replace 'Defines headers for GetDigitalTwin operation.', 'Defines headers for GetAsync operation.'
			$protocolLayerModelsClassCode = $protocolLayerModelsClassCode -replace 'Defines headers for UpdateDigitalTwin operation.', 'Defines headers for UpdateAsync operation.'
			$protocolLayerModelsClassCode = $protocolLayerModelsClassCode -replace 'namespace Microsoft.Azure.Devices.Generated.Models', 'namespace Microsoft.Azure.Devices'
			$protocolLayerModelsClassCode = $protocolLayerModelsClassCode -replace 'DigitalTwinGetDigitalTwinHeaders', 'DigitalTwinGetHeaders'
			$protocolLayerModelsClassCode = $protocolLayerModelsClassCode -replace 'DigitalTwinUpdateDigitalTwinHeaders', 'DigitalTwinUpdateHeaders'
			$protocolLayerModelsClassCode = $protocolLayerModelsClassCode -replace 'public string ETag { get; set; }', 'public string ETag { get; internal set; }'
			$protocolLayerModelsClassCode = $protocolLayerModelsClassCode -replace 'public string Location { get; set; }', 'public string Location { get; internal set; }'
			$protocolLayerModelsClassCode = $protocolLayerModelsClassCode -replace 'public partial class DigitalTwinInvokeRootLevelCommandHeaders', 'internal partial class DigitalTwinInvokeRootLevelCommandHeaders'
			$protocolLayerModelsClassCode = $protocolLayerModelsClassCode -replace 'public partial class DigitalTwinInvokeComponentCommandHeaders', 'internal partial class DigitalTwinInvokeComponentCommandHeaders'
			[IO.File]::WriteAllText($_.FullName, ($protocolLayerModelsClassCode -join "`r`n"))
		}

	# Rename the class file names as well
	Rename-Item ./Generated/Models/DigitalTwinGetDigitalTwinHeaders.cs DigitalTwinGetHeaders.cs
	Rename-Item ./Generated/Models/DigitalTwinUpdateDigitalTwinHeaders.cs DigitalTwinUpdateHeaders.cs

	# Edit the protocol layer to make all classes and interfaces internal, remove references to Generated.Models namespace
	Get-ChildItem ./Generated *.cs |
		Foreach-Object {
			$protocolLayerClassCode = ($_ | Get-Content)
			$protocolLayerClassCode = $protocolLayerClassCode -replace 'public partial class', 'internal partial class'
			$protocolLayerClassCode = $protocolLayerClassCode -replace 'public partial interface', 'internal partial interface'
			$protocolLayerClassCode = $protocolLayerClassCode -replace 'public static partial class', 'internal static partial class'
			$protocolLayerClassCode = $protocolLayerClassCode -notmatch 'using Models;'
			[IO.File]::WriteAllText($_.FullName, ($protocolLayerClassCode -join "`r`n"))
		}

	# Edit the protocol layer interface to return the correct response types
	Get-ChildItem . Generated/IDigitalTwin.cs |
		Foreach-Object {
			$IDigitalTwinInterfaceClassCode = ($_ | Get-Content)
			$IDigitalTwinInterfaceClassCode = $IDigitalTwinInterfaceClassCode -replace 'IList<object> digitalTwinPatch', 'string digitalTwinPatch'
			$IDigitalTwinInterfaceClassCode = $IDigitalTwinInterfaceClassCode -replace 'Task<HttpOperationResponse<object,DigitalTwinGetDigitalTwinHeaders>>', 'Task<HttpOperationResponse<string,DigitalTwinGetHeaders>>'
			$IDigitalTwinInterfaceClassCode = $IDigitalTwinInterfaceClassCode -replace 'Task<HttpOperationHeaderResponse<DigitalTwinUpdateDigitalTwinHeaders>>', 'Task<HttpOperationHeaderResponse<DigitalTwinUpdateHeaders>>'
			$IDigitalTwinInterfaceClassCode = $IDigitalTwinInterfaceClassCode -replace 'object payload = default\(object\)', 'string payload = default'
			$IDigitalTwinInterfaceClassCode = $IDigitalTwinInterfaceClassCode -replace 'Task<HttpOperationResponse<object,DigitalTwinInvokeRootLevelCommandHeaders>>', 'Task<HttpOperationResponse<string,DigitalTwinInvokeRootLevelCommandHeaders>>'
			$IDigitalTwinInterfaceClassCode = $IDigitalTwinInterfaceClassCode -replace 'Task<HttpOperationResponse<object,DigitalTwinInvokeComponentCommandHeaders>>', 'Task<HttpOperationResponse<string,DigitalTwinInvokeComponentCommandHeaders>>'
			[IO.File]::WriteAllText($_.FullName, ($IDigitalTwinInterfaceClassCode -join "`r`n"))
		}

	# Edit the protocol layer http requests to take string without json escaping anything, and return the correct response types
	Get-ChildItem . Generated/DigitalTwin.cs |
		Foreach-Object {
			$DigitalTwinClassCode = ($_ | Get-Content)
			$DigitalTwinClassCode = $DigitalTwinClassCode -replace '_responseContent = string.Empty;' , '_responseContent = null;'
			$DigitalTwinClassCode = $DigitalTwinClassCode -replace 'Rest.Serialization.SafeJsonConvert.DeserializeObject<object>\(_responseContent, Client.DeserializationSettings\);', '_responseContent;'
			$DigitalTwinClassCode = $DigitalTwinClassCode -replace 'Task<HttpOperationResponse<object,DigitalTwinGetDigitalTwinHeaders>>', 'Task<HttpOperationResponse<string,DigitalTwinGetHeaders>>'
			$DigitalTwinClassCode = $DigitalTwinClassCode -replace 'new HttpOperationResponse<object,DigitalTwinGetDigitalTwinHeaders>', 'new HttpOperationResponse<string,DigitalTwinGetHeaders>'
			$DigitalTwinClassCode = $DigitalTwinClassCode -replace '_httpResponse.GetHeadersAsJson\(\).ToObject<DigitalTwinGetDigitalTwinHeaders>', '_httpResponse.GetHeadersAsJson().ToObject<DigitalTwinGetHeaders>'
			$DigitalTwinClassCode = $DigitalTwinClassCode -replace 'IList<object> digitalTwinPatch', 'string digitalTwinPatch'
			$DigitalTwinClassCode = $DigitalTwinClassCode -replace '_requestContent = Rest.Serialization.SafeJsonConvert.SerializeObject\(digitalTwinPatch, Client.SerializationSettings\);', '_requestContent = digitalTwinPatch;'
			$DigitalTwinClassCode = $DigitalTwinClassCode -replace 'Task<HttpOperationHeaderResponse<DigitalTwinUpdateDigitalTwinHeaders>>' , 'Task<HttpOperationHeaderResponse<DigitalTwinUpdateHeaders>>'
			$DigitalTwinClassCode = $DigitalTwinClassCode -replace 'new HttpOperationHeaderResponse<DigitalTwinUpdateDigitalTwinHeaders>', 'new HttpOperationHeaderResponse<DigitalTwinUpdateHeaders>'
			$DigitalTwinClassCode = $DigitalTwinClassCode -replace '_httpResponse.GetHeadersAsJson\(\).ToObject<DigitalTwinUpdateDigitalTwinHeaders>', '_httpResponse.GetHeadersAsJson().ToObject<DigitalTwinUpdateHeaders>'
			$DigitalTwinClassCode = $DigitalTwinClassCode -replace 'object payload = default\(object\)', 'string payload = default'
			$DigitalTwinClassCode = $DigitalTwinClassCode -replace '_requestContent = Rest.Serialization.SafeJsonConvert.SerializeObject\(payload, Client.SerializationSettings\);', '_requestContent = payload;'
			$DigitalTwinClassCode = $DigitalTwinClassCode -replace 'Task<HttpOperationResponse<object,DigitalTwinInvokeRootLevelCommandHeaders>>' , 'Task<HttpOperationResponse<string,DigitalTwinInvokeRootLevelCommandHeaders>>'
			$DigitalTwinClassCode = $DigitalTwinClassCode -replace 'new HttpOperationResponse<object,DigitalTwinInvokeRootLevelCommandHeaders>', 'new HttpOperationResponse<string,DigitalTwinInvokeRootLevelCommandHeaders>'
			$DigitalTwinClassCode = $DigitalTwinClassCode -replace 'Task<HttpOperationResponse<object,DigitalTwinInvokeComponentCommandHeaders>>' , 'Task<HttpOperationResponse<string,DigitalTwinInvokeComponentCommandHeaders>>'
			$DigitalTwinClassCode = $DigitalTwinClassCode -replace 'new HttpOperationResponse<object,DigitalTwinInvokeComponentCommandHeaders>', 'new HttpOperationResponse<string,DigitalTwinInvokeComponentCommandHeaders>'
			[IO.File]::WriteAllText($_.FullName, ($DigitalTwinClassCode -join "`r`n"))
		}

	# Edit the protocol layer extensions class to return the correct response types
	Get-ChildItem . Generated/DigitalTwinExtensions.cs |
		Foreach-Object {
			$DigitalTwinExtensionsClassCode = ($_ | Get-Content)
			$DigitalTwinExtensionsClassCode = $DigitalTwinExtensionsClassCode -replace 'IList<object> digitalTwinPatch', 'string digitalTwinPatch'
			$DigitalTwinExtensionsClassCode = $DigitalTwinExtensionsClassCode -replace 'Task<DigitalTwinUpdateDigitalTwinHeaders>' , 'Task<DigitalTwinUpdateHeaders>'
			$DigitalTwinExtensionsClassCode = $DigitalTwinExtensionsClassCode -replace 'object payload = default\(object\)', 'string payload = default'
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