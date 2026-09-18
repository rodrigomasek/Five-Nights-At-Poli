
using UnityEngine;
using UnityEngine.InputSystem;

public class ExitDoor : MonoBehaviour
{
    [Header("Interacción")]
    public Key teclaAbrir = Key.E;

    private bool jugadorCerca = false;
    private PlayerMovement jugador;

    void Update()
    {
        if (!jugadorCerca || jugador == null)
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

        jugador =
            other.GetComponent<PlayerMovement>();

        if (jugador != null)
        {
            jugadorCerca = true;

            Debug.Log(
                "Estás cerca de la puerta. Pulsa E para escapar."
            );
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        jugadorCerca = false;
        jugador = null;
    }

    void IntentarAbrir()
    {
        if (jugador.TieneLlave())
        {
            Debug.Log(
                "¡Tenés la llave! ¡Escapaste!"
            );

            // Por ahora solamente mostramos
            // que la victoria funciona.
        }
        else
        {
            Debug.Log(
                "La puerta está cerrada. Necesitás una llave."
            );
        }
    }
}
