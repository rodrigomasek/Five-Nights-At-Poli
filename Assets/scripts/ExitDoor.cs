
using UnityEngine;
using UnityEngine.InputSystem;

public class ExitDoor : MonoBehaviour
{
    [Header("Interacción")]
    public Key teclaAbrir = Key.E;

    [Header("Game Manager")]
    public GameManager gameManager;

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

        // Actualizar el mensaje según tenga o no la llave.
        if (interactionUI != null)
        {
            if (jugador.TieneLlave())
            {
                interactionUI.MostrarMensaje(
                    "Presioná E para escapar"
                );
            }
            else
            {
                interactionUI.MostrarMensaje(
                    "Necesitás una llave para escapar"
                );
            }
        }

        // Comprobar que haya un teclado conectado.
        if (Keyboard.current == null)
            return;

        if (Keyboard.current[teclaAbrir].wasPressedThisFrame)
        {
            IntentarAbrir();
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

            Debug.Log(
                "Jugador cerca de la puerta."
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

    void IntentarAbrir()
    {
        if (jugador.TieneLlave())
        {
            Debug.Log("¡Tenés la llave! ¡Escapaste!");

            // Ocultar el mensaje antes de mostrar la victoria.
            if (interactionUI != null)
            {
                interactionUI.OcultarMensaje();
            }

            if (objectiveManager != null)
            {
                objectiveManager.CompletarObjetivoEscape();
            }
            else
            {
                Debug.LogWarning(
                    "No asignaste el ObjectiveManager en la puerta."
                );
            }

            if (gameManager != null)
            {
                gameManager.Victoria();
            }
            else
            {
                Debug.LogError(
                    "No asignaste el GameManager en la puerta."
                );
            }
        }
        else
        {
            Debug.Log(
                "La puerta está cerrada. Necesitás una llave."
            );
        }
    }
}
