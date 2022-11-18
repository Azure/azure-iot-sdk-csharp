param ApplicationInsightsName string {
  default: '${resourceGroup().name}-ai'
  metadata: {
      description: 'The name of application insights.'      
  }
}

param StorageAccountName string {
  minLength: 3
  maxLength: 24
  metadata: {
    description: 'The name of the storage account used by the IoT hub.'
  }
}

param UserObjectId string {
  metadata: {
    description: 'Signed in user objectId'
  }
}

param HubUnitsCount int {
  default: 1
  metadata: {
    description: 'The number of IoT hub units to be deployed.'
  }
}

param OperationInsightsLocation string {
  default: 'westus2',
  metadata: {
    description: 'The location for Microsoft.OperationalInsights/workspaces.'
  }
}

param DpsCustomAllocatorRunCsxContent string

param DpsCustomAllocatorProjContent string

param FarRegion string {
  default: 'southeastasia'
  metadata: {
    description: 'The region for the second IoT hub in a DPS that is far away from the test devices.'
  }
}

param WebRegion string {
  default: 'CentralUS'
  metadata: {
    description: 'The region for the website hosting the Azure function.'
  }
}

param HubName string {
  default: '${resourceGroup().name}-hub'
  metadata: {
    description: 'The name of the main IoT hub used by tests.'
  }
}

param ConsumerGroupName string {
  default: 'e2e-tests'
  metadata: {
    description: 'The IotHub consumer group name.'
  }
}

param UserAssignedManagedIdentityName string {
  default: '${resourceGroup().name}-user-msi'
  metadata: {
    description: 'The name of the user assigned managed identity.'
  }
}

param EnableIotHubSecuritySolution bool {
  default: false
  metadata: {
    description: 'Flag to indicate if IoT hub should have security solution enabled.'
  }
}

param FarHubName string {
  default: '${resourceGroup().name}-hubfar'
  metadata: {
    description: 'The name of the far away IoT hub used by tests.'
  }
}

param DpsName string {
  default: '${resourceGroup().name}-dps'
  metadata: {
    description: 'The name of DPS used by tests.'
  }
}

param DpsCustomAllocatorFunctionName string {
  default: 'DpsCustomAllocator'
}

param KeyVaultName string {
  default: '${resourceGroup().name}-kv'
  minLength: 3
  maxLength: 24
  metadata: {
    description: 'The name of the key vault for storing secrets needed for running tests.'
  }
}

param OperationalInsightsName string {
  default: '${resourceGroup().name}-oi'
  metadata: {
    description: 'The name of the operational insights instance.'
  }
}

param SecuritySolutionName string {
  default: '${resourceGroup().name}-ss'
  metadata: {
    description: 'The name of the security solution instance.'
  }
}

param ServerFarmName string {
  default: '${resourceGroup().name}-srv'
  metadata: {
    description: 'The name of the server farm to host a function app for DPS custom allocation.'
  }
}

param WebsiteName string {
  default: '${resourceGroup().name}-web'
  metadata: {
    description: 'The name of the server farm to host a function app for DPS custom allocation'
  }
}

param BlobServiceName string {
  default: 'default'
  metadata: {
    description: 'The name of BlobService inside the StorageAccount.'
  }
}

param ContainerName string {
  default: 'fileupload'
  metadata: {
    description: 'The name of the Container inside the BlobService.'
  }
}

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

resource storageAccount 'Microsoft.Storage/storageAccounts@2019-06-01' = {
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

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2019-06-01' = {
  name: '${storageAccount.name}/${BlobServiceName}'
  properties: {
    deleteRetentionPolicy: {
      enabled: false
    }
  }
}

resource container 'Microsoft.Storage/storageAccounts/blobServices/containers@2019-06-01' = {
  name: '${blobService.name}/${ContainerName}'
  properties: {
    publicAccess: 'None'
  }
}

resource iotHub 'Microsoft.Devices/IotHubs@2020-01-01' = {
  name: HubName
  location: resourceGroup().location
  identity: {
    principalId: ''
    tenantId: ''
    type: 'SystemAssigned'
  }
  properties: {
    eventHubEndpoints: {
      events: {
        retentionTimeInDays: 1
        partitionCount: 10
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
    capacity: 1
  }
  dependsOn: [
    container
  ]
}

resource consumerGroups 'Microsoft.Devices/IotHubs/eventHubEndpoints/ConsumerGroups@2018-04-01' = {
  name: '${iotHub.name}/events/${ConsumerGroupName}'
  properties: {
  }
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

resource operationalInsightsWorkspaces 'Microsoft.OperationalInsights/workspaces@2017-03-15-preview' = {
  name: OperationalInsightsName
  location: resourceGroup().location
  properties: {
  }
}

resource iotSecuritySolution 'Microsoft.Security/IoTSecuritySolutions@2019-08-01' = {
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
output storageAccountConnectionString string = 'DefaultEndpointsProtocol=https;AccountName=${StorageAccountName}AccountKey=${listkeys(storageAccount.id, '2019-06-01').keys[0].value};EndpointSuffix=core.windows.net'
output workspaceId string = (EnableIotHubSecuritySolution) ? '${reference(operationalInsightsWorkspaces.id, '2017-03-15-preview').customerId}' : ''
output customAllocationPolicyWebhook string = 'https://${WebsiteName}.azurewebsites.net/api/${DpsCustomAllocatorFunctionName}?code=${listkeys(functionKeysId, '2019-08-01').default}'
output keyVaultName string = KeyVaultName
output instrumentationKey string = reference(applicationInsights.id, '2015-05-01').InstrumentationKey
