using UnityEngine;
using UnityEngine.InputSystem;

public class NPCRescue : MonoBehaviour
{
    [Header("Interacción")]
    public Key teclaLiberar = Key.E;
    public float tiempoLiberacion = 5f;

    [Header("Destino")]
    public Transform destinoSalida;

    [Header("Interfaz")]
    public InteractionUI interactionUI;

    private bool jugadorCerca = false;
    private bool liberado = false;
    private float progresoLiberacion = 0f;

    void Start()
    {
        if (interactionUI == null)
        {
            interactionUI = FindFirstObjectByType<InteractionUI>();
        }
    }

    void Update()
    {
        if (!jugadorCerca || liberado)
            return;

        if (Keyboard.current == null)
            return;

        if (Keyboard.current[teclaLiberar].isPressed)
        {
            progresoLiberacion += Time.deltaTime;

            float restante = Mathf.Max(
                0f,
                tiempoLiberacion - progresoLiberacion
            );

            if (interactionUI != null)
            {
                interactionUI.MostrarMensaje(
                    "Liberando estudiante... " +
                    restante.ToString("F1") + " s"
                );
            }

            if (progresoLiberacion >= tiempoLiberacion)
            {
                LiberarNPC();
            }
        }
        else
        {
            progresoLiberacion = 0f;

            if (interactionUI != null)
            {
                interactionUI.MostrarMensaje(
                    "Mantené E para liberar al estudiante"
                );
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        DetectarJugador(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        DetectarJugador(other);
    }

    void DetectarJugador(Collider2D other)
    {
        if (!other.CompareTag("Player") || liberado)
            return;

        if (!jugadorCerca)
        {
            jugadorCerca = true;

            Debug.Log("Jugador detectado cerca de " + name);

            if (interactionUI != null)
            {
                interactionUI.MostrarMensaje(
                    "Mantené E para liberar al estudiante"
                );
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        jugadorCerca = false;
        progresoLiberacion = 0f;

        if (interactionUI != null)
        {
            interactionUI.OcultarMensaje();
        }
    }

    void LiberarNPC()
    {
        if (destinoSalida == null)
        {
            Debug.LogError(
                name + ": no asignaste el destino de salida."
            );
            return;
        }

        liberado = true;

        transform.position = destinoSalida.position;

        progresoLiberacion = 0f;
        jugadorCerca = false;

        if (interactionUI != null)
        {
            interactionUI.MostrarMensaje(
                "¡Estudiante rescatado!"
            );
        }

        Debug.Log(name + " fue rescatado.");
    }

    public bool EstaLiberado()
    {
        return liberado;
    }
}