---
page_type: sample
description: "Contains several projects to help you get started with Azure Cognitive Search and the .NET SDK"
languages:
- csharp
products:
- azure
- azure-cognitive-search
---

# Getting Started with Azure Cognitive Search using .NET

This repository includes several projects that show you how to use the [**Azure.Search.Documents**](https://docs.microsoft.com/dotnet/api/overview/azure/search.documents-readme) client library in Azure SDK for .NET to create C# applications that use Azure Cognitive Search.

An earlier version of the samples that were built using the [**Microsoft.Azure.Search**] client libraries can be found in the **v10** branch of this repo. To download those versions, switch the branch from **master** to **v10**, and then select **Code** to download or open those samples.

## DotNetHowTo

This sample is a simple .NET Core console application that shows you how to:

* Create and delete a search index
* Upload documents
* Search and filter documents within an index

To run this sample, open the solution in Visual Studio and modify **appsettings.json** to use valid values for your search service.

<!-- For detailed instructions, see [How to develop in C# using Azure.Search.Documents](https://docs.microsoft.com/azure/search/search-howto-dotnet-sdk-v11).  -->

## DotNetHowToEncryptionUsingCMK

This sample demonstrates how to create a synonym-map and an index that are encrypted with a customer-managed key in Azure Key Vault. This sample uses several services that must be set up in advance: Azure Key Vault, Azure Active Directory, and modifications to your existing search service.

The **appsettings.json** file provides placeholders for service information.

For detailed instructions, see [How to configure customer-managed keys for data encryption in Azure Cognitive Search](https://docs.microsoft.com/azure/search/search-security-manage-encryption-keys).

## DotNetHowToSynonyms

This sample demonstrates the benefits of adding a synonym map using "before-and-after" queries:

* Creates a search index
* Searches for documents using terms that do not appear in the indexed documents (query returns no results)
* Define and upload synonym rules
* Re-run the queries. This time, query results are found due to matching synonyms in the synonym list.

To run this sample, open the solution in Visual Studio and modify **app.config** to use valid values for your search service.

## DotNetHowToIndexers

This sample is a simple .NET Core console application that shows how create and run a search indexer that retrieves data from an Azure SQL database.

Before you can run this sample, you will need an Azure SQL database that contains sample data used by the indexer. You will also need to modify settings in **appsettings.json**.

1. Create a new database in Azure SQL named hotels.
1. Run the `data\hotels.sql` script provided in this sample against your Azure SQL database.
1. Open the DotNetHowToIndexers.sln in Visual Studio.
1. Update the appsettings.json with your service name, api key, and connection string to your Azure SQL database.
1. Compile and Run the project using Visual Studio.

## DotNetHowToSecurityTrimming

This sample demonstrates how to implement document-level security in Azure Search using filters.

For more information, see [Security filters for trimming Azure Search results using Active Directory identities](https://docs.microsoft.com/azure/search/search-security-trimming-for-azure-search-with-aad).

## DotNetETagsExplainer

This sample is a .NET Core console application that shows how to use ETags to update Azure Cognitive Search resources safely in the presence of concurrency. The code in this sample simulates concurrent write operations so that you can see how that condition is handled.

To run this sample, open the solution in Visual Studio and modify **appsettings.json** to use valid values for your search service.

## Retired samples

The following samples have been removed from the master branch.

### DotNetHowToAutocomplete

This sample was not migrated to use the Azure.Search.Documents client library. It has been replaced by a project in the [create-first-app](https://github.com/Azure-Samples/azure-search-dotnet-samples/tree/master/create-first-app) sample in the [azure-search-dotnet-samples](https://github.com/Azure-Samples/azure-search-dotnet-samples) repository. Alternatively, you can look at the v10 version of the sample.

### DotNetHowToMultipleDataSources

This sample was not migrated to use the Azure.Search.Documents client library. It has been replaced by the [multiple-data-sources](https://github.com/Azure-Samples/azure-search-dotnet-samples/tree/master/multiple-data-sources) sample in the [azure-search-dotnet-samples](https://github.com/Azure-Samples/azure-search-dotnet-samples) repository. Alternatively, you can look at the v10 version of the sample.

### DotNetSample

This sample was not migrated to use the Azure.Search.Documents client library. We recommend that you refer to **DotNetHowToIndexers** to view code that calls the Azure SQL Indexer. Alternatively, you can look at the v10 version of the sample.

---

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.