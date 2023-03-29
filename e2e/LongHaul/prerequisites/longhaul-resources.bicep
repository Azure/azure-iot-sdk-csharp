@description('The name of application insights.')
param ApplicationInsightsName string = '${resourceGroup().name}-ai'

@description('The location for the Application insights instance.')
param AiLocation string = 'centralus'

@description('Signed in user objectId')
param UserObjectId string

@minLength(3)
@maxLength(24)
@description('The name of the storage account used by the IoT hub.')
param StorageAccountName string

@minLength(3)
@maxLength(24)
@description('The name of the key vault for storing secrets needed for running tests.')
param KeyVaultName string = '${resourceGroup().name}-kv'

@description('The name of the main IoT hub used by tests.')
param HubName string = '${resourceGroup().name}-hub'

@description('The location of the IoT hub.')
param HubLocation string = resourceGroup().location

@description('The number of IoT hub units to be deployed.')
param HubUnitsCount int = 1

@description('The name of BlobService inside the StorageAccount.')
param BlobServiceName string = 'default'

@description('The name of the Container inside the BlobService.')
param ContainerName string = 'fileupload'

var hubKeysId = resourceId('Microsoft.Devices/IotHubs/Iothubkeys', HubName, 'iothubowner')

resource oiWorkspace 'Microsoft.OperationalInsights/workspaces@2020-08-01' = {
  name: ApplicationInsightsName
  location: AiLocation
}

resource applicationInsights 'microsoft.insights/components@2020-02-02-preview' = {
  dependsOn: [
    oiWorkspace
  ]
  name: ApplicationInsightsName
  location: AiLocation
  kind: 'web'
  properties: {
    Application_Type: 'web'
  }
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2021-06-01' = {
  name: StorageAccountName
  location: HubLocation
  sku: {
    name: 'Standard_LRS'
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
            'get', 'list', 'set', 'delete'
          ]
          certificates: [
            'get', 'list', 'create', 'delete'
          ]
          keys: [
            'get', 'list', 'create', 'delete'
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

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2021-06-01' = {
  parent: storageAccount
  name: BlobServiceName
  properties: {
    deleteRetentionPolicy: {
      enabled: false
    }
  }
}

resource container 'Microsoft.Storage/storageAccounts/blobServices/containers@2021-06-01' = {
  parent: blobService
  name: ContainerName
  properties: {
    publicAccess: 'None'
  }
}

resource iotHub 'Microsoft.Devices/IotHubs@2021-07-01' = {
  name: HubName
  location: HubLocation
  properties: {
    eventHubEndpoints: {
      events: {
        retentionTimeInDays: 1
        partitionCount: 4
      }
    }
    rootCertfificate: {
      enableRootCertificateV2: true
    }
    routing: {
        routes: [
          {
            name: 'DeviceConnections'
            source: 'DeviceConnectionStateEvents'
            condition: 'true'
            endpointNames: [
                'events'
            ]
            isEnabled: true
          }
          {
            name: 'DeviceTwinChangeEvents'
            source: 'TwinChangeEvents'
            condition: 'true'
            endpointNames: [
                'events'
            ]
            isEnabled: true
          }
      ]
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
    storageEndpoints: {
      '$default': {
        sasTtlAsIso8601: 'PT1H'
        connectionString: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${listkeys(storageAccount.id, '2022-09-01').keys[0].value}'
        containerName: ContainerName
      }
    }
    enableFileUploadNotifications: true
  }
  sku: {
    name: 'S1'
    capacity: HubUnitsCount
  }
  dependsOn: [
    container
  ]
}

output hubName string = HubName
output hubConnectionString string = 'HostName=${HubName}.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=${listkeys(hubKeysId, '2021-07-01').primaryKey}'
output storageAccountConnectionString string = 'DefaultEndpointsProtocol=https;AccountName=${StorageAccountName};AccountKey=${listkeys(storageAccount.id, '2021-06-01').keys[0].value};EndpointSuffix=core.windows.net'
output instrumentationKey string = reference(applicationInsights.id, '2020-02-02-preview').InstrumentationKey
output keyVaultName string = KeyVaultName
