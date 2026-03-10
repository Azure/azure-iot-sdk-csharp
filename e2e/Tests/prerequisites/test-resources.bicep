@minLength(3)
@maxLength(24)
@description('The name of the storage account used by the IoT hub.')
param StorageAccountName string

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

@description('The name of the operational insights instance.')
param OperationalInsightsName string = '${resourceGroup().name}-oi'

@description('The location for Microsoft.OperationalInsights/workspaces.')
param OperationInsightsLocation string = 'westus2'

@description('The name of BlobService inside the StorageAccount.')
param BlobServiceName string = 'default'

@description('The name of the Container inside the BlobService.')
param ContainerName string = 'fileupload'

var hubKeysId = resourceId('Microsoft.Devices/IotHubs/Iothubkeys', HubName, 'iothubowner')
var dpsKeysId = resourceId('Microsoft.Devices/ProvisioningServices/keys', DpsName, 'provisioningserviceowner')


resource storageAccount 'Microsoft.Storage/storageAccounts@2021-02-01' = {
  name: StorageAccountName
  location: resourceGroup().location
  sku: {
    name: 'Standard_LRS'
    tier: 'Standard'
  }
  kind: 'StorageV2'
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

resource iotHub 'Microsoft.Devices/IotHubs@2021-03-03-preview' = {
  name: HubName
  location: resourceGroup().location
  identity: {
    type: 'SystemAssigned'
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
    StorageEndpoints: {
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

output hubName string = HubName
output hubConnectionString string = 'HostName=${HubName}.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=${listkeys(hubKeysId, '2019-11-04').primaryKey}'
output dpsName string = DpsName
output dpsConnectionString string = 'HostName=${DpsName}.azure-devices-provisioning.net;SharedAccessKeyName=provisioningserviceowner;SharedAccessKey=${listkeys(dpsKeysId, '2017-11-15').primaryKey}'
output storageAccountConnectionString string = 'DefaultEndpointsProtocol=https;AccountName=${StorageAccountName};AccountKey=${listkeys(storageAccount.id, '2019-06-01').keys[0].value};EndpointSuffix=core.windows.net'