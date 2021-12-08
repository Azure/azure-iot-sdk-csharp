@description('The name of application insights.')
param ApplicationInsightsName string = '${resourceGroup().name}-ai'

@minLength(3)
@maxLength(24)
@description('The name of the storage account used by the IoT hub.')
param StorageAccountName string

@description('Signed in user objectId')
param UserObjectId string

param DpsCustomAllocatorRunCsxContent string

param DpsCustomAllocatorProjContent string

@description('The region for the second IoT hub in a DPS that is far away from the test devices.')
param FarRegion string ='southeastasia'

@description('The region for the website hosting the Azure function.')
param WebRegion string = 'CentralUS'

@description('The name of the main IoT hub used by tests.')
param HubName string = '${resourceGroup().name}-hub'

@description('The number of IoT hub units to be deployed.')
param HubUnitsCount int = 1

@description('The IoT hub consumer group name.')
param ConsumerGroupName string = 'e2e-tests'

@description('The name of the far away IoT hub used by tests.')
param FarHubName string = '${resourceGroup().name}-hubfar'

@description('The name of DPS used by tests.')
param DpsName string ='${resourceGroup().name}-dps'

param DpsCustomAllocatorFunctionName string = 'DpsCustomAllocator'

@minLength(3)
@maxLength(24)
@description('The name of the key vault for storing secrets needed for running tests.')
param KeyVaultName string = '${resourceGroup().name}-kv'

@description('The name of the operational insights instance.')
param OperationalInsightsName string = '${resourceGroup().name}-oi'

@description('The location for Microsoft.OperationalInsights/workspaces.')
param OperationInsightsLocation string = 'westus2'

@description('The name of the security solution instance.')
param SecuritySolutionName string = '${resourceGroup().name}-ss'

@description('The name of the server farm to host a function app for DPS custom allocation.')
param ServerFarmName string = '${resourceGroup().name}-srv'

@description('The name of the server farm to host a function app for DPS custom allocation')
param WebsiteName string = '${resourceGroup().name}-web'

@description('The name of BlobService inside the StorageAccount.')
param BlobServiceName string = 'default'

@description('The name of the Container inside the BlobService.')
param ContainerName string = 'fileupload'

@description('The name of the user assigned managed identity.')
param UserAssignedManagedIdentityName string

@description('Flag to indicate if IoT hub should have security solution enabled.')
param EnableIotHubSecuritySolution bool = false

var hubKeysId = resourceId('Microsoft.Devices/IotHubs/Iothubkeys', HubName, 'iothubowner')
var farHubKeysId =  resourceId('Microsoft.Devices/IotHubs/Iothubkeys', FarHubName, 'iothubowner')
var dpsKeysId = resourceId('Microsoft.Devices/ProvisioningServices/keys', DpsName, 'provisioningserviceowner')
var functionKeysId = resourceId('Microsoft.Web/sites/functions', WebsiteName, DpsCustomAllocatorFunctionName)

resource applicationInsights 'Microsoft.Insights/components@2015-05-01' = {
  name: ApplicationInsightsName
  kind: 'web'
  location: 'WestUs'
  properties: {
    Application_Type: 'web'
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2018-02-14' = {
  name: KeyVaultName
  location: resourceGroup().location
  properties: {
    enabledForDeployment: false
    enabledForTemplateDeployment: false
    enabledForDiskEncryption: false
    accessPolicies: [
      {
        objectId: UserObjectId
        tenantId: subscription().tenantId
        permissions: {
          secrets: [
            'all'
          ]
          certificates: [
            'all'
          ]
          keys: [
            'all'
          ]
        }
      }
    ]
    tenantId: subscription().tenantId
    sku: {
      name: 'standard'
      family: 'A'
    }
    enableSoftDelete: true
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
      ipRules: [
      ]
      virtualNetworkRules: [
      ]
    }
  }
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2021-02-01' = {
  name: StorageAccountName
  location: resourceGroup().location
  sku: {
    name: 'Standard_LRS'
    tier: 'Standard'
  }
  kind: 'Storage'
  properties: {
    supportsHttpsTrafficOnly: true
    encryption: {
      services: {
        file: {
          keyType: 'Account'
          enabled: true
        }
        blob: {
          keyType: 'Account'
          enabled: true
        }
      }
      keySource: 'Microsoft.Storage'
    }
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2021-02-01' = {
  name: '${storageAccount.name}/${BlobServiceName}'
  properties: {
    deleteRetentionPolicy: {
      enabled: false
    }
  }
}

resource container 'Microsoft.Storage/storageAccounts/blobServices/containers@2021-02-01' = {
  name: '${blobService.name}/${ContainerName}'
  properties: {
    publicAccess: 'None'
  }
}

resource userAssignedManagedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2018-11-30' = {
  name: UserAssignedManagedIdentityName
  location: resourceGroup().location
}

resource iotHub 'Microsoft.Devices/IotHubs@2021-03-03-preview' = {
  name: HubName
  location: resourceGroup().location
  identity: {
    type: 'SystemAssigned, UserAssigned'
    userAssignedIdentities: {
      '${userAssignedManagedIdentity.id}' : {}
    }
  }
  properties: {
    eventHubEndpoints: {
      events: {
        retentionTimeInDays: 1
        partitionCount: 4
      }
    }
    cloudToDevice: {
      defaultTtlAsIso8601: 'PT1H'
      maxDeliveryCount: 100
      feedback: {
        ttlAsIso8601: 'PT1H'
        lockDurationAsIso8601: 'PT5S'
        maxDeliveryCount: 100
      }
    }
    messagingEndpoints: {
      fileNotifications: {
        ttlAsIso8601: 'PT1H'
        lockDurationAsIso8601: 'PT5S'
        maxDeliveryCount: 100
      }
    }
    StorageEndPoints: {
      '$default': {
        sasTtlAsIso8601: 'PT1H'
        connectionString: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${listkeys(storageAccount.id, '2019-06-01').keys[0].value}'
        containerName: ContainerName
      }
    }
    enableFileUploadNotifications: true
  }
  sku: {
    name: 'S1'
    tier: 'Standard'
    capacity: HubUnitsCount
  }
  dependsOn: [
    container
  ]
}

