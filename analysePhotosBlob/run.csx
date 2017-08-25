#r "SendGrid"
#r "Newtonsoft.Json"
#r "Microsoft.WindowsAzure.Storage"

using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using SendGrid.Helpers.Mail;

// Parameters...
static string API_KEY = Environment.GetEnvironmentVariable("VISION_API_KEY");
static string API_ENDPOINT = "https://westeurope.api.cognitive.microsoft.com/vision/v1.0/analyze?visualFeatures=Categories,Tags,Description,Faces,ImageType,Color";

// Triggered Azure Function
// - Bound to a CloudBlockBlob as the input trigger
public static void Run(CloudBlockBlob triggerBlob, string name, TraceWriter log, out Mail email)
{
    Mail msg = new Mail();
    email = msg;
    log.Info($"### Triggered function on new blob: {triggerBlob.Name}. KEY {API_KEY}");

    // Build simple JSON request containing blob URL and POST to Computer Vision API
    // - Note, we just pass the URL of the blob (image file) to the API
    dynamic request = new { url = triggerBlob.Uri.ToString() };
    var api_resp = callCognitiveServiceApi(request); 
    //log.Info($"{api_resp}");
    if(api_resp.message != null) {
        log.Error($"### !ERROR! {api_resp.message}");
        return;
    }

    // Grab and print to log values from API response
    var desc = api_resp.description.captions[0].text.ToString();
    var confidence = Math.Round((double)api_resp.description.captions[0].confidence * 100);

    log.Info($"### I'm about {confidence}% confident that is {desc}");
    foreach(var face in api_resp.faces) {
        log.Info($"### I also spotted a {face.age} year old {face.gender}");
    }
    log.Info("### Top tags for image are:");
    for(var t = 0; t < Math.Min(api_resp.tags.Count, 5); t++) {
        var tag_name = api_resp.tags[t].name;
        var tag_conf = Math.Round((double)api_resp.tags[t].confidence * 100);
        log.Info($"###  -  {tag_name} {tag_conf}%");
    }    

    // Format email for SendGrid
    msg.Subject = $"Azure Functions demo result for: {triggerBlob.Name}";
    var htmlEmail = formatEmail(api_resp, triggerBlob.Uri.ToString());
    Content content = new Content {
        Type = "text/html",
        Value = htmlEmail
    };
    msg.AddContent(content);   
    log.Info($"### Emailing results via SendGrid");
}


//
// Simple HTTP POST call and JSON convert results 
//
public static dynamic callCognitiveServiceApi(dynamic request_obj)
{
    var client = new HttpClient();
    var content = new StringContent(JsonConvert.SerializeObject(request_obj), Encoding.UTF8, "application/json");
    
    content.Headers.Add("Ocp-Apim-Subscription-Key", API_KEY);
    var resp = client.PostAsync(API_ENDPOINT, content).Result;
    dynamic resp_obj = JsonConvert.DeserializeObject( resp.Content.ReadAsStringAsync().Result );
    return resp_obj;
}


//
// Format HTML with interesting stuff returned from the API 
//
public static string formatEmail(dynamic api_resp, string url) 
{
    var desc = api_resp.description.captions[0].text.ToString();
    var confidence = Math.Round((double)api_resp.description.captions[0].confidence * 100);
    string htmlout = $"<h1 style='color:grey'>Azure Functions &amp; Cognitive Services Demo Results</h1><img src='{url}' style='max-width:800px'>";
    htmlout += $"<h1>I'm {confidence}% confident that photo is {desc}</h1><div style='font-size:130%'>";

    htmlout += "<h3>Most dominant colours are:</h3><ul>";
    foreach(var colour in api_resp.color.dominantColors) {
        htmlout += $"<li>{colour}</li>";
    }
    htmlout += "</ul>";

    if(api_resp.faces.Count > 0) {
        htmlout += "<h3>I found some people in the photo too:</h3><ul>";
        foreach(var face in api_resp.faces) {
            htmlout += $"<li>I spotted a {face.age} year old {face.gender}</li>";
        }   
        htmlout += "</ul>";
    } 

    htmlout += "<h3>Top things I found in the image are:</h3><ul>";
    for(var t = 0; t < Math.Min(api_resp.tags.Count, 5); t++) {
        var tag_name = api_resp.tags[t].name;
        var tag_conf = Math.Round((double)api_resp.tags[t].confidence * 100);
        htmlout += $"<li>{tag_name} {tag_conf}%</li>";
    } 

    htmlout += $"<div></ul><br/><br/><br/><hr><details><h4>Full API response in JSON</h4><pre>{api_resp}</pre></details>";
    return htmlout;
}