# Release Note - Version 3.0

Version 3.0 is a breaking release. This means that what was previously supported might not be supported in versions 3.0 and newer.

## New Features

- Python future compatibility. This version will support Python 3.7 and all newer / future versions.
- It is no longer necessary to 'build' the python modules before using them.
- Every .NET type can be inherited from and every plugin type can be created.
- It now works on Mac OS.

## Breaking Changes

- Python 2.X is no longer supported.
- Python versions less or equal to 3.6 are also not supported.

That means, at the time of writing Python 3.7, 3.8, 3.9, 3.10 and 3.11 are supported, but the new Python Plugin is future compatible, so 3.12 and onwards are expected to be supported as well.

- It is no longer possible to build a C# DLL.
