# PasswordManager-Backend
This is the backend repository of Password Manager

The backend is hosted on Azure and the architecture is:
![Backend Architecture](https://user-images.githubusercontent.com/29853549/129343570-f83116ef-12cc-4103-abaa-36625c63aa50.png)

The backend contains following resources:
1. KeyVault (PasswordManagerVault) - This stores all the secrets for the password manager, and also the encyrption key. This has network restrictions in place which only allows for vNet addresses to access the Keyvault. Along with this, it has Access Policies in place to ensure only the function app is able to access the keyvault secrets and keys.
2. Function App (passwordmanagerfn) - This hosts all the APIs. This is also network restricted within the subnets of the Virtual Network, so it is inaccessible from any other IP. Along with this, the function app is also protected by Azure AD, so for one to access the APIs they must have the function code + valid access token.
3. App Service Plan (passwordmanagerfn) - This is the app service plan on which the function app is hosted.
4. Application Insights (passwordmanagerfn) - This is where all the function app logs are recorded and can be used for analysis and request tracing.
5. Storage (passwordmanagerfn) - This is utilized by the azure function app.
6. Virtual Network (PasswordManagerVNet) - This helps to secure resources, ensuring they are only accessible within the VNet, and not from amy other IP.
7. API Management (PasswordManagerVNet) - This is used to expose the APIs to external applications, while ensuring security. For someone to access these APIs, along with the APIM subscription key, they will also need to pass the Azure AD access token.

## Security Overview
1. API can only be accessed via APIM
2. To access API, one needs APIM subscription key + Azure AD account
3. Function App API are inaccessible to external applications because of the VNet
4. Keyvault is inaccessible to anyone except function app because of the VNet + Access Policy
5. The secrets sent over the API are encrypted and can be decrytped only by the end application, thus there is no risk of security credentials leak over API calls.
