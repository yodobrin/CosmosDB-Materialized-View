using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
// using System;
// using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;  

// add name space


namespace materialized_view
{
   
internal class Program
{
    static async Task Main(string[] args)
    {
        Console.Title = "Host";

        using var host = Host
            .CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostBuilderContext, configurationBuilder) =>
            {
                configurationBuilder
                    .AddJsonFile("appsetting.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables();
            })

            .Build();

        await host.StartAsync();
        // print out the configuration
        var config = host.Services.GetService(typeof(IConfiguration)) as IConfiguration;
        if (config == null)
        {
            throw new Exception("Could not get configuration");
        }

        string sensorId = string.Empty;

            var cosmosDBInfo = new CosmosDBInfo()
            {
                EndpointUri = config.GetValue<string>("CosmosDB:EndpointURI"),
                Key = config.GetValue<string>("CosmosDB:Key"),
                Database = config.GetValue<string>("CosmosDB:Database"),
                Collection = config.GetValue<string>("CosmosDB:Collection")
            };
        if (args.Count() == 1)
        {
            sensorId = args[0];
        }

        if (string.IsNullOrEmpty(sensorId))
        {
            Console.WriteLine("Please specify SensorId range. Eg: sensor-data-producer 1-10");
            return;
        }

            int s = 0;
            int e = 0;

            if (sensorId.Contains("-"))
            {
                var split = sensorId.Split('-');
                if (split.Count() != 2) {
                    Console.WriteLine("Range must be in the form N-M, where N and M are positive integers. Eg; 1-10");
                    return;
                }
                
                Int32.TryParse(split[0], out s);
                Int32.TryParse(split[1], out e);
            } else 
            {
                s = 1;
                Int32.TryParse(sensorId, out e);                
            }

            if (s == 0 || e == 0)
            {
                Console.WriteLine("Provided SensorId must be an integer number or a range of positive integers in the form N-M. Eg: 1-10");
                return;
            }
            var tasks = new List<Task>();
            var cts = new CancellationTokenSource();

            var simulator = new Simulator(cosmosDBInfo, cts.Token);

            foreach (int i in Enumerable.Range(s, e))
            {
                tasks.Add(new Task(async () => await simulator.Run(i), TaskCreationOptions.LongRunning));
            }

            tasks.ForEach(t => t.Start());

            Console.WriteLine("Press any key to terminate simulator");
            Console.ReadKey(true);

            cts.Cancel();
            Console.WriteLine("Cancel requested...");

            await Task.WhenAll(tasks.ToArray());

            Console.WriteLine("Done.");
        await host.StopAsync();
    }
}

public class SensorData 
{
    [JsonProperty("deviceId")]
    public string ?Id { get; set; }

    [JsonProperty("value")]
    public double Value { get; set; }

    [JsonProperty("timestamp")]
    public string ?TimeStamp { get; set; }

    [JsonProperty("id")]
    public string ?id { get; set; }

    public override string ToString()
    {
        return string.Format($"{Id}: {TimeStamp} - {Value}");
    }
}
public class CosmosDBInfo {
    public string ?EndpointUri;
    public string ?Key;
    public string ?Database;
    public string ?Collection;
}

class Simulator {

    private CancellationToken _token;
    private CosmosClient _client;
    private CosmosDBInfo _cosmosDB;

    public Simulator(CosmosDBInfo cosmosDB, CancellationToken token)
    {
        _token = token;
        _cosmosDB = cosmosDB;
        _client = new CosmosClient(_cosmosDB.EndpointUri, _cosmosDB.Key);  
        
    }

    public async Task Run(int sensorId)
    {
        var database = await _client.CreateDatabaseIfNotExistsAsync( _cosmosDB.Database );  
        var container = await database.Database.CreateContainerIfNotExistsAsync(_cosmosDB.Collection, "/deviceId");  
        Random random = new Random();

        while (!_token.IsCancellationRequested)
        {
            var sensorData = new SensorData()
            {
                Id = sensorId.ToString().PadLeft(3, '0'),
                Value = 100 + random.NextDouble() * 100,
                TimeStamp = DateTime.UtcNow.ToString("o"),
                id = Guid.NewGuid().ToString()
            };

            Console.WriteLine(sensorData);

            bool documentCreated = false;
            // int tryCount = 0;

            while(!documentCreated )
            {
                var response = await container.Container.CreateItemAsync(sensorData, new PartitionKey(sensorData.Id));  
                documentCreated = true;
            }

            if (documentCreated == false)
            {
                throw new ApplicationException("Cannot create document after trying 3 times");
            }

            await Task.Delay(random.Next(500) + 750);
        }            
    }
}    
}