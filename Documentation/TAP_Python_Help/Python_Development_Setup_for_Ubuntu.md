# Python Development Setup for Ubuntu

Ubuntu ships with both Python 3 and Python 2 pre-installed. There are a few more packages and development tools to install to ensure that we have a robust set-up for the Python 
development environment: 

# Prerequisites
1. You will need a computer with Ubuntu installed, as well as have administrative access to that machine and an internet connection.

2. Keysight Floating License needs to be installed in Ubuntu.

# Dotnet core and Python Setup
1. To make sure that our versions are up-to-date, let’s update and upgrade the system with apt-get:

    `sudo apt-get update`
    
    `sudo apt-get -y upgrade`
    
2. Setup Dotnet core 2.1.105

    2.1. Add the keys:
    
    `sudo apt-key adv --keyserver packages.microsoft.com --recv-keys EB3E94ADBE1229CF`
         
    `sudo apt-key adv --keyserver packages.microsoft.com --recv-keys 52E16F86FEE04B979B07E28DB02C46DF417A0893`
         
    2.2. Install dotnet core
    
    `sudo apt-get install dotnet-sdk-2.1.105` (if there is unmet dependencies error, we can run "sudo apt --fix-broken install")
         
    2.3. To verify successful installation, in a new folder, create a new console project with:
    
    `dotnet new console`
         
    `dotnet run`
         
    return: `Hello World!` In the terminal

3. Install Python

    3.1. For **Python 2.7**:
    
    `sudo apt install python-dev`
         
    `sudo apt install python2.7 python-pip`

    3.2. For **Python 3.6**:
    
    `sudo apt install python3.6-dev`
         
    `sudo apt install python3.6 python3-pip`
         
 	3.2.1 To verify that the symbolic link of python is point to python3.6
         
    `sudo ln -s /usr/bin/python3.6 /usr/bin/python`
        
    3.2.2 Setup environment variable. 
         
    `sudo mkdir -p /usr/local/lib/pythonplugin`
    
    `sudo ln -s /usr/lib/python3.6/config-3.6m-x86_64-linux-gnu/libpython3.6.so /usr/local/lib/pythonplugin/libpython3.6.so`
         
    The following lines are to be added into .bashrc:
         
    `LD_LIBRARY_PATH=$LD_LIBRARY_PATH:/usr/local/lib/pythonplugin`
         
    `export LD_LIBRARY_PATH`


	***Troubleshooting Tips***

	In the scenario of the following error happens, please install python 3.7, and create a symbolic link.
	
	`Error: System.DllNotFoundException: Unable to load shared library 'python3.7' or one of its dependencies.`

	Install Python 3.7 and symbolic link creation:

	`sudo apt install python3.7-dev`

	`sudo ln -s /usr/lib/python3.7/config-3.7m-x86_64-linux-gnu/libpython3.7.so /usr/local/lib/pythonplugin/libpython3.7.so`
         
    3.3. For **Python 3.7**:
    
    `sudo apt install python3.7-dev`

	`sudo apt install python3.7 python3-pip`

	3.3.1 To verify that the symbolic link of python is point to python3.7
         
    `sudo ln -s /usr/bin/python3.7 /usr/bin/python`
        
    3.3.2 Setup environment variable. 
         
    `sudo mkdir -p /usr/local/lib/pythonplugin`
    
    `sudo ln -s /usr/lib/python3.7/config-3.7m-x86_64-linux-gnu/libpython3.7.so /usr/local/lib/pythonplugin/libpython3.7.so`
         
    The following lines are to be added into .bashrc:
         
    `LD_LIBRARY_PATH=$LD_LIBRARY_PATH:/usr/local/lib/pythonplugin`
         
    `export LD_LIBRARY_PATH`
    
    3.4 For installing multiple Python versions:
    
    Steps in 3.1, 3.2 and 3.3 are repeated for the all Python versions. The following additional steps to setup Python version's interchangeability.
    
    3.4.1 Setup symbolic link for Python 2.7
    
    `sudo ln -s /usr/lib/python2.7/config-2.7m-x86_64-linux-gnu/libpython2.7.so /usr/local/lib/pythonplugin/libpython2.7.so`
            