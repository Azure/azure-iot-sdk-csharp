# Docker for WSL setup for local test

This script will setup docker images and run instances of these images to support E2E testing.

See [e2eTestsSetup.ps1](https://github.com/Azure/azure-iot-sdk-csharp/blob/main/e2e/test/prerequisites/E2ETestsSetup/e2eTestsSetup.ps1) script for E2E test setup prerequisites.

- Open powershell and run WSL and then run the script [docker-setup.sh](https://github.com/Azure/azure-iot-sdk-csharp/blob/main/e2e/test/docker/docker-setup.sh) in <repo>/e2e/test/docker directory.

  ```Shell
  .\docker-setup.sh
  ```

- This script needs to run each time to setup and run docker images for e2e testing.

- This script needs to re-run after reboot or when the IP address of WSL host and container changes.

- There are IP addresses to host mapping that needs to copy to the host system /etc/hosts file (C:\Windows\System32\drivers\etc\hosts)

## Common script issues

- If after running the script, the output does not show valid IP addresses for the hostnames, restart the PC and re-run this script

- If the docker instances exit immediately, restart the PC and re-run this script

