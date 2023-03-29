
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
// using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Extensions.Configuration;
using materialized_view;

namespace ChangeFeedSample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            IConfiguration configuration = BuildConfiguration();
            string ?databaseName = configuration["Database"];
            string ?sourceContainerName = configuration["MonitoredCollection"];
            string ?leaseContainerName = configuration["LeasesContainerName"];
            string ?materializedViewCollection = configuration["MaterializedViewCollection"];
            if( databaseName == null || sourceContainerName == null || leaseContainerName == null || materializedViewCollection == null)
            {
                throw new ArgumentNullException("Missing 'Database', 'MonitoredCollection', or 'LeasesContainerName' settings in configuration.");
            }            

            CosmosClient cosmosClient = BuildCosmosClient(configuration);
            Container leaseContainer = cosmosClient.GetContainer(databaseName, leaseContainerName);
            Container viewContainer = cosmosClient.GetContainer(databaseName, materializedViewCollection);

            var builder = cosmosClient.GetContainer(databaseName, sourceContainerName)  
                            .GetChangeFeedProcessorBuilder<SensorData>(processorName: "changeFeedSample", 
                            onChangesDelegate:  (IReadOnlyCollection<SensorData> changes, 
                            CancellationToken cancellationToken) =>                                    
                                     HandleChangesAsync(changes, cancellationToken, viewContainer) 
                                )  
                                .WithInstanceName("consoleHost")  
                                .WithLeaseContainer(leaseContainer);  
  
            ChangeFeedProcessor changeFeedProcessor = builder.Build();  

            await changeFeedProcessor.StartAsync(); 
            await Console.Out.WriteLineAsync("Started");
            _ = await Console.In.ReadLineAsync();

            
        }
        private static async Task HandleChangesAsync(IReadOnlyCollection<SensorData> changes, CancellationToken cancellationToken, Container viewContainer)
            {
            List<Task> tasks = new List<Task>();
            // group the changes by device id to handle micro batches (todo)            
            foreach (SensorData item in changes)
            {
                Console.WriteLine($"Detected operation for item with id {item.id}, created at {item.TimeStamp}.");
                // tasks.Add (UpdateDeviceMaterializedViewAsync(item, viewContainer));
                await UpdateDeviceMaterializedViewAsync(item, viewContainer);                             
            }
            // await Task.WhenAll(tasks);  
            Console.WriteLine("Finished handling batch of changes.");
            }

        private static async Task UpdateDeviceMaterializedViewAsync(SensorData sensorData, Container viewContainer)  
            {  
                string deviceId = sensorData.Id!;  
                // a flag to indicate whether the view document is new or not
                bool newItem = false;
                // a flag to indicate whether the view document is updated or not due to too many results
                bool except = false;
                
                // Retrieve the corresponding view document with the specified deviceId  
                DeviceMaterializedView? viewItem = null;
                try{

                    var query = new QueryDefinition("SELECT * FROM c WHERE c.deviceId = @deviceId")  
                        .WithParameter("@deviceId", deviceId);  
                    var iterator = viewContainer.GetItemQueryIterator<DeviceMaterializedView>(query);  
                    var results = new List<DeviceMaterializedView>();  
                    while (iterator.HasMoreResults)  
                    {  
                        var response = await iterator.ReadNextAsync();  
                        results.AddRange(response.ToList());  
                    }                      
                    
                    if (results.Count() == 1)
                    {
                        // Console.WriteLine($"UpdateDeviceMaterializedViewAsync queryable size: {results.Count()}");              
                        viewItem = results.First();
                        Console.WriteLine($" viewItem from cosmos: {viewItem.Name}");
                    }else if(results.Count() > 1)
                    {
                        Console.WriteLine($"UpdateDeviceMaterializedViewAsync query result: {results.Count()} indicate an issue");
                        except = true;
                    }else if(results.Count() == 0)
                    {
                        // Console.WriteLine($"UpdateDeviceMaterializedViewAsync queryable size: {results.Count()}");
                        viewItem = null;
                    }
                    
                }catch(Exception ex) 
                {
                    Console.WriteLine($" CosmosException in query: {ex}");
                }
                
                if (viewItem == null && !except)  
                {  
                    // If no view document was found, create a new one  
                    viewItem = new DeviceMaterializedView  
                    {  
                        DeviceId = deviceId ,
                        Name = $"A - {deviceId}",
                        Type = "Device",
                        Id = Guid.NewGuid().ToString()
                    };  
                    newItem = true;
                } else if (viewItem !=null && !except)
                {                    
                    viewItem.AggregationSum += sensorData.Value;                      
                    viewItem.LastValue = sensorData.Value;  
                    viewItem.TimeStamp = sensorData.TimeStamp!;  
                    viewItem.Count += 1;
                }else if(except)
                {
                    Console.WriteLine($"More than one item with the same deviceId: {deviceId}");
                }                        
            
                // Upsert the updated view document back into the view container  
                // Console.WriteLine($"Upserting new item for materialized view:  {viewItem.Name}");
                try{
                    if(newItem)
                    {
                        var response = await viewContainer.CreateItemAsync(viewItem, new PartitionKey(deviceId));  
                        // print message to console
                        Console.WriteLine($"Created new materialized view for device {viewItem} - {response.StatusCode}");
                    }                        
                    else{
                        var response = await viewContainer.UpsertItemAsync(viewItem, new PartitionKey(deviceId));
                        // print message to console
                        Console.WriteLine($"Updated materialized view for device {viewItem} - {response.StatusCode}");
                    }
                    
                }catch(CosmosException ex) 
                {
                    Console.WriteLine($" CosmosException in upsert: {ex}");
                }
                
                Console.WriteLine($"--------Updated materialized view for device {viewItem}-------");
            } 
        private static IConfiguration BuildConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();
        }

        private static CosmosClient BuildCosmosClient(IConfiguration configuration)
        {
            return new CosmosClient(configuration["EndpointURI"], configuration["Key"]);            
        }

        
    }

   
}
