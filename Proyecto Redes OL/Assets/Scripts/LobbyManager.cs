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
    [SerializeField] private GameObject menuCanvas;
    [SerializeField] private GameObject chatPanel;  // Declarar la referencia al chatPanel
    [SerializeField] private GameObject disconnectButton;  // Referencia al botón de desconexión


    private static LobbyManager singleton;

    private Dictionary<ulong, bool> playersReady = new Dictionary<ulong, bool>(); // Diccionario de jugadores y su estado de "Listo"

    private void Start()
    {
        startGameButton.SetActive(false);  // Deshabilitar el botón "Start" al inicio
    }
    private void Awake()
    {
        // Si ya hay una instancia, destruye este objeto para evitar duplicados
        if (singleton == null)
        {
            singleton = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
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

        // Solo el servidor habilita la escena y la desactivación del canvas
        if (menuCanvas != null)
        {
            // Desactiva el menú en el servidor
            DisableMenuCanvas();
        }

        // Llama al ClientRpc para desactivar el canvas en todos los clientes
        DisableMenuCanvasClientRpc();

        // Cambia la escena solo en el servidor
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
    public void DisableMenuCanvas()
    {
        startGameButton.SetActive(false);
        disconnectButton.SetActive(true);
        // Desactiva todos los paneles del menú y el lobby, excepto el chat
        if (menuCanvas != null)
        {
            // Aquí desactivas todos los elementos menos el chatPanel y el disconnectButton
            foreach (Transform child in menuCanvas.transform)
            {
                if (child.gameObject != chatPanel && child.gameObject != disconnectButton)
                {
                    child.gameObject.SetActive(false);
                }
            }
        }
    }

    [ClientRpc]
    public void DisableMenuCanvasClientRpc()
    {
        startGameButton.SetActive(false);
        disconnectButton.SetActive(true);
        // Desactiva todos los paneles del menú y el lobby en todos los clientes, excepto el chatPanel
        if (menuCanvas != null)
        {
            foreach (Transform child in menuCanvas.transform)
            {
                if (child.gameObject != chatPanel && child.gameObject != disconnectButton)
                {
                    child.gameObject.SetActive(false);
                }
            }
        }
    }
}
