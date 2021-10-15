# Python Development Setup for Ubuntu

This plugin supports Python versions 2.7, 3.6, 3.7, and 3.8. Please refer to the following steps for setting up the development environment for each Python version.

# Prerequisites

1. You will need a computer with Ubuntu 18.04 installed, administrative access to that machine, and an internet connection.

2. .NET Core 2.1 installed.

3. OpenTAP 9.15 or above installed.

# Python Setup

1. Make sure the versions are up-to-date before installing Python.

    `sudo apt update`

2. Create a directory for the environment variable required by the plugin.

    `sudo mkdir -p /usr/local/lib/pythonplugin`

3. Setup the environment variable.

    `echo 'LD_LIBRARY_PATH=$LD_LIBRARY_PATH:/usr/local/lib/pythonplugin' >> ~/.bashrc`

    `echo 'export LD_LIBRARY_PATH' >> ~/.bashrc`

## Development using Python 2.7

1. Install the Python 2.7 

    `sudo apt install python2.7-dev -y`

2. Setup the symbolic link of the Python 2.7 library for the plugin.

    ```bash
    sudo ln -s /usr/lib/python2.7/config-x86_64-linux-gnu/libpython2.7.so /usr/local/lib/pythonplugin/libpython2.7.so
    ```

## Development using Python 3.6

1. Install the Python 3.6

    `sudo apt install python3.6-dev -y`

    - *Remarks: Python 3.7 is required for the development using Python 3.6*

    `sudo apt install python3.7-dev -y`

2. Setup the symbolic link of the Python 3.6 and Python 3.7 libraries for the plugin.

    ```bash
    sudo ln -s /usr/lib/python3.6/config-3.6m-x86_64-linux-gnu/libpython3.6.so /usr/local/lib/pythonplugin/libpython3.6.so

    sudo ln -s /usr/lib/python3.7/config-3.7m-x86_64-linux-gnu/libpython3.7.so /usr/local/lib/pythonplugin/libpython3.7.so
    ```
## Development using Python 3.7

1. Install the Python 3.7

    `sudo apt install python3.7-dev -y`

2. Setup the symbolic link of the Python 3.7 library for the plugin.

    ```bash
    sudo ln -s /usr/lib/python3.7/config-3.7m-x86_64-linux-gnu/libpython3.7.so /usr/local/lib/pythonplugin/libpython3.7.so
    ```
## Development using Python 3.8

1. Install the Python 3.8

    `sudo apt install python3.8-dev -y`

2. Setup the symbolic link of the Python 3.8 library for the plugin.

    ```bash
    sudo ln -s /usr/lib/python3.8/config-3.8-x86_64-linux-gnu/libpython3.8.so /usr/local/lib/pythonplugin/libpython3.8.so
    ```

# After Python Installation

You can start using the python plugin with a new terminal session, or load the environment variable in the current terminal session with `source ~/.bashrc`.

Please refer to [Building the Python Examples for Ubuntu](./Python_Development_Examples/Building_the_Python_Examples_for_Ubuntu.md) for example of using the python plugin.

# Interchangeability of the Python versions

You can install multiple Python of different versions according to the steps abovementioned. However, you must set the version of the Python to be loaded by the plugin with the following command.

`tap python set-version <Version>`

The `<Version>` argument available is *2.7, 3.6, 3.7, or 3.8*. 