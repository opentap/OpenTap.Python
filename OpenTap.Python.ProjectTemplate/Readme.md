# OpenTAP / Python Project Template

This is a project template for building OpenTAP plugins with Python.

You can generally implement any kind of OpenTAP plugin with this, but naturally some things are simpler to build than others.

The easiest path is building test steps, instruments and result listeners.

A C# project has been included here for a few reasons:
1. It is an easy way to manage an OpenTAP installation
    
    To download and install the packages you need just run `dotnet build` to install everything into a bin folder. If you want to install additional packages, simply add them in the csproj file.

2. If you want to implement a plugin that can be called from a an external C# or Python plugin, you need to define the C# API
    
    If you define a new type of instrument and others want to use it, they can do it through your 'CSharpAPI'


## Getting Started
1. go to the .csproj file and uncomment a line to get an editor installed.

2. From a shell, enter the plugin folder and call.

      ```shell
      dotnet build
      ./bin/tap.exe editor # Start an editor
      # or ./bin/tap.exe tui
      # or ./bin/tap.exe editorx
      ```

## Building The TapPackage

Consider using git for managing your Python project. If you dont want to use git, edit package.xml, replaceing Version="$(GitVersion)" with e.g Version="0.1.0". 

From the root of the project folder:

```shell
bin/tap package create ./package.xml
```

This should create a package with you package name.

