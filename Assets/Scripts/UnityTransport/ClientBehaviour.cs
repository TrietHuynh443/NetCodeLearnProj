using System;
using System.Collections;
using System.Text;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

public class ClientBehaviour : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    NetworkDriver _driver;
    NetworkConnection _connection;

    private bool _isReceivedPong = true;
    private float _lastSendPing;
    private bool _isStop;
    private bool _isConnected;

    void Start()
    {
        _driver = NetworkDriver.Create();
        var endpoint = NetworkEndpoint.AnyIpv4.WithPort(7777);
        _connection = _driver.Connect(endpoint);
        StartCoroutine(WaitForConnectSuccess());
        StartCoroutine(PingServer());
        StartCoroutine(GetPong());
    }

    private IEnumerator WaitForConnectSuccess()
    {
        while (_connection.PopEvent(_driver, out _) != NetworkEvent.Type.Connect)
        {
            yield return null;
        }
        _isConnected = true;
        Debug.Log("Connect success to server");
    }

    private IEnumerator GetPong()
    {
        yield return new WaitUntil(() => _isConnected);

        while (!_isStop)
        {
            NetworkEvent.Type cmd;
            if ((cmd = _connection.PopEvent(_driver, out var stream)) == NetworkEvent.Type.Data)
            {
                NativeArray<byte> data = new NativeArray<byte>(stream.Length, Allocator.Temp);
                stream.ReadBytes(data);
                try
                {
                    string value = System.Text.Encoding.ASCII.GetString(data);
                    if (value == "pong")
                    {
                        _isReceivedPong = true;
                    }
                    Debug.Log($"Got the value {value} back from the server.");
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                Debug.Log("Client got disconnected from server.");
                _connection = default;
                _isStop = true;
            }
            yield return null;
        }
        
    }

    private IEnumerator PingServer()
    {
        yield return new WaitUntil(() => _isConnected);
        
        _lastSendPing = Time.time;
        while (!_isStop)
        {
            if (_isReceivedPong && Time.time - _lastSendPing >= 3f)
            {
                _isReceivedPong = false;
                _lastSendPing = Time.time;
                Debug.Log($"Sending ping at {_lastSendPing}");
                _driver.BeginSend(_connection, out var writer);
                var data = Encoding.UTF8.GetBytes("ping");
                writer.WriteBytes(data);
                _driver.EndSend(writer);
            }

            yield return null;
        }
    }

    void OnDestroy()
    {
        _driver.Dispose();
    }

    void Update()
    {
        _driver.ScheduleUpdate().Complete();

        if (!_connection.IsCreated)
        {
            return;
        }
    }
}
