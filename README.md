---
services: search
platforms: dotnet
author: brjohnst
---

# Getting Started with Azure Search using .NET

This sample includes two projects that are meant to help when getting started with Azure Search and .NET.  It leverages the [Azure Search .NET SDK](https://www.nuget.org/packages/Microsoft.Azure.Search) as well as many best practices. The first sample DotNetHowTo is a simple .NET console application that shows how to:

* Create and Delete an Azure Search index
* Upload Documents to an Azure Search index
* Search & Filter documents within an index

The second sample AzureSearchDotNetSample is a more detailed example that demonstrates:
* How to use the Azure Search Indexer to ingest data from common stores (such as Azure SQL) to populate an Azure Search index
* A simple ASP.net MVC application that allow you to search and view results from an Azure Search index

## Running the DotNetHowTo sample

* Open the DotNetHowTo.sln project in Visual Studio
* Upload the App.config with the service and api details of your Azure Search service
* Compile and Run the project

## Running the AzureSearchDotNetSample sample

* Open the AzureSearchDotNetSample.sln project in Visual Studio
* Upload the App.config in the DataIndexer project with the service and api details of your Azure Search service
* Upload the Web.config in the SimpleSearchMVCApp project with the service and api details of your Azure Search service
* Compile and Run the DataIndexer project to create an Azure Search and populate it with content from an Azure SQL database
* Compile and Run the SimpleSearchMVCApp project to search and view the results from this index

## More information

For more details on these samples, please refer to these articles on Azure.com:

  - [How to use Azure Search from a .NET Application](https://azure.microsoft.com/en-us/documentation/articles/search-howto-dotnet-sdk/).
  - [Get started with your first Azure Search application in .NET](https://azure.microsoft.com/en-us/documentation/articles/search-get-started-dotnet/).