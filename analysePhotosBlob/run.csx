using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;

// Parameters
const string API_KEY = "XXXXXXXXXXXXXXXXXXXXXXXX";
const string API_ENDPOINT = "https://api.projectoxford.ai/vision/v1.0/describe";
const string LOGIC_APP_ENDPOINT = "https://prod-01.northeurope.logic.azure.com:443/workflows/7420133847ba422fa162794501e3c078/triggers/manual/run?api-version=2016-06-01&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=4nLf31aooFepxZ73DvX48ORpBUUjQ0ZJvZfuGGKAwxk";
const string EMAIL_TO = "benc.uk@gmail.com";

// Triggered Azure Function
// - Bound to a CloudBlockBlob as the input trigger
public static void Run(CloudBlockBlob triggerBlob, string name, TraceWriter log)
{
    log.Info($"### Triggered function on new blob: {triggerBlob.Name}.");

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

    // Send email with result to myself, using Azure Logic App
    dynamic email_req = new {     
        email_to = EMAIL_TO,
        email_subject= $"Azure - image recognition results for: {triggerBlob.Name}",
        email_body = $"<h1 style='color:grey'>Azure Logic Apps and Functions Demo results</h1><h2>That photo looks like: {desc}</h2><br/><img style='max-width:1024px' src='{triggerBlob.Uri.ToString()}'/><br/><h2>Photo tags:<br/>{tags}</h2><br/> BYE!",
    };
    var email_resp = postREST(LOGIC_APP_ENDPOINT, email_req, log);
    log.Info("### EMAIL SENT "+ email_resp.ToString());
}

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