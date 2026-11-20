
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

    [Header("Sprites")]
    public SpriteRenderer spriteRendererEstudiante;
    public Sprite spriteAtado;
    public Sprite spriteLibre;

    private ObjectiveManager objectiveManager;

    private bool jugadorCerca = false;
    private bool liberado = false;

    private float progresoLiberacion = 0f;

    // =====================================================
    // INICIALIZACION
    // =====================================================

    void Start()
    {
        if (interactionUI == null)
        {
            interactionUI =
                FindFirstObjectByType<InteractionUI>();
        }

        objectiveManager =
            FindFirstObjectByType<ObjectiveManager>();

        if (objectiveManager == null)
        {
            Debug.LogError(
                name + ": no se encontró ObjectiveManager."
            );
        }

        // Buscar SpriteRenderer automáticamente
        if (spriteRendererEstudiante == null)
        {
            spriteRendererEstudiante =
                GetComponentInChildren<SpriteRenderer>(true);
        }

        if (spriteRendererEstudiante == null)
        {
            Debug.LogError(
                name + ": no se encontró SpriteRenderer."
            );
        }
        else if (spriteAtado != null)
        {
            // La estudiante comienza atada
            spriteRendererEstudiante.sprite = spriteAtado;
        }

        if (spriteAtado == null || spriteLibre == null)
        {
            Debug.LogWarning(
                name + ": faltan asignar los sprites."
            );
        }

        if (destinoSalida == null)
        {
            Debug.LogError(
                name + ": no tiene asignado Destino Salida."
            );
        }
    }

    // =====================================================
    // UPDATE
    // =====================================================

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

    // =====================================================
    // DETECCION DEL JUGADOR
    // =====================================================

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

            Debug.Log(
                "Jugador detectado cerca de " + name
            );

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

    // =====================================================
    // LIBERAR ESTUDIANTE
    // =====================================================

    void LiberarNPC()
    {
        if (liberado)
            return;

        if (destinoSalida == null)
        {
            Debug.LogError(
                name + ": no asignaste el destino de salida."
            );
            return;
        }

        if (spriteRendererEstudiante == null ||
            spriteLibre == null)
        {
            Debug.LogError(
                name + ": no se puede cambiar al sprite libre."
            );
            return;
        }

        // Evitar que vuelva a liberarse
        liberado = true;

        // Cambiar apariencia
        spriteRendererEstudiante.sprite = spriteLibre;

        // Trasladar a la salida
        transform.position = destinoSalida.position;

        progresoLiberacion = 0f;
        jugadorCerca = false;

        // Completar objetivo
        if (objectiveManager != null)
        {
            objectiveManager.CompletarObjetivoRescatarNPC();
        }

        if (interactionUI != null)
        {
            interactionUI.MostrarMensaje(
                "¡Estudiante rescatado!"
            );
        }

        Debug.Log(
            name + " fue rescatado y trasladado a la salida."
        );
    }

    // =====================================================
    // CONSULTA DE ESTADO
    // =====================================================

    public bool EstaLiberado()
    {
        return liberado;
    }
}
