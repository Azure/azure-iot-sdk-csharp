# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

Write-Host Run dotnet first experience.
dotnet new

Write-Host List active docker containers
docker ps -a

.\build.ps1 -clean -configuration Debug

.\build.ps1 -clean -configuration Release -e2etests

Write-Host ""
Write-Host "Gated build completed successfully."
Write-Host ""
