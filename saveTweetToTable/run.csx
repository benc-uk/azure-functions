#r "Newtonsoft.Json"
using System;
using System.Net;
using Newtonsoft.Json;

public static async Task<object> Run(HttpRequestMessage req, ICollector<Tweet> outputTweetTable, TraceWriter log)
{
    // Get input HTTP request, and deserialize from JSON to a dyanmic 
    string jsonContent = await req.Content.ReadAsStringAsync();
    dynamic tweet_input = JsonConvert.DeserializeObject(jsonContent);

    log.Info($"### New tweet received: "+tweet_input.TweetId); 

    try {
        // Create POCO to hold tweet data
        Tweet t = new Tweet();
        string dayISO = tweet_input.CreatedAtIso.ToString("o").Substring(0, 10);
        t.PartitionKey = dayISO;
        t.RowKey = tweet_input.TweetId;
        t.Text = tweet_input.TweetText;
        t.User = tweet_input.TweetedBy;
        t.Lang = tweet_input.TweetLanguageCode;

        // Add POCO to collection for the Webjob SDK to magically push into the output Table
        outputTweetTable.Add(t);
    } catch(Exception e) {
        // Bummer return a HTTP 400 and spit out some logs
        log.Error("!!! "+e.ToString());
        return req.CreateResponse(HttpStatusCode.BadRequest, new {
            greeting = $"ERROR! "+e
        });
    }

    // All cool, return HTTP 200
    log.Info($"### Tweet inserted into Azure table OK, bye"); 
    return req.CreateResponse(HttpStatusCode.OK, new {
        greeting = $"OK!"
    });
}

//
// Basic POCO data structure for our Table entity 
//
public class Tweet
{
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public string Text { get; set; }
    public string User { get; set; }   
    public string Lang { get; set; }       
}
