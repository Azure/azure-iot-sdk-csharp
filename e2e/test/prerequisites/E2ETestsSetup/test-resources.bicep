@description('The name of application insights.')
param ApplicationInsightsName string = '${resourceGroup().name}-ai'

@minLength(3)
@maxLength(24)
@description('The name of the storage account used by the IoT hub.')
param StorageAccountName string

@description('Signed in user objectId')
param UserObjectId string

@description('The region for the website hosting the Azure function.')
param WebRegion string = 'CentralUS'

@description('The name of the main IoT hub used by tests.')
param HubName string = '${resourceGroup().name}-hub'

@description('The number of IoT hub units to be deployed.')
param HubUnitsCount int = 1

@description('The IoT hub consumer group name.')
param ConsumerGroupName string = 'e2e-tests'

@description('The name of DPS used by tests.')
param DpsName string ='${resourceGroup().name}-dps'

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

@description('The name of BlobService inside the StorageAccount.')
param BlobServiceName string = 'default'

@description('The name of the Container inside the BlobService.')
param ContainerName string = 'fileupload'

@description('The name of the user assigned managed identity.')
param UserAssignedManagedIdentityName string

@description('Flag to indicate if IoT hub should have security solution enabled.')
param EnableIotHubSecuritySolution bool = false

var hubKeysId = resourceId('Microsoft.Devices/IotHubs/Iothubkeys', HubName, 'iothubowner')
var dpsKeysId = resourceId('Microsoft.Devices/ProvisioningServices/keys', DpsName, 'provisioningserviceowner')

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

output hubName string = HubName
output hubConnectionString string = 'HostName=${HubName}.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=${listkeys(hubKeysId, '2019-11-04').primaryKey}'
output dpsName string = DpsName
output dpsConnectionString string = 'HostName=${DpsName}.azure-devices-provisioning.net;SharedAccessKeyName=provisioningserviceowner;SharedAccessKey=${listkeys(dpsKeysId, '2017-11-15').primaryKey}'
output storageAccountConnectionString string = 'DefaultEndpointsProtocol=https;AccountName=${StorageAccountName};AccountKey=${listkeys(storageAccount.id, '2019-06-01').keys[0].value};EndpointSuffix=core.windows.net'
output workspaceId string = (EnableIotHubSecuritySolution) ? '${reference(operationalInsightsWorkspaces.id, '2017-03-15-preview').customerId}' : ''
output keyVaultName string = KeyVaultName
output instrumentationKey string = reference(applicationInsights.id, '2015-05-01').InstrumentationKey
