using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

[Serializable]
class ChatUserdata : INetworkSerializable
{
    public string chatUsername;
    public ulong chatUserId;

    // Se usa solo para serialización, sin lógica de asignación automática.
    void INetworkSerializable.NetworkSerialize<T>(BufferSerializer<T> serializer)
    {
        serializer.SerializeValue(ref chatUsername);
        serializer.SerializeValue(ref chatUserId);
    }
}

public class ChatUserList : NetworkBehaviour
{
    private static ChatUserList singleton;

    [SerializeField] private List<ChatUserdata> chatUsers = new List<ChatUserdata>();

    private void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += ClientConnectedCallbackMethod;
            NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnectedCallbackMethod;
        }

        if (IsServer)
        {
            UpdatUserConnectedListClientRpc(chatUsers.ToArray());
        }
    }

    public static ChatUserList Singleton
    {
        get { return singleton; }
    }

    private void Awake()
    {
        if (singleton == null)
        {
            singleton = this;
            DontDestroyOnLoad(gameObject); // ⬅️ Evita que se destruya al cambiar de escena
        }
        else
        {
            Destroy(gameObject); // ⬅️ Evita duplicados
        }
    }


    public string GetUsernameById(ulong userId)
    {
        var user = chatUsers.FirstOrDefault(u => u.chatUserId == userId);
        return user != null ? user.chatUsername : "Unknown";
    }

    private void ClientConnectedCallbackMethod(ulong connectedClientId)
    {
        if (NetworkManager.Singleton.LocalClientId == connectedClientId)
        {
            string username = ChatConnectionManager.Singleton.ChatUserNameInput.text;

            ChatUserdata newUserChatData = new ChatUserdata
            {
                chatUserId = connectedClientId,
                chatUsername = username
            };

            AddConnectedClientServerRpc(newUserChatData);
        }
    }

    private void ClientDisconnectedCallbackMethod(ulong disconnectedClientId)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            chatUsers.RemoveAll(u => u.chatUserId == disconnectedClientId);
            UpdatUserConnectedListClientRpc(chatUsers.ToArray());
        }

        if (disconnectedClientId == NetworkManager.Singleton.LocalClientId)
        {
            chatUsers.Clear();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void AddConnectedClientServerRpc(ChatUserdata newChatUserData)
    {
        chatUsers.Add(newChatUserData);
        UpdatUserConnectedListClientRpc(chatUsers.ToArray());
    }

    [ClientRpc]
    private void UpdatUserConnectedListClientRpc(ChatUserdata[] userList)
    {
        chatUsers = userList.ToList();
    }
}
