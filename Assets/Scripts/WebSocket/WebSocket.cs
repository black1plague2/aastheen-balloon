using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace NativeWebSocket
{
    public enum WebSocketState
    {
        Connecting,
        Open,
        Closing,
        Closed
    }

    public enum WebSocketCloseCode
    {
        Normal              = 1000,
        Away                = 1001,
        ProtocolError       = 1002,
        UnsupportedData     = 1003,
        NoStatus            = 1005,
        Abnormal            = 1006,
        InvalidData         = 1007,
        PolicyViolation     = 1008,
        TooBig              = 1009,
        MandatoryExtension  = 1010,
        ServerError         = 1011,
        TlsHandshakeFailure = 1015,
        Undefined           = 0
    }

    public class WebSocket
    {
        public event Action                    OnOpen;
        public event Action<string>            OnError;
        public event Action<WebSocketCloseCode> OnClose;
        public event Action<byte[]>            OnMessage;

        private ClientWebSocket          _socket;
        private CancellationTokenSource  _cts;
        private string                   _url;

        // Network thread enqueues; main thread drains via DispatchMessageQueue()
        private readonly ConcurrentQueue<byte[]> _incoming  = new ConcurrentQueue<byte[]>();
        private volatile bool            _closePending;
        private WebSocketCloseCode       _closeCode = WebSocketCloseCode.Normal;

        public WebSocketState State
        {
            get
            {
                if (_socket == null) return WebSocketState.Closed;
                switch (_socket.State)
                {
                    case System.Net.WebSockets.WebSocketState.Connecting:    return WebSocketState.Connecting;
                    case System.Net.WebSockets.WebSocketState.Open:          return WebSocketState.Open;
                    case System.Net.WebSockets.WebSocketState.CloseSent:
                    case System.Net.WebSockets.WebSocketState.CloseReceived: return WebSocketState.Closing;
                    default:                                                  return WebSocketState.Closed;
                }
            }
        }

        public WebSocket(string url) => _url = url;

        public async Task Connect()
        {
            try
            {
                _cts    = new CancellationTokenSource();
                _socket = new ClientWebSocket();
                await _socket.ConnectAsync(new Uri(_url), _cts.Token);
                OnOpen?.Invoke();
                _ = ReceiveLoop();
            }
            catch (Exception e)
            {
                OnError?.Invoke(e.Message);
            }
        }

        private async Task ReceiveLoop()
        {
            var buffer = new byte[8192];
            var ms     = new MemoryStream();
            try
            {
                while (_socket.State == System.Net.WebSockets.WebSocketState.Open)
                {
                    ms.SetLength(0);
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                        if (result.MessageType == WebSocketMessageType.Close) break;
                        ms.Write(buffer, 0, result.Count);
                    }
                    while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _closeCode    = result.CloseStatus.HasValue
                            ? MapCloseCode((int)result.CloseStatus.Value)
                            : WebSocketCloseCode.Normal;
                        _closePending = true;
                        break;
                    }

                    _incoming.Enqueue(ms.ToArray());
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                Debug.LogWarning("[WebSocket] ReceiveLoop: " + e.Message);
                _closeCode    = WebSocketCloseCode.Abnormal;
                _closePending = true;
            }
        }

        // Call every Update() from MonoBehaviour — fires OnMessage / OnClose on the main thread
        public void DispatchMessageQueue()
        {
            while (_incoming.TryDequeue(out var msg))
                OnMessage?.Invoke(msg);

            if (_closePending)
            {
                _closePending = false;
                OnClose?.Invoke(_closeCode);
            }
        }

        public async Task SendText(string text)
        {
            if (_socket == null || _socket.State != System.Net.WebSockets.WebSocketState.Open) return;
            var bytes = Encoding.UTF8.GetBytes(text);
            await _socket.SendAsync(new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text, true, _cts.Token);
        }

        public async Task Close()
        {
            if (_socket != null &&
                (_socket.State == System.Net.WebSockets.WebSocketState.Open ||
                 _socket.State == System.Net.WebSockets.WebSocketState.CloseReceived))
            {
                try { await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", CancellationToken.None); }
                catch { }
            }
            _cts?.Cancel();
        }

        private static WebSocketCloseCode MapCloseCode(int code) =>
            Enum.IsDefined(typeof(WebSocketCloseCode), code)
                ? (WebSocketCloseCode)code
                : WebSocketCloseCode.Undefined;
    }
}
