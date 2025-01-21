using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ChatManager : NetworkBehaviour
{
    private static ChatManager singleton;

    [SerializeField]private TMPro.TMP_Text chatLog;
    [SerializeField]private TMPro.TMP_Text userListLog;

    [SerializeField]private TMPro.TMP_InputField messageToSendInput;

    [SerializeField] private TMPro.TMP_InputField recipientUserIdInput; // Para ingresar el ID del destinatario
    [SerializeField] private TMPro.TMP_Text recipientPanel; // Panel para seleccionar destinatario (opcional)


    public static ChatManager Singleton
    {
        get
            { return singleton; }
    }

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

    public void SendMesagge()//Cliente local que enviara el mensaje llama server RPC y este llamara al clientRPC que se ejecutara en todos los clientes
    {
        // Verifica si el campo de entrada está vacío
        if (string.IsNullOrEmpty(messageToSendInput.text))
        {
            chatLog.text += "\nPlease enter a message.";
            return; // Salimos del método si no hay un ID ingresado
        }

        string username = ChatUserList.Singleton.GetUsernameById(NetworkManager.Singleton.LocalClientId); 
        string fullMessage = $"{username}: {messageToSendInput.text}";
        SendMessageServerRpc(fullMessage);
        messageToSendInput.text = "";
    }

    public void SendMessageToUser()
    {
        if (string.IsNullOrEmpty(messageToSendInput.text))
        {
            recipientPanel.text += "\nPlease enter a message for the recipient.";
            return; // Salimos del método si no hay un ID ingresado
        }
        // Verifica si el campo de entrada está vacío
        if (string.IsNullOrEmpty(recipientUserIdInput.text))
        {
            recipientPanel.text += "\nPlease enter the recipient ID.";
            return; // Salimos del método si no hay un ID ingresado
        }

        // Intentamos analizar el texto ingresado como un ulong
        if (!ulong.TryParse(recipientUserIdInput.text, out ulong recipientId))
        {
            recipientPanel.text += "\nThe recipient ID must be a valid number.";
            return; // Salimos del método si el análisis falla
        }
        if (recipientId == NetworkManager.Singleton.LocalClientId)
        {
            recipientPanel.text += "\nYou can't send a message to yourself.";
            return; // Salimos del método si el análisis falla
        }

        // Obtenemos el nombre de usuario del remitente
        string username = ChatUserList.Singleton.GetUsernameById(NetworkManager.Singleton.LocalClientId);
        string fullMessage = $"{username} (private): {messageToSendInput.text}";

        // Mostramos el mensaje localmente en el chat del remitente
        recipientPanel.text += "\n" + fullMessage;

        // Enviamos el mensaje al servidor para que valide y envíe al destinatario
        ValidateAndSendMessageToUserServerRpc(recipientId, fullMessage);

        // Limpiamos el campo de entrada
        messageToSendInput.text = "";
    }


    [ServerRpc(RequireOwnership =false)]//Solo se ejecutan si eres el owner, para que sea cualquiera se agrega el "Require..."
    private void SendMessageServerRpc(string messageToSend)//Llamado por cualquier cliente y ejecutado en el Servidor
    {
        SendMessageClientRpc(messageToSend);
    }
    [ClientRpc]
    private void SendMessageClientRpc(string messageToSend)//LLamado por el servidor y ejecutado en todos los clientes
    {
        chatLog.text += "\n" + messageToSend;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendMessageToUserServerRpc(ulong recipientId, string messageToSend)
    {
        // Llamamos a un ClientRpc que solo mostrará el mensaje al destinatario correspondiente
        SendMessageToUserClientRpc(recipientId, messageToSend);
    }

    // RPC que se ejecuta solo en el destinatario
    [ClientRpc]
    private void SendMessageToUserClientRpc(ulong recipientId, string messageToSend)
    {
        // Verificamos si el cliente local es el destinatario
        if (NetworkManager.Singleton.LocalClientId == recipientId)
        {
            recipientPanel.text += "\n" + messageToSend;
        }
    }
    [ServerRpc(RequireOwnership = false)]
    private void ValidateAndSendMessageToUserServerRpc(ulong recipientId, string messageToSend, ServerRpcParams serverRpcParams = default)
    {
        // Validamos si el destinatario está conectado
        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(recipientId))
        {
            // Enviamos un mensaje de error de vuelta al cliente remitente
            SendErrorMessageClientRpc("The client with ID " + recipientId + " does not exist or is offline.", serverRpcParams.Receive.SenderClientId);
            return;
        }

        // Si el destinatario está conectado, enviamos el mensaje solo a él
        SendMessageToUserClientRpc(recipientId, messageToSend);
    }

    [ClientRpc]
    private void SendMessageToUserClientRpc(ulong recipientId, string messageToSend, ClientRpcParams clientRpcParams = default)
    {
        // Verificamos si el cliente local es el destinatario
        if (NetworkManager.Singleton.LocalClientId == recipientId)
        {
            recipientPanel.text += "\n" + messageToSend;
        }
    }

    [ClientRpc]
    private void SendErrorMessageClientRpc(string errorMessage, ulong senderClientId, ClientRpcParams clientRpcParams = default)
    {
        // Mostramos el error solo al cliente que intentó enviar el mensaje
        if (NetworkManager.Singleton.LocalClientId == senderClientId)
        {
            recipientPanel.text += "\n" + errorMessage;
        }
    }

}
