using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using Python.Runtime;

namespace OpenTap.Python;

/// <summary>
/// This class emulated the behavior of the pydebug server, but it is written entirely in C#.
///
/// This implements the Debug Adapter Protocol also described here:  https://microsoft.github.io/debug-adapter-protocol/specification
/// </summary>
class DebugServerClientHandler
{
    record BreakStackFrames(StackFrame[] stackFrames, int totalFrames, int threadId);
    record StackFrame(int id, string name, int line, int column, object source);
    class NextLine
    {
        public int ThreadId { get; set; }
        public int CurrentLine { get; set; }
        public string Name { get; set; }
        public int BreakLevel { get; set; }
        public string Mode { get; set; }
    }
    
    static readonly object successObject = new ();
    static int threadCounter = 0;
    static readonly ConditionalWeakTable<TapThread, object> knownThreads = new();
    static readonly List<WeakReference<TapThread>> threads = new();
    
    // a running number for marking each response.
    int responseSeq;
    
    readonly ConcurrentQueue<(string, object)> events = new ();
    readonly TcpClient client;
    readonly StreamReader reader;
    readonly StreamWriter writer;
    readonly object clientLock = new ();
    readonly Dictionary<string, HashSet<int>> breakPoints = new ();
    
    // incremented when execution is waiting for continuing. When it is non-zero,
    // it means that we are waiting to continue.
    int isWaiting;
    object debugState;
    
    // These values are set when a breakpoint is hit, otherwise they wont
    // have valid state.
    BreakStackFrames breakStackFrames = null;
    ConcurrentQueue<Action> processor;
    CancellationToken breakCancel;
    Dictionary<int, PyObject> ScopeVariableReferences;
    Dictionary<(PyObject, PyObject), int> ScopeVariableReferenceLookup;
    
    readonly ConcurrentDictionary<string, int> sourceReference = new();
    
    public DebugServerClientHandler(TcpClient client)
    {
        this.client = client;
        var stream = client.GetStream();
        reader = new StreamReader(stream);
        writer = new StreamWriter(stream);
        TapThread.Start(ProcessRequests);
        TapThread.Start(ProcessEvents);
    }

    void SendMessage(object obj)
    {
        var retstr = JsonSerializer.Serialize(obj);
        var enc = $"Content-Length: {retstr.Length}\r\n\r\n";
        var content = enc + retstr;
        writer.Write(content);  
        writer.Flush();
    }

    JsonDocument ReadMessage()
    {
        int contentLength = 0;
        while (true)
        {
            var line = reader.ReadLine();
            if (line == null) break;
            var s = line.Split(':').Select(x => x.Trim()).ToArray();
            if (s.Length > 1)
            {
                if (s[0] == "Content-Length")
                {
                    contentLength = int.Parse(s[1]);
                }
                continue;
            }

            if (line == "")
            {
                char[] bytes = new char[contentLength];
                reader.Read(bytes, 0, contentLength);
                int reqSeq = -1;
                string type = null;
                
                var js = JsonDocument.Parse(bytes);

                if (js.RootElement.TryGetProperty("type", out var reqElem))
                    type = reqElem.GetString();

                if (js.RootElement.TryGetProperty("seq", out var seqElem))
                    reqSeq = seqElem.GetInt32();

                if (type != "request")
                    throw new Exception("Invalid type of message.");
                if (reqSeq == -1)
                    throw new Exception("Invalid sequence number");
                return js;
            }
        }

        return null;
    }

    void ProcessEvents()
    {
        while (client.Connected)
        {
            TapThread.ThrowIfAborted();
            if (events.TryDequeue(out var r))
            {
                lock (clientLock)
                {
                    try
                    {
                        var evt = WrapEvent(r.Item1, r.Item2);
                        SendMessage(evt);
                    }
                    catch
                    {
                        // lost event
                    }
                }
            }
            else
            {
                TapThread.Sleep(100);
            }
        }
    }

