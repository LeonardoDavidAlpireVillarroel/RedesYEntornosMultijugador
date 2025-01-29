using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MoveSceneManager : NetworkBehaviour
{
    [SerializeField] private string loadLevel; // Nombre de la escena a cargar
    private HashSet<ulong> playersInZone = new HashSet<ulong>();  // Lista de jugadores en la zona
    [SerializeField] private bool lever1Active = false;  // Estado de la palanca 1
    [SerializeField] private bool lever2Active = false;  // Estado de la palanca 2

    // Cuando un jugador entra en la zona de la activación
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && other.TryGetComponent<NetworkObject>(out NetworkObject networkObject))
        {
            // Notificar al servidor que un jugador ha entrado en la zona
            PlayerEnteredZoneServerRpc(networkObject.OwnerClientId);
        }
    }

    // Cuando un jugador sale de la zona de la activación
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && other.TryGetComponent<NetworkObject>(out NetworkObject networkObject))
        {
            // Notificar al servidor que un jugador ha salido de la zona
            PlayerExitedZoneServerRpc(networkObject.OwnerClientId);
        }
    }

    // ServerRpc que se llama cuando un jugador entra en la zona
    [ServerRpc(RequireOwnership = false)]
    private void PlayerEnteredZoneServerRpc(ulong clientId)
    {
        playersInZone.Add(clientId);  // Añadir el jugador a la lista de jugadores en la zona
        CheckConditions();  // Comprobar si se cumplen las condiciones para cambiar de nivel
    }

    // ServerRpc que se llama cuando un jugador sale de la zona
    [ServerRpc(RequireOwnership = false)]
    private void PlayerExitedZoneServerRpc(ulong clientId)
    {
        playersInZone.Remove(clientId);  // Eliminar el jugador de la lista de jugadores en la zona
    }

    // Método para activar la palanca 1
    public void ActivateLever1()
    {
        lever1Active = true;  // Activar la palanca 1
        CheckConditions();  // Comprobar si se cumplen las condiciones para cambiar de nivel
    }

    // Método para activar la palanca 2
    public void ActivateLever2()
    {
        lever2Active = true;  // Activar la palanca 2
        CheckConditions();  // Comprobar si se cumplen las condiciones para cambiar de nivel
    }

    // Comprobar las condiciones para cambiar de nivel
    private void CheckConditions()
    {
        if (IsServer)  // Solo el servidor puede comprobar y cambiar de nivel
        {
            int totalPlayers = NetworkManager.Singleton.ConnectedClientsIds.Count;  // Obtener el total de jugadores conectados

            // Si todos los jugadores están en la zona y ambas palancas están activadas
            if (playersInZone.Count == totalPlayers && lever1Active && lever2Active)
            {
                Debug.Log("Condiciones cumplidas. Cambiando de nivel.");
                RequestChangeScene(loadLevel);  // Solicitar el cambio de escena
            }
        }
    }

    // Solicitar el cambio de escena y la reaparición de los jugadores
    private void RequestChangeScene(string sceneName)
    {
        if (IsServer)  // Solo el servidor puede cambiar de nivel
        {
            ResetPlayerStates(); // Restablecer el estado de los jugadores y las palancas
            
            var instantiationManager = FindObjectOfType<InstantiationManager>();
            if (instantiationManager != null)
            {
                // Delegar el cambio de escena y la reaparición a InstantiationManager
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
                    playerController.leverUsed = false; // Restablecer el estado de la interacción con la palanca
                }
            }
        }
        // Opcional: Restablecer los estados de las palancas en la escena
        Lever.ResetAllLevers();
    }
}
