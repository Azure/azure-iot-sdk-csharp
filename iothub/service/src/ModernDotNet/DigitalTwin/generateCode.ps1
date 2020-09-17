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
			$protocolLayerClassCode = $protocolLayerClassCode -replace 'public static partial class', 'internal static partial class'
			[IO.File]::WriteAllText($_.FullName, ($protocolLayerClassCode -join "`r`n"))
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