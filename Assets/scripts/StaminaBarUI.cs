
using UnityEngine;
using UnityEngine.UI;

public class StaminaBarUI : MonoBehaviour
{
    [Header("Referencias")]
    public PlayerMovement jugador;
    public Slider barra;
    public CanvasGroup canvasGroup;
    public Image relleno;

    [Header("Colores")]
    public Color colorVerde = Color.green;
    public Color colorAmarillo = Color.yellow;
    public Color colorRojo = Color.red;

    [Header("Aparicion y desaparicion")]
    public float velocidadAparicion = 5f;
    public float velocidadDesaparicion = 2f;
    public float margenStaminaLlena = 0.02f;

    private float alphaObjetivo = 0f;

    void Start()
    {
        if (jugador == null)
            jugador = GetComponentInParent<PlayerMovement>();

        if (barra == null)
            barra = GetComponent<Slider>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (jugador == null || barra == null ||
            canvasGroup == null || relleno == null)
        {
            Debug.LogError(
                "StaminaBarUI: falta asignar alguna referencia."
            );

            enabled = false;
            return;
        }

        barra.minValue = 0f;
        barra.maxValue = jugador.staminaMaxima;
        barra.value = jugador.staminaMaxima;

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        ActualizarColor(1f);
    }

    void Update()
    {
        float staminaMaxima = jugador.staminaMaxima;

        if (staminaMaxima <= 0f)
            return;

        float staminaActual =
            Mathf.Clamp(jugador.staminaActual, 0f, staminaMaxima);

        float porcentaje = staminaActual / staminaMaxima;

        barra.maxValue = staminaMaxima;
        barra.value = staminaActual;

        ActualizarColor(porcentaje);

        // Mostrar la barra si falta stamina.
        if (staminaActual < staminaMaxima - margenStaminaLlena)
        {
            alphaObjetivo = 1f;
        }
        else
        {
            // Ocultarla cuando la stamina está llena.
            alphaObjetivo = 0f;
        }

        float velocidadActual = alphaObjetivo > canvasGroup.alpha
            ? velocidadAparicion
            : velocidadDesaparicion;

        canvasGroup.alpha = Mathf.MoveTowards(
            canvasGroup.alpha,
            alphaObjetivo,
            velocidadActual * Time.deltaTime
        );
    }

    void ActualizarColor(float porcentaje)
    {
        if (porcentaje > 0.5f)
        {
            float t = Mathf.InverseLerp(0.5f, 1f, porcentaje);

            relleno.color = Color.Lerp(
                colorAmarillo,
                colorVerde,
                t
            );
        }
        else
        {
            float t = Mathf.InverseLerp(0f, 0.5f, porcentaje);

            relleno.color = Color.Lerp(
                colorRojo,
                colorAmarillo,
                t
            );
        }
    }
}
