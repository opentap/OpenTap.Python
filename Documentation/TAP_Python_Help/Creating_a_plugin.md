# Creating an OpenTAP plugin

Developing Python plugin projects can be done in two ways. 

1. Develop the plugin in an isolated 'project' folder. This is recommended for bigger projects.
2. Develop the plugin inside the 'Packages' folder. This way it's easy to get started and make something quick, but maintaining the project becomes harder in the long run.


### Developing the plugin in an insolated project folder.

This way of developing plugins makes it easier to do source control, manage dependencies and makes builds very reproducible. It also makes it simple to build the plugin using Continous Integration.


1. Setting up the project. 

   First find the location of your tap.exe application. This is usually found inside your OpenTAP installation folder. Here you need to have the Python plugin installed. Do so by running.

   ```tap package install Python```

   Now, you have access to the new-project command. This needs two pieces of information:
   - Where do you want your plugin to be located?
   - What do you want to call the project?

   Then you can call the new-project command:
   ```tap python new-project --project-name "TestProject" --directory /Users/user/code/work/py-test-project```

2. Selecting an editor and install OpenTAP.
   in the file named (in this case) './TestProject.Api/TestProject.Api.csproj' you can select which editor to use. To install the Editor application, change

   ```<!-- <OpenTapPackageReference Include="Editor"/> -->```
   
   to 
   
   ``` <OpenTapPackageReference Include="Editor"/>```

   In this file you can also add other package dependencies as needed.

   When this is done run `dotnet build` from the root of the project. Now you should have a `bin` folder containing `tap` and the editor you've selected.

   Now run `bin/Debug/Editor.exe` (if you've selected the Editor package) to load the editor application.

3. Build a package file
   
   Let's say that you've developed your plugin and want to share it with the world. This is simple. From the root of the project, invoke:
   ```sh
   bin/tap package create package.xml
   ```
   This will compile all files in your python module and insert it into a TapPackage file in the right location.

   If you want to add more files to the package, you can open the package.xml file and modify according to your needs. For more information on this check out the [doc.opentap.io](OpenTAP Developer Guide).
   






## Develop the plugin inside the Packages folder.

This way has been used since version 2 of the Python plugin. It

Python development is done in modules (.py files) that reside in a Python package folder.

Each module contains the code for one or more OpenTAP plugins.

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
