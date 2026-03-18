using UnityEngine;
using UnityEngine.InputSystem;

public class DebugConsoleToggle : MonoBehaviour
{
    public static DebugConsoleToggle Instance { get; private set; }
    [Header("References")]
    [SerializeField] private CanvasGroup cg;

    [Header("Settings")]
    [SerializeField] private bool isActive = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            UpdateAlpha();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (Keyboard.current.commaKey.wasPressedThisFrame)
            UpdateAlpha();
    }

    private void UpdateAlpha()
    {
        isActive = !isActive;
        cg.alpha = isActive ? 1 : 0;
    }
}
