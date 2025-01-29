using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Lever : NetworkBehaviour
{
    [SerializeField] private GameObject leverObject; // Representación visual de la palanca
    public NetworkVariable<bool> isActivated = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server); // Estado sincronizado

    public static int activatedLeversCount = 0; // Ya no es estático para evitar problemas en red
    public const int totalLevers = 2; // Mantener fijo el total de palancas

    private void OnTriggerStay(Collider other)
    {
        PlayerController playerController = other.GetComponent<PlayerController>();

        if (other.CompareTag("Player") && Input.GetKeyDown(KeyCode.E) && !isActivated.Value && !playerController.leverUsed)
        {
            playerController.leverUsed = true;
            ActivateLeverServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ActivateLeverServerRpc()
    {
        isActivated.Value = true; // Sincronizar estado con `NetworkVariable`
        activatedLeversCount++;

        UpdateLeverStateClientRpc(isActivated.Value); // Actualiza visualmente en todos los jugadores
        UpdateLeverStatusClientRpc(activatedLeversCount, totalLevers); // Actualiza UI en todos los jugadores

        if (activatedLeversCount == totalLevers)
        {
            NotifyAllPlayersToChangeSceneServerRpc();
        }
    }

    [ClientRpc]
    private void UpdateLeverStateClientRpc(bool state)
    {
        isActivated.Value = state;

        if (leverObject != null)
        {
            leverObject.SetActive(!state);
        }
    }

    [ClientRpc]
    private void UpdateLeverStatusClientRpc(int activatedLevers, int totalLevers)
    {
        PlayerUIManager.Singleton.UpdateLeverStatus(activatedLevers, totalLevers); // Actualiza la UI en todos los jugadores
    }

    [ServerRpc(RequireOwnership = false)]
    private void NotifyAllPlayersToChangeSceneServerRpc()
    {
        Debug.Log("Ambas palancas activadas. Cambiar de nivel.");
    }

    public static void ResetAllLevers()
    {
        Lever[] allLevers = FindObjectsOfType<Lever>(); // Encuentra todas las palancas en la escena.

        foreach (var lever in allLevers)
        {
            lever.ResetLever(); // Llama al método para resetear cada palanca.

        }
    }

    // Método para reiniciar una palanca individualmente.
    public void ResetLever()
    {
        activatedLeversCount = 0;
        isActivated.Value = false; // Desactiva la palanca en la red.

        if (leverObject != null)
        {
            leverObject.SetActive(true); // Reactiva la representación visual de la palanca.
        }
    }
}
