using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ChatManager : NetworkBehaviour
{
    private static ChatManager singleton;

    [SerializeField]private TMPro.TMP_Text chatLog;


    [SerializeField]private TMPro.TMP_InputField messageToSendInput;




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
            DontDestroyOnLoad(gameObject);
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



    [ServerRpc(RequireOwnership =false)]
    private void SendMessageServerRpc(string messageToSend)
    {
        SendMessageClientRpc(messageToSend);
    }
    [ClientRpc]
    private void SendMessageClientRpc(string messageToSend)
    {
        chatLog.text += "\n" + messageToSend;
    }
}
