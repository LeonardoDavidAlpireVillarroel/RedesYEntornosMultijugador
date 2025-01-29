using TMPro;
using UnityEngine;

public class PlayerUIManager : MonoBehaviour
{
    public static PlayerUIManager Singleton { get; private set; }

    [SerializeField] private TMP_Text leverStatusText; // 🔹 UI donde se mostrará el estado de las palancas

    private void Awake()
    {
        if (Singleton == null)
            Singleton = this;
        else
            Destroy(gameObject);
    }

    public void UpdateLeverStatus(int activatedLevers, int totalLevers)
    {
        leverStatusText.text = $"Levers: {activatedLevers}/{totalLevers} Activated"; // 🔹 Muestra cuántas están activadas
    }
}
