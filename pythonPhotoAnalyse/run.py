import sys
import os
import json
import requests
import sendgrid
from sendgrid.helpers.mail import *

# Parameters...
API_KEY = os.environ['VISION_API_KEY']
API_ENDPOINT = "https://westeurope.api.cognitive.microsoft.com/vision/v1.0/describe"

# INPUT TRIGGER - Read the image file 
# Note. With the Python SDK we are passed the filename, so we need to read() it from the filesystem
img_data = open(os.environ['photoBlob'], mode='rb').read()
print(os.environ['photoBlob'])
print("### Image content fetched from blob; {:,} bytes".format(len(img_data)))

# Call Cognitive Service Vision API
print("### Calling cognative computer vision API. Uploading image...")
api_result = requests.post(API_ENDPOINT, headers={'Ocp-Apim-Subscription-Key': API_KEY, 'accept': 'application/json', 'content-type': 'application/octet-stream'}, data=img_data)

print("### API result: "+str(api_result.status_code))
if(api_result.status_code != 200):
  print("!!! API ERROR " + str(api_result.content))
  sys.exit("API error, exiting")

# Grab API results
api_result_object = api_result.json()

desc = api_result_object['description']['captions'][0]['text']
tags = [tag.encode('utf-8') for tag in api_result_object['description']['tags']]

print("### I spy with my little eye: {0}".format(desc))

# Send email using SendGrid
sg = sendgrid.SendGridAPIClient(apikey=os.environ.get('my-sendgrid-key'))
from_email = Email("changeme@gmail.com")
to_email = Email("changeme@gmail.com")
subject = "Azure Functions Python demo result"
content = Content("text/html", "<h1 style='color:grey'>Azure Functions with Python &amp; Cognitive Services Demo Results</h1><h2>That photo looks like: {0}</h2><br/><img style='max-width:1024px' src='{1}'/><br/><h2>Photo tags:<br/>{2}</h2><br/> BYE!".format(desc, "", tags))
mail = Mail(from_email, subject, to_email, content)

print("### Sending email via SendGrid...")
response = sg.client.mail.send.post(request_body=mail.get())
print("### SendGrid response: {0}".format(response.status_code))
