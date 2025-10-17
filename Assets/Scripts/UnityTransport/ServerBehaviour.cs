using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Jobs;
using Unity.Networking.Transport;
using UnityEngine;

public class ServerBehaviour : MonoBehaviour
{
    NetworkDriver _driver;
    NativeList<NetworkConnection> _connections;

    private bool _isStop;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _driver = NetworkDriver.Create(new NetworkSettings()
        {
            
        });
        _connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);
        var endpoint = NetworkEndpoint.AnyIpv4.WithPort(7777);
        if (_driver.Bind(endpoint) != 0)
        {
            Debug.LogError("Failed to bind to port 7777.");
            return;
        }

        _driver.Listen();
        Debug.Log($"Start Server at {_driver.GetLocalEndpoint().ToString()}");
        StartCoroutine(AcceptNewConnection());
    }
    

    private IEnumerator AcceptNewConnection()
    {
        // Accept new connections.
        while (_driver.Listening)
        {
            NetworkConnection c;
            if ((c = _driver.Accept()) != default)
            {
                _connections.Add(c);
                Debug.Log("Accepted a connection.");
            }
            yield return null;
        }
    }

    private void FixedUpdate()
    {

    }

    // Update is called once per frame
    void Update()
    {
        foreach (var t in _connections)
        {
            DataStreamReader stream;
            NetworkEvent.Type cmd;
            if ((cmd = _driver.PopEventForConnection(t, out stream)) == NetworkEvent.Type.Empty)
            {
                continue;
            }
            
            if (cmd == NetworkEvent.Type.Disconnect)
            {
                Debug.Log($"Disconnect from {t.ToString()}");
                break;
            }

            if (cmd != NetworkEvent.Type.Data) continue;
            
            var rawData = new NativeArray<byte>(stream.Length, Allocator.Temp);
            try
            {
                stream.ReadBytes(rawData);
                string value = Encoding.UTF8.GetString(rawData.ToArray());
                Debug.Log($"Receive: {value}");
                if (value == "ping")
                {
                    Debug.Log($"Send pong back");
                    _driver.BeginSend(NetworkPipeline.Null, t, out var writer);
                    writer.WriteBytes(Encoding.UTF8.GetBytes("pong"));
                    _driver.EndSend(writer);
                }

            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        _driver.ScheduleUpdate().Complete();
    }
    
    void OnDestroy()
    {
        for (int i = 0; i < _connections.Length; ++i)
        {
            if (!_connections[i].IsCreated)
            {
                _connections.RemoveAtSwapBack(i);
                --i;
            }
        }
        if (_driver.IsCreated)
        {
            _driver.Dispose();
            _connections.Dispose();
        }
    }
}
