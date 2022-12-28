using System.Net;
using System.Text;
using System.Runtime.CompilerServices;

namespace HttpServer;

internal class HttpServer
{
    void Log(string message, [CallerMemberName] string name = "?")
        => Console.WriteLine($"[{name}][TID{Thread.CurrentThread.ManagedThreadId}]: {message}.");

    async Task<HttpListenerContext?> TryGetContextAsync(HttpListener listener)
    {
        HttpListenerContext? context = null;
        try
        {
            context = await listener.GetContextAsync();
        }
        catch (Exception) { }
        return context;
    }

    void Response(HttpListenerResponse response, string body)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(body);
        response.ContentLength64 = buffer.Length;
        response.OutputStream.Write(buffer, 0, buffer.Length);
    }

    async Task Handle(HttpListenerContext context)
    {
        Log("Call");
        int delayMsec = 0;
        string responseBody = "";
        switch (context.Request.RawUrl)
        {
            case "/":
                delayMsec = 3000;
                responseBody = "<HTML><BODY>Hello world!</BODY></HTML>";
                break;
            case "/test":
                delayMsec = 8000;
                responseBody = "<HTML><BODY>Hello test!</BODY></HTML>";
                break;
        }
        var delay = Task.Delay(delayMsec);
        Log($"Delay {delayMsec}");
        await delay;
        Response(context.Response, responseBody);
        Log("Release the connection");
    }

    async Task Listen(HttpListener listener, Func<HttpListenerContext, Task> handler)
    {
        Log("Call");
        while (listener.IsListening)
        {
            Log("Waiting for a new connection");
            var context = await TryGetContextAsync(listener);
            if (context is null)
            {
                Log("Canceled");
            }
            else
            {
                Log("Connected");
                Task.Run(() => handler(context!).Wait());
            }
        }
    }

    void Run(CancellationToken token)
    {
        HttpListener listener = new();
        listener.Prefixes.Add("http://localhost:8080/");

        token.Register(() =>
        {
            Log("Stopping listening");
            listener.Stop();
            Log("Stopped");
        });

        Log("Start listening");
        listener.Start();
        Log("Started");
        Listen(listener, Handle).Wait();
    }

    private static void Main(string[] args)
    {
        HttpServer server = new();
        using CancellationTokenSource cts = new();
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            cts.Cancel(true);
        };
        server.Run(cts.Token);
        server.Log("Bye-bye");
    }
}