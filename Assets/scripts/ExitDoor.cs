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

    [Header("NPC")]
    public NPCRescue npc01;
    public NPCRescue npc02;

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
            int rescatados = 0;

            if (npc01 != null && npc01.EstaLiberado())
                rescatados++;

            if (npc02 != null && npc02.EstaLiberado())
                rescatados++;

            if (!jugador.TieneLlave())
            {
                interactionUI.MostrarMensaje(
                    "Necesitás una llave para escapar"
                );
            }
            else if (rescatados < 2)
            {
                interactionUI.MostrarMensaje(
                    "Rescatá a los estudiantes (" +
                    rescatados + "/2)"
                );
            }
            else
            {
                interactionUI.MostrarMensaje(
                    "Presioná E para escapar"
                );
            }
        }

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

            Debug.Log("Jugador cerca de la puerta.");
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
        if (npc01 == null || npc02 == null)
        {
            Debug.LogError(
                "No asignaste los dos NPC en ExitDoor."
            );
            return;
        }

        if (!jugador.TieneLlave())
        {
            Debug.Log("Necesitás una llave.");
            return;
        }

        if (!npc01.EstaLiberado() || !npc02.EstaLiberado())
        {
            Debug.Log("Todavía faltan estudiantes por rescatar.");
            return;
        }

        Debug.Log("¡Todos los requisitos cumplidos!");

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
                "No asignaste el ObjectiveManager."
            );
        }

        if (gameManager != null)
        {
            gameManager.Victoria();
        }
        else
        {
            Debug.LogError(
                "No asignaste el GameManager."
            );
        }
    }
}