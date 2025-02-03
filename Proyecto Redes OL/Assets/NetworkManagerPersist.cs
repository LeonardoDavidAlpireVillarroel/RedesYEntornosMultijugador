using UnityEngine;
using Unity.Netcode;

public class NetworkManagerPersist : MonoBehaviour
{
    private void Awake()
    {
        // Asegura que el NetworkManager persista entre escenas
        if (NetworkManager.Singleton == null)
        {
            DontDestroyOnLoad(gameObject); // No destruir al cambiar de escena
        }
        else
        {
            Destroy(gameObject); // Evita duplicados
        }
    }
}
