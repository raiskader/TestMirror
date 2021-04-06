using System;
using System.Net;
using libuv2k.Native;
using UnityEngine;

namespace libuv2k.Examples
{
    public class TestClient : MonoBehaviour
    {
        // configuration
        public ushort Port = 7777;
        // libuv can be ticked multiple times per frame up to max so we don't
        // deadlock
        public const int LibuvMaxTicksPerFrame = 100;

        // Libuv state
        //
        // IMPORTANT: do NOT create new Loop & Client here, otherwise a loop is
        //            also allocated if we run a test while a scene with this
        //            component on a GameObject is openened.
        //
        //            we need to create it when needed and dispose when we are
        //            done, otherwise dispose isn't called until domain reload.
        //
        public Loop loop; // public for tests
        public TcpStream client; // public for tests

        public bool IsConnected() =>
            client != null && client.IsActive;

        // libuv doesn't resolve host name, and it needs ipv4.
        public void Connect(string ip)
        {
            if (client != null)
                return;

            // connect client
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
            IPAddress address = IPAddress.Parse(ip);
            IPEndPoint remoteEndPoint = new IPEndPoint(address, Port);

            Debug.Log("Libuv connecting to: " + ip + ":" + Port);
            client = new TcpStream(loop);
            client.NoDelay(true);
            client.onClientConnect = OnLibuvConnected;
            client.onMessage = OnLibuvMessage;
            client.onError = OnLibuvError;
            client.onClosed = OnLibuvClosed;
            client.ConnectTo(localEndPoint, remoteEndPoint);
        }

        public bool Send(ArraySegment<byte> segment)
        {
            if (loop != null && client != null)
            {
                // IMPORTANT: segment is pinned. don't immediately overwrite it
                //            in next send...
                client.Send(segment);
                return true;
            }
            return false;
        }

        public void Disconnect()
        {
            // Dispose will disconnect, and OnLibuvClosed will clean up
            client?.Dispose();
            client = null;
        }

        // libuv callbacks /////////////////////////////////////////////////////
        void OnLibuvConnected(TcpStream handle, Exception exception)
        {
            // close if errors (AFTER setting up onClosed callback!)
            if (exception != null)
            {
                Debug.Log($"libuv cl: client error {exception}");
                handle.Dispose();
                return;
            }

            Debug.Log($"libuv cl: client connected.");
        }

        // segment is valid until function returns.
        void OnLibuvMessage(TcpStream handle, ArraySegment<byte> segment)
        {
            Debug.Log("libuv cl: data=" + BitConverter.ToString(segment.Array, segment.Offset, segment.Count));
        }

        void OnLibuvError(TcpStream handle, Exception error)
        {
            Debug.Log($"libuv cl: read error {error}");
        }

        // OnClosed is called after we closed the stream
        void OnLibuvClosed(TcpStream handle)
        {
            // set client to null so we can't send to an old reference anymore
            Debug.Log("libuv cl: closed connection");
            client = null;
        }

        // MonoBehaviour ///////////////////////////////////////////////////////
        public void Awake()
        {
            libuv2k.Initialize();

            // configure
            Log.Info = Debug.Log;
            Log.Warning = Debug.LogWarning;
            Log.Error = Debug.LogError;

            // IMPORTANT: create loop only once
            // DO NOT dispose in OnDisconnect, since that's called from within
            // a loop's uv_run. disposing while it's running could crash libuv.
            loop = new Loop();
        }

        public void Update()
        {
            // tick libuv while loop is valid
            // IMPORTANT: tick even if server is null.
            //            when disposing the client, uv_close callbacks are
            //            only called in the next loop update, so we need to
            //            keep updating the loop even if client was disposed.
            if (loop != null)
            {
                // Run with UV_RUN_NOWAIT returns 0 when nothing to do, but we
                // should avoid deadlocks via LibuvMaxTicksPerFrame
                for (int i = 0; i < LibuvMaxTicksPerFrame; ++i)
                {
                    if (loop.Run(uv_run_mode.UV_RUN_NOWAIT) == 0)
                    {
                        //Debug.Log("libuv cl ticked only " + i + " times");
                        break;
                    }
                }
            }
        }

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(5, 5, 150, 400));
            GUILayout.Label("Client:");
            if (GUILayout.Button("Connect 127.0.0.1"))
            {
                Connect("127.0.0.1");
            }
            if (GUILayout.Button("Send 0x01, 0x02"))
            {
                Send(new ArraySegment<byte>(new byte[]{0x01, 0x02}));
            }
            if (GUILayout.Button("Disconnect"))
            {
                Disconnect();
            }
            GUILayout.EndArea();
        }

        public void OnDestroy()
        {
            // IMPORTANT: dispose loop only once
            // DO NOT dispose in OnDisconnect, since that's called from within
            // a loop's uv_run. disposing while it's running could crash libuv.
            loop?.Dispose();
            loop = null;

            // clean up everything else properly
            libuv2k.Shutdown();
        }
    }
}