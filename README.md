# Demo & Examples of Azure Functions
![icon](https://docs.microsoft.com/en-gb/azure/media/index/azurefunctions.svg)
This is a small shared repo of demo and sample Azure Functions. They are either demos or fulfil a basic usecase.

Several of these functions require secure bits of info like secret keys, these are fetched from environmental variables. To set these variables go into the Function App, and configure app settings as [described here](https://docs.microsoft.com/en-us/azure/azure-functions/functions-how-to-use-azure-function-app-settings)

## Quick Deploy to Azure
If you wish to deploy a new Function App with all of these demo functions you can use the following quick deployment ARM template  
[![deploy](https://raw.githubusercontent.com/benc-uk/azure-arm/master/etc/azuredeploy.png)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fbenc-uk%2Fazure-arm%2Fmaster%2Fpaas-other%2Ffunction-app-withcode%2Fazuredeploy.json)  

Where additional dependant Azure resources are needed, an ARM template is supplied in each function folder called **'resources-deploy.json'**. Configuring the input and output bindings of the function to point at these resources is currently left as a manual task, but is normally trivial

## Functions List

### analysePhotosBlob
This reads a photo from blob storage, then analyses the image with the Azure Cognitive Services (Computer Vision API) then sends the results as an email, through SendGrid. The results will contain a description of what the computer vision service thinks the photo is of
* Triggering from files uploaded to blob storage
* Integration with a REST API
* Integration with Cognitive Services
* Required app settings: VISION_API_KEY, SENDGRID_API_KEY
* Dependant Azure resources: Cognitive API, Storage Account (reuse the one that the Function App requires)
* Requires a SendGrid account and API key, [which is free to sign up and use](https://app.sendgrid.com/signup)

### serviceBusDemo
This function is bound to a Service Bus queue, any messages posted on the queue are read, deseralized as JSON, then pushed as output to blob storage
* Triggering from Service Bus queue
* Output to Blobs
* Dependant Azure resources: Service Bus with a single queue

### azureHealthOMS
An integration use case that reads from the Azure Resource Health API and pushes data over to OMS Log Analytics, so that it's avaiable to query and report/alert on
JSON transformation and REST API calls are used
* Integration with a REST API
* JSON manipulation
* Azure API authentication (fetching token for Azure AD)
* Creating HMAC-SHA256 signed headers
* Dependant Azure resources: Log Analytics workspace

### callLibraryDLL
Integration with native/external code in a DLL, Function is a HTTP trigger which in turn triggers a method in the DLL
* Triggering from HTTP 
* .NET Reflection to load a DLL
* Passing data from HTTP request to DLL method callLibraryDLL

### iotEventsDemo
This function is bound to a Service Bus queue, which in turn is bound to an IoT Event Hub. Any incoming device messages sent to the IoT hub are picked up and saved to blob storage and put into an Azure Table.  
Full details are included in a **[separate dedicated Github repo](https://github.com/benc-uk/azure-iot-demo)**
* Triggering from Service Bus queue
* Output to Blobs
* Output to Azure Table storage
* Dependant Azure resources: Service Bus with a single queue

### saveTweetToTable 
Example of both HTTP JSON webhook and Azure Tables as output, designed to be called from Azure Logic App which is reading Tweets and calling this function
* Triggering from HTTP Webhook + JSON
* Output to HTTP response
* Output to Azure Table storage
* Dependant Azure resources: Storage Account (reuse the one that the Function App requires)

### pythonPhotoAnalyse 
Example of using Python in Azure Functions, Python support is still experimental. This function replicates the same scenario as *analysePhotosBlob* and has the same requirements
* See [README.md](pythonPhotoAnalyse/) in function sub-folder for setup of Python

### exampleApi_Get & exampleApi_Delete
These functions are used with the Function Proxies feature to create a serverless REST API. The proxy is set up to route HTTP requests to `/apidemo/{id}` to the backend functions `exampleApi_Get` & `exampleApi_Delete` the HTTP method used (e.g. `GET` or `DELETE`) determines which function is invoked, using the `{request.method}` parameter.  
The functions are written in Node.js and effectively do nothing but return a JSON response. See [`proxies.json`](proxies.json) for more information. Note. You will need to change the code in the proxy URL to match your Function App `_master` secret host key value
* Use of Node.js with Functions
* Use of Function Proxies

