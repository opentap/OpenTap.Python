__Summary__

Python is a very popular programming language in the test and automation space. The Python plugin makes it possible to use Python and OpenTAP together, allowing the full OpenTAP API to be used directly from Python.

The plugin supports Python version >3.7. Read more about Python at www.python.org.

__More Information__
- Documentation can be found at https://github.com/opentap/opentap.python/
- Example code is located in ```%TAP_PATH%\Packages\Python\MyExamplePlugin\```

__Developers__
- Lim Jing Huey
- Kyler Lee
- Gordon Ong
- Jingwei Liang
- Joseph Hoff 
- Navjodh Dhillon
- Rolf Madsen (*Maintainer* @rmadsen-ks, rolf_madsen@keysight.com)

__ A note regarding Python.Net __
We are shipping a custom built version of Python.Net with the plugin. This is the file py_deploy.bin embedded in the plugin dll. The source code for this version of Python.Net can be found at [https://github.com/opentap/opentap.python](https://github.com/opentap/opentap.python).
The py_deploy.bin file is just a zip file with the a directory corresponding to each build of PythonNet.