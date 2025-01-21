using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MoveScene : NetworkBehaviour
{
    [SerializeField] private string loadLevel; // Nombre de la escena a cargar
    private HashSet<ulong> playersInZone = new HashSet<ulong>(); // Conjunto que almacena los IDs de los jugadores dentro de la zona

    // Se llama cuando un jugador entra en la zona del trigger
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && other.TryGetComponent<NetworkObject>(out NetworkObject networkObject))
        {
            // Notificar al servidor que un jugador ha entrado en la zona
            PlayerEnteredZoneServerRpc(networkObject.OwnerClientId);
        }
    }

    // Se llama cuando un jugador sale de la zona del trigger
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && other.TryGetComponent<NetworkObject>(out NetworkObject networkObject))
        {
            // Notificar al servidor que un jugador ha salido de la zona
            PlayerExitedZoneServerRpc(networkObject.OwnerClientId);
        }
    }

    // RPC que se llama en el servidor cuando un jugador entra en la zona
    [ServerRpc(RequireOwnership = false)]
    private void PlayerEnteredZoneServerRpc(ulong clientId)
    {
        playersInZone.Add(clientId); // Agregar al jugador a la lista de jugadores en la zona
        CheckPlayersInZone();       // Verificar si todos los jugadores están en la zona
    }

    // RPC que se llama en el servidor cuando un jugador sale de la zona
    [ServerRpc(RequireOwnership = false)]
    private void PlayerExitedZoneServerRpc(ulong clientId)
    {
        playersInZone.Remove(clientId); // Eliminar al jugador de la lista de jugadores en la zona
    }

    // Verifica si todos los jugadores están en la zona y si las palancas están activadas
    private void CheckPlayersInZone()
    {
        int totalPlayers = NetworkManager.Singleton.ConnectedClientsIds.Count; // Número total de jugadores conectados

        // Verificar si todos los jugadores están en la zona y las palancas están activadas
        if (playersInZone.Count == totalPlayers && Lever.activatedLeversCount == Lever.totalLevers)
        {
            Debug.Log("Todos los jugadores están en la zona y ambas palancas están activadas. Cambiando de nivel.");
            RequestChangeScene(loadLevel); // Solicitar el cambio de escena si se cumple la condición
        }
    }

    // Solicita el cambio de escena si se cumplen las condiciones
    private void RequestChangeScene(string sceneName)
    {
        if (IsServer) // Solo el servidor puede cambiar de escena
        {
            var instantiationManager = FindObjectOfType<InstantiationManager>(); // Buscar el gestor de instanciación
            if (instantiationManager != null)
            {
                instantiationManager.ChangeSceneAndRespawn(sceneName); // Delegar el cambio de escena al gestor de instanciación
            }
            else
            {
                Debug.LogError("No se encontró el script InstantiationManager en la escena.");
            }
        }
    }
}
