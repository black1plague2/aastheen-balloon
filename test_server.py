# Balloon Pop VR - WebSocket Test Server
#
# Run instructions:
#   1. pip install websockets
#   2. python test_server.py
#   3. In Unity, connect to ws://<your-pc-ip>:8765
#      (find your IP with: ipconfig | findstr IPv4)
#   4. The server will auto-send prescription then stream sensor data
#   5. Press Ctrl+C to stop

import asyncio
import json
import math
import time
import websockets

PORT = 8765
SENSOR_INTERVAL = 0.1  # seconds between sensor messages

connected_clients: set = set()


async def handle_client(websocket):
    client_ip = websocket.remote_address[0]
    connected_clients.add(websocket)
    print(f"[{_ts()}] Client connected: {client_ip}  (total: {len(connected_clients)})")

    try:
        # Wait 2 seconds then send prescription
        await asyncio.sleep(2.0)

        prescription = {
            "type": "prescription",
            "targetRotation": 45.0,
            "holdTimeMs": 800.0,
            "repCount": 5,
            "balloonSize": 0.35,
            "spawnInterval": 3.0,
            "sessionDuration": 120.0,
        }
        await websocket.send(json.dumps(prescription))
        print(f"[{_ts()}] Prescription sent to {client_ip}")

        # Give GameManager a moment to apply the prescription, then start the session
        await asyncio.sleep(1.0)
        start_cmd = {"type": "command", "command": "start"}
        await websocket.send(json.dumps(start_cmd))
        print(f"[{_ts()}] Start command sent to {client_ip}")

        # Stream sensor data and listen for incoming messages concurrently
        await asyncio.gather(
            stream_sensor(websocket, client_ip),
            receive_messages(websocket, client_ip),
        )

    except websockets.exceptions.ConnectionClosedOK:
        print(f"[{_ts()}] Client disconnected cleanly: {client_ip}")
    except websockets.exceptions.ConnectionClosedError as e:
        print(f"[{_ts()}] Connection error ({client_ip}): {e}")
    except Exception as e:
        print(f"[{_ts()}] Unexpected error ({client_ip}): {e}")
    finally:
        # Always remove from set regardless of how the connection ended
        connected_clients.discard(websocket)
        print(f"[{_ts()}] Cleaned up {client_ip}  (remaining: {len(connected_clients)})")


async def stream_sensor(websocket, client_ip):
    start = time.time()
    try:
        while True:
            elapsed = time.time() - start
            # Sine wave: oscillates 0 → 90 → 0, period ~6 seconds
            rotation = 45.0 + 45.0 * math.sin((2 * math.pi / 6.0) * elapsed)

            sensor = {
                "type": "sensor",
                "rotation": round(rotation, 2),
                "speed": 5.0,
                "warning": "",
            }
            await websocket.send(json.dumps(sensor))
            await asyncio.sleep(SENSOR_INTERVAL)
    except websockets.exceptions.ConnectionClosed:
        pass  # handled in handle_client


async def receive_messages(websocket, client_ip):
    try:
        async for raw in websocket:
            try:
                msg = json.loads(raw)
                if msg.get("type") == "rep_done":
                    print(f"[{_ts()}] rep_done from {client_ip}: {json.dumps(msg)}")
                else:
                    print(f"[{_ts()}] Message from {client_ip}: {json.dumps(msg)}")
            except json.JSONDecodeError:
                print(f"[{_ts()}] Non-JSON from {client_ip}: {raw}")
    except websockets.exceptions.ConnectionClosed:
        pass  # handled in handle_client


def _ts():
    return time.strftime("%H:%M:%S")


async def main():
    print(f"Balloon Pop test server starting on ws://0.0.0.0:{PORT}")
    print("Connect Unity to ws://<your-pc-ip>:8765")
    print("Press Ctrl+C to stop\n")
    async with websockets.serve(handle_client, "0.0.0.0", PORT):
        await asyncio.Future()  # run forever


if __name__ == "__main__":
    asyncio.run(main())
