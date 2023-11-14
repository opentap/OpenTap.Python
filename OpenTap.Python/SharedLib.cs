using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace OpenTap.Python;

class SharedLib
{
    public static bool IsWin32 => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public static bool IsMacOs => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    static IntPtr load(string name)
    {
        if (IsWin32)
            return LoadLibrary(name);
        return libdl.dlopen(name, rtld_now);
    }

    void close(IntPtr ptr)
    {
        if (IsWin32)
        {
            // this is not possible on win32.
        }
        else
        {
            libdl.dlclose(ptr);
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
            var error = libdl.dlerror();
            if(error != null)
                throw new Exception("Unable to load python: " + error.ToString());
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
            libdl.dlerror();
        }
    }


    [DllImport("kernel32.dll")]
    static extern IntPtr LoadLibrary(string dllToLoad);

    const int rtld_now = 2;

    interface ILibDL
    {
        IntPtr dlopen(string filename, int flags);
        int dlclose(IntPtr handle);
        string dlerror();
    }

    class libdl1 : ILibDL
    {
        [DllImport("libdl.so")]
        static extern IntPtr dlopen(string fileName, int flags);

        [DllImport("libdl.so")]
        static extern int dlclose(IntPtr handle);

        [DllImport("libdl.so")]

        static extern string dlerror();

        IntPtr ILibDL.dlopen(string fileName, int flags) => dlopen(fileName, flags);
        int ILibDL.dlclose(IntPtr handle) => dlclose(handle);
        string ILibDL.dlerror() => dlerror();

    }

    class libdl2 : ILibDL
    {
        [DllImport("libdl.so.2")]
        static extern IntPtr dlopen(string fileName, int flags);
        
        [DllImport("libdl.so.2")]
        static extern int dlclose(IntPtr handle);

        [DllImport("libdl.so.2")]
        static extern string dlerror();
        
        IntPtr ILibDL.dlopen(string fileName, int flags) => dlopen(fileName, flags);
        int ILibDL.dlclose(IntPtr handle) => dlclose(handle);
        string ILibDL.dlerror() => dlerror();
    }

    static readonly ILibDL libdl;

    static SharedLib()
    {
        if (!IsWin32)
        {
            try
            {
                libdl = new libdl2();
                // call dlerror to ensure library is resolved
                libdl.dlerror();
            }
            catch (DllNotFoundException)
            {
                libdl = new libdl1();
            }
        }
    }
    
    IntPtr lib = IntPtr.Zero;
    public SharedLib(IntPtr ptr)
    {
        lib = ptr;
    }
    
    [DllImport("kernel32", CharSet = CharSet.Ansi, SetLastError = true)]
    static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
    [DllImport("libdl.so", CharSet = CharSet.Ansi)]
    static extern IntPtr dlsym(IntPtr handle, string symbol);
    
    public IntPtr GetSymbol(string symbolName)
    {
        if (lib == IntPtr.Zero)
            throw new InvalidOperationException("Library handle is null.");
        
        if (IsWin32)
            return GetProcAddress(lib, symbolName);
        
        return dlsym(lib, symbolName);
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