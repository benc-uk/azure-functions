# Demo & Examples of Azure Functions

This is a small repo of my demo and sample Azure Functions. They are either demos or fulfil a basic usecase

### analysePhotosBlob
This reads a photo from blob storage, then analyses the image with the Azure Cognitive APIs (computer vision) then sends the results as an email, through a Logic App. The results will contain what the computer vision service thinks the photo is of. Demonstrates:
* Triggering from blob storage
* Integration with a REST API
* Integration with Logic Apps via REST

### serviceBusDemo
This function is bound to a Service Bus queue, any messages posted on the queue are read, deseralized as JSON, then pushed as output to blob storage
* Triggering from Service Bus queue
* Output to blobs

### azureHealthOMS
An integration use case that reads from the Azure Resource Health API and pushes data over to OMS Log Analytics, so that it's avaiable to query and report/alert on
JSON transformation and REST API calls are used
* Integration with a REST API
* JSON manipulation
* Azure API authentication (fetching token for Azure AD)
* Creating HMAC-SHA256 signed headers
