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

    private List<ulong> playersReady = new List<ulong>(); // Lista de jugadores listos

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
            if (!playersReady.Contains(playerId))
            {
                playersReady.Add(playerId);
                Debug.Log($"Jugador {playerId} ha marcado 'Listo'.");
            }

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
        if (!playersReady.Contains(playerId))
        {
            playersReady.Add(playerId);
            userList.text += playerId + "\n"; // Mostrar ID del jugador
            Debug.Log($"Servidor: Jugador {playerId} ha marcado 'Listo'.");
        }

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

    public void StartGame()
    {
        if (!IsServer) return;

        NetworkManager.Singleton.SceneManager.LoadScene("Level1", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
}
