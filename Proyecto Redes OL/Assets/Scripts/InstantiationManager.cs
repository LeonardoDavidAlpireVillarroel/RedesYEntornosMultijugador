using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InstantiationManager : NetworkBehaviour
{
    [SerializeField]
    private GameObject characterPrefab; // Prefab del personaje a instanciar

    private readonly Dictionary<ulong, GameObject> spawnedCharacters = new Dictionary<ulong, GameObject>(); // Diccionario de personajes instanciados por cliente

    // Se suscribe al evento de carga de escena cuando el manager se activa
    private void OnEnable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.SceneManager.OnSceneEvent += HandleSceneEvent; // Registra el evento para cuando se cargue una escena
        }
    }

    // Se desuscribe del evento de carga de escena cuando el manager se desactiva
    private void OnDisable()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnSceneEvent -= HandleSceneEvent; // Elimina el manejador de evento de carga de escena
        }
    }

    // Maneja los eventos de cambio de escena
    private void HandleSceneEvent(SceneEvent sceneEvent)
    {
        if (sceneEvent.SceneEventType == SceneEventType.LoadComplete && IsServer) // Solo en el servidor
        {
            Debug.Log("Nueva escena cargada. Instanciando personajes.");
            Lever.ResetAllLevers(); // Resetea todas las palancas
            SpawnCharactersForClients(); // Instancia los personajes para todos los clientes
        }
    }

    // Cambia de escena y respawnea a todos los jugadores
    public void ChangeSceneAndRespawn(string newSceneName)
    {
        if (IsServer) // Solo el servidor puede cambiar de escena
        {
            Debug.Log("Eliminando personajes antes de cambiar de escena.");
            DespawnAllCharacters(); // Elimina todos los personajes existentes

            Debug.Log($"Cargando la nueva escena: {newSceneName}");
            NetworkManager.Singleton.SceneManager.LoadScene(newSceneName, LoadSceneMode.Single); // Carga la nueva escena
        }
    }

    // Instancia personajes para todos los clientes
    private void SpawnCharactersForClients()
    {
        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds) // Recorre todos los clientes conectados
        {
            if (!spawnedCharacters.ContainsKey(clientId)) // Si el cliente no tiene un personaje instanciado
            {
                SpawnCharacterForClient(clientId); // Instancia el personaje para ese cliente
            }
        }
    }

    // Instancia un personaje para un cliente específico
    private void SpawnCharacterForClient(ulong clientId)
    {
        if (characterPrefab == null) // Verifica que el prefab del personaje esté asignado
        {
            Debug.LogError("El prefab del personaje no está asignado.");
            return;
        }

        Vector3 spawnPosition = GetSpawnPosition(clientId); // Obtiene la posición de aparición
        GameObject characterInstance = Instantiate(characterPrefab, spawnPosition, Quaternion.identity); // Instancia el personaje

        DontDestroyOnLoad(characterInstance); // Hace que el personaje persista entre escenas

        NetworkObject networkObject = characterInstance.GetComponent<NetworkObject>();
        if (networkObject != null) // Verifica que el prefab tenga el componente NetworkObject
        {
            networkObject.SpawnWithOwnership(clientId); // Asigna la propiedad del objeto al cliente
            spawnedCharacters[clientId] = characterInstance; // Guarda el personaje en el diccionario

            if (clientId == NetworkManager.Singleton.LocalClientId) // Si es el cliente local
            {
                var playerController = characterInstance.GetComponent<PlayerController>();
                playerController?.InitializeCamera(); // Inicializa la cámara para el jugador local
            }

            Debug.Log($"Personaje instanciado para el cliente {clientId}");
        }
        else
        {
            Debug.LogError("El prefab no tiene un componente NetworkObject.");
        }
    }

    // Destruye todos los personajes instanciados
    private void DespawnAllCharacters()
    {
        foreach (var clientId in new List<ulong>(spawnedCharacters.Keys)) // Itera sobre las claves del diccionario
        {
            DespawnCharacter(clientId); // Destruye el personaje de cada cliente
        }
    }

    // Destruye el personaje de un cliente específico
    private void DespawnCharacter(ulong clientId)
    {
        if (spawnedCharacters.TryGetValue(clientId, out GameObject character)) // Si el personaje existe en el diccionario
        {
            if (character != null)
            {
                NetworkObject networkObject = character.GetComponent<NetworkObject>();
                if (networkObject != null && networkObject.IsSpawned) // Verifica que el objeto esté instanciado en la red
                {
                    networkObject.Despawn(true); // Desinstancia el personaje de la red
                }
                Destroy(character); // Elimina el objeto del personaje
            }
            spawnedCharacters.Remove(clientId); // Elimina el personaje del diccionario
        }
    }

    // Calcula una posición de aparición basada en el ID del cliente
    private Vector3 GetSpawnPosition(ulong clientId)
    {
        float radius = 5f; // Define el radio de aparición
        float angle = clientId * Mathf.PI * 2 / NetworkManager.Singleton.ConnectedClientsIds.Count; // Calcula un ángulo único por cliente
        float x = Mathf.Cos(angle) * radius;
        float z = Mathf.Sin(angle) * radius;
        return new Vector3(x, 1.1f, z); // Retorna la posición calculada
    }

    // Solicita un cambio de escena desde un cliente
    [ServerRpc(RequireOwnership = false)]
    public void RequestSceneChangeServerRpc(string newSceneName, ServerRpcParams rpcParams = default)
    {
        if (IsServer) // Solo el servidor puede cambiar la escena
        {
            Debug.Log($"Cliente {rpcParams.Receive.SenderClientId} solicitó cambiar a la escena {newSceneName}");
            ChangeSceneAndRespawn(newSceneName); // Cambia de escena y respawnea a los jugadores
        }
    }
}
