# Limitations

There are a few known limitations that currently applies to the Python OpenTAP integration. 

- It is not possible to add child test steps from the constructor of a Python test step. This is due to the object not being fully instanced at the time of modification. To get around this problem a 'Load' button can be added so that the steps can be loaded manually.
- Debugging is only known to work in Visual Studio PTVS (Python Tools for Visual Studio). In some threading scenarios debugging is not possible, but generally it is.
- Loading OpenTAP from a Python script is not currently supported. There are probably ways to get around this limitation.