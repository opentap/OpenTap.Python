using System.Net.Sockets;
using Python.Runtime;

namespace OpenTap.Python;

/// <summary> A custom debug server emulating what pydebug can do. </summary>
class DebugServer
{
    public static DebugServer Instance { get; } = new ();
    
    public int Port { get; set; }

    TcpListener listener;
    TapThread thread;
    DebugServerClientHandler handler;
    
    private DebugServer(){}
    
    public void Start()
    { 
        listener = new TcpListener(Port);
        listener.Start();
        thread = TapThread.Start(AcceptClient);
    }

    void AcceptClient()
    {
        while (TapThread.Current.AbortToken.IsCancellationRequested == false)
        {
            var cli = listener.AcceptTcpClient();
            if (handler != null)
            {
                Log.CreateSource("Debug").Error("Only one debugging client can be connected at a time.");
                cli.Close();
                continue;
            }

            handler = new DebugServerClientHandler(cli);
            handler.Disconnected += () => handler = null;
        }
    }
    
    public int TraceCallback(PyObject arg1, Runtime.PyFrameObject arg2, Runtime.TraceWhat arg3, PyObject arg4)
    {
        // the active threads always needs to be updated.
        if(arg3 == Runtime.TraceWhat.Call)
            DebugServerClientHandler.GetThreadId(TapThread.Current);
        handler?.TraceCallback(arg1, arg2, arg3, arg4);
        return 0;
    }

    public void Stop()
    {
        listener.Stop();
        thread.Abort();
        thread = null;
        listener = null;
        handler = null;
    }
}