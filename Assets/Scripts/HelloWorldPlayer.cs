using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class HelloWorldPlayer : NetworkBehaviour
{
    [SerializeField] private TextMeshProUGUI _playerNameUI;
    private Vector3 _horizontal;
    private Vector3 _vertical;
    private bool _isNetworkSpawned = false;
    
    private void Update()
    {
        if(!IsOwner || IsServer || !_isNetworkSpawned) return;
        HandleInput();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _isNetworkSpawned = true;
        _playerNameUI.text = $"Player {NetworkObjectId.ToString()}";
    }

    private void HandleInput()
    {
        _horizontal = Input.GetAxis("Horizontal") * Time.deltaTime * Vector3.right;
        _vertical = Input.GetAxis("Vertical") *  Time.deltaTime * Vector3.forward;
        
        SendUpdatePositionToServerRpc(_horizontal ,_vertical, NetworkObjectId);
    }

    [Rpc(SendTo.Everyone)]
    private void SendUpdatePositionToServerRpc(Vector3 horizontal, Vector3 vertical, ulong clientId) //E
    {
        if (NetworkObjectId == clientId)
        {
            _horizontal = horizontal;
            _vertical = vertical;
        }
    }

    private void FixedUpdate()
    {
        Move();
    }
    private void Move()
    {
        transform.position += _horizontal;
        transform.position += _vertical;
    }
}
