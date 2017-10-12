---
services: search
platforms: dotnet
author: brjohnst
---

# Getting Started with Azure Search using .NET

This sample includes three projects that are meant to help when getting started with Azure Search and .NET.  It leverages the [Azure Search .NET SDK](https://aka.ms/search-sdk) as well as many best practices. The first sample DotNetHowTo is a simple .NET Core console application that shows how to:

* Create and Delete an Azure Search index
* Upload Documents to an Azure Search index
* Search & Filter documents within an index

The second sample DotNetHowToIndexers is a simple .NET Core console application that shows how to:

* Create and Delete an Azure Search index
* Create an Azure Search indexer for the following types of data sources:
  * Azure SQL
  * Azure Table Storage
  * Azure Cosmos DB

The third sample DotNetHowToSynonyms demonstrates how to incorporate the Synonyms feature in your application step by step:

* Create an Azure Search index
* Search documents using terms that do not appear in the indexed documents
* Define and upload synonym rules
* Repeat the search

Lastly, the sample AzureSearchDotNetSample is a more detailed example that demonstrates:

* How to use the Azure Search Indexer to ingest data from common stores (such as Azure SQL) to populate an Azure Search index
* A simple ASP.net MVC application that allow you to search and view results from an Azure Search index

## Running the DotNetHowTo sample

* Open the DotNetHowTo.sln project in Visual Studio
* Update the appsettings.json with the service and api details of your Azure Search service
* Compile and Run the project

## Running the DotNetHowToIndexers sample

* Open the DotNetHowToIndexers.csproj project in Visual Studio.
* Update the appsettings.json with the service and api details of your Azure Search service,
  along with whatever data source you choose (Azure SQL, Azure Table Storage, or Azure Cosmos DB).
  * If you are choosing to use Azure SQL, run the `data\hotels.sql` script against your Azure SQL database
    to populate it with suitable sample data.
  * If you are choosing to use Azure Table Storage run `powershell data\hotels-table-storage.ps1 -StorageAccountName <Your storage account name> -StorageAccountKey <Your storage account key>.
    This will automatically create a `hotels` table under your storage account with the data set in `data\hotels.json`.
  * If you are choosing to use Azure Cosmos DB, upload `data\hotels.json` to a Cosmos DB collection of your choice.
    Follow the instructions at https://docs.microsoft.com/en-us/azure/cosmos-db/import-data#JSON.
* Compile and Run the project, specifying the correct command line parameter for which data source you are using:
  * AzureSQL for Azure SQL
  * AzureTableStorage for Azure Table Storage
  * AzureCosmosDB for Azure Cosmos DB
* Alternatively, download the .NET Core SDK at https://www.microsoft.com/net/core and
  issue a `dotnet run <DataSource>` command from the DotNetHowToIndexers directory.

## Running the DotNetHowToSynonyms sample

* Open the DotNetHowToSynonyms.sln project in Visual Studio
* Update the App.config with the service and api details of your Azure Search service
* Compile and Run the project

## Running the AzureSearchDotNetSample sample

* Open the AzureSearchDotNetSample.sln project in Visual Studio
* Update the Web.config in the SimpleSearchMVCApp project with the service and api details of your Azure Search service
* Update the App.config in the DataIndexer project with the service and api details of your Azure Search service
* Compile and Run the DataIndexer project to create an Azure Search and populate it with content from an Azure SQL database
* Compile and Run the SimpleSearchMVCApp project to search and view the results from this index

## More information

For more details on the "how-to" sample, please refer to this article:

  - [How to use Azure Search from a .NET Application](https://docs.microsoft.com/azure/search/search-howto-dotnet-sdk).

For more details on the "how-to" sample for synonyms, please refer to this article:

  - [Synonym C# tutorial for Azure Search](https://aka.ms/azsdotnetsynonyms).

---

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
