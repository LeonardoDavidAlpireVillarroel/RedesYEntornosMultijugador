using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class LobbyManager : NetworkBehaviour
{
    [SerializeField] GameObject readyButton; // Botón de 'Listo'
    [SerializeField] GameObject startGameButton; // Botón de 'Iniciar Juego'
    [SerializeField] private List<ulong> playersReady = new List<ulong>();  // Lista de jugadores que han marcado 'Listo'
    [SerializeField] private TMPro.TMP_Text userList; // Panel de texto donde se pueden listar los jugadores


    // Al inicio, desactivar el botón de "Start"
    private void Start()
    {
        startGameButton.SetActive(false);  // Deshabilitar el botón "Start" al inicio
    }

    // Método llamado cuando un jugador presiona "Listo"
    public void OnPlayerReady()
    {
        if (IsServer)  // Solo el servidor puede manejar la lista de jugadores listos
        {
            ulong playerId = NetworkManager.Singleton.LocalClientId;

            // Si el jugador no está en la lista de jugadores listos, agregarlo
            if (!playersReady.Contains(playerId))
            {
                playersReady.Add(playerId);
                Debug.Log($"Jugador {playerId} ha marcado 'Listo'.");
            }

            // Verificar si todos los jugadores están listos
            if (playersReady.Count == NetworkManager.Singleton.ConnectedClientsList.Count)
            {
                // Si todos están listos, habilitar el botón "Start"
                EnableStartButtonClientRpc();
            }
        }
        else
        {
            // Si no es el servidor, notificar al servidor usando ServerRpc
            OnPlayerReadyServerRpc(NetworkManager.Singleton.LocalClientId);
        }
    }


    // ServerRpc llamado desde el cliente para que el servidor actualice la lista de jugadores listos
    [ServerRpc(RequireOwnership = false)]
    public void OnPlayerReadyServerRpc(ulong playerId)
    {
        if (!playersReady.Contains(playerId))
        {
            playersReady.Add(playerId);  // Agregar al jugador a la lista de listos
            userList.text += playerId + "\n";  // Mostrar el ID del jugador en la interfaz
            Debug.Log($"Servidor: Jugador {playerId} ha marcado 'Listo'.");
        }

        // Verificar si todos los jugadores están listos
        if (playersReady.Count == NetworkManager.Singleton.ConnectedClientsList.Count)
        {
            EnableStartButtonClientRpc();  // Habilitar el botón "Start" para todos los jugadores
        }
    }

    // ClientRpc que habilita el botón "Start" para todos los clientes
    [ClientRpc]
    public void EnableStartButtonClientRpc()
    {
        startGameButton.SetActive(true);  // Activar el botón de 'Iniciar Juego'
        Debug.Log("El botón de 'Start' ha sido habilitado para todos los jugadores.");
    }

    // Método que se llama cuando se hace clic en el botón "Start"
    public void StartGame()
    {
        if (!IsServer) return;  // Solo el servidor puede iniciar el juego

        // Cargar la escena del juego
        NetworkManager.Singleton.SceneManager.LoadScene("Level1", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
}