    void ProcessRequests()
    {
        try
        {
            while (client.Connected)
            {
                TapThread.ThrowIfAborted();
                var msg = ReadMessage();
                if (msg == null) continue;
                var command = msg.RootElement.GetProperty("command").GetString();
                var reqseq = msg.RootElement.GetProperty("seq").GetInt32();
                lock (clientLock)
                {
                    msg.RootElement.TryGetProperty("arguments", out var arguments);
                    try
                    {
                        var responseBody = ProcessCommand(command, arguments);
                        if (responseBody != null)
                            SendMessage(WrapResponse(command, responseBody, reqseq));
                    }
                    catch
                    {
                        // lost event.
                    }
                }
            }
        }
        finally
        {
            Disconnected?.Invoke();
        }
    }
    
    object WrapEvent(string eventName, object body)
    {
        if (body == null || body == successObject)
        {
            return new
            {
                @event = eventName,
                type = "event",
                seq = Interlocked.Increment(ref responseSeq)
            };
        }

        return new
        {
            @event = eventName,
            type = "event",
            seq = Interlocked.Increment(ref responseSeq),
            body = body
        };        
    }

    object WrapResponse(string cmd, object body, int reqseq)
    {
        if (body == successObject)
        {
            return new
            {
                seq = Interlocked.Increment(ref responseSeq),
                type = "response",
                request_seq = reqseq,
                success = true,
                command = cmd
            };    
        }
        return new
        {
            seq = Interlocked.Increment(ref responseSeq),
            type = "response",
            request_seq = reqseq,
            success = true,
            command = cmd,
            body = body
        };
    }

    void PushEvent(string name, object evt) 
    {
        events.Enqueue((name, evt));
    }
    
    object ProcessCommand(string command, JsonElement args)
    {
        switch (command)
        {
            case "initialize":
                return InitializeRequest();
            case "attach":
                return AttachRequest(args);
            case "setBreakpoints":
                return SetBreakpointsRequest(args);
            case "setFunctionBreakpoints":
                return SetFunctionBreakpointsRequest(args);
            case "setExceptionBreakpoints":
                return SetExceptionBreakpointsRequest(args);
            case "configurationDone":
                return successObject;
            case "threads":
                return ThreadsRequest();
            case "stackTrace":
                return StackTraceRequest(args);
            case "source":
                return SourceRequest(args);
            case "variables":
                return VariablesRequest(args);
            case "next":
                return NextRequest(args);
            case "continue":
                return ContinueRequest(args);
            case "stepIn":
                return StepInRequest(args);
            case "stepOut":
                return StepOutRequest(args);
            case "disconnect":
                client.Close();
                return successObject;
            case "scopes":
                return ScopesRequest(args);
        }
        return null;
    }



    object InitializeRequest()
    {
        PushEvent("initialized", successObject);
        var exception_breakpoint_filters = new object[]
        {
            new {
                filter= "raised",
                label= "Raised Exceptions",
                @default= false,
                description= "Break whenever any exception is raised."
            }
        };

        return new
        {
            supportsCompletionsRequest = true,
            supportsConditionalBreakpoints = true,
            supportsConfigurationDoneRequest = true,
            supportsDebuggerProperties = true,
            supportsDelayedStackTraceLoading = true,
            supportsEvaluateForHovers = true,
            supportsExceptionInfoRequest = true,
            supportsExceptionOptions = true,
            supportsFunctionBreakpoints = true,
            supportsHitConditionalBreakpoints = true,
            supportsLogPoints = true,
            supportsModulesRequest = true,
            supportsSetExpression = true,
            supportsSetVariable = true,
            supportsValueFormattingOptions = true,
            supportsTerminateDebuggee = true,
            supportsGotoTargetsRequest = true,
            supportsClipboardContext = true,
            exceptionBreakpointFilters = exception_breakpoint_filters,
            supportsStepInTargetsRequest = true,
        };
    }
    
    object AttachRequest(JsonElement args) =>  successObject;

