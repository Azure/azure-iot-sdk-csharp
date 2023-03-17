@description('The name of application insights.')
param ApplicationInsightsName string = '${resourceGroup().name}-ai'

@minLength(3)
@maxLength(24)
@description('The name of the storage account used by the IoT hub.')
param StorageAccountName string

@description('The name of the main IoT hub used by tests.')
param HubName string = '${resourceGroup().name}-hub'

@description('The number of IoT hub units to be deployed.')
param HubUnitsCount int = 1

@description('The IoT hub consumer group name.')
param ConsumerGroupName string = 'longhaul'

@description('The name of BlobService inside the StorageAccount.')
param BlobServiceName string = 'default'

@description('The name of the Container inside the BlobService.')
param ContainerName string = 'fileupload'

var hubKeysId = resourceId('Microsoft.Devices/IotHubs/Iothubkeys', HubName, 'iothubowner')

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: ApplicationInsightsName
  kind: 'web'
  location: 'westus2'
  properties: {
    Application_Type: 'web'
  }
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2021-06-01' = {
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

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2021-06-01' = {
  name: '${storageAccount.name}/${BlobServiceName}'
  properties: {
    deleteRetentionPolicy: {
      enabled: false
    }
  }
}

resource container 'Microsoft.Storage/storageAccounts/blobServices/containers@2021-06-01' = {
  name: '${blobService.name}/${ContainerName}'
  properties: {
    publicAccess: 'None'
  }
}

resource iotHub 'Microsoft.Devices/IotHubs@2021-07-01' = {
  name: HubName
  location: resourceGroup().location
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
        connectionString: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${listkeys(storageAccount.id, '2022-09-01').keys[0].value}'
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

resource consumerGroups 'Microsoft.Devices/IotHubs/eventHubEndpoints/ConsumerGroups@2021-07-01' = {
  name: '${iotHub.name}/events/${ConsumerGroupName}'
}

output hubName string = HubName
output hubConnectionString string = 'HostName=${HubName}.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=${listkeys(hubKeysId, '2021-07-01').primaryKey}'
output storageAccountConnectionString string = 'DefaultEndpointsProtocol=https;AccountName=${StorageAccountName};AccountKey=${listkeys(storageAccount.id, '2021-06-01').keys[0].value};EndpointSuffix=core.windows.net'
output instrumentationKey string = reference(applicationInsights.id, '2022-06-15').InstrumentationKey