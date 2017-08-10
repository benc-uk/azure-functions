## Python Setup

These steps are required to set-up a python Function with a custom runtime version and libraries installable via pip.
Info taken from [this excellent blog](https://prmadi.com/running-python-code-on-azure-functions-app/) and duplicated here

### Setup Steps

1. Open up the Kudu console, from your Function App in the Azure Portal, click *Platform Features* -> *Console* or go to `https://<your_function_app_name>.scm.azurewebsites.net/DebugConsole` and run the following command. It might take several minutes to complete.

        nuget.exe install -Source https://www.siteextensions.net/api/v2/ -OutputDirectory D:\home\site\tools python352x64  
        
2. Move the output to the tools directory with the following command
         
         mv /d/home/site/tools/python352x64.3.5.2.6/content/python35/* /d/home/site/tools/

3. From the same console window you can now use pip to install libraries, e.g.
       
        D:\home\site\tools\python.exe -m pip install requests  
        
For this example function to work, install the following libraries `requests`, `python_http_client` and `sendgrid`
