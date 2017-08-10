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
        // Create POCO to hold tweet loaded from JSON in the input request
        Tweet t = new Tweet();
        string dayISO = tweet_input.CreatedAtIso.ToString("o").Substring(0, 10);
        t.PartitionKey = dayISO;
        t.RowKey = tweet_input.TweetId;
        t.Text = tweet_input.TweetText;
        t.User = tweet_input.TweetedBy;
        t.Lang = tweet_input.TweetLanguageCode;

        log.Info($"### RT count: "+tweet_input.RetweetCount);

        // Add POCO to collection for the Webjob SDK to magically push into the output Table
        // Modified to ignore retweets
        if(tweet_input.RetweetCount == 0) {
            log.Info($"### Adding tweet to table"); 
            outputTweetTable.Add(t);
        }
    } catch(Exception e) {
        // Bummer return a HTTP 400 and spit out some logs
        log.Error("!!! "+e.ToString());
        return req.CreateResponse(HttpStatusCode.BadRequest, new {
            greeting = $"ERROR! "+e
        });
    }

    // All cool, return HTTP 200
    log.Info($"### Tweet processed OK, bye"); 
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
