# func-reset-key

## Request access token

```shell
POST https://login.microsoftonline.com/microsoft.onmicrosoft.com/oauth2/v2.0/token
```

## Regenerate primary key

```shell
POST https://management.azure.com/subscriptions/dcf66641-6312-4ee1-b296-723bb0a999ba/resourceGroups/rg-apim-ussc-demo/providers/Microsoft.ApiManagement/service/apim-dev-ussc-demo/subscriptions/6169bbb8a952b1005f070001/regeneratePrimaryKey?api-version=2020-12-01
```

## Set primary key

```shell
PATCH https://management.azure.com/subscriptions/dcf66641-6312-4ee1-b296-723bb0a999ba/resourceGroups/rg-apim-ussc-demo/providers/Microsoft.ApiManagement/service/apim-dev-ussc-demo/subscriptions/6169bbb8a952b1005f070001?api-version=2020-12-01

{
    "properties": {
        "primaryKey": "asdf",
        "secondaryKey": "fdas"
    }
}
```