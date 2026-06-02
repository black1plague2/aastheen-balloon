using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using NativeWebSocket;

public class WSClient : MonoBehaviour
{
    public static WSClient Instance;

    [Header("Connection")]
    public int serverPort = 8765;

    private const int discoveryPort = 8766;

    private WebSocket          websocket;
    private CancellationTokenSource cts;
    private volatile string    pendingIP = null;

    private void Awake() => Instance = this;

    [Header("Debug")]
    public bool debugDirectConnect = false;
    public string debugIP = "192.168.137.1";

    private void Start()
    {
        cts = new CancellationTokenSource();
        if (debugDirectConnect)
            ConnectWebSocket(debugIP);
        else
            Task.Run(() => ListenForServer(cts.Token));
    }

    private void Update()
    {
        websocket?.DispatchMessageQueue();

        if (pendingIP != null)
        {
            string ip = pendingIP;
            pendingIP = null;
            ConnectWebSocket(ip);
        }
    }

    // Runs on background thread — blocks until a broadcast arrives or cancelled
    private void ListenForServer(CancellationToken token)
    {
        Debug.Log("[WSClient] Searching for Flutter server on port " + discoveryPort);
        try
        {
            using var udp = new UdpClient(discoveryPort);
            udp.EnableBroadcast    = true;
            udp.Client.ReceiveTimeout = 500; // 500ms so we can check cancellation

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var ep   = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = udp.Receive(ref ep);
                    string msg  = Encoding.UTF8.GetString(data);

                    if (msg.StartsWith("AASTHEEN_SERVER"))
                    {
                        Debug.Log("[WSClient] Server found at " + ep.Address);
                        pendingIP = ep.Address.ToString();
                        return;
                    }
                }
                catch (SocketException) { } // timeout — loop and check cancellation
            }
        }
        catch (Exception e)
        {
            if (!token.IsCancellationRequested)
                Debug.LogError("[WSClient] Discovery error: " + e.Message);
        }
    }

    private async void ConnectWebSocket(string ip)
    {
        string url = $"ws://{ip}:{serverPort}/ws/balloon";
        websocket  = new WebSocket(url);

        websocket.OnOpen    += ()  => Debug.Log("[WSClient] Connected → " + url);
        websocket.OnError   += (e) => Debug.LogError("[WSClient] Error: " + e);
        websocket.OnClose   += (e) => Debug.Log("[WSClient] Closed: " + e);
        websocket.OnMessage += (bytes) => HandleMessage(Encoding.UTF8.GetString(bytes));

        await websocket.Connect();
    }

    private void HandleMessage(string json)
    {
        try
        {
            var baseMsg = JsonUtility.FromJson<SensorMessage>(json);
            if (baseMsg == null || string.IsNullOrEmpty(baseMsg.type)) return;

            switch (baseMsg.type)
            {
                case "sensor":
                {
                    var msg = JsonUtility.FromJson<SensorMessage>(json);
                    GameManager.Instance.OnSensorData(msg.rotation, msg.speed, msg.warning);
                    break;
                }
                case "prescription":
                {
                    var msg = JsonUtility.FromJson<PrescriptionMessage>(json);
                    GameManager.Instance.OnPrescriptionReceived(
                        msg.targetRotation, msg.holdTimeMs, msg.repCount,
                        msg.balloonSize,    msg.spawnInterval, msg.sessionDuration);
                    break;
                }
                case "command":
                {
                    var msg = JsonUtility.FromJson<CommandMessage>(json);
                    if (msg != null && !string.IsNullOrEmpty(msg.command))
                        GameManager.Instance.OnCommand(msg.command);
                    break;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[WSClient] Parse error: " + e.Message);
        }
    }

    public async void SendRepDone(int score, float rotationAchieved, float heldMs)
    {
        if (websocket == null || websocket.State != WebSocketState.Open) return;

        var msg = new RepDoneMessage
        {
            type             = "rep_done",
            score            = score,
            rotationAchieved = rotationAchieved,
            heldMs           = heldMs
        };
        await websocket.SendText(JsonUtility.ToJson(msg));
    }

    private async void OnDisable()
    {
        cts?.Cancel();
        if (websocket != null)
        {
            await websocket.Close();
            websocket = null;
        }
    }

    private async void OnApplicationQuit()
    {
        cts?.Cancel();
        if (websocket != null)
            await websocket.Close();
    }
}
