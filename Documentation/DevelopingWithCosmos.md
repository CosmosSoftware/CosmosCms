# Developing with Cosmos
The Cosmos Content Management Platform, or "Cosmos," is an open source project with two repositories. The first is for the "Publisher." This is the web application that hosts or "publishes" your web content on the Internet. It is built lean, with only the code and logic needed to host the website.  No content editing or management happens here. This project is a "stock" Visual Studio ASP.NET Core web application with the [Cosmos Common NuGet package](https://www.nuget.org/packages/CDT.Cosmos.Cms.Common/) installed and configured.

You do not have to use the Publisher above. You can add the NuGet package above to your ASP.NET Core application. Essentially, this adds Cosmos publishing to your website. You can mix Cosmos with functionality from hundreds of thousands of packages available through [NuGet](https://www.nuget.org) and [npm](https://www.npmjs.com/).

This repository is the second repository.  It contains a Visual Studio solution with several projects--three of which are unit test projects, and a fourth produces the NuGet package used by the Publisher, and the remainder make up what is called the "Editor."

The "Editor" is where web content is created and managed. It is your tool to manage website "layouts" and it is where web pages are authored. Editor also is a file manager. Upload JavaScript, CSS, imagery, videos and other types of files using "Editor."  It also manages Content Distribution Network (CDN) updates.


## Developing with Publisher Repository

Most people will build customizations using the publisher and/or Cosmos Common NuGet package.  It is with publisher that people can "mash up" the functionality it comes with, with their own creative works.

The "Publisher" [repository is found on GitHub](https://github.com/CosmosSoftware/CDT.Cosmos.Cms.Website).

For developing with publisher, any IDE that can be used with ASP.NET (Core) will do.  Examples include Visual Studio Code, or Visual Studio Community or Professional or Enterprise Editions.


## Developing with Editor Repository

If you would like to make contributions to the [Editor repository](https://github.com/CosmosSoftware/Cosmos.Cms) you will need a [Telerik UI for ASP.NET Core](https://www.telerik.com/aspnet-core-ui) developer's license. [If you do not have a license, you can get a free trial](https://www.telerik.com/).  You DO NOT need a Telerik license to develop with the Publisher or any other of Cosmos CMS repos.

The GitHub actions within the editor repository need the following secrets set:

To enable Telerik NuGet package installation, you will need:
* TELERIK_USER (User name of your Telerik Account)
* TELERIK_PASSWORD (Passoword of your account)

For Docker container build and deployment to Docker Hub:
* DOCKERLOGIN
* DOCKERPASSWORD

For build and publishing of the Cosmos Common NuGet Package:
* NUGET_KEY


## Unit Testing Requirements

As of this writing, the Editor repository  has just over 100 unit tests contained in three projects.  These are designed to be run from Visual Studio. Any [edition (including the free Community Edition)](https://visualstudio.microsoft.com/vs/compare/) will work. As of this writing, we are using Visual Studio Enterprise, version 16.11.2.

You will also need access to database,blob storage, web application services in Azure and Amazon Web Services.  You will also need CDN and SendGrid resources from Microsoft Azure, and, an Akamai developer account.

### Secrets Files

To run and debug Cosmos CMS on a development server you will need to use a "secrets file" to hold your development configuration.

PLEASE! Do NOT put this information in the appsettings.json file!!

Here is and example of an "Editor" secrets file.

```JSONC
{
  //
  // This is an example "secrets" JSON file for an "EDITOR."
  // 
  // Normally this information would not appear in a repository.
  // Keep this file safe!
  // 
  // When in development, this JSON file would be kept in "User Secrets" on the local 
  // machine. 
  // When in development, the variables in this JSON file would be kept in "Secrets."
  // 
  // WARNING!!!!
  //
  // NEVER EVER put this information in the "appsettings.json" file. Exposure risk is HIGH!
  //
  "CosmosAllowSetup": "false", // When an app is in production, either set this to false or remove it.
  "CosmosAllowConfigEdit": "true", // When true this enables the configuration editor. Set to false it disables it.
  "CosmosPrimaryCloud": "azure", // Put the cloud name this is installed. Values can be "amazon" or "azure".
  "CosmosAdminEmail": "your@email.com", // Put the main administrator name here. This may be depreciated in the future
  "CosmosSendGridApiKey": "[YOUR SEND GRID KEY GOES HERE]",
  "CosmosPublisherUrl": "https://publisher.yourdomain.com",
  "CosmosStorageUrl": "https://publicurl.tostorageaccountwebsite.com", // This will depend on the S3 or Azure Storage account
  "CosmosBlobContainer": "$web", // Try and use this name in either Amazon S3 and Azure Storage
  "CosmosEditorUrl": "https://editor.yourdomain.com",
  "CosmosSecretKey": "[Create a random LONG string of characters here]", // This is used with "distributed publishing units."
  "ConnectionStrings": {
    "DefaultConnection": "[Standard connection string to your SQL server]",
    // The following is only for Azure.  This will be updated for S3
    "BlobConnection": "[Your connection string]"
  }
}
```

Here is an example of a JSON file for a "Publisher." Note the simplicity.

```JSONC
{
  //
  // This is an example "secrets" JSON file for an "EDITOR."
  // 
  // Normally this information would not appear in a repository.
  // Keep this file safe!
  // 
  // When in development, this JSON file would be kept in "User Secrets" on the local 
  // machine. 
  // When in development, the variables in this JSON file would be kept in "Secrets."
  // 
  // WARNING!!!!
  //
  // NEVER EVER put this information in the "appsettings.json" file. Exposure risk is HIGH!
  //
  "CosmosPrimaryCloud": "azure", // Put the cloud name this is installed. Values can be "amazon" or "azure".
  "ConnectionStrings": {
    "DefaultConnection": "[Standard connection string to your SQL server]"
  }
}
```

### Cloud Resources

*IMPORTANT: Do not use a database, Redis cache or blob storage account for unit testing that is also being used for a website or for development. Conflicts or dataloss may occur.*

Here is a complete list of items needed to run all the unit tests:

* Amazon Web Services
  * Website Hosting
    * Amazon EC2, or
    * AWS Elastic Beanstalk
   * Storage
     * Amazon RDS for SQL Server
     * Simple Storage Service (S3)
    * AWS Secrets Manager

* Microsoft Azure
  * Website Hosting
    * App Services
  * Storage
    * Azure SQL Database
    * Storage Account
  * Communications
    * SendGrid Account
    * Azure CDN
   * Azure Key Vault

* Akamai
  * Akamai Developer Account

## Unit Tests

*TIP: When setting up your unit test environment make sure your development computers can connect to the all your cloud resources.*

The three unit test projects use "secrets" to connect to cloud resources. This includes user account names and passwords, and other kinds of keys and connection information. Follow best practice when working in development and [store these secrets securely](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-5.0&tabs=windows).

Secrets are usually stored as JSON files.  Here are examples of the format and syntax of each file:

### Unit Test Project: CDT.Akamai.Tests

This unit tests pulls information from a secret called "Akamai" stored in an Azure Vault--the connection to which is defined in the following JSON string.

```js
{
  // This is an example of the secrets.json file used for the Akami unit tests.
  "ClientId": "MY_CLIENT_ID",
  "Key": "MY_VAULT_KEY",
  "VaultUrl": "https://[MYVAULT].vault.azure.net/"
}
```

### Unit Test Project: CDT.Cosmos.BlobService.Tests

The blob servive and Cosmos common unit test share the same Azure Vault secret. You can create this secret using the Configuration Editor that comes with the "Editor."

To use the "Configuration Editor,"  install a Cosmos Editor without any configuration.  It will automatically go into "setup mode." From here you can open the Configuration Editor. It is in the form of a dialog "wizard" which steps you through screens to build your configuration.  When done, it allows you to copy the configuration--which you then "paste" into an Azure Vault secret. The secret name must match the one below in the "secrets.json" file.

```js
{
  // This is an example of the secrets.json file used with the Blob Service tests.
  "ClientId": "MY_CLIENT_ID",
  "Key": "MY_VAULT_KEY",
  "TenantId": "MY_TENANT_ID",
  "VaultUrl": "https://[MYVAULT].vault.azure.net/",
  "UseAzureVault": "true",
  "UseDefaultCredential": "false",
  "SecretName": "MY_SECRET_NAME"
}
```

### Unit Test Project: CDT.Cosmos.Cms.Common.Tests

The "secrets.json" used here is the same as above with the exception of a few added fields.  These fields are used for testing connections and functionality to the AWS Secrets Manager.

```js
{
  // This is an example of the secrets.json file used with the Blob Service tests.
  "ClientId": "MY_CLIENT_ID",
  "Key": "MY_VAULT_KEY",
  "TenantId": "MY_TENANT_ID",
  "VaultUrl": "https://[MYVAULT].vault.azure.net/",
  "UseAzureVault": "true",
  "UseDefaultCredential": "false",
  "SecretName": "MY_SECRET_NAME",
  "CosmosUseAwsSecretsMgr": "true",
  "CosmosAwsSecretsRegion": "AWS_REGION",
  "CosmosAwsKeyId": "MY_AWS_KEY_ID",
  "CosmosAwsSecretAccessKey": "MY_AWS_SECRET_ACCESS_KEY"
}
```
