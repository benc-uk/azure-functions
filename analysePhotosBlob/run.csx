#r "SendGrid"
#r "Newtonsoft.Json"
#r "Microsoft.WindowsAzure.Storage"

using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using SendGrid.Helpers.Mail;

// Parameters...
static string API_KEY = Environment.GetEnvironmentVariable("VISION_API_KEY");
static string API_ENDPOINT = "https://westeurope.api.cognitive.microsoft.com/vision/v1.0/describe";

// Triggered Azure Function
// - Bound to a CloudBlockBlob as the input trigger
public static void Run(CloudBlockBlob triggerBlob, string name, TraceWriter log, out Mail email)
{
    Mail msg = new Mail();
    email = msg;    
    log.Info($"### Triggered function on new blob: {triggerBlob.Name}. KEY {API_KEY}");

    // Build simple JSON request containing blob URL and POST to Computer Vision API
    // - Note, we just pass the URL of the blob (image file) to the API
    dynamic request = new {url = triggerBlob.Uri.ToString()};
    var api_resp = postREST(API_ENDPOINT, request, log);

    if(api_resp.message != null) {
        log.Error($"### !ERROR! {api_resp.message}");
        return;
    }
    // Grab values from API response
    var desc = api_resp.description.captions[0].text.ToString();
    var tags = api_resp.description.tags.ToString();

    log.Info($"### I spy with my little eye: {desc}");

    // Format email for SendGrid
    msg.Subject = $"Azure Functions demo result for: {triggerBlob.Name}";
    Content content = new Content {
        Type = "text/html",
        Value = $"<h1 style='color:grey'>Azure Functions &amp; Cognitive Services Demo Results</h1><h2>That photo looks like: {desc}</h2><br/><img style='max-width:1024px' src='{triggerBlob.Uri.ToString()}'/><br/><h2>Photo tags:<br/>{tags}</h2><br/> BYE!"
    };
    msg.AddContent(content);   
    log.Info($"### Emailing results via SendGrid");
}

//
// Simple HTTP POST call and JSON convert results 
//
public static dynamic postREST(string url, dynamic request_obj, TraceWriter log)
{
    var client = new HttpClient();
    //var content = new StringContent(request_obj.ToString(), Encoding.UTF8, "application/json");
    var content = new StringContent(JsonConvert.SerializeObject(request_obj), Encoding.UTF8, "application/json");
    
    content.Headers.Add("Ocp-Apim-Subscription-Key", API_KEY);
    var resp = client.PostAsync(url, content).Result;
    dynamic resp_obj = JsonConvert.DeserializeObject( resp.Content.ReadAsStringAsync().Result );
    return resp_obj;
}