    object SetBreakpointsRequest(JsonElement args)
    {
        var idbase = breakPoints.Count;
        var source = args.GetProperty("source");
        var name = source.TryGetProperty("name", out var p) ? p.GetString() : "";
        var path = source.GetProperty("path").GetString();
        var lines = args.GetProperty("lines").EnumerateArray().Select(x => x.GetInt32()).ToArray();
        
        breakPoints[name] = new HashSet<int>(lines);
        
        return new
        {
            breakpoints = lines.Select( (line, id) => new {
                verified = true,
                id = idbase + id,
                line = line,
                source = new
                    {
                        name = name,
                        path = path,
                    }
                }).ToArray()
        };
    }
    
    object SetFunctionBreakpointsRequest(JsonElement args)
    {
        /*var source = args.GetProperty("source");
        var name = source.GetProperty("name").GetString();
        var path = source.GetProperty("path").GetString();
        var lines = source.GetProperty("lines").EnumerateArray().Select(x => x.GetInt32()).ToArray();*/
        return new
        {
            breakpoints = Array.Empty<object>()
        };
    }
    
    object SetExceptionBreakpointsRequest(JsonElement args)
    {
        return successObject;
    }
    
    object ThreadsRequest()
    {
        List<int> threadIds = new List<int>();
        foreach (var trd in threads.ToArray())
        {
            if (!trd.TryGetTarget(out var t))
            {
                threads.Remove(trd);
            }
            else
            {
                threadIds.Add(GetThreadId(t));
            }
        }
        return new
        {
            
            threads = threadIds
                .Select(tid =>
                        new
                        {
                            id = tid,
                            name = $"Thread{tid}"
                        }
                ).ToArray()
        };
    }

    object StackTraceRequest(JsonElement args)
    {
        int threadId = args.GetProperty("threadId").GetInt32();
        //int startFrame = args.GetProperty("startFrame").GetInt32();
        //int levels = args.GetProperty("levels").GetInt32();
        if (breakStackFrames != null && breakStackFrames.threadId == threadId)
            return breakStackFrames;
        
        return new
        {
            stackFrames = new object[] { },
            totalFrames = 1
        };
    }

    object SourceRequest(JsonElement args)
    {
        
        //:{"sourceReference":1,"source":{"path":"C:\\Keysight\\Development\\python\\bin\\Debug\\Packages\\PythonExamples\\EnumUsage.py","sourceReference":1}},
        var file = args.GetProperty("source").GetProperty("path").GetString();
        var refId = args.GetProperty("source").GetProperty("sourceReference").GetInt32();
        sourceReference[file] = refId;
        if (File.Exists(file) == false) return successObject;
        return new
        {
            content = File.ReadAllText(file),
            sourceReference = refId
        };
    }

    object NextRequest(JsonElement args)
    {
        if (debugState == null)
        {
            throw new InvalidOperationException("Unexpected debug state");
        }

        debugState = new NextLine
        {
            ThreadId = args.GetProperty("threadId").GetInt32(),
            CurrentLine = breakStackFrames.stackFrames.FirstOrDefault()?.line ?? -1,
            Name = breakStackFrames.stackFrames.FirstOrDefault().name,
            BreakLevel = breakStackFrames.totalFrames
        };
        
        return successObject;
    }
    object StepOutRequest(JsonElement args)
    {
        var threadId = args.GetProperty("threadId").GetInt32();
        debugState = new NextLine
        {
            ThreadId = threadId,
            CurrentLine = breakStackFrames.stackFrames.FirstOrDefault()?.line ?? -1,
            Name = breakStackFrames.stackFrames.FirstOrDefault().name,
            BreakLevel = breakStackFrames.totalFrames,
            Mode = "stepOut"
        };
        return successObject;
    }

