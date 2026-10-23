
using UnityEngine;
using UnityEngine.InputSystem;

public class HidingSpot : MonoBehaviour
{
    [Header("Interacción")]
    public Key teclaEsconderse = Key.E;

    [Header("Interfaz")]
    public InteractionUI interactionUI;

    [Header("Detección del profesor")]
    public CircleCollider2D zonaRevision;

    private bool jugadorCerca = false;
    private PlayerMovement jugador;

    void Start()
    {
        if (zonaRevision == null)
        {
            zonaRevision = GetComponent<CircleCollider2D>();
        }

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
            if (jugador.EstaEscondido())
            {
                interactionUI.MostrarMensaje(
                    "Presioná E para salir del escondite"
                );
            }
            else
            {
                interactionUI.MostrarMensaje(
                    "Presioná E para esconderte"
                );
            }
        }

        if (Keyboard.current == null)
            return;

        if (Keyboard.current[teclaEsconderse].wasPressedThisFrame)
        {
            EnemyMovement profesor =
                FindFirstObjectByType<EnemyMovement>();

            if (!jugador.EstaEscondido())
            {
                if (profesor != null)
                {
                    profesor.JugadorEntroAlEscondite();
                }

                jugador.AlternarEscondido();
            }
            else
            {
                jugador.AlternarEscondido();

                if (profesor != null)
                {
                    profesor.JugadorSalioDelEscondite();
                }
            }
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
                    "Presioná E para esconderte"
                );
            }
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

    public bool HayJugadorEscondido()
    {
        if (jugador == null)
            return false;

        return jugador.EstaEscondido();
    }
}
