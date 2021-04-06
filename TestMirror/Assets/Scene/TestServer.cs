using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;
using libuv2k.Native;

namespace libuv2k.Examples
{
    public class TestServer : MonoBehaviour
    {
        // configuration
        public ushort Port = 7777;
        // libuv can be ticked multiple times per frame up to max so we don't
        // deadlock
        public const int LibuvMaxTicksPerFrame = 100;

        // Libuv state
        //
        // IMPORTANT: do NOT create new Loop & Server here, otherwise a loop is
        //            also allocated if we run a test while a scene with this
        //            component on a GameObject is openened.
        //
        //            we need to create it when needed and dispose when we are
        //            done, otherwise dispose isn't called until domain reload.
        //
        Loop loop;
        TcpStream server;
        public Dictionary<int, TcpStream> connections = new Dictionary<int, TcpStream>(); // public for tests
        int nextConnectionId = 0;

        public bool IsActive() => server != null;

        public void StartServer()
        {
            if (server != null)
                return;

            // start server
            IPEndPoint EndPoint = new IPEndPoint(IPAddress.Any, Port);

            Debug.Log($"libuv sv: starting TCP..." + EndPoint);
            server = new TcpStream(loop);
            server.SimultaneousAccepts(true);
            server.onServerConnect = OnLibuvConnected;
            server.Listen(EndPoint);
            Debug.Log($"libuv sv: TCP started!");
        }

        public bool Send(int connectionId, ArraySegment<byte> segment)
        {
            if (server != null && connections.TryGetValue(connectionId, out TcpStream connection))
            {
                // TODO real world setup needs framing
                // IMPORTANT: segment is pinned. don't immediately overwite it
                //            in next send...
                connection.Send(segment);
                return true;
            }
            return false;
        }

        public bool Disconnect(int connectionId)
        {
            if (server != null && connections.TryGetValue(connectionId, out TcpStream connection))
            {
                // Dispose will disconnect, and OnLibuvClosed will clean up
                connection.Dispose();
                return true;
            }
            return false;
        }

        public string GetAddress(int connectionId)
        {
            if (server != null && connections.TryGetValue(connectionId, out TcpStream connection))
            {
                return connection.GetPeerEndPoint().Address.ToString();
            }
            return "";
        }

        public void StopServer()
        {
            if (server != null)
            {
                server.Dispose();
                server = null;
                connections.Clear();
                Debug.Log("libuv sv: TCP stopped!");
            }
        }

        // libuv callbacks /////////////////////////////////////////////////////
        void OnLibuvConnected(TcpStream handle, Exception error)
        {
            // setup callbacks for the new connection
            handle.onMessage = OnLibuvMessage;
            handle.onError = OnLibuvError;
            handle.onClosed = OnLibuvClosed;

            // close if errors (AFTER setting up onClosed callback!)
            if (error != null)
            {
                Debug.Log($"libuv sv: client connection failed {error}");
                handle.Dispose();
                return;
            }

            // assign a connectionId via UserToken.
            // this is better than using handle.InternalHandle.ToInt32() because
            // the InternalHandle isn't available in OnLibuvClosed anymore.
            handle.UserToken = nextConnectionId++;

            Debug.Log("libuv sv: client connected with connectionId=" + (int)handle.UserToken);
            connections[(int)handle.UserToken] = handle;
        }

        void OnLibuvMessage(TcpStream handle, ArraySegment<byte> segment)
        {
            // find connection
            if (connections.TryGetValue((int)handle.UserToken, out TcpStream connection))
            {
                Debug.Log("libuv sv: data=" + BitConverter.ToString(segment.Array, segment.Offset, segment.Count) + " from connectionId=" + (int)handle.UserToken);
            }
        }

        void OnLibuvError(TcpStream handle, Exception error)
        {
            Debug.Log($"libuv sv: error {error}");
            connections.Remove((int)handle.UserToken);
        }

        // OnClosed is called after we closed the stream
        void OnLibuvClosed(TcpStream handle)
        {
            Debug.Log($"libuv sv: closed connection with connectionId={(int)handle.UserToken}");
            connections.Remove((int)handle.UserToken);
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
            //            when disposing the server, uv_close callbacks are
            //            only called in the next loop update, so we need to
            //            keep updating the loop even if server was disposed.
            if (loop != null)
            {
                // Run with UV_RUN_NOWAIT returns 0 when nothing to do, but we
                // should avoid deadlocks via LibuvMaxTicksPerFrame
                for (int i = 0; i < LibuvMaxTicksPerFrame; ++i)
                {
                    if (loop.Run(uv_run_mode.UV_RUN_NOWAIT) == 0)
                    {
                        //Debug.Log("libuv sv ticked only " + i + " times");
                        break;
                    }
                }
            }
        }

        void OnGUI()
        {
            int firstclient = connections.Count > 0 ? connections.First().Key : -1;

            GUILayout.BeginArea(new Rect(160, 5, 250, 400));
            GUILayout.Label("Server:");
            if (GUILayout.Button("Start"))
            {
                StartServer();
            }
            if (GUILayout.Button("Send 0x01, 0x02 to " + firstclient))
            {
                Send(firstclient, new ArraySegment<byte>(new byte[]{0x01, 0x02}));
            }
            if (GUILayout.Button("Disconnect connection " + firstclient))
            {
                Disconnect(firstclient);
            }
            if (GUILayout.Button("Stop"))
            {
                StopServer();
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
