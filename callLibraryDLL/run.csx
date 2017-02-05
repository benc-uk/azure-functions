using System.Net;
using System.Linq;
using System.Reflection;

// Must be an absolute path.
private const string PATH_TO_LIB = @"D:\home\site\wwwroot\wrapNativeDLL\ExampleClassLibrary.dll";
// You would need to know this from creating the library. 
// NB. You could use a loop/reflection if desired.
private const string DESIRED_TYPE = "ExampleClass";

private static dynamic LoadLibrary(TraceWriter log)
{
    //Load the .dll
    var classLib = Assembly.LoadFile(PATH_TO_LIB);
    //An exception will be thrown here if the desired type is not found
    var type = classLib.GetExportedTypes().Single(t=>t.Name.Equals(DESIRED_TYPE));
    log.Info($"Loaded type: {type.FullName}");
    //This creates a dynamic instance of the type, this means it's simpler to call in usage
    return Activator.CreateInstance(type);
}

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{    
    // Parse query parameter
    string name = req.GetQueryNameValuePairs().FirstOrDefault(q => q.Key.Equals("name")).Value;
    // Get request body
    dynamic data = await req.Content.ReadAsAsync<object>();
    // Set name to query string or body data
    name = name ?? data?.name;
    log.Info($"parameter<name>: {name}");
    // Load local class library
    dynamic c = LoadLibrary(log);
    string response = c.Hello(name);
    log.Info($"<response>: {response}");
    return name == null
        ? req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a name on the query string or in the request body")
        : req.CreateResponse(HttpStatusCode.OK,response);
}

//
// Source code of ExampleClassLibrary.dll
// 

/* 
using System;
namespace ExampleClassLibrary
{
    public class ExampleClass
    {
        public string Hello(string name)
        {
            return string.Format("Hello {0}", name);
        }
    }
}
*/