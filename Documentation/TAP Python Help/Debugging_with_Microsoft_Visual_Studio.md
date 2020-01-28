# Debugging with Microsoft Visual Studio
Python development supports debugging through attaching the debugger to the OpenTAP executable process. In the following steps, remote debugging through attaching the debugger in Microsoft Visual Studio environment is shown.

1. Build Python Scripts (refer to Step 2 of [Creating an OpenTAP Plugin with Python for Windows](Creating a Plugin with Python for Windows.md)).

2. Launch OpenTAP.

3. Start up Microsoft Visual Studio, and select Debug, Attach to Process.

![](./Images/PythonDebug_AttachToProcess.png)

4. Click on Select, then select `Debug these code types:`, select Python and click OK.
 
![](./Images/PythonDebug_DebugTheseCodeTypes.png)

5. Select Editor.exe from the available process list and click attach.

![](./Images/PythonDebug_KeysightTapGuiExe.png)

6. If there is no error message shown, and the stop button is enabled, you can now feel free to debug the Python script.

![](./Images/PythonDebug_StopButtonEnabled.png)

