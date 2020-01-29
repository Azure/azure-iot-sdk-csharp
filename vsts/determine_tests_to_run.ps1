# For pull requests, this script checks the git diff between the merging branch and the upstream branch to see
#  if any code changes were made to iothub clients or provisioning clients. As a result, this script sets environment
#  variables to signal later vsts scripts to run or not to run certain e2e tests

# For all other builds (nightly builds, for instance), this script will set environment variables to run all e2e tests.

Write-Host "Determining tests to run..."
$Env:RUN_IOTHUB_TESTS = "false"
$Env:RUN_PROVISIONING_TESTS = "false"

$targetBranch = ($env:TARGET_BRANCH)
if (($env:TARGET_BRANCH).toLower().Contains("system.pullrequest.targetbranch"))
{
    Write-Host "Assuming this build is not a pull request build, running all tests"

    $Env:RUN_IOTHUB_TESTS = "true"
    $Env:RUN_PROVISIONING_TESTS = "true"

    exit 0
}

$GitDiff = & git diff origin/$targetBranch --name-only
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
	    # If code changes were made to provisioning package, only need to run provisioning tests
		if ($line.toLower().Contains("provisioning") -or $line.toLower().Contains("security"))
		{
			$Env:RUN_PROVISIONING_TESTS = "true"
		}

        # If code changes were made to iot hub clients
		if ($line.toLower().Contains("iothub"))
		{
            $Env:RUN_IOTHUB_TESTS = "true"
            $Env:RUN_PROVISIONING_TESTS = "true"
		}

        # Both provisioning and iot hub depend on shared package, so run all tests
		if ($line.toLower().Contains("shared"))
		{
			$Env:RUN_IOTHUB_TESTS = "true"
			$Env:RUN_PROVISIONING_TESTS = "true"
		}

        # Any changes to e2e test folder should run all tests
		if ($line.toLower().Contains("e2e"))
        {
            $Env:RUN_IOTHUB_TESTS = "true"
            $Env:RUN_PROVISIONING_TESTS = "true"
        }
	}
}

if ($Env:RUN_IOTHUB_TESTS -eq "true")
{
    Write-Host "Will run iot hub tests"
}
else
{
    Write-Host "Will not run iot hub tests"
}

if ($Env:RUN_PROVISIONING_TESTS -eq "true")
{
    Write-Host "Will run provisioning tests"
}
else
{
    Write-Host "Will not run provisioning tests"
}

