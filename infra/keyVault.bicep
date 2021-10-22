param logAnalyticsWorkspaceName string
param keyVaultName string
param subscriptionKeyName string
param backendForFrontEndFunctionAppName string
param backendApiFunctionAppName string
@secure()
param subscriptionKeyValue string
param backendForFrontEndClientSecretName string
@secure()
param backendForFrontEndClientSecretValue string

resource backendForFrontEndFunctionApp 'Microsoft.Web/sites@2021-02-01' existing = {
  name: backendForFrontEndFunctionAppName
}

resource backendApiFunctionApp 'Microsoft.Web/sites@2021-02-01' existing = {
  name: backendApiFunctionAppName
}

resource keyVault 'Microsoft.KeyVault/vaults@2021-06-01-preview' = {
  name: keyVaultName
  location: resourceGroup().location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: false
    accessPolicies: [
      {
        tenantId: subscription().tenantId
        objectId: backendForFrontEndFunctionApp.identity.principalId
        permissions: {
          secrets: [
            'get'
            'set'
          ]
        }
      }
       {
        tenantId: subscription().tenantId
        objectId: backendApiFunctionApp.identity.principalId
        permissions: {
          secrets: [
            'get'
            'set'
          ]
        }
      } 
    ]
  }  
}

resource keyVaultSubscriptionKeySecret 'Microsoft.KeyVault/vaults/secrets@2021-04-01-preview' = {
  name: '${keyVault.name}/${subscriptionKeyName}'
  properties: {
    value: subscriptionKeyValue
  }  
}

resource keyVaultBackendForFrontEndClientSecretSecret 'Microsoft.KeyVault/vaults/secrets@2021-04-01-preview' = {
  name: '${keyVault.name}/${backendForFrontEndClientSecretName}'
  properties: {
    value: backendForFrontEndClientSecretValue
  }  
}

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2021-06-01' existing = {
  name: logAnalyticsWorkspaceName
}

resource diagnosticSettings 'Microsoft.Insights/diagnosticsettings@2017-05-01-preview' = {
  name: 'Logging'
  scope: keyVault
  properties: {
    workspaceId: logAnalytics.id
    logs: [
      {
        category: 'AuditEvent'
        enabled: true
      }
      {
        category: 'AzurePolicyEvaluationDetails'
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

output keyVaultName string = keyVault.name
