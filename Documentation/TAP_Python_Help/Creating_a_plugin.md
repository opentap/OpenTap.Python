# Creating an OpenTAP plugin
Python development is done in modules (.py files) that reside in a Python package folder. 
Each module contains the code for one or more OpenTAP plugins.

## Creating a Basic Plugin

In the following steps, *PythonExample* refers to your project folder. Follow these steps to create a Python plugin:

1. Create Python modules for your plugins. To start from scratch, develop your code in a folder within **%TAP_PATH%\\Packages\\**. To begin with the Python package build:

    a. Go to $TAP_PATH/Packages/ and make a new folder with the desired plugin name. This has to be a valid Python module name.

2. Add a python file to your folder containing your code. For example:
   ```py
      # Packages/MyPythonProject/step1.py
      import opentap
      class Step1(opentap.TestStep):
         def __init__(self):
            super().__init__()
         def Run(self):
            super().Run()
            self.log.Debug("Step1 Executed")
   ```
3. Test your plugin in OpenTAP and modify your code as needed. Rebuild the plugin if you add new types or properties to the plugin types.

4. When the package is complete and ready for distribution, you can create a .TapPackage file. 

   To do this, you need to add a package.xml file to the project. 
   For an example of this see the [package.xml](https://raw.githubusercontent.com/opentap/OpenTap.Python/dev/OpenTap.Python.Examples/package.xml) used to build the PythonExamples project.

   When you have the package.xml run:

   ```
   tap package create "Packages/MyPythonProject/package.xml
   ```
   This should result in a TapPackage file containing your python files, ready for distribution.

Your plugin package is complete and ready for distribution. When viewed in OpenTAP Package Manager, users will be able to see and install your package.

![](./Images/python_package_in_tap.png)

## Adding Pip Package Dependencies

The Python ecosystem has it's own package system for managing packages that you might need in your project.
By adding a requirements file, you can let the OpenTAP package system know that you need some pip packages installed
as well as your OpenTap package.

For example numpy is a popular package for data processing. 

Let's say we want to make sure that numpy is installed when our TapPackage is installed.

1. First create a ```requirements.txt``` inside your plugin project. This could look like this
   ```
      numpy>1.23
   ```
2. Add the requirements.txt file to your package.xml file:
   ``` 
   <!-- ... -->
   <File Path="Packages/PythonExamples/requirements.txt">
     <PythonRequirements/> <!-- this defines pip dependencies -->
   </File>
   <!-- ... -->
   ```
3. Build your package as usual. 

When you install your package, the Pip dependencies will now also be installed in your python path.

## Projects Outside the OpenTAP Installation

It is possible to create projects outside the OpenTAP installation folder, although it can give issues with packaging and project discovery.

First, it is necessesary to add an additional search path in the Python settings. This can be done through the settings or by using a command line action:

```sh
# add a python-projects folder to the search paths.
# inside python-projects there can be a number of modules each defining a plugin module.
tap python search-path --add /home/user/code/python-projects/
```

After doing this, OpenTAP should be able to locate the plugins defined inside that folder structure. 
The folder structure can contain many sub-folders with projects.

This will cause a problem when you want to create a TapPackage file. TapPackage files tries to install the plugin into the OpenTAP installation folder structure and in theory this is not a problem. 
The only problem is that your added search-path projects does not map directly to the way projects are set up inside the OpenTAP installation folder structure.
This can be fixed by using the `<ProjectFile/>` element in your package XML file. Take for example this package XML:
```xml
<?xml version="1.0" encoding="UTF-8"?>
<Package Name="python-test1" xmlns="http://keysight.com/Schemas/tap" Version="$(GitVersion)" OS="Windows,Linux,MacOS">
    <Description>Description of your package.</Description>
    <Dependencies>
        <PackageDependency Package="OpenTAP" Version="^9.18.2" />
        <PackageDependency Package="Python" Version="^3.0.0-beta" />
    </Dependencies>
    <Files>
        <File Path="python-test1/*.py">
             <ProjectFile/>  <!-- these files should be moved into Packages/python-test1/*.py -->
        </File>
        <File Path="requirements.txt">
            <PythonRequirements/>
           <!-- This file should be moved to Packages/python-test1/requirements.txt -->
           <ProjectFile/>
        </File>
    </Files>
</Package>
```
`ProjectFile` does either:

1. If the file name starts with the project name, prepend `Packages/`.
2. If the file name does not start with the project name, prepend `Packages/[project name]`.

If this is not wanted, simply omit the ProjectFile element.

So, the python-projects folder might look something like this:
```
/home/user/code/python-projects/
                           plugin1/
                              step.py
                              step2.py
                              package.xml
                              requirements.txt
                           plugin2/
                              instrument.py
                              package.xml
                              requirements.txt
```

However, when plugin1.TapPackage and plugin2.TapPackage, these will get deployed into the OpenTAP folder as such:

```
/opentap-installation/
    Dependencies/...
    Packages/
       plugin1/
           step.py
           step2.py
           package.xml
           requirements.txt
       plugin2/
           instrument.py
           package.xml
           requirements.txt
       ....
    SessionLogs/...
    Settings/...
    tap.exe
    OpenTap.dll
    ...
```

In either scenario, OpenTAP will be able to find them due to their location with respect to the search path.

*Warning:* If you set the search path and install the TapPackage plugin in the same installation it is undefined what happens. 
It is recommended to remove the search path when installing the TapPackage for a given Python-based plugin.
