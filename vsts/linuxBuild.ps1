# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

.\vsts\gatedBuild.ps1 -framework $env:FRAMEWORK

if ($LASTEXITCODE -ne 0)
{
    throw "Windows build failed"
}
