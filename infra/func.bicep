param appInsightsName string
param logAnalyticsWorkspaceName string
param longName string
param storageAccountName string
param keyVaultName string
param subscriptionKeySecretName string
param backendForFrontEndApiFunctionAppName string
param backendApiFunctionAppName string
param backendForFrontEndClientId string
@secure()
param backendForFrontEndSecretName string

resource appServicePlan 'Microsoft.Web/serverfarms@2021-01-15' = {
  name: 'asp-${longName}'
  location: resourceGroup().location
  kind: 'functionapp'
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  properties: {
    reserved: true
  }
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2021-04-01' existing = {
  name: storageAccountName
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: appInsightsName
}

var backendForFrontEndClientSecretConfigurationName = 'BackendForFrontEnd__ClientSecret'

resource backendForFrontEndFunction 'Microsoft.Web/sites@2021-01-15' = {
  name: backendForFrontEndApiFunctionAppName
  location: resourceGroup().location
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    reserved: true
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${listKeys(resourceId('Microsoft.Storage/storageAccounts', storageAccount.name), '2019-06-01').keys[0].value}'
        }
        {
          name: 'AZURE_STORAGE_CONNECTION_STRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${listKeys(resourceId('Microsoft.Storage/storageAccounts', storageAccount.name), '2019-06-01').keys[0].value}'
        }
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: appInsights.properties.InstrumentationKey
        }
        {
          name: 'BackendAPI__SubscriptionKey'
          value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=${subscriptionKeySecretName})'
        }
        {
          name: backendForFrontEndClientSecretConfigurationName
          value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=${backendForFrontEndSecretName})'
        }
        {
          name: 'BackendAPI__Uri'
          value: 'https://${backendApiFunction.name}.azurewebsites.net/api/getbackenddata'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~3'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet'
        }
      ]
    }
  }
}

resource backendForFrontendCors 'Microsoft.Web/sites/config@2021-02-01' = {
  name: '${backendForFrontEndFunction.name}/web'
  properties: {
    cors: {
      allowedOrigins: [
        '*'
      ]
    }
  }
}

resource backendForFrontendAuthentication 'Microsoft.Web/sites/config@2021-02-01' = {
  name: '${backendForFrontEndFunction.name}/authsettingsV2'
  properties: {
    globalValidation: {
      requireAuthentication: true
      unauthenticatedClientAction: 'RedirectToLoginPage'
    }
    identityProviders: {
      azureActiveDirectory: {
        registration: {
          clientId: backendForFrontEndClientId
          clientSecretSettingName: backendForFrontEndClientSecretConfigurationName
          openIdIssuer: 'https://login.microsoftonline.com/${subscription().tenantId}'
        }
        validation: {
          allowedAudiences: [
            'api://${backendForFrontEndClientId}'
          ]
        }
      }
      login: {
        tokenStore: {
          enable: true
        }
      }
    }
  }
}

resource backendApiFunction 'Microsoft.Web/sites@2021-01-15' = {
  name: backendApiFunctionAppName
  location: resourceGroup().location
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    reserved: true
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${listKeys(resourceId('Microsoft.Storage/storageAccounts', storageAccount.name), '2019-06-01').keys[0].value}'
        }
        {
          name: 'AZURE_STORAGE_CONNECTION_STRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${listKeys(resourceId('Microsoft.Storage/storageAccounts', storageAccount.name), '2019-06-01').keys[0].value}'
        }
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: appInsights.properties.InstrumentationKey
        }
        {
          name: 'BackendAPI__SubscriptionKey'
          value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=${subscriptionKeySecretName})'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~3'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet'
        }
      ]
    }
  }
}

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2021-06-01' existing = {
  name: logAnalyticsWorkspaceName
}

resource functionAppDiagnosticSettings 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'Logging'
  scope: backendForFrontEndFunction
  properties: {
    workspaceId: logAnalyticsWorkspace.id
    logs: [
      {
        category: 'FunctionAppLogs'
        enabled: true
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
      }
    ]
  }
}

output backendForFrontEndFunctionAppName string = backendForFrontEndFunction.name
output backendApiFunctionAppName string = backendApiFunction.name
