using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MoveSceneManager : NetworkBehaviour
{
    [SerializeField] private string loadLevel;
    private HashSet<ulong> playersInZone = new HashSet<ulong>();
    [SerializeField] private bool lever1Active = false;
    [SerializeField] private bool lever2Active = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && other.TryGetComponent<NetworkObject>(out NetworkObject networkObject))
        {
            PlayerEnteredZoneServerRpc(networkObject.OwnerClientId);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && other.TryGetComponent<NetworkObject>(out NetworkObject networkObject))
        {
            PlayerExitedZoneServerRpc(networkObject.OwnerClientId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlayerEnteredZoneServerRpc(ulong clientId)
    {
        playersInZone.Add(clientId);
        CheckConditions();
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlayerExitedZoneServerRpc(ulong clientId)
    {
        playersInZone.Remove(clientId);
    }

    public void ActivateLever1()
    {
        lever1Active = true;
        CheckConditions();
    }

    public void ActivateLever2()
    {
        lever2Active = true;
        CheckConditions();
    }

    private void CheckConditions()
    {
        if (IsServer)
        {
            int totalPlayers = NetworkManager.Singleton.ConnectedClientsIds.Count;
            if (playersInZone.Count == totalPlayers && lever1Active && lever2Active)
            {
                Debug.Log("Condiciones cumplidas. Cambiando de nivel.");
                RequestChangeScene(loadLevel);
            }
        }
    }

    private void RequestChangeScene(string sceneName)
    {
        if (IsServer)
        {
            ResetPlayerStates();
            var instantiationManager = FindObjectOfType<InstantiationManager>();
            if (instantiationManager != null)
            {
                instantiationManager.ChangeSceneAndRespawn(sceneName);
            }
            else
            {
                Debug.LogError("No se encontró el script InstantiationManager en la escena.");
            }
        }
    }

    private void ResetPlayerStates()
    {
        foreach (var clientId in playersInZone)
        {
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            {
                var playerController = client.PlayerObject.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    playerController.leverUsed = false;
                }
            }
        }
        Lever.ResetAllLevers();
    }
}
