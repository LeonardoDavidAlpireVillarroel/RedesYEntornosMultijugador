using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class PlayerUIController : NetworkBehaviour
{
    private GameObject disconnectButton;

    private void Start()
    {
        if (IsOwner)
        {
            disconnectButton = GameObject.Find("DisconnectButton");
            if (disconnectButton != null)
            {
                disconnectButton.SetActive(true);
                disconnectButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(Disconnect);
            }
        }
    }

    public void Disconnect()
    {
        if (IsOwner)
        {
            NetworkManager.Singleton.Shutdown();
            SceneManager.sceneLoaded += OnMultiplayerSceneLoaded;
            SceneManager.LoadScene("Multiplayer");
        }
    }

    private void OnMultiplayerSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Multiplayer")
        {
            GameObject disconnectedFeedbackPanel = GameObject.Find("DisconnectedFeedbackPanel");
            if (disconnectedFeedbackPanel != null)
            {
                disconnectedFeedbackPanel.SetActive(true);
            }
            SceneManager.sceneLoaded -= OnMultiplayerSceneLoaded;
        }
    }
}
