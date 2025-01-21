using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class PlayerUIController : NetworkBehaviour
{
    private GameObject disconnectButton; // Referencia al bot�n de desconexi�n

    private void Start()
    {
        // Verifica si este objeto es el propietario
        if (IsOwner)
        {
            // Busca el bot�n de desconexi�n en la jerarqu�a de la escena
            disconnectButton = GameObject.Find("DisconnectButton");

            // Si el bot�n se encuentra en la escena, act�valo y conecta el evento
            if (disconnectButton != null)
            {
                disconnectButton.SetActive(true); // Activa el bot�n para el jugador propietario

                // Conecta el evento de clic al m�todo Disconnect
                disconnectButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(Disconnect);
            }
        }
    }

    public void Disconnect()
    {
        // Solo permite desconectar si este objeto es el propietario
        if (IsOwner)
        {
            // Desconecta al jugador de la red
            NetworkManager.Singleton.Shutdown();

            // Registra el evento para cuando la nueva escena sea cargada
            SceneManager.sceneLoaded += OnMultiplayerSceneLoaded;

            // Carga la escena "Multiplayer"
            SceneManager.LoadScene("Multiplayer");
        }
    }

    private void OnMultiplayerSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Verifica si la escena cargada es "Multiplayer"
        if (scene.name == "Multiplayer")
        {
            // Busca el panel "DisconnectedFeedbackPanel" en la nueva escena
            GameObject disconnectedFeedbackPanel = GameObject.Find("DisconnectedFeedbackPanel");

            // Si el panel se encuentra, act�valo
            if (disconnectedFeedbackPanel != null)
            {
                disconnectedFeedbackPanel.SetActive(true);
            }

            // Desuscribirse del evento de carga de escena para evitar llamadas posteriores
            SceneManager.sceneLoaded -= OnMultiplayerSceneLoaded;
        }
    }
}
