# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

# For pull requests, this script checks the git diff between the merging branch and the upstream branch to see
#  if any code changes were made to iothub clients or provisioning clients.
# 
# For all other builds (nightly builds, for instance), this script will set environment variables to run all e2e tests.

function IsPullRequestBuild
{
	return !($env:TARGET_BRANCH -and $env:TARGET_BRANCH.toLower().Contains("system.pullrequest.targetbranch"))
}

function ShouldSkipIotHubTests 
{	
	if (IsPullRequestBuild) 
	{
		return $false
	}
	
	return !(DoChangesAffectAnyOfFolders @("iothub", "common", "shared", "e2e", "vsts"))
}

function ShouldSkipDPSTests 
{
	if (ShouldSkipIotHubTests) 
	{
		return !(DoChangesAffectAnyOfFolders @("Provisioning", "Security"))
	}
	
	#Provisioning tests depend on iot hub packages, so if iot hub tests aren't being skipped, neither should provisioning tests
	return $false
}

function ShouldSkipDigitalTwinTests 
{
	if (ShouldSkipIotHubTests) 
	{
		return !(DoChangesAffectAnyOfFolders @("Digitaltwin"))
	}
	
	#Digital twin tests depend on iot hub packages, so if iot hub tests aren't being skipped, neither should digital twin tests
	return $false
}

# $folderNames is an array of strings where each string is the name of a folder within the codebase to look for in the git diff between the source and target branches
# For instance, $folderNames can be "iothub", "common", "shared" if you want to see if and changes happened within the iothub folder, the common folder, or in the shared folder
function DoChangesAffectAnyOfFolders($folderNames) 
{
	#TARGET_BRANCH is defined by the yaml file that calls this script. It is equal to the azure devops pre-defined variable "$(System.PullRequest.TargetBranch)" which contains either
	# the target branch of the pull request build if it is a pull request build, or a default value "system.pullrequest.targetbranch" if it is not a pull request build.
	$GitDiff = & git diff origin/$env:TARGET_BRANCH --name-only
	ForEach ($line in $($GitDiff -split "`r`n")) 
	{
		if ($line.EndsWith(".md", "CurrentCultureIgnoreCase") -or $line.EndsWith(".png", "CurrentCultureIgnoreCase")) 
		{
			# These file types are ignored when determining if source code changes require running e2e tests
		}
		elseif ($line.toLower().Contains("sample")) 
		{
			# Sample changes don't necessitate running e2e tests
		}
		else 
		{
			foreach ($folderName in $folderNames) 
			{
				if ($line.toLower().Contains($folderName.toLower())) 
				{
					return $true
				}
			}
		}
	}
	
	return $false
}