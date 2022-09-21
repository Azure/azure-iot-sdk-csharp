# Docker for WSL setup for local test

## Intro

This script will setup docker images and run instances of these images to support E2E testing.

See [e2eTestsSetup.ps1](https://github.com/Azure/azure-iot-sdk-csharp/blob/main/e2e/test/prerequisites/E2ETestsSetup/e2eTestsSetup.ps1) script for E2E test setup prerequisites.

## Before running docker-setup.sh script

- Install WSL.
  - See installation details here: <https://docs.microsoft.com/windows/wsl/install>.
  - Reboot after install.
- Install Docker on WSL (For docker on Ubuntu, see installation details here: <https://docs.docker.com/engine/install/ubuntu/#install-using-the-repository>.
  - See installation details for other Linux distribution here: <https://docs.docker.com/engine/install/>).
  - Reboot after install.

  ## Running the docker-setup.sh script

- Run wsl (See click on the linux distribution installed or open powershell and enter 'wsl')
  
- Change directory to the folder in the repo with the script [docker-setup.sh](https://github.com/Azure/azure-iot-sdk-csharp/blob/main/e2e/test/docker/docker-setup.sh)
  
  ```Shell
  cd <repo directory>/e2e/test/docker
  .\docker-setup.sh
  ```

- The script output a list of IPs to hosts mapping at completion, copy these entries to the host system /etc/hosts file (default in $Env:windir\System32\drivers\etc\hosts, not in WSL)

## Common script issues

- This script needs to run each time to setup docker instance with the updated network setup for e2e testing.
- This script needs to re-run after reboot or when the IP address of WSL host and container changes (WSL virtual network IP changes each time it starts).
- Update the host file in the host system where the E2E test code and IDE will run on (not WSL).
- If after running the script, the output does not show valid IP addresses for the hostnames, restart the PC and re-run this script.
- If the docker instances exit immediately, restart the PC and re-run this script.
