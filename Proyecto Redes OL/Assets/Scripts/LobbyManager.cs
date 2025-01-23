using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class LobbyManager : NetworkBehaviour
{
    [SerializeField] GameObject readyButton; // Bot�n de 'Listo'
    [SerializeField] GameObject startGameButton; // Bot�n de 'Iniciar Juego'
    [SerializeField] private List<ulong> playersReady = new List<ulong>();  // Lista de jugadores que han marcado 'Listo'
    [SerializeField] private TMPro.TMP_Text userList; // Panel de texto donde se pueden listar los jugadores


    // Al inicio, desactivar el bot�n de "Start"
    private void Start()
    {
        startGameButton.SetActive(false);  // Deshabilitar el bot�n "Start" al inicio
    }

    // M�todo llamado cuando un jugador presiona "Listo"
    public void OnPlayerReady()
    {
        if (IsServer)  // Solo el servidor puede manejar la lista de jugadores listos
        {
            ulong playerId = NetworkManager.Singleton.LocalClientId;

            // Si el jugador no est� en la lista de jugadores listos, agregarlo
            if (!playersReady.Contains(playerId))
            {
                playersReady.Add(playerId);
                Debug.Log($"Jugador {playerId} ha marcado 'Listo'.");
            }

            // Verificar si todos los jugadores est�n listos
            if (playersReady.Count == NetworkManager.Singleton.ConnectedClientsList.Count)
            {
                // Si todos est�n listos, habilitar el bot�n "Start"
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

        // Verificar si todos los jugadores est�n listos
        if (playersReady.Count == NetworkManager.Singleton.ConnectedClientsList.Count)
        {
            EnableStartButtonClientRpc();  // Habilitar el bot�n "Start" para todos los jugadores
        }
    }

    // ClientRpc que habilita el bot�n "Start" para todos los clientes
    [ClientRpc]
    public void EnableStartButtonClientRpc()
    {
        startGameButton.SetActive(true);  // Activar el bot�n de 'Iniciar Juego'
        Debug.Log("El bot�n de 'Start' ha sido habilitado para todos los jugadores.");
    }

    // M�todo que se llama cuando se hace clic en el bot�n "Start"
    public void StartGame()
    {
        if (!IsServer) return;  // Solo el servidor puede iniciar el juego

        // Cargar la escena del juego
        NetworkManager.Singleton.SceneManager.LoadScene("Level1", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
}