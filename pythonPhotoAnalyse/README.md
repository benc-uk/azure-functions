## Python Setup

These steps are required to set-up a python Function with custom modules & extensions.
Info taken from [this blog](https://michael-mckenna.com/how-to-import-python-extension-modules-in-azure-functions/) and duplicated here

**Note:** In Azure we're going to install all these modules in a virtual environment so we've got a nice isolated area with all our packages. 
So Python knows to import our libraries from this location make sure you have 
`sys.path.append(os.path.abspath(os.path.join(os.path.dirname( __file__ ), '..', 'env/Lib/site-packages')))` in any file that imports these libraries.

### Setup Steps

1. Open up kudu by clicking the "Go to Kudu" button under "Function app settings". Navigate to your function folder

        cd D:\home\site\wwwroot

2. Set up your python virtual environment, the python executable is just installed in the normal place. Here "python-env" is the new directory that will be created in your wwwroot
         
         D:\Python27\Scripts\virtualenv.exe python-env

3. Activate your virtual environment 
       
        "python-env/Scripts/activate.bat"
        
4. Install modules using pip, e.g. install requests
 
        pip install requests
        
5. Exit Kudu
