using System.IO;
using System.Linq;
using System.Net.Sockets;

namespace OpenTap.Python;

/// <summary> This 'fake' debug server sits between pydebug and vs code, recording all the events that takes place.
/// This is used for reverse-engineering and developing the DebugServer.
/// </summary>
class FakeDebugServer
{
    public static FakeDebugServer Instance { get; } = new();
    
    public int Port { get; set; }
    public int Port2 { get; set; }

    static readonly TraceSource log = Log.CreateSource("FakeDebug");
    TcpListener listener;
    TapThread thread;
    public void Start()
    { 
        log.Debug("Fake debugger on port {0}", Port);
        listener = new TcpListener(Port);
        listener.Start();
        thread = TapThread.Start(AcceptClient);
    }

    void AcceptClient()
    {
        while (TapThread.Current.AbortToken.IsCancellationRequested == false)
        {
            var cli = listener.AcceptTcpClient();
            TapThread.Start(() => HandleClient(cli));
        }
    }
    
    public void HandleClient(TcpClient client)
    {
        var file1 = "session." + Port + ".txt";
        var file2 = "session." + Port2 + ".txt";
        File.Delete(file1);
        File.Delete(file2);
        using (var fstr1 = File.OpenWrite(file1))
        using (var fstr2 = File.OpenWrite(file2))
        using (var cli2 = new TcpClient())
        {
            
            cli2.Connect("localhost", Port2);

            var str1 = client.GetStream();
            var str2 = cli2.GetStream();
            TapThread.Start(() =>
            {
                while (client.Connected)
                {
                    TapThread.ThrowIfAborted();
                    byte[] buffer = new byte[500];
                    str1.ReadTimeout = 500;
                    try
                    {
                        if (str1.DataAvailable == false)
                        {
                            TapThread.Sleep(50);
                            continue;
                        }

                        int read = str1.Read(buffer, 0, buffer.Length);
                        if (read == -1)
                            break;

                        if (read > -1)
                        {
                            fstr1.Write(buffer, 0, read);
                            fstr1.Flush();
                            try
                            {
                                log.Info("<<< {0}",
                                    string.Join(" ", System.Text.Encoding.UTF8.GetString(buffer.Take(read).ToArray())));
                            }
                            catch
                            {

                            }

                            str2.Write(buffer, 0, read);
                        }
                    }
                    catch (IOException)
                    {
                        // continue;
                    }
                }
            });

            while (client.Connected)
            {
                TapThread.ThrowIfAborted();
                byte[] buffer = new byte[500];
                str2.ReadTimeout = 50;
                try
                {
                    if (str2.DataAvailable == false)
                    {
                        TapThread.Sleep(50);
                        continue;
                    }
                    int read = str2.Read(buffer, 0, buffer.Length);
                    if (read == -1)
                        break;
                    if (read > -1)
                    {
                        try
                        {
                            log.Info(">>> {0}", System.Text.Encoding.UTF8.GetString(buffer.Take(read).ToArray()));
                        }
                        catch
                        {

                        }

                        str1.Write(buffer, 0, read);
                        fstr2.Write(buffer, 0, read);
                        fstr2.Flush();
                    }
                }
                catch (IOException)
                {
                    
                }
            }
            
        }
    }
    
    public void Stop()
    {
        listener.Stop();
    }

}