__Summary__

Python is a very popular programming language in test and automation. The Python plugin makes it possible to use Python and TAP together, allowing the full TAP SDK to be used directly from Python.

The plugin is called Python because it interacts with the most common Python implementation also known as "Python", more specifically version 2.7 (3.6 support coming later). Read more about Python at www.python.org. Other Python implementations also exist, but these are not supported by this plugin.

__More Information__
- [The Wiki] (http://gitlab.it.keysight.com/tap-plugins/CPython/wikis/home)
- Example code is located in ```%TAP_PATH%\Packages\Python\MyExamplePlugin\```

__Developers__
- Lim Jing Huey
- Kyler Lee
- Gordon Ong
- Jingwei Liang
- Joseph Hoff 
- Navjodh Dhillon
- Rolf Madsen (*Maintainer* @romadsen-ks, rolf_madsen@keysight.com)

__ A note regarding Python.Net __
We are shipping a custom built version of Python.Net with the plugin. This is the file py_deploy.bin embedded in the plugin dll. The source code for this version of Python.Net can be found at [https://github.com/rmadsen-ks/pythonnet](github).
The py_deploy.bin file is just a zip file with the a directory corresponding to each build of PythonNet.