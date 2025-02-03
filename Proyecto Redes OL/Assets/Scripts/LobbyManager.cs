using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : NetworkBehaviour
{
    [SerializeField] GameObject readyButton;
    [SerializeField] GameObject startGameButton;
    [SerializeField] private TMPro.TMP_Text userList;

    private Dictionary<ulong, bool> playersReady = new Dictionary<ulong, bool>(); // Diccionario de jugadores y su estado de "Listo"

    private void Start()
    {
        startGameButton.SetActive(false);  // Deshabilitar el botón "Start" al inicio
    }

    public void OnPlayerReady()
    {
        if (IsServer)
        {
            ulong playerId = NetworkManager.Singleton.LocalClientId;

            // Agregar el jugador a la lista de listos si no está en ella
            if (!playersReady.ContainsKey(playerId))
            {
                playersReady.Add(playerId, false);
            }

            // Marcar al jugador como listo
            playersReady[playerId] = true;
            Debug.Log($"Jugador {playerId} ha marcado 'Listo'.");

            // Actualiza la lista de jugadores en todos los clientes
            UpdatePlayerListClientRpc(playersReady.Keys.ToArray(), playersReady.Values.ToArray());

            // Verificar si todos los jugadores están listos
            if (playersReady.Count == ConnectionManager.Singleton.connectedClients.Count)
            {
                EnableStartButtonClientRpc();
            }
        }
        else
        {
            // Notificar al servidor que el jugador está listo
            OnPlayerReadyServerRpc(NetworkManager.Singleton.LocalClientId);
        }
    }

    // ServerRpc para actualizar la lista de jugadores listos
    [ServerRpc(RequireOwnership = false)]
    public void OnPlayerReadyServerRpc(ulong playerId)
    {
        if (!playersReady.ContainsKey(playerId))
        {
            playersReady.Add(playerId, false);
            userList.text += playerId + " - Not Ready\n"; // Mostrar ID del jugador con "Not Ready"
            Debug.Log($"Servidor: Jugador {playerId} ha marcado 'Listo'.");
        }

        // Actualiza la lista de jugadores en todos los clientes
        UpdatePlayerListClientRpc(playersReady.Keys.ToArray(), playersReady.Values.ToArray());

        if (playersReady.Count == ConnectionManager.Singleton.connectedClients.Count)
        {
            EnableStartButtonClientRpc(); // Habilitar botón de "Start"
        }
    }

    [ClientRpc]
    public void EnableStartButtonClientRpc()
    {
        startGameButton.SetActive(true);
        Debug.Log("El botón de 'Start' ha sido habilitado para todos los jugadores.");
    }

    // Este método será invocado cuando se haga clic en el botón "Start"
    public void StartGame()
    {
        if (!IsServer) return;

        NetworkManager.Singleton.SceneManager.LoadScene("Level1", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    // Este ClientRpc actualizará la lista de jugadores en todos los clientes
    [ClientRpc]
    public void UpdatePlayerListClientRpc(ulong[] playerIds, bool[] readyStatuses)
    {
        for (int i = 0; i < playerIds.Length; i++)
        {
            string status = readyStatuses[i] ? "Ready" : "Not Ready";
            userList.text += $"\nJugador {playerIds[i]} ha marcado 'Listo'.";

        }
    }
}