    object StepInRequest(JsonElement args)
    {
        var threadId = args.GetProperty("threadId").GetInt32();
        debugState = new NextLine
        {
            ThreadId = threadId,
            CurrentLine = breakStackFrames.stackFrames.FirstOrDefault()?.line ?? -1,
            Name = breakStackFrames.stackFrames.FirstOrDefault().name,
            BreakLevel = breakStackFrames.totalFrames,
            
            Mode = "stepIn"
        };
        return successObject;
    }

    object ContinueRequest(JsonElement args)
    {
        if (debugState == null)
        {
            throw new InvalidOperationException("Unexpected debug state");
        }

        debugState = null;
        return successObject;
    }

    object ScopesRequest(JsonElement args)
    {
        var frameId = args.GetProperty("frameId").GetInt32();
        return new
        {
            scopes = new[]
            {
                new
                {
                    name = "Locals",
                    variablesReference = frameId,
                    expensive = false,
                    presentationHint = "locals",
                    source = new {}
                },
                new
                {
                    name = "Globals",
                    variablesReference = 0,
                    expensive = false,
                    presentationHint = "globals",
                    source = new {}
                }
            }
        };
    }

    T Process<T>(Func<T> f)
    {
        var cancel = breakCancel;
        var sem = new SemaphoreSlim(0);
        T result = default;
        processor.Enqueue(() =>
        {
            result = f();
            sem.Release();
        });
        try
        {
            sem.Wait(cancel);
        }
        catch
        {
            return default;
        }
        
        return result;
    }
    
    object VariablesRequest(JsonElement args)
    {
        return Process<object>(() =>
        {
            var varRef = args.GetProperty("variablesReference").GetInt32();
            if (ScopeVariableReferences.TryGetValue(varRef, out var v))
            {
                bool hasKeys = v.HasAttr("keys");

                using var items = hasKeys ? v.InvokeMethod("keys") : v.Dir();
                using var iter = new PyIterable(items);
                var vars = new List<object>();
                foreach (var i in iter)
                {
                    try
                    {
                        bool dispose_i, dispose_value;
                        var value = hasKeys ? v.GetItem(i) : v.GetAttr(i);

                        if (!ScopeVariableReferenceLookup.TryGetValue((value, i), out var i2))
                        {
                            
                            i2 = ScopeVariableReferenceLookup[(value, i)] = ScopeVariableReferences.Count + 1;
                            ScopeVariableReferences[i2] = value;
                            dispose_i = false;
                            dispose_value = false;
                        }
                        else
                        {
                            dispose_i = true;
                            dispose_value = true;
                        }

                        vars.Add(new
                        {
                            name = i.ToString(),
                            value = value.ToString(),
                            type = "string",
                            evaluateName = i.ToString(),
                            variablesReference = i2
                        });
                        if (dispose_i)
                            i.Dispose();

                        if (dispose_value)
                            value.Dispose();
                    }
                    catch
                    {
                        continue;
                    }
                }

                return new
                {
                    variables = vars.ToArray()
                };
            }

            return new
            {
                variables = new[]
                {
                    new
                    {
                        name = "special variables"
                    }
                }
            };
        });
    }

