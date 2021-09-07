# Python Development Process Overview

**For Windows**

Creating a plugin with Python involves five basic steps:

1. Write the code to create one or more Python modules that contain TestSteps, Instruments, DUTs, ResultListeners, and/or ComponentSettings.

2. Edit the `__init__.py` file to import the modules.

3. Build your plugin with `tap`, which creates a C# .dll file from your Python code. This enables OpenTAP to load your plugin.

4. Start OpenTAP and create a test plan using your new test steps and/or resources. Edit your Python modules and rebuild your plugin until development is complete.

5. Build the .TapPackage package, which allows the plugin to be distributed.

For details on these steps, see [Creating an OpenTAP Plugin with Python for Windows](./Creating a plugin with Python for Windows.md).

If you would like to build the examples first, see [Building the Python Examples for Windows](./Python Development Examples/Building the Python Examples for Windows.md).

For debugging steps, see [Debugging with Microsoft Visual Studio](./Debugging_with_Microsoft_Visual_Studio.md).

**For Ubuntu**

Creating a plugin with Python involves five basic steps:

1. Write the code to create one or more Python modules that contain TestSteps, Instruments, DUTs, ResultListeners, and/or ComponentSettings.

2. Edit the `__init__.py` file to import the modules.

3. Build your plugin with `tap`, which creates a C# .dll file from your Python code. This enables OpenTAP to load your plugin.

4. Edit the resources xml files located in Settings folder to reflect your new changes, and locate them accordingly in ~/.tap/Settings folder.

5. Edit the Test plan templates in PythonExampleTestPlans folder to include your new test steps. Edit your Python modules and rebuild your plugin until development is complete.

6. Build the .TapPackage package, which allows the plugin to be distributed.

For details on these steps, see [Creating an OpenTAP Plugin with Python for Ubuntu](./Creating a plugin with Python for Ubuntu.md).

If you would like to build the examples first, see [Building the Python Examples for Ubuntu](./Python Development Examples/Building the Python Examples for Ubuntu.md).