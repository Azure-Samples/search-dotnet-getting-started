---
services: search
platforms: dotnet
author: brjohnst
---

# Getting Started with Azure Search using .NET

This sample includes several projects that are meant to help when getting started with Azure Search and .NET. It leverages the [Azure Search .NET SDK](https://aka.ms/search-sdk) as well as many best practices.

The DotNetHowTo sample is a simple .NET Core console application that shows how to:

* Create and Delete an Azure Search index
* Upload Documents to an Azure Search index
* Search & Filter documents within an index

The DotNetETagsExplainer sample shows how to use ETags to update Azure Search resources safely in the presence of concurrency.

The DotNetHowToIndexers sample is a simple .NET Core console application that shows how to:

* Create and Delete an Azure Search index
* Create an Azure Search indexer for Azure SQL

The DotNetHowToMultipleDataSources sample demonstrates combining multiple Azure data sources into a single search index using indexers:

* Create and Delete an Azure Search index
* Create two Azure Search indexers, one for Azure SQL and one for Cosmos DB
* Target the same Azure Search index using both indexers

The DotNetHowToSynonyms sample demonstrates how to incorporate the Synonyms feature in your application step by step:

* Create an Azure Search index
* Search documents using terms that do not appear in the indexed documents
* Define and upload synonym rules
* Repeat the search

The DotNetHowToSecurityTrimming sample demonstrates how to implement document-level security in Azure Search using filters.

The DotNetSample project is a more detailed example that demonstrates:

* How to use the Azure Search Indexer to ingest data from common stores (such as Azure SQL) to populate an Azure Search index
* A simple ASP.net MVC application that allow you to search and view results from an Azure Search index

The DotNetHowToAutocomplete sample demonstrates several techniques for adding auto-complete and suggestions to your search experience.  It demonstrates the following:

* Implement a search input box
* Add support for an auto-complete list that pulls from a remote source 
* Retrieve suggestions and auto-complete using the .Net SDK and REST API
* Support client-side caching to improve performance 

The DotNetHowToEncryptionUsingCMK sample demonstrates how to create a synonym-map and an index that are encrypted with customer-managed key in Azure Key Vault.

## Running the DotNetHowTo sample

* Open the DotNetHowTo.sln project in Visual Studio
* Update the appsettings.json with the service and api details of your Azure Search service
* Compile and Run the project

## Running the DotNetETagsExplainer sample

* Open the DotNetETagsExplainer.sln project in Visual Studio
* Update the appsettings.json with the service and api details of your Azure Search service
* Compile and Run the project

## Running the DotNetHowToIndexers sample

* Open the DotNetHowToIndexers.csproj project in Visual Studio.
* Update the appsettings.json with your service name, api key, and connection string to your Azure SQL database.
* Run the `data\hotels.sql` script against your Azure SQL database.
* Compile and Run the project using Visual Studio 2017.
* Alternatively, download the .NET Core SDK at https://www.microsoft.com/net/core and
  issue a `dotnet run` command from the DotNetHowToIndexers directory.

## Running the DotNetHowToMultipleDataSources sample

* Open the DotNetHowToMultipleDataSources.csproj project in Visual Studio.
* Update the appsettings.json with your service name, api key, and connection string to your Azure SQL database and Cosmos DB database
* Run the `data\hotels.sql` script against your Azure SQL database.
* Upload the json files in `HotelsJson` to a SQL collection called `hotels` in your Cosmos DB database
* Compile and Run the project using Visual Studio 2017.
* Alternatively, download the .NET Core SDK at https://www.microsoft.com/net/core and
  issue a `dotnet run` command from the DotNetHowToMultipleDataSources directory.

## Running the DotNetHowToSynonyms sample

* Open the DotNetHowToSynonyms.sln project in Visual Studio
* Update the App.config with the service and api details of your Azure Search service
* Compile and Run the project

## Running the DotNetHowToSecurityTrimming sample

See [Security filters for trimming Azure Search results using Active Directory identities](https://docs.microsoft.com/azure/search/search-security-trimming-for-azure-search-with-aad)

## Running the DotNetSample sample

* Open the AzureSearchDotNetSample.sln project in Visual Studio
* Update the Web.config in the SimpleSearchMVCApp project with the service and api details of your Azure Search service
* Update the App.config in the DataIndexer project with the service and api details of your Azure Search service
* Compile and Run the DataIndexer project to create an Azure Search and populate it with content from an Azure SQL database
* Compile and Run the SimpleSearchMVCApp project to search and view the results from this index

## Running the DotNetHowToAutocomplete sample

* Open the DotNetHowToAutocomplete.sln project in Visual Studio
* Compile and Run the project

## Running the DotNetHowToEncryptionUsingCMK sample

* Open the DotNetHowToEncryptionUsingCMK.sln project in Visual Studio
* Compile and Run the project

## More information

For more details on the "how-to" sample, please refer to this article:

  - [How to use Azure Search from a .NET Application](https://docs.microsoft.com/azure/search/search-howto-dotnet-sdk).

For more details on the "how-to" sample for synonyms, please refer to this article:

  - [Synonym C# tutorial for Azure Search](https://aka.ms/azsdotnetsynonyms).

---

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
