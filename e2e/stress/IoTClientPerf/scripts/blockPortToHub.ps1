#Requires -RunAsAdministrator

<#
.SYNOPSIS
  Blocks a port using Windows Firewall
.DESCRIPTION
  Blocks a port for a specific IoT Hub using Windows Firewall.
.PARAMETER <Parameter_Name>
    <Brief description of parameter input required. Repeat this attribute if required>
.INPUTS
  <Inputs if any, otherwise state None>
.OUTPUTS
  <Outputs if any, otherwise state None - example: Log file stored in C:\Windows\Temp\<name>.log>
  
.EXAMPLE
  <Example goes here. Repeat this attribute for more than one example>
#>

param (
    [string] $IotHubHostName = $null,
    [int] $BlockDurationSeconds = 10,
    [switch] $cleanOnly = $false
)

if (-not $cleanOnly)
{
    if ($IotHubHostName -eq $null)
    { 
        throw "-IotHubHostName is required."
    }

    Write-Host -NoNewline "Getting IP address for $IotHubHostName . . . "
    $resolveResponse = Resolve-DnsName -Name $IotHubHostName -Type A
    $ipaddress = ($resolveResponse | Where-Object {$_.Type -eq "A"}).IPAddress
    Write-Host $ipaddress
}

$err = 0

try
{
    if (-not $cleanOnly)
    {
        Write-Host "`tBlocking HTTPS"
        New-NetFirewallRule -DisplayName "IoTClientPerf HTTPS $($ipaddress):443" -Protocol TCP -Action Block -Direction Outbound -RemoteAddress $ipaddress -RemotePort 443 -ErrorAction Stop | Out-Null
        Write-Host "`tBlocking AMQPS"
        New-NetFirewallRule -DisplayName "IoTClientPerf MQTTS $($ipaddress):8883" -Protocol TCP -Action Block -Direction Outbound -RemoteAddress $ipaddress -RemotePort 8883 -ErrorAction Stop  | Out-Null
        Write-Host "`tBlocking MQTTS"
        New-NetFirewallRule -DisplayName "IoTClientPerf AMQPS $($ipaddress):5671" -Protocol TCP -Action Block -Direction Outbound -RemoteAddress $ipaddress -RemotePort 5671 -ErrorAction Stop | Out-Null

        Write-Host -NoNewLine "Waiting $BlockDurationSeconds seconds . . ."
        Start-Sleep $BlockDurationSeconds
        Write-Host "Done"
    }
}
catch
{
    Write-Error "An error occured: $_."
    $err = 1
}
finally
{
    Write-Host "Removing all firewall rules..."
    Get-NetFirewallRule | Where-Object {$_.DisplayName -like "IoTClientPerf*"} | ForEach-Object { Write-Host "`t$($_.DisplayName)"; Remove-NetFirewallRule -DisplayName ($_.DisplayName) -ErrorAction Continue }
}

exit $err
