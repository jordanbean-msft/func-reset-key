param appName string
param environment string
param location string
param subscriptionKeySecretName string
@secure()
param subscriptionKeySecretValue string
param backendForFrontEndClientId string
@secure()
param backendForFrontEndClientSecret string

var longName = '${appName}-${location}-${environment}'
var backendApiFunctionAppName = 'func-backendApi-${longName}'
var backendForFrontEndFunctionAppName = 'func-backendForFrontEnd-${longName}'
var keyVaultName = 'kv-${longName}'
var backendForFrontEndClientSecretName = 'BackendForFrontEndClientSecret'

module loggingDeployment 'logging.bicep' = {
  name: 'loggingDeployment'
  params: {
    longName: longName
    backendApiFunctionAppName: backendApiFunctionAppName
    backendForFrontEndFunctionAppName: backendForFrontEndFunctionAppName
  }
}

module storageDeployment 'storage.bicep' = {
  name: 'storageDeployment'
  params: {
    longName: longName
    logAnalyticsWorkspaceName: loggingDeployment.outputs.logAnalyticsWorkspaceName
  }
}

module functionAppDeployment 'func.bicep' = {
  name: 'functionAppDeployment'
  params: {
    appInsightsName: loggingDeployment.outputs.appInsightsName
    backendApiFunctionAppName: backendApiFunctionAppName
    backendForFrontEndApiFunctionAppName: backendForFrontEndFunctionAppName
    keyVaultName: keyVaultName
    logAnalyticsWorkspaceName: loggingDeployment.outputs.logAnalyticsWorkspaceName
    longName: longName
    storageAccountName: storageDeployment.outputs.storageAccountName
    subscriptionKeySecretName: subscriptionKeySecretName
    backendForFrontEndClientId: backendForFrontEndClientId
    backendForFrontEndSecretName: backendForFrontEndClientSecretName
  }
}

module keyVaultDeployment 'keyVault.bicep' = {
  name: 'keyVaultDeployment'
  dependsOn: [
    functionAppDeployment
  ]
  params: {
    backendApiFunctionAppName: backendApiFunctionAppName
    backendForFrontEndFunctionAppName: backendForFrontEndFunctionAppName
    keyVaultName: keyVaultName
    logAnalyticsWorkspaceName: loggingDeployment.outputs.logAnalyticsWorkspaceName
    subscriptionKeyName: subscriptionKeySecretName
    subscriptionKeyValue: subscriptionKeySecretValue
    backendForFrontEndClientSecretName: backendForFrontEndClientSecretName
    backendForFrontEndClientSecretValue: backendForFrontEndClientSecret
  }
}

output storageAccountName string = storageDeployment.outputs.storageAccountName
output logAnalyticsWorkspaceName string = loggingDeployment.outputs.logAnalyticsWorkspaceName
output backendApiFunctionAppName string = backendApiFunctionAppName
output backendForFrontEndFunctionAppName string = backendForFrontEndFunctionAppName
output appInsightsName string = loggingDeployment.outputs.appInsightsName
output keyVaultName string = keyVaultDeployment.outputs.keyVaultName
