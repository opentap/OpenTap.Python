# Release Note - Version 3.0

Version 3.0 is a breaking release. This means that what was previously supported might not be supported in versions 3.0 and newer.

## New Features

- Newest Python version compatibility. This version will support Python 3.7-3.11.
- It is no longer necessary to 'build' the python modules before using them.
- Every .NET type can be inherited from and every plugin type can be created.
- Added support for Mac OS.
- Added support for Arm64 architectures.
- Added support for debugging on Linux and MacOS (see *Known Issues*).
- Python projects can now be added in the Packages folder and does not need an ```__init__``` file for classes to be discovered.
- Pip support when Python packages are installed.
- 
## Breaking Changes

- Python 2.X is no longer supported.
- Python versions less or equal to 3.6 are also not supported.

That means, at the time of writing Python 3.7, 3.8, 3.9, 3.10 and 3.11 are supported, but the new Python Plugin is future compatible, so 3.12 and onwards are expected to be supported as well.

- It is no longer possible to build a C# DLL containing a C# API for the Python code.

## Known Issues

- Debugging using 'Remote Attach' in Visual Studio Code or PyCharm has limited support due to threading issues.
  It might needed to run opentap.debug_this_thread() in code where debugging is needed.