using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : NetworkBehaviour
{
    [SerializeField] private GameObject readyButton;
    [SerializeField] private GameObject startGameButton;
    [SerializeField] private TMP_Text userList;
    [SerializeField] private GameObject menuCanvas;
    [SerializeField] private GameObject chatPanel;
    [SerializeField] private GameObject disconnectButton;

    private static LobbyManager singleton;
    private Dictionary<ulong, bool> playersReady = new Dictionary<ulong, bool>();

    private void Start()
    {
        startGameButton.SetActive(false); // Desactiva el botón de inicio al comienzo
    }

    private void Awake()
    {
        // Asegura que solo exista una instancia de LobbyManager
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
            if (!playersReady.ContainsKey(playerId))
            {
                playersReady.Add(playerId, false);
            }
            playersReady[playerId] = true; // Marca al jugador como listo
            Debug.Log($"Jugador {playerId} ha marcado 'Listo'.");
            UpdatePlayerListClientRpc(playersReady.Keys.ToArray(), playersReady.Values.ToArray());

            // Habilita el botón de inicio si todos los jugadores están listos
            if (playersReady.Count == ConnectionManager.Singleton.connectedClients.Count)
            {
                EnableStartButtonClientRpc();
            }
        }
        else
        {
            OnPlayerReadyServerRpc(NetworkManager.Singleton.LocalClientId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void OnPlayerReadyServerRpc(ulong playerId)
    {
        if (!playersReady.ContainsKey(playerId))
        {
            playersReady.Add(playerId, false);
            userList.text += playerId + " - Not Ready\n";
            Debug.Log($"Servidor: Jugador {playerId} ha marcado 'Listo'.");
        }
        UpdatePlayerListClientRpc(playersReady.Keys.ToArray(), playersReady.Values.ToArray());

        if (playersReady.Count == ConnectionManager.Singleton.connectedClients.Count)
        {
            EnableStartButtonClientRpc();
        }
    }

    [ClientRpc]
    public void EnableStartButtonClientRpc()
    {
        startGameButton.SetActive(true); // Habilita el botón de inicio en todos los clientes
        Debug.Log("El botón de 'Start' ha sido habilitado para todos los jugadores.");
    }

    public void StartGame()
    {
        if (!IsServer) return;
        if (menuCanvas != null)
        {
            DisableMenuCanvas(); // Desactiva el menú en el servidor
        }
        DisableMenuCanvasClientRpc(); // Desactiva el menú en todos los clientes
        NetworkManager.Singleton.SceneManager.LoadScene("Level1", LoadSceneMode.Single);
    }

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
        // Desactiva todos los paneles del menú y lobby excepto el chat y el botón de desconexión
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

    [ClientRpc]
    public void DisableMenuCanvasClientRpc()
    {
        startGameButton.SetActive(false);
        disconnectButton.SetActive(true);
        // Desactiva los paneles en todos los clientes excepto el chat y el botón de desconexión
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