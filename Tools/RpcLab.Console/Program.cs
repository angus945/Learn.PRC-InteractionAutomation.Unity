using Module.Communication.JsonRpc;
using Module.Communication.JsonRpc.WebSocket;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://127.0.0.1:6123");

WebApplication app = builder.Build();

app.UseWebSockets();

app.Map("/rpc", async context =>
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = 400;
            return;
        }

        Console.WriteLine(
            "Unity connection incoming...");

        using var socket =
            await context.WebSockets
                .AcceptWebSocketAsync();

        RpcMethodCatalog methods =
            new RpcMethodRegistry()
                .RegisterMethod<string, string>(
                    "lab.console.echo",
                    (text, cancellationToken) =>
                    {
                        Console.WriteLine(
                            "Echo request: " + text);

                        return Task.FromResult(text);
                    })
                .Build();

        using IRpcConnection rpc =
            WebSocketRpcConnection.Attach(
                socket,
                methods);

        Console.WriteLine(
            "Unity connected.");

        try
        {
            FrameReply result =
                await rpc.CallAsync<
                    FrameRequest,
                    FrameReply>(
                    "lab.unity.frame",
                    new FrameRequest
                    {
                        Message =
                            "Hello from Console"
                    });

            Console.WriteLine();
            Console.WriteLine(
                "Reply from Unity:");

            Console.WriteLine(
                "  Message : " +
                result.Message);

            Console.WriteLine(
                "  Frame   : " +
                result.Frame);

            Console.WriteLine(
                "  Thread  : " +
                result.ThreadId);

            Console.WriteLine();
            Console.WriteLine(
                "Waiting for disconnect...");

            await rpc.Completion;
        }
        catch (RpcConnectionException exception)
        {
            Console.WriteLine(
                "RPC disconnected: " +
                exception.Message);
        }
        catch (Exception exception)
        {
            Console.WriteLine(
                exception);
        }

        Console.WriteLine(
            "Unity disconnected.");
    });

Console.WriteLine(
    "RPC Lab Console");

Console.WriteLine(
    "Listening: ws://127.0.0.1:6123/rpc");

Console.WriteLine();

await app.RunAsync();


public sealed class FrameRequest
{
    public string Message { get; set; } =
        string.Empty;
}

public sealed class FrameReply
{
    public string Message { get; set; } =
        string.Empty;

    public int Frame { get; set; }

    public int ThreadId { get; set; }
}