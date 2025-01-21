using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

[Serializable]
struct ChatUserdata : INetworkSerializable //Al trabajar con RPC elementos complejos, hay que explicarle como vamos a trabajar para que se serialice a traves de una interfaz de serializacion
{

    public string chatUsername;
    public ulong chatUserId;

    void INetworkSerializable.NetworkSerialize<T>(BufferSerializer<T> serializer)
    {
        serializer.SerializeValue(ref chatUsername);
        serializer.SerializeValue(ref chatUserId);
    }
}


public class ChatUserList : NetworkBehaviour
{
    public TMPro.TMP_Text userListLog;
    private static ChatUserList singleton;

    [SerializeField]List<ChatUserdata> chatUsers;

    private void Start()
    {
        chatUsers = new List<ChatUserdata>();
        NetworkManager.Singleton.OnClientConnectedCallback += ClientConnectedCallbackMethod;
        NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisonnectCallbackMethod;

    }

    public static ChatUserList Singleton
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

    public string GetUsernameById(ulong userId)
    {
        var user = chatUsers.FirstOrDefault(u => u.chatUserId == userId);
        return user.chatUsername ?? "Unknown"; // Si no encuentra el usuario, devuelve "Unknown"
    }

    private void ClientConnectedCallbackMethod(ulong connectedClientId)
    {
        if (NetworkManager.Singleton.LocalClientId == connectedClientId)
        {
            ChatUserdata newUserChatData = new ChatUserdata();
            newUserChatData.chatUserId=NetworkManager.Singleton.LocalClientId;
            newUserChatData.chatUsername = ChatConnectionManager.Singleton.ChatUserNameInput.text;

            AddConnectedClientServerRpc(newUserChatData);
        }
    }
    private void ClientDisonnectCallbackMethod(ulong disconnectedClientId)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            for (int i = chatUsers.Count-1; i >= 0; i--)
            {
                if (chatUsers[i].chatUserId == disconnectedClientId)
                {
                    chatUsers.RemoveAt(i);
                }
            }
            UpdatUserConnectedListClientRpc(chatUsers.ToArray());
        }
        if (disconnectedClientId == NetworkManager.Singleton.LocalClientId)
        {
            chatUsers.Clear();
        }
    }


    [ServerRpc(RequireOwnership =false)]
    private void AddConnectedClientServerRpc(ChatUserdata newChatUserData)//Se puede pasar esto como parametro porque implementamos la interfaz INetworkSer...
    {
        chatUsers.Add(newChatUserData);
        UpdatUserConnectedListClientRpc(chatUsers.ToArray());
    }

    [ClientRpc]
    private void UpdatUserConnectedListClientRpc(ChatUserdata [] userList)//No es lo ideal pasar arrays por ser pesados, en este caso como no hay necesidad de optimizacion se hara asi
    {
        chatUsers = userList.ToList();
        UpdateCharUserLog();
    }

    private void UpdateCharUserLog()
    {
        userListLog.text = "";
        for (int i = 0; i < chatUsers.Count; i++)
        {
            userListLog.text += chatUsers[i].chatUserId + "-" + chatUsers[i].chatUsername + "\n";
        }
    }
}
