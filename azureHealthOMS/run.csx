using System.Security.Cryptography;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

// Change keys and other business as required
static string EVENT_TYPE    = "AzureHealth";
static string WORKSPACE_ID  = "26d0a5a4-ef74-4129-a665-15924e3956a5";
static string SHARED_KEY    = Environment.GetEnvironmentVariable("OMS_SHARED_KEY");
static string AZURE_SUB     = "52512f28-c6ed-403e-9569-82a9fb9fec91";
static string AZURE_TENANT  = "72f988bf-86f1-41af-91ab-2d7cd011db47";
static string AZURE_CLID    = "2ea33f5c-676b-4821-9b30-d5228e8ef4c2";
static string AZURE_KEY     = Environment.GetEnvironmentVariable("AZURE_AUTH_SECRET");
static TraceWriter logger;

//
// Main function, runs on timer
//
public static void Run(TimerInfo myTimer, TraceWriter log)
{
    logger = log;
    log.Info($"*** Function started at: {DateTime.Now}");    
    
    // Get login token so we can access Azure REST API
    string token = getAzureLoginToken(AZURE_TENANT, AZURE_CLID, AZURE_KEY);
    if(token == null) {
        log.Error($"### ERROR NO AUTH TOKEN! FUNCTION ABORTING");
        return;
    }
    log.Info($"*** Azure auth token fetched, now querying resource health API..."); 

    // Call the Azure res health API, returns a chunk of JSON
    var health_data = getAzureHealth(token);
    if(health_data == null) {
        log.Error($"### ERROR NO API DATA! FUNCTION ABORTING");
        return;
    }
    logger.Info($"*** Got health data from Azure, with {health_data.value.Count} resources");

    // Loop over the results (JSON array)
    // Push results into a list for us to serialise out later
    var records = new List<dynamic>();
    foreach(var res in health_data.value) {
        // The id field contains lots of info we need to yank it out with a regex
        // We grab the subscription ID, resource group & resource name
        string id = res.id.ToString();
        Regex regex = new Regex(@"subscriptions\/(.*?)\/resourceGroups\/(.*?)\/providers\/.*\/(.*?)\/providers", RegexOptions.IgnoreCase);
        Match match = regex.Match(id);
        string rec_sub = "";
        string rec_rg = "";
        string rec_name = "";
        if (match.Success) {
            rec_sub = match.Groups[1].Value;
            rec_rg = match.Groups[2].Value;
            rec_name = match.Groups[3].Value;
        } else {
            log.Error("### Yikes!, error matching values from health data");
            return;
        }

        // I shouldn't have to do this but the OMS API can't handle UTF8 encoding!
        var rec_summ = res.properties.summary.ToString();
        rec_summ = Regex.Replace(rec_summ, @"[^\u0000-\u007F]+", string.Empty);

        // Anonymous object to hold what we want to send to Log Analytics
        dynamic record = new {
            resourceName = rec_name,
            resourceGroup = rec_rg,
            subscription = rec_sub,
            availabilityState = res.properties.availabilityState.ToString(),
            summary = rec_summ,
            detailedStatus = res.properties.detailedStatus.ToString(),
            occuredTime = res.properties.occuredTime.ToString(),
            reportedTime = res.properties.reportedTime.ToString()
        };
        // Push into the list
        records.Add(record);
    }

    // OK, serialise the list to a JSON string
    string log_events_json = JsonConvert.SerializeObject(records);

    // This mumbo jumbo is for the HMAC-SHA256 signature
    // - This is what the Log Analytics API wants to auth our request 
    var timestamp = DateTime.UtcNow.ToString("r");
    string sig_string = "POST\n" + log_events_json.Length + "\napplication/json\n" + "x-ms-date:" + timestamp + "\n/api/logs";
    string hashed_sig = buildSignature(sig_string, SHARED_KEY);
    string signature = WORKSPACE_ID + ":" + hashed_sig;

    // We're done, lets post our JSON chunk of log events to the API
    postToLogAnalytics(signature, timestamp, log_events_json);
}

//
// Make REST API call, needs auth token from getAzureLoginToken
//
public static dynamic getAzureHealth(string token)
{
    string url = "https://management.azure.com/subscriptions/" + AZURE_SUB + "/providers/Microsoft.ResourceHealth/availabilityStatuses?api-version=2015-01-01";
            
    var client = new HttpClient();
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var resp = client.GetAsync(url).Result;   
    if(resp.IsSuccessStatusCode) {
        dynamic obj = JsonConvert.DeserializeObject(resp.Content.ReadAsStringAsync().Result);
        return obj;
    } else {
        logger.Info($"### Nightmare! Could not fetch Azure resource health from API:\n {resp}");
        return null;
    }
}

//
// Fetch Azure auth token, requires tennant, client-id and client-secret-key
//
public static string getAzureLoginToken(string tenant_id, string client_id, string secret)
{
    string url = "https://login.microsoftonline.com/" + tenant_id + "/oauth2/token";

    // API requires x-www-form-urlencoded POST data, this is easist way to do it
    var form_content = new FormUrlEncodedContent(new[] {
        new KeyValuePair<string, string>("grant_type", "client_credentials"), 
        new KeyValuePair<string, string>("client_id", client_id),
        new KeyValuePair<string, string>("client_secret", secret),
        new KeyValuePair<string, string>("resource", "https://management.azure.com/")
    });
    var client = new HttpClient();
    var resp = client.PostAsync(url, form_content).Result;   
    if(resp.IsSuccessStatusCode) {
        dynamic obj = JsonConvert.DeserializeObject( resp.Content.ReadAsStringAsync().Result );
        return (string)obj.access_token;
    } else {
        logger.Error($"### Bummer can't login to Azure:\n {resp}");
        return null;
    }
}

//
// Create a HMAC-SHA256 & Base64 encoded signature
//
public static string buildSignature(string message, string secret)
{
    var encoding = new System.Text.ASCIIEncoding();
    byte[] keyByte = Convert.FromBase64String(secret);
    byte[] messageBytes = encoding.GetBytes(message);
    using (var hmacsha256 = new HMACSHA256(keyByte))
    {
        byte[] hash = hmacsha256.ComputeHash(messageBytes);
        return Convert.ToBase64String(hash);
    }
}

//
// Send JSON array of events to Log Analytics
//
public static void postToLogAnalytics(string signature, string date, string json_event_array)
{
    string url = "https://"+ WORKSPACE_ID +".ods.opinsights.azure.com/api/logs?api-version=2016-04-01";
            
    var string_content = new StringContent(json_event_array, Encoding.UTF8);
    var content_header = new MediaTypeHeaderValue("application/json");
    //content_header.CharSet = "utf-8";
    string_content.Headers.ContentType = content_header;
    var client = new HttpClient();
    client.DefaultRequestHeaders.Add("Log-Type", EVENT_TYPE);
    client.DefaultRequestHeaders.Add("x-ms-date", date);
    client.DefaultRequestHeaders.Add("time-generated-field", "");
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("SharedKey", signature);
    
    var resp = client.PostAsync(url, string_content).Result;   
    if(resp.IsSuccessStatusCode) {
        logger.Info("*** Health data posted to OMS Log Analytics OK");
    } else {
        logger.Error($"### Bummer can't post events to Log Analytics:\n {resp}");
    }
}
