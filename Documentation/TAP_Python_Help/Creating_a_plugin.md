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
         def Run(self);
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
