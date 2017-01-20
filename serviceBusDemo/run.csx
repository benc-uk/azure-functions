#r "Newtonsoft.Json"
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;

public static void Run(string inputQueueMsg, out string outputBlob, TraceWriter log)
{
    log.Info($"C# ServiceBus queue trigger function processed message: {inputQueueMsg}");

    // JSON object, so we need deserialize
    dynamic msg_obj = JsonConvert.DeserializeObject(inputQueueMsg);

    // Do "stuff" with the data!
    double new_value = msg_obj.value / 34.9112d;
    string new_msg =  msg_obj.message.ToString().Substring(0, 10);

    // construct blob output data, simple pipe delimited
    string output_blob_content = new_value + " | " + msg_obj.isEnabled + " | " + new_msg;

    // Store as an Azure blob, just by assigning a string, magic!
    outputBlob = output_blob_content;
}