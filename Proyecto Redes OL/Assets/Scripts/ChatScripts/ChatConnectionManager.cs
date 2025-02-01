using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class ChatConnectionManager : MonoBehaviour
{
    private static ChatConnectionManager singleton;
    public static ChatConnectionManager Singleton => singleton;

    [SerializeField] TMPro.TMP_InputField chatUserNameInput;
    public TMPro.TMP_InputField ChatUserNameInput => chatUserNameInput;

    private void Awake()
    {
        if (singleton == null)
        {
            singleton = this;

            // Buscar el primer TMP_InputField en la escena (si no está asignado en el inspector)
            if (chatUserNameInput == null)
            {
                chatUserNameInput = FindObjectOfType<TMPro.TMP_InputField>();
            }

            DontDestroyOnLoad(this.gameObject); // Si deseas que este objeto persista entre escenas
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    // Función para obtener la lista de jugadores conectados desde ConnectionManager
    public List<Username> GetConnectedPlayers()
    {
        // Acceder a la lista de jugadores conectados de ConnectionManager
        return ConnectionManager.Singleton.connectedClients;
    }
}