using OpenTap;
using Python.Runtime;
using System;
using System.Reflection;
using System.Threading;

namespace Keysight.OpenTap.Plugins.Python
{
    /// <summary>
    /// Class for ensuring most calls to python are made from the same thread.
    /// This is essential for debuggers to work as they assume this is the situation.
    /// </summary>
    static class PyThread
    {
        static Mutex mutex = new Mutex();
        static ManualResetEvent evt = new ManualResetEvent(false);
        static ManualResetEvent evt2 = new ManualResetEvent(false);
        static Action act;
        static Thread pyThread;
        public static bool PyInitialized;
        public static bool IsWin32 = Environment.OSVersion.Platform.ToString().Contains("Win");

        static void pyThreadMain(Action startup)
        {
            startup();
            while (true)
            {
                evt.WaitOne();
                using (var gil = Py.GIL())
                        act();
                act = null;
                evt.Reset();
                evt2.Set();
            }
        }
        internal static void Start(Action f)
        {
            if (pyThread == null)
            {
                pyThread = new Thread(() => pyThreadMain(f)) { IsBackground = true, Name = "pythread" };
                if (IsWin32)
                    pyThread.SetApartmentState(ApartmentState.STA);

                pyThread.Start();
            }
            else
            {
                Invoke(f);
            }
        }

        static class TapThreadHack
        {
            // Since test plan aborts are based on the parent thread and thread local variables.
            // and since python code needs to be called from the same thread.
            // we need to fake a parent thread while the action runs inside the python thread.
            // however, those APIs are not public in OpenTAP, so we need to hack it for now.
            // otherwise test plan abort wont work.

            static FieldInfo threadKey;
            static ConstructorInfo tapThreadCtor;
            static TapThreadHack()
            {
                tapThreadCtor = typeof(TapThread).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0];
                threadKey = PluginManager.LocateType("OpenTap.ThreadManager").GetField("ThreadKey", BindingFlags.Static | BindingFlags.NonPublic);
            }

            public static IDisposable WithTemporaryParentThread(TapThread newParent)
            {
                var reset = new ParentThreadReset(TapThread.Current);
                threadKey.SetValue(null, newParent);
                return reset;
            }

            class ParentThreadReset : IDisposable
            {
                TapThread previousParent;
                public ParentThreadReset(TapThread previous)
                {
                    previousParent = previous;
                }
                public void Dispose()
                {
                    threadKey.SetValue(null, previousParent);
                }
            }
        }

        /// <summary> Invokes the action f in the python thread. If quick is specified it means it will only try to get the thread otherwise it will just use GIL. In this case, it should only be used for simple python calls. </summary>
        static public void Invoke(Action f, bool quick = false)
        {
            if (!PyInitialized)
            {
                throw new Exception("Python is not initialized");
            }
            if (Thread.CurrentThread == pyThread)
            {
                using (var gil = Py.GIL())
                    f();
                return;
            }
            if (false == PythonSettings.Current.DebugThreadingMode || quick)
            {
                
                using (var gil = Py.GIL())
                  f(); // just go ahead, we dont want to wait for this.
                return;
            }
            if (!mutex.WaitOne(0))
            {
                using (var gil = Py.GIL())
                    f(); // just go ahead, we dont want to wait for this.
                 return;
            }
            else { } // we got the mutex.
            try
            {
                var tapThread = TapThread.Current;
                Exception ex = null;
                act = () =>
                {
                    try
                    {
                        using (TapThreadHack.WithTemporaryParentThread(tapThread))
                            f();
                    }catch(Exception e)
                    {
                        ex = e;
                    }
                };
                evt.Set(); // signal we can start.
                evt2.WaitOne();
                evt2.Reset(); // the  next one needs to wait as well.
                if (ex != null)
                    throw ex;
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }
    }
}
