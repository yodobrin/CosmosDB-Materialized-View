# CosmosDB - Materilized View

Creating a materialized view using custom changefeed processor. 

## Overview

here's a summary of the steps to create a materialized view using a custom changefeed processor on Azure Cosmos DB SQL API: 

- Create a container to store the materialized view results

- Set up a changefeed processor to listen for changes to the source data

- Write custom code to execute the query and store the results in the materialized view container

- No additional mechanism is needed to refresh the materialized view container as the changefeed processor will automatically update the container whenever new data is added, modified or deleted in the source data.

<details>
<summary>Further Reading</summary>

Creating a materialized view using a custom changefeed processor is an effective way to streamline and optimize data processing workflows on Azure Cosmos DB SQL API. A materialized view is a precomputed summary of data that is stored in a container, making queries much faster and reducing the amount of processing power required to execute them. The custom changefeed processor listens for changes to the source data and updates the materialized view container accordingly, ensuring that the view stays up-to-date with the source data. 

To create a materialized view using a custom changefeed processor on Azure Cosmos DB SQL API, there are several steps that need to be followed. First, a container needs to be created to store the results of the materialized view. This container should have the same structure as the output of the query. 

Next, a changefeed processor needs to be set up to listen for changes to the source data. The Azure Cosmos DB Change Feed Processor library can be used to create the changefeed processor. 

Once the changefeed processor is set up, custom code needs to be written to execute the query and store the results in the materialized view container. This code will depend on the specific tools and frameworks being used and will involve connecting to the data source, executing the query, and writing the results to the materialized view container. 

Finally, no additional mechanism is needed to refresh the materialized view container as the changefeed processor will automatically update the container whenever new data is added, modified or deleted in the source data. 

Overall, creating a materialized view using a custom changefeed processor on Azure Cosmos DB SQL API can significantly improve query performance and optimize data processing workflows. By following the above steps and leveraging the right tools and frameworks, a robust and efficient system can be created for storing and querying data.

</details>

## What's included?

The sample includes code to generate simulated sensor data and push it to a Cosmos DB container, along with IaC to create the environment and set up the custom changefeed processor. The processor listens for changes to the sensor data and updates a materialized view in real-time, improving query performance. Overall, the sample provides a comprehensive solution for creating a materialized view using a custom changefeed processor on Azure Cosmos DB SQL API.

> Note: The sample is written in C# and uses the .NET 7 framework. It is not intended to be a production-ready solution, but rather a proof of concept to demonstrate how to create a materialized view using a custom changefeed processor on Azure Cosmos DB SQL API. A production ready solution would be hosted on a dynamic compute platform such as Azure Container App or AKS, and would be designed to handle large amounts of data and scale to meet demand. 

## Prerequisites

- An active Azure subscription with proper quota and authorization. If you don't have an Azure subscription, you can create a free account here: https://azure.microsoft.com/free/

- Clone the this repository.

- Install the Bicep CLI. You can download the latest version of Bicep from here: https://github.com/Azure/bicep/releases

- Install .NET 7. You can download the latest version of .NET from here: https://dotnet.microsoft.com/download/dotnet/7.0 


## Step by Step Guide

### Step 1: Run the IaC - creating required resources

The first step is to run the IaC to create the required resources. This includes a Cosmos DB account, a Cosmos DB, a container to store the sensor data, a lease container and a container to store the materialized view results. Addtional resources that could be created in a production-ready solution will include application insights and other monitoring/operational resources.

> Note: the iac code is missing at this point, will be added soon. you could create the above required resources manually. take note for the endpoint URI and the key for the Cosmos DB account, together with the names of the DataBase and containers.

### Step 2: Configure Data Producer

The data producer is a console application that generates simulated sensor data and pushes it to a Cosmos DB container. The data producer is written in C# and uses the .NET 7 framework. `The appsetting.json` file need to include the following parameters:

```json
{  
  "CosmosDB": {  
    "EndpointURI": "https://<your-cosmos-account-name>.documents.azure.com:443/",  
    "Key": "<a read/write key>",  
    "Database": "<your-db-name>",  
    "Collection": "<your-sensor-container-name>""  
  }  
} 
```
### Step 3: Configure View Processor

The change feed processor needs four parameters to be configured:

```json
{    
    "EndpointURI": "https://<your-cosmos-account-name>.documents.azure.com:443/",  
    "Key": "<a read/write key>",
    "Database": "<your-db-name>",  
    "MonitoredCollection": "<your-sensor-container-name>" ,
    "MaterializedViewCollection": "<your-materilized-view-container-name>" ,
    "LeasesContainerName": "leases"  
} 
```

### Step 4: Run the Data Producer

Navigate to the floder `data-producer` and run the following command:

```bash
dotnet run 1 
```
This will start generating sensor data for a single sensor, you can produce with this simple producer up to 10 sensors. you could also provide a range for example:

```bash
dotnet run 4-10 
```

Any keystorke will stop the producer.

### Step 5: Run the View Processor

Navigate to the floder `view-processor` and run the following command:

```bash
dotnet run 
```

### Step 6: Query the Materialized View

In Azure portal navigate to your Cosmos DB account and open the materialized view container. You can query the container using the SQL API. For example, to query the average temperature for each sensor, you can run the following query:

```sql
SELECT c.Name, c.aggregationSum, c.count FROM c
```

You should see the items aggregationSum and count updated in real-time as the data producer generates new data.

