
using UnityEngine;
using UnityEngine.InputSystem;

public class HidingSpot : MonoBehaviour
{
    [Header("Interacción")]
    public Key teclaEsconderse = Key.E;

    private bool jugadorCerca = false;
    private PlayerMovement jugador;

    private EnemyMovement profesor;

    void Start()
    {
        profesor = FindFirstObjectByType<EnemyMovement>();

        if (profesor == null)
        {
            Debug.LogWarning(
                "No se encontró ningún EnemyMovement en la escena."
            );
        }
    }

    void Update()
    {
        if (!jugadorCerca || jugador == null)
            return;

        if (Keyboard.current[teclaEsconderse].wasPressedThisFrame)
        {
            // ==============================
            // SALIR DEL ESCONDITE
            // ==============================

            if (jugador.EstaEscondido())
            {
                jugador.AlternarEscondido();

                if (profesor != null)
                {
                    profesor.JugadorSalioDelEscondite();
                }

                return;
            }

            // ==============================
            // ENTRAR AL ESCONDITE
            // ==============================

            if (profesor != null)
            {
                profesor.JugadorEntroAlEscondite();
            }

            jugador.AlternarEscondido();
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
                "Jugador cerca del escondite. Pulsa E para esconderte."
            );
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        jugadorCerca = false;

        // Si está escondido mantenemos la referencia
        // para que pueda salir con E.
        if (jugador != null &&
            !jugador.EstaEscondido())
        {
            jugador = null;
        }

        Debug.Log(
            "Jugador salió del área del escondite."
        );
    }
}
