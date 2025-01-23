using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class WinGame : NetworkBehaviour
{
    [SerializeField] private GameObject winPanel; // Panel de victoria en el Canvas, se activa cuando se cumplen las condiciones.
    [SerializeField] private string lobbySceneName = "Multiplayer"; // Nombre de la escena del lobby.

    private HashSet<ulong> playersInZone = new HashSet<ulong>(); // Almacena los IDs de los jugadores que entran a la zona de victoria.

    // Método que se ejecuta cuando un jugador entra en la zona de victoria.
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && other.TryGetComponent<NetworkObject>(out NetworkObject networkObject))
        {
            PlayerEnteredZoneServerRpc(networkObject.OwnerClientId); // Notifica al servidor sobre el jugador que entró en la zona.
        }
    }

    // Método que se ejecuta cuando un jugador sale de la zona de victoria.
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && other.TryGetComponent<NetworkObject>(out NetworkObject networkObject))
        {
            PlayerExitedZoneServerRpc(networkObject.OwnerClientId); // Notifica al servidor sobre el jugador que salió de la zona.
        }
    }

    // RPC que se llama cuando un jugador entra en la zona de victoria.
    [ServerRpc(RequireOwnership = false)]
    private void PlayerEnteredZoneServerRpc(ulong clientId)
    {
        playersInZone.Add(clientId); // Añade al jugador a la zona.
        CheckPlayersInZone(); // Revisa si se cumplen las condiciones para mostrar el panel de victoria.
    }

    // RPC que se llama cuando un jugador sale de la zona de victoria.
    [ServerRpc(RequireOwnership = false)]
    private void PlayerExitedZoneServerRpc(ulong clientId)
    {
        playersInZone.Remove(clientId); // Elimina al jugador de la zona.
    }

    // Revisa si todos los jugadores están en la zona y si ambas palancas están activadas.
    private void CheckPlayersInZone()
    {
        int totalPlayers = NetworkManager.Singleton.ConnectedClientsIds.Count; // Obtiene el número total de jugadores.

        // Si todos los jugadores están en la zona y las palancas están activadas, muestra el panel de victoria.
        if (playersInZone.Count == totalPlayers && Lever.activatedLeversCount == Lever.totalLevers)
        {
            Debug.Log("Todos los jugadores están en la zona y ambas palancas están activadas. Mostrando panel de victoria.");
            ShowWinPanel();
        }
    }

    // Muestra el panel de victoria en el servidor.
    private void ShowWinPanel()
    {
        if (IsServer)
        {
            NotifyClientsWinClientRpc(); // Llama a un RPC para que todos los clientes vean el panel de victoria.
        }
    }

    // RPC que notifica a todos los clientes para que muestren el panel de victoria.
    [ClientRpc]
    private void NotifyClientsWinClientRpc()
    {
        if (winPanel != null)
        {
            winPanel.SetActive(true); // Activa el panel de victoria en todos los clientes.
        }
    }

    // Método que se llama cuando un jugador presiona el botón de regresar al lobby.
    public void RequestReturnToLobby()
    {
        if (IsServer)
        {
            ChangeSceneForAllClients(); // Si el servidor presiona el botón, cambia la escena para todos.
        }
        else
        {
            RequestReturnToLobbyServerRpc(); // Si es un cliente, pide al servidor que cambie la escena.
        }
    }

    // RPC que el cliente usa para pedir al servidor que cambie la escena.
    [ServerRpc(RequireOwnership = false)]
    private void RequestReturnToLobbyServerRpc(ServerRpcParams rpcParams = default)
    {
        Debug.Log($"Jugador {rpcParams.Receive.SenderClientId} solicitó volver al lobby.");
        ChangeSceneForAllClients(); // Cambia la escena para todos los jugadores.
    }

    // Cambia la escena para todos los jugadores.
    private void ChangeSceneForAllClients()
    {
        DestroyAllPlayers(); // Destruye los personajes antes de cambiar de escena.
        NotifyClientsToChangeSceneClientRpc(lobbySceneName); // Notifica a los clientes sobre el cambio de escena.
        NetworkManager.Singleton.SceneManager.LoadScene(lobbySceneName, UnityEngine.SceneManagement.LoadSceneMode.Single); // Cambia la escena en el servidor.
    }

    // Método que destruye todos los personajes antes de cambiar de escena.
    private void DestroyAllPlayers()
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null)
            {
                Destroy(client.PlayerObject.gameObject); // Elimina al jugador de la escena.
            }
        }
    }

    // RPC que notifica a todos los clientes que deben cambiar a la nueva escena.
    [ClientRpc]
    private void NotifyClientsToChangeSceneClientRpc(string sceneName)
    {
        Debug.Log($"Cambiando a la escena {sceneName} para todos los clientes.");
    }
}
