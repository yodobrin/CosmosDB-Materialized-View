using System;
using Newtonsoft.Json;
    

    public class DeviceMaterializedView
    {
        [JsonProperty("Name")]
        public string ?Name;

        [JsonProperty("aggregationSum")]
        public double AggregationSum;

        [JsonProperty("lastValue")]
        public double LastValue;

        [JsonProperty("type")]
        public string ?Type;

        [JsonProperty("deviceId")]
        public string ?DeviceId;

        [JsonProperty("lastUpdate")]
        public string ?TimeStamp;

         [JsonProperty("id")]
        public string ?Id;
        [JsonProperty("count")]
        public double Count;

        public override string ToString()
        {
            return $"DeviceMaterializedView: {Name} {AggregationSum} {Count} {LastValue} {Type} {DeviceId} {TimeStamp} {Id}";
        }

        // method that creates a dummy DeviceMaterializedView
        public static DeviceMaterializedView CreateDummy()
        {
            var result = new DeviceMaterializedView()
            {
                Name = "Dummy",
                AggregationSum = 0,
                LastValue = 0,
                Type = "Dummy",
                DeviceId = "1000",
                TimeStamp = DateTime.Now.ToString(),
                Count = 0,
                Id = Guid.NewGuid().ToString()
            };

            return result;
        }
    }

