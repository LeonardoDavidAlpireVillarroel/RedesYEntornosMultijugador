using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Netcode;
using System;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System.Threading.Tasks;
using Unity.Services.Relay.Models;
using System.Linq.Expressions;
using Unity.Services.Relay;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;

public class NewBehaviourScript : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI joinRoomCodeTextField;
    [SerializeField] TMP_InputField input_roomCodeToJoin;



    private async void Start()
    {
        joinRoomCodeTextField.text = " - - ";
        NetworkManager.Singleton.OnClientDisconnectCallback += DisconnectFromRelay;

        await UnityServices.InitializeAsync();
        //await AuthenticationService.Instance.SignInAnonymouslyAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }



    public async void StartRelayServer_CreateRoom()
    {
        if (string.IsNullOrWhiteSpace(ChatConnectionManager.Singleton.ChatUserNameInput.text))
        {
            ConnectionManager.Singleton.errorMessage.text= "Debes introducir un nombre antes de crear la sala.";
            return; //Evita que se cree la sala sin un nombre
        }
        string joinCode = await StartHostWithRelay();
        joinRoomCodeTextField.text = "";
        joinRoomCodeTextField.text += joinCode;

    }
    private async Task<String> StartHostWithRelay(int maxConnections = 10)
    {
        Allocation allocation;
        try 
        { 
            allocation=await RelayService.Instance.CreateAllocationAsync(maxConnections);
        }
        catch
        {
            Debug.Log("Creating allocation for relay server failed");
            throw;
        }
        if (allocation != null)
        {
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(allocation, "dtls"));
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            return NetworkManager.Singleton.StartHost() ? joinCode : null;

        }
        else
        {
            return "Error";
        }
        
    }
    public async void JoinRelayServer()
    {
        if (string.IsNullOrWhiteSpace(ChatConnectionManager.Singleton.ChatUserNameInput.text))
        {
            ConnectionManager.Singleton.errorMessage.text = "Debes introducir un nombre antes de unirte a la sala.";
            return; //Evita que el jugador se conecte sin nombre
        }
        await StartClientJoinRelayServer(input_roomCodeToJoin.text);
    }

    public async Task<bool> StartClientJoinRelayServer(string joinCode)
    {
        JoinAllocation joinAllocation=await RelayService.Instance.JoinAllocationAsync(joinCode);

        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAllocation, "dtls"));

        return !string.IsNullOrEmpty(joinCode) && NetworkManager.Singleton.StartClient();
    }

    public void DisconnectFromRelay(ulong obj)
    {
        joinRoomCodeTextField.text = " - - ";
    }
}