resource consumerGroups 'Microsoft.Devices/IotHubs/eventHubEndpoints/ConsumerGroups@2018-04-01' = {
  name: '${iotHub.name}/events/${ConsumerGroupName}'
}

resource farIotHub 'Microsoft.Devices/IotHubs@2020-01-01' = {
  name: FarHubName
  location: FarRegion
  sku: {
    name: 'S1'
    capacity: 1
  }
  properties: {
  }
}

resource provisioningService 'Microsoft.Devices/provisioningServices@2017-11-15' = {
  name: DpsName
  location: resourceGroup().location
  sku: {
    name: 'S1'
    capacity: 1
  }
  properties: {
    iotHubs: [
      {
        location: resourceGroup().location
        connectionString: 'HostName=${iotHub.name}.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=${listkeys(hubKeysId, '2020-01-01').primaryKey}'
      }
      {
        location: FarRegion
        connectionString: 'HostName=${farIotHub.name}.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=${listkeys(farHubKeysId, '2020-01-01').primaryKey}'
      }
    ]
  }
}

resource operationalInsightsWorkspaces 'Microsoft.OperationalInsights/workspaces@2017-03-15-preview' = if (EnableIotHubSecuritySolution) {
  name: OperationalInsightsName
  location: OperationInsightsLocation
  properties: {
  }
}

resource iotSecuritySolution 'Microsoft.Security/IoTSecuritySolutions@2019-08-01' = if (EnableIotHubSecuritySolution) {
  name: SecuritySolutionName
  location: resourceGroup().location
  properties: {
    workspace: operationalInsightsWorkspaces.id
    status: 'Enabled'
    export: [
      'RawEvents'
    ]
    disabledDataSources: [
    ]
    displayName: SecuritySolutionName
    iotHubs: [
      iotHub.id
    ]
    recommendationsConfiguration: [
    ]
    unmaskedIpLoggingStatus: 'Enabled'
  }
}

resource serverfarm 'Microsoft.Web/serverfarms@2018-11-01' = {
  name: ServerFarmName
  location: WebRegion
  kind: ''
  properties: {
    name: ServerFarmName
  }
  sku: {
    Tier: 'Dynamic'
    Name: 'Y1'
  }
}

resource website 'Microsoft.Web/sites@2018-11-01' = {
  name: WebsiteName
  location: WebRegion
  kind: 'functionapp'
  properties: {
    name: WebsiteName
    siteConfig: {
      appSettings: [
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '3.0.14916'
        }
        {
          name: 'FUNCTIONS_V2_COMPATIBILITY_MODE'
          value: 'true'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet'
        }
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${listkeys(storageAccount.id, '2019-06-01').keys[0].value};EndpointSuffix=core.windows.net'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${listkeys(storageAccount.id, '2019-06-01').keys[0].value};EndpointSuffix=core.windows.net'
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: toLower(DpsCustomAllocatorFunctionName)
        }
      ]
    }
    serverFarmId: serverfarm.id
  }
}

resource functions 'Microsoft.Web/sites/functions@2018-11-01' = {
  name: '${website.name}/${DpsCustomAllocatorFunctionName}'
  properties: {
    config: {
      bindings: [
        {
          name: 'req'
          type: 'httpTrigger'
          direction: 'in'
          schedule: DpsCustomAllocatorFunctionName
        }
        {
          name: '$return'
          type: 'http'
          direction: 'out'
        }
      ]
      disabled: false
    }
    files: {
      'run.csx': base64ToString((DpsCustomAllocatorRunCsxContent))
      'function.proj': base64ToString(DpsCustomAllocatorProjContent)
    }
  }
}

output hubName string = HubName
output hubConnectionString string = 'HostName=${HubName}.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=${listkeys(hubKeysId, '2019-11-04').primaryKey}'
output farHubHostName string = reference(farIotHub.id).hostName
output farHubConnectionString string = 'HostName=${FarHubName}.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=${listkeys(farHubKeysId, '2019-11-04').primaryKey}'
output dpsName string = DpsName
output dpsConnectionString string = 'HostName=${DpsName}.azure-devices-provisioning.net;SharedAccessKeyName=provisioningserviceowner;SharedAccessKey=${listkeys(dpsKeysId, '2017-11-15').primaryKey}'
output storageAccountConnectionString string = 'DefaultEndpointsProtocol=https;AccountName=${StorageAccountName};AccountKey=${listkeys(storageAccount.id, '2019-06-01').keys[0].value};EndpointSuffix=core.windows.net'
output workspaceId string = (EnableIotHubSecuritySolution) ? '${reference(operationalInsightsWorkspaces.id, '2017-03-15-preview').customerId}' : ''
output customAllocationPolicyWebhook string = 'https://${WebsiteName}.azurewebsites.net/api/${DpsCustomAllocatorFunctionName}?code=${listkeys(functionKeysId, '2019-08-01').default}'
output keyVaultName string = KeyVaultName
output instrumentationKey string = reference(applicationInsights.id, '2015-05-01').InstrumentationKey
