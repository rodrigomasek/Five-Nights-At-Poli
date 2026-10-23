
using UnityEngine;
using UnityEngine.InputSystem;

public class KeyItem : MonoBehaviour
{
    [Header("Interacción")]
    public Key teclaRecoger = Key.E;

    [Header("Objetivos")]
    public ObjectiveManager objectiveManager;

    [Header("Interfaz")]
    public InteractionUI interactionUI;

    private bool jugadorCerca = false;
    private PlayerMovement jugador;

    void Start()
    {
        if (interactionUI == null)
        {
            interactionUI = FindFirstObjectByType<InteractionUI>();
        }
    }

    void Update()
    {
        if (!jugadorCerca || jugador == null)
            return;

        if (interactionUI != null)
        {
            interactionUI.MostrarMensaje(
                "Presioná E para recoger la llave"
            );
        }

        if (Keyboard.current == null)
            return;

        if (Keyboard.current[teclaRecoger].wasPressedThisFrame)
        {
            RecogerLlave();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        jugador = other.GetComponent<PlayerMovement>();

        if (jugador != null)
        {
            jugadorCerca = true;

            if (interactionUI != null)
            {
                interactionUI.MostrarMensaje(
                    "Presioná E para recoger la llave"
                );
            }

            Debug.Log(
                "Estás cerca de la llave. Pulsa E para recogerla."
            );
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        jugadorCerca = false;
        jugador = null;

        if (interactionUI != null)
        {
            interactionUI.OcultarMensaje();
        }
    }

    void RecogerLlave()
    {
        jugador.ObtenerLlave();

        Debug.Log("¡Llave recogida!");

        if (interactionUI != null)
        {
            interactionUI.OcultarMensaje();
        }

        if (objectiveManager != null)
        {
            objectiveManager.CompletarObjetivoLlave();
        }
        else
        {
            Debug.LogWarning(
                "No asignaste el ObjectiveManager en la llave."
            );
        }

        gameObject.SetActive(false);
    }
}
