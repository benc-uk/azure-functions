#r "Newtonsoft.Json"
#r "Microsoft.WindowsAzure.Storage"
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Blob;

public static void Run(string myQueueItem, TraceWriter log, out string outputBlob, ICollector<deviceWindSpeed> outputTable)
{
    log.Info($"C# ServiceBus queue trigger function processed message: {myQueueItem}");

    // JSON object, so we need deserialize
    dynamic event_obj = JsonConvert.DeserializeObject(myQueueItem);

    // Do "stuff" with the data here
    string device_id = event_obj.deviceId;
    string uuid = event_obj.uuid;
    double wind_speed =  event_obj.windSpeed;

    var timestamp = DateTime.UtcNow.ToString("o");

    // construct blob output data, simple commma delimited
    string csv_content = timestamp + "," + device_id + "," + wind_speed;

    // Store as an Azure blob, just by assigning a string, magic!
    outputBlob = csv_content; 

    // Also output to a table
    deviceWindSpeed table_entity = new deviceWindSpeed();
    table_entity.PartitionKey = device_id;    
    table_entity.RowKey = uuid;   
    table_entity.WindSpeed = wind_speed;
    outputTable.Add(table_entity);
}



//
// Basic POCO data structure for our Table entity 
//
public class deviceWindSpeed
{
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public double WindSpeed { get; set; }   
}
