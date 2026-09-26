#nullable enable

using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Module.Communication.JsonRpc;
using Module.Communication.JsonRpc.WebSocket;
using Module.Communication.Unity3D;
using UnityEngine;

namespace Project.RpcLab.Runtime
{
    public sealed class RpcLabBootstrap : MonoBehaviour
    {
        [SerializeField]
        private UnityRpcHost _rpcHost = null!;

        [SerializeField]
        private string _endpoint = "ws://127.0.0.1:6123/rpc";

        private CancellationTokenSource? _connectCancellation;
        private ClientWebSocket? _connectingSocket;

        public string Status { get; private set; } = "Stopped";

        [ContextMenu("RPC Lab/Connect")]
        public async void Connect()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Enter Play Mode first.", this);
                return;
            }

            if (_connectCancellation != null)
            {
                Debug.LogWarning("Connection attempt already running.", this);
                return;
            }

            if (_rpcHost == null)
            {
                Debug.LogError("UnityRpcHost is not assigned.", this);
                return;
            }

            if (_rpcHost.IsRunning)
            {
                Debug.LogWarning("RPC connection already running.", this);
                return;
            }

            ClientWebSocket? socket = null;
            CancellationTokenSource? cancellation = null;

            try
            {
                Status = "Connecting";

                cancellation =
                    new CancellationTokenSource(TimeSpan.FromSeconds(5));

                _connectCancellation = cancellation;

                socket = new ClientWebSocket();
                _connectingSocket = socket;

                Debug.Log("Connecting to " + _endpoint, this);

                await socket.ConnectAsync(new Uri(_endpoint), cancellation.Token);

                cancellation.Token.ThrowIfCancellationRequested();

                RpcMethodCatalog methods = new RpcMethodRegistry()
                        .RegisterMethod<FrameRequest, FrameReply>("lab.unity.frame", HandleFrameRequestAsync)
                        .Build();

                ClientWebSocket ownedSocket = socket;

                _rpcHost.Initialize(methods, wrappedMethods => WebSocketRpcConnection.Attach(ownedSocket, wrappedMethods));

                // Ownership 已交給 WebSocketRpcConnection。
                socket = null;
                _connectingSocket = null;

                Status = "Connected";

                Debug.Log(
                    "RPC connected.",
                    this);

                string echo =
                    await _rpcHost.Client
                        .CallAsync<string, string>(
                            "lab.console.echo",
                            "Hello from Unity");

                Debug.Log(
                    "Console echo: " + echo,
                    this);
            }
            catch (OperationCanceledException)
            {
                Status = "Cancelled";

                Debug.LogWarning(
                    "RPC connection cancelled.",
                    this);
            }
            catch (Exception exception)
            {
                Status = "Failed";

                if (_rpcHost != null &&
                    _rpcHost.IsRunning)
                {
                    _rpcHost.Stop();
                }

                Debug.LogException(
                    exception,
                    this);
            }
            finally
            {
                if (socket != null)
                {
                    socket.Dispose();
                }

                _connectingSocket = null;

                if (ReferenceEquals(
                    _connectCancellation,
                    cancellation))
                {
                    _connectCancellation = null;
                }

                if (cancellation != null)
                {
                    cancellation.Dispose();
                }
            }
        }

        [ContextMenu("RPC Lab/Disconnect")]
        public void Disconnect()
        {
            if (_connectCancellation != null)
            {
                _connectCancellation.Cancel();
            }

            if (_connectingSocket != null)
            {
                _connectingSocket.Dispose();
                _connectingSocket = null;
            }

            if (_rpcHost != null &&
                _rpcHost.IsRunning)
            {
                _rpcHost.Stop();
            }

            Status = "Stopped";

            Debug.Log(
                "RPC disconnected.",
                this);
        }

        private Task<FrameReply> HandleFrameRequestAsync(
            FrameRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            FrameReply reply =
                new FrameReply
                {
                    Message = request.Message,
                    Frame = Time.frameCount,
                    ThreadId =
                        Thread.CurrentThread.ManagedThreadId
                };

            return Task.FromResult(reply);
        }

        private void OnDisable()
        {
            Disconnect();
        }

        private void OnDestroy()
        {
            Disconnect();
        }

        [Serializable]
        public sealed class FrameRequest
        {
            public string Message { get; set; } =
                string.Empty;
        }

        [Serializable]
        public sealed class FrameReply
        {
            public string Message { get; set; } =
                string.Empty;

            public int Frame { get; set; }

            public int ThreadId { get; set; }
        }
    }
}