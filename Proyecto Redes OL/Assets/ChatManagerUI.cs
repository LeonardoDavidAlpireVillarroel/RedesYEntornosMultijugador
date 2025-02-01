using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ChatManagerUI : NetworkBehaviour
{
    private static ChatManagerUI singleton;

    [SerializeField] private TMPro.TMP_Text chatLog;
    [SerializeField] private TMPro.TMP_InputField messageToSendInput;
    [SerializeField] private Button sendButton;

    public static ChatManagerUI Singleton => singleton;

    private void Awake()
    {
        if (singleton == null)
        {
            singleton = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
    }


    public void SendMessage()
    {
        if (messageToSendInput == null || chatLog == null)
        {
            Debug.LogError("Error: chatLog or messageToSendInput is not assigned in the Inspector.");
            return;
        }

        if (string.IsNullOrEmpty(messageToSendInput.text))
        {
            chatLog.text += "\nPlease enter a message.";
            return;
        }

        if (ConnectionManager.Singleton == null)
        {
            Debug.LogError("Error: ConnectionManager instance is not available.");
            return;
        }

        // Verificar si el usuario está en la lista
        var currentUser = ConnectionManager.Singleton.connectedClients.Find(c => c.userId == NetworkManager.Singleton.LocalClientId);

        if (currentUser.Equals(default(Username)))
        {
            Debug.LogError("Error: Username not found in ConnectionManager.");
            return;
        }

        string username = currentUser.username;

        if (string.IsNullOrEmpty(username))
        {
            Debug.LogError("Error: Username is empty.");
            return;
        }

        Debug.Log($"Enviando mensaje como: {username}");

        string fullMessage = $"{username}: {messageToSendInput.text}";
        SendMessageServerRpc(fullMessage, NetworkManager.Singleton.LocalClientId);
        messageToSendInput.text = "";
    }


    [ServerRpc(RequireOwnership = false)]
    private void SendMessageServerRpc(string messageToSend, ulong senderId)
    {
        Debug.Log($"Mensaje recibido del cliente {senderId}: {messageToSend}");
        SendMessageClientRpc(messageToSend);
    }

    [ClientRpc]
    private void SendMessageClientRpc(string messageToSend)
    {
        chatLog.text += "\n" + messageToSend;
    }
}
