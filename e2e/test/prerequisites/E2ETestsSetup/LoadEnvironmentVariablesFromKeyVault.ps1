param(
    [Parameter(Mandatory)]
    [string] $SubscriptionId,

    [Parameter(Mandatory)]
    [string] $KeyVaultName
)

Function Connect-AzureSubscription()
{
    # Ensure the user is logged in
    try
    {
        $azureContext = az account show
    }
    catch
    {
    }

    if (-not $azureContext)
    {
        Write-Host "`nPlease login to Azure..."
        az login
        $azureContext = az account show
    }

    # Ensure the desired subscription is selected
    $sub = az account show --output tsv --query id
    if ($sub -ne $SubscriptionId)
    {
        Write-Host "`nSelecting subscription $SubscriptionId`n"
        az account set --subscription $SubscriptionId
    }

    return $azureContext
}

Connect-AzureSubscription | Out-Null

# Load all secrets from KeyVault and set the appropriate Environment Variable
$ids = az keyvault secret list --subscription $SubscriptionId --vault-name $KeyVaultName --query '[*].id' --output tsv
ForEach ($id in $ids)
{
    # az keyvault secret show does not return a name in its properties so we need to extract it from the id
    # The ids have parts seperated by / and extracting the 5th part gets us the name of the Key
    # After we extract the name of the Key, we also want to replace dashes with underscores so that we convert it to the correct Environment Vairable name to be set
    # Ex: https://test-kv.vault.azure.net/secrets/IOTHUB-NAME/f7953b248a3e46cfa6fc59651794db2e - Extract IOTHUB-NAME and convert to IOTHUB_NAME
    $name = $id.split('/')[4].Replace('-', '_')
    Write-Host("Setting Environment Variable $name")
    $value = az keyvault secret show --subscription $SubscriptionId --id $id --query 'value' --output tsv
    Set-Item -Path Env:$name -Value "$value"
}
