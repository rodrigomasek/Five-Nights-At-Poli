using UnityEngine;
using UnityEngine.InputSystem;

public class TeleportZone : MonoBehaviour
{
    [Header("Interacción")]
    public Key teclaInteraccion = Key.F;
    public string mensajeInteraccion = "Presioná F para subir";

    [Header("Destino del jugador")]
    public Transform destino;

    [Header("Destino del profesor")]
    [Tooltip("Punto de llegada del profesor en el otro piso. Debe estar sobre el NavMesh de ese piso.")]
    public Transform destinoProfesor;
    public PisoEscuela pisoDestinoProfesor = PisoEscuela.Piso2;

    [Header("Interfaz")]
    public InteractionUI interactionUI;

    private bool jugadorCerca;
    private Transform jugador;
    private Rigidbody2D rbJugador;

    void Start()
    {
        if (interactionUI == null)
            interactionUI = FindFirstObjectByType<InteractionUI>();

        if (destino == null)
            Debug.LogError("No se asignó un destino para: " + gameObject.name);

        if (destinoProfesor == null)
            Debug.LogWarning("No se asignó Destino Profesor en: " + gameObject.name);
    }

    void Update()
    {
        if (!jugadorCerca || jugador == null)
            return;

        if (interactionUI != null)
            interactionUI.MostrarMensaje(mensajeInteraccion);

        if (Keyboard.current == null)
            return;

        if (Keyboard.current[teclaInteraccion].wasPressedThisFrame)
            TeletransportarJugador();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        jugador = other.transform;
        rbJugador = other.GetComponent<Rigidbody2D>();
        jugadorCerca = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        jugadorCerca = false;
        jugador = null;
        rbJugador = null;

        if (interactionUI != null)
            interactionUI.OcultarMensaje();
    }

    void TeletransportarJugador()
    {
        if (destino == null || jugador == null)
            return;

        // Comprobar la visión ANTES de mover al jugador.
        EnemyMovement enemigo = FindAnyObjectByType<EnemyMovement>();
       bool profesorLoVeia = enemigo != null;

        if (rbJugador != null)
        {
            rbJugador.position = destino.position;
            rbJugador.linearVelocity = Vector2.zero;
        }
        else
        {
            jugador.position = destino.position;
        }

        Physics2D.SyncTransforms();

        // Informar siempre el piso real del jugador. El profesor solo
        // comenzará a seguirlo si lo veía justo antes del teletransporte.
        if (enemigo != null)
        {
            enemigo.NotificarCambioPisoJugador(
                pisoDestinoProfesor,
                destinoProfesor,
                profesorLoVeia
            );
        }

        jugadorCerca = false;
        jugador = null;
        rbJugador = null;

        if (interactionUI != null)
            interactionUI.OcultarMensaje();

        Debug.Log("Jugador teletransportado correctamente.");
    }
}
