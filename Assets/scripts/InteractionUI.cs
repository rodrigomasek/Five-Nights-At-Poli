
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class InteractionUI : MonoBehaviour
{
    [Header("Texto")]
    public TMP_Text interactionText;

    [Header("Animación")]
    [Min(0.01f)]
    public float duracionDesvanecimiento = 0.2f;

    private CanvasGroup canvasGroup;
    private float alphaObjetivo = 0f;
    private string ultimoMensaje = "";

    void Awake()
    {
        if (interactionText == null)
        {
            Debug.LogError(
                "InteractionUI: no asignaste Interaction Text."
            );
            return;
        }

        // Buscamos el CanvasGroup en el propio objeto del texto.
        canvasGroup =
            interactionText.GetComponent<CanvasGroup>();

        // Si no existe, lo agregamos automáticamente.
        if (canvasGroup == null)
        {
            canvasGroup =
                interactionText.gameObject.AddComponent<CanvasGroup>();
        }

        // Empezamos con el texto invisible.
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    void Start()
    {
        OcultarMensaje();
    }

    void Update()
    {
        if (canvasGroup == null)
            return;

        // Transición gradual hacia la transparencia deseada.
        canvasGroup.alpha = Mathf.MoveTowards(
            canvasGroup.alpha,
            alphaObjetivo,
            Time.unscaledDeltaTime / duracionDesvanecimiento
        );

        // Pruebas temporales con P y O.
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            MostrarMensaje(
                "¡Sistema de interacción funcionando!"
            );
        }

        if (Keyboard.current.oKey.wasPressedThisFrame)
        {
            OcultarMensaje();
        }
    }

    public void MostrarMensaje(string mensaje)
    {
        if (interactionText == null || canvasGroup == null)
            return;

        // Evitamos reasignar el mismo mensaje en cada frame.
        if (ultimoMensaje != mensaje)
        {
            interactionText.text = mensaje;
            ultimoMensaje = mensaje;
        }

        // El objeto debe estar activo para poder verse.
        if (!interactionText.gameObject.activeSelf)
        {
            interactionText.gameObject.SetActive(true);
        }

        alphaObjetivo = 1f;
    }

    public void OcultarMensaje()
    {
        if (interactionText == null || canvasGroup == null)
            return;

        alphaObjetivo = 0f;
    }
}
