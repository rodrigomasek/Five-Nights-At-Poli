
using UnityEngine;
using UnityEngine.InputSystem;

public class HidingSpot : MonoBehaviour
{
    [Header("Interacción")]
    public Key teclaEsconderse = Key.E;

    [Header("Detección del profesor")]
    public CircleCollider2D zonaRevision;

    private bool jugadorCerca = false;
    private PlayerMovement jugador;

    void Start()
    {
        if (zonaRevision == null)
        {
            zonaRevision =
                GetComponent<CircleCollider2D>();
        }
    }

    void Update()
    {
        if (!jugadorCerca || jugador == null)
            return;

        if (Keyboard.current[teclaEsconderse].wasPressedThisFrame)
        {
            // Buscamos al profesor.
            EnemyMovement profesor =
                FindFirstObjectByType<EnemyMovement>();

            // Si el jugador NO está escondido,
            // estamos intentando entrar.
            if (!jugador.EstaEscondido())
            {
                if (profesor != null)
                {
                    // IMPORTANTE:
                    // Esto se hace ANTES de esconder al jugador.
                    profesor.JugadorEntroAlEscondite();
                }

                jugador.AlternarEscondido();
            }
            else
            {
                // Si ya estaba escondido,
                // simplemente sale.
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

        jugador =
            other.GetComponent<PlayerMovement>();

        if (jugador != null)
        {
            jugadorCerca = true;

            Debug.Log(
                "Jugador cerca del escondite. Pulsa E para esconderte."
            );
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        jugadorCerca = false;
        jugador = null;

        Debug.Log(
            "Jugador salió del área del escondite."
        );
    }

    public bool HayJugadorEscondido()
    {
        if (jugador == null)
            return false;

        return jugador.EstaEscondido();
    }
}
