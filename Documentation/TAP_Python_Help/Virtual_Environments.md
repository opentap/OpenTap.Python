# Virtual Environments

A virtual environment is a folder which overlays the Python system installation with different packages and tools. 
This can be useful in order to isolate a specific environment and can be thought of as a Python-specific container.

For this to work with OpenTap.Python, you need to take a few extra steps.

1. tap python set-path <path-to-virtual-environment>
2. tap python set-library-path <path-to-your-libpython-library>

The libpython library file should match the python version needed in your virtual environment, otherwise you might get strange results.