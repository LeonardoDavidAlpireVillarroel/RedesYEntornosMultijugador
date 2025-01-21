using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Lever : NetworkBehaviour
{
    [SerializeField] private bool isActivated = false; // Estado de la palanca, indica si la palanca est� activada.
    [SerializeField] private GameObject leverObject; // El objeto visual de la palanca (para mostrar o esconder).
    public static int activatedLeversCount = 0; // Contador de palancas activadas a nivel global.
    public static int totalLevers = 2; // Total de palancas en el mapa (se puede modificar dependiendo de la cantidad de palancas en la escena).

    // Este m�todo se llama cuando un jugador est� cerca de la palanca y presiona la tecla "E" para activarla.
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && Input.GetKeyDown(KeyCode.E) && !isActivated)
        {
            ActivateLeverServerRpc(); // Llama al ServerRpc para activar la palanca en el servidor.
        }
    }

    // RPC para activar la palanca en el servidor.
    [ServerRpc(RequireOwnership = false)]
    private void ActivateLeverServerRpc(ServerRpcParams rpcParams = default)
    {
        isActivated = true; // Marca la palanca como activada.

        // Incrementa el contador de palancas activadas.
        activatedLeversCount++;

        // Actualiza el estado de la palanca en todos los clientes.
        UpdateLeverStateClientRpc(isActivated);

        // Verifica si todas las palancas est�n activadas y si es el servidor, permite el cambio de nivel.
        if (activatedLeversCount == totalLevers)
        {
            NotifyAllPlayersToChangeSceneServerRpc(); // Notifica a todos los jugadores que pueden cambiar de nivel.
        }
    }

    // RPC que actualiza el estado de la palanca en todos los clientes.
    [ClientRpc]
    private void UpdateLeverStateClientRpc(bool state)
    {
        isActivated = state; // Actualiza el estado local de la palanca en el cliente.

        // L�gica visual para reflejar el cambio de estado (ejemplo: desactivar el objeto visual cuando la palanca est� activada).
        if (leverObject != null)
        {
            leverObject.SetActive(!state); // Desactiva el objeto visual si la palanca est� activada.
        }
    }

    // RPC para notificar a todos los jugadores que las palancas est�n activadas y pueden avanzar al siguiente nivel.
    [ServerRpc(RequireOwnership = false)]
    private void NotifyAllPlayersToChangeSceneServerRpc()
    {
        // Aqu� puedes agregar la l�gica de cambio de escena, por ejemplo, habilitar un trigger o hacer que todos los jugadores cambien de escena.
        Debug.Log("Ambas palancas activadas. Cambiar de nivel.");
        // Implementa la l�gica para cambiar de nivel aqu�, por ejemplo:
        // SceneManager.LoadScene("NextScene");
    }

    // M�todo est�tico para reiniciar todas las palancas a su estado inicial.
    public static void ResetAllLevers()
    {
        Lever[] allLevers = FindObjectsOfType<Lever>(); // Encuentra todas las palancas en la escena.
        activatedLeversCount = 0; // Reinicia el contador global de palancas activadas.

        // Reinicia cada palanca en la escena.
        foreach (var lever in allLevers)
        {
            lever.ResetLever();
        }
    }

    // M�todo para reiniciar una palanca individualmente.
    public void ResetLever()
    {
        isActivated = false; // Marca la palanca como desactivada.

        // Asegura que el objeto visual de la palanca se muestre como activo.
        if (leverObject != null)
        {
            leverObject.SetActive(true); // Activa el objeto visual de la palanca.
        }
    }
}