    void WaitForContinueOrNext(PyObject pyFrameObject)
    {
        if (debugState != null)
            throw new InvalidOperationException("Unexpected debug state.");
        
        var thisState = new
        {
            reason = "breakpoint",
            threadId = GetThreadId(TapThread.Current),
            preserveFocusHint = false,
            allThreadsStopped = true
        };
        PushEvent("stopped",
            debugState = thisState 
            );
        // now wait until next, continue or disconnect.

        List<StackFrame> stackFrames = new List<StackFrame>();
        Dictionary<int, PyObject> kv = new ();
        
        var fi = pyFrameObject;
        kv[0] = fi.GetAttr("f_globals");
        while (fi.IsNone() == false)
        {
            using var code = fi.GetAttr("f_code");
            using var it = fi.GetAttr("f_code").InvokeMethod("co_lines");
            using var line = fi.GetAttr("f_lineno");
            var locals = fi.GetAttr("f_locals");
            using var coName = code.GetAttr("co_name");
            using var coFilename = code.GetAttr("co_filename");
            kv[stackFrames.Count + 1] = locals;
            StackFrame item = new(stackFrames.Count + 1, coName.ToString(), line.ToInt32(CultureInfo.InvariantCulture),
                1, new { path = coFilename.ToString(), sourceReference = 1 });
            stackFrames.Add(item);
            var fi2 = fi;
            fi = fi.GetAttr("f_back");
            fi2.Dispose();
        }

        fi.Dispose();

        ScopeVariableReferences = kv;
        ScopeVariableReferenceLookup = new();

        breakStackFrames = new BreakStackFrames(stackFrames.ToArray(), stackFrames.Count, thisState.threadId);
        var cancel = new CancellationTokenSource();
        breakCancel = cancel.Token;
        processor = new ConcurrentQueue<Action>();
        Interlocked.Increment(ref isWaiting);
        while (debugState == thisState)
        {
            if (client.Connected == false)
            {
                debugState = null;
            }
            while (processor.TryDequeue(out var item))
            {
                item();
            }
            TapThread.Sleep(10);
        }

        
        foreach (var elem in ScopeVariableReferences)
        {
            elem.Value.Dispose();
        }

        foreach (var elem in ScopeVariableReferenceLookup)
        {
            elem.Key.Item1.Dispose();
            elem.Key.Item2.Dispose();
        }

        ScopeVariableReferences.Clear();
        ScopeVariableReferences = null;
        breakStackFrames = null;
        ScopeVariableReferenceLookup = null;
        cancel.Cancel();
        Interlocked.Decrement(ref isWaiting);
        ContinuedEvent();
    }

    void ContinuedEvent()
    {
        PushEvent("continued", new {threadId = GetThreadId(TapThread.Current), allThreadsContinued = true});
    }

    // The current thread 'id' is currently connected to the current TAP thread.
    // but it is not certain that this is a good way of doing it.
    public static int GetThreadId(TapThread t0)
    {
        return (int)knownThreads.GetValue(t0, t =>
        {
            lock (threads)
            {
                threads.Add(new WeakReference<TapThread>(t));
            }
            return Interlocked.Increment(ref threadCounter);
        });
    }
    
    // This trace callback is invoked from the python interpreter through the callback given to PyEval_SetTrace.
    public void TraceCallback(PyObject pyObject, Runtime.PyFrameObject pyFrameObject, Runtime.TraceWhat arg3, PyObject arg4)
    {
        if (debugState is NextLine nl)
        {
            if (nl.ThreadId != GetThreadId(TapThread.Current))
                return;
            if (nl.Mode == "stepIn")
            {
                // step in is as simple as just stepping to the next line executed.
                debugState = null;
                return;
            }
            var p1 = pyFrameObject.AsPyObject();
            int frameCount = 0;
            while (p1.IsNone() == false)
            {
                frameCount += 1;
                var p2 = p1;
                p1 = p1.GetAttr("f_back");
                p2.Dispose();
            }

            if (frameCount > nl.BreakLevel) return;
            if (nl.Mode == "stepOut")
            {
                if (frameCount > Math.Max(1, nl.BreakLevel - 1)) return;
            }
            debugState = null;
            using var p3 = pyFrameObject.AsPyObject();
            // ok, next location found
            WaitForContinueOrNext(p3);
            if (frameCount == 0 && arg3 == Runtime.TraceWhat.Return)
            {
                debugState = null;
            }
            
            return;
        }
        var i = pyFrameObject.GetLineNumber();
        
        if (arg3 == Runtime.TraceWhat.Line)
        {
            
            using var p = pyFrameObject.AsPyObject();
            foreach (var kv in breakPoints)
            {
                if (kv.Value.Contains(i))
                {
                    // breakpoint found! Wait for the user to continue somehow.
                    WaitForContinueOrNext(p);
                    return;
                }
            }
        }
    }

    public event Action Disconnected;
}