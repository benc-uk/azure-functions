import sys
import os
import json
sys.path.append(os.path.abspath(os.path.join(os.path.dirname( __file__ ), '..', 'env/Lib/site-packages')))
import requests


# Parameters...
API_KEY = os.environ['VISION_API_KEY']
API_ENDPOINT = "https://westus.api.cognitive.microsoft.com/vision/v1.0/describe"
LOGIC_APP_ENDPOINT = "https://prod-01.northeurope.logic.azure.com:443/workflows/7420133847ba422fa162794501e3c078/triggers/manual/run?api-version=2016-06-01&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=4nLf31aooFepxZ73DvX48ORpBUUjQ0ZJvZfuGGKAwxk"
EMAIL_TO = "benc.uk@gmail.com"

api_headers = {'Ocp-Apim-Subscription-Key': API_KEY, 'accept': 'application/json', 'content-type': 'application/octet-stream'}
email_headers = {'accept': 'application/json', 'content-type': 'application/json'}

# INPUT TRIGGER - Read the image file 
# Note. With the Python SDK we are passed the filename, so we need to read() it from the filesystem
img_data = open(os.environ['photoBlob'], mode='rb').read()
print("### Image content fetched from blob; {:,} bytes".format(len(img_data)))

print("### Calling cognative computer vision API. Uploading image...")
api_result = requests.post(API_ENDPOINT, headers=api_headers, data=img_data)

print("### API result: "+str(api_result.status_code))
if(api_result.status_code != 200):
  print("!!! API ERROR " + str(api_result.content))
  sys.exit("API error, exiting")

api_result_object = api_result.json()

desc = api_result_object['description']['captions'][0]['text']
tags = [tag.encode('utf-8') for tag in api_result_object['description']['tags']]

print("### I spy with my little eye: {0}".format(desc))

email_dict = {
    'email_to': EMAIL_TO,
    'email_subject': "Azure - image recognition results for: {0}".format(os.environ['photoBlob']),
    'email_body': "<h1 style='color:grey'>Azure Logic Apps and Functions Demo results</h1><h2>That photo looks like: {0}</h2><br/><img style='max-width:1024px' src='{1}'/><br/><h2>Photo tags:<br/>{2}</h2><br/> BYE!".format(desc, "", tags)
}

print("### Calling Azure Logic app for email sending stuff...")
email_result = requests.post(LOGIC_APP_ENDPOINT, headers=email_headers, data=json.dumps(email_dict, ensure_ascii=False))

print("### LOGIC APP result: "+str(email_result.status_code))
if(email_result.status_code != 200):
  print("!!! LOGIC APP ERROR " + str(email_result.content))
  sys.exit("LOGIC APP error, exiting")

print("### EMAIL SENT " + str(email_result.json()))
