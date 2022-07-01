using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace OpenTap.Python;

class SharedLib
{
    public static bool IsWin32 => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    static IntPtr load(string name)
    {
        if (IsWin32)
            return LoadLibrary(name);
        return dlopen(name, rtld_now);
    }

    void close(IntPtr ptr)
    {
        if (IsWin32)
        {
            // this is not possible on win32.
        }
        else
        {
            dlclose(ptr);
        }
    }
            

    static string checkError()
    {
        if (IsWin32)
        {
            var err = Marshal.GetLastWin32Error();
            if (err != 0)
            {
                return new Win32Exception(err).ToString();
            }
        }
        else
        {
            var error = dlerror();
            if(error != null)
                return "Unable to load python: " + error.ToString();
        }

        return null;
    }

    static void clearError()
    {
        if (IsWin32)
        {
                    
        }
        else
        {
            dlerror();
        }
    }


    [DllImport("kernel32.dll")]
    static extern IntPtr LoadLibrary(string dllToLoad);

    const int rtld_now = 2;
    [DllImport("libdl.so")]
    static extern IntPtr dlopen(string fileName, int flags);
        
    [DllImport("libdl.so")]
    static extern int dlclose(IntPtr handle);

    [DllImport("libdl.so")]

    static extern string dlerror();

    IntPtr lib = IntPtr.Zero;

    public SharedLib(IntPtr ptr)
    {
        lib = ptr;
    }

    public static SharedLib Load(string name)
    {
        clearError();
        IntPtr p = load(name);
        if (p == IntPtr.Zero)
        {
            checkError();
            return null;
        }
        return new SharedLib(p);
    }

    public void Unload()
    {
        if (lib != IntPtr.Zero)
        {
            close(lib);
            lib = IntPtr.Zero;
        }
    }
}