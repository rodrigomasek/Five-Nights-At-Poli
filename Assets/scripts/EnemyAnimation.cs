
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAnimation : MonoBehaviour
{
    [Header("Referencias")]
    public Animator animator;

    [Header("Configuracion")]
    public float velocidadMinimaAnimacion = 0.05f;
    public float suavizadoAnimacion = 0.08f;

    [Header("Direccion inicial")]
    public Vector2 direccionInicial = new Vector2(0f, -1f);

    private NavMeshAgent agent;

    private Vector2 ultimaDireccion;

    private int moveXHash;
    private int moveYHash;
    private int speedHash;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (animator == null)
        {
            Debug.LogError(
                "EnemyAnimation: No se encontró Animator en el profesor."
            );

            enabled = false;
            return;
        }

        if (agent == null)
        {
            Debug.LogError(
                "EnemyAnimation: No se encontró NavMeshAgent."
            );

            enabled = false;
            return;
        }

        moveXHash = Animator.StringToHash("MoveX");
        moveYHash = Animator.StringToHash("MoveY");
        speedHash = Animator.StringToHash("Speed");

        ultimaDireccion = direccionInicial.normalized;

        animator.SetFloat("MoveX", ultimaDireccion.x);
        animator.SetFloat("MoveY", ultimaDireccion.y);
        animator.SetFloat("Speed", 0f);

        Debug.Log("Animaciones del profesor inicializadas.");
    }

    void Update()
    {
        if (agent == null || animator == null)
            return;

        ActualizarAnimaciones();
    }

    void ActualizarAnimaciones()
    {
        Vector2 velocidad = new Vector2(
            agent.velocity.x,
            agent.velocity.y
        );

        float velocidadActual = velocidad.magnitude;

        bool estaMoviendose =
            velocidadActual > velocidadMinimaAnimacion &&
            !agent.isStopped;

        if (estaMoviendose)
        {
            // Guardar la última dirección de movimiento.
            ultimaDireccion = velocidad.normalized;
        }

        // Mantener la última dirección cuando está quieto.
        animator.SetFloat(
            moveXHash,
            ultimaDireccion.x,
            suavizadoAnimacion,
            Time.deltaTime
        );

        animator.SetFloat(
            moveYHash,
            ultimaDireccion.y,
            suavizadoAnimacion,
            Time.deltaTime
        );

        // Controlar la transición entre caminar e idle.
        animator.SetFloat(
            speedHash,
            estaMoviendose ? velocidadActual : 0f,
            suavizadoAnimacion,
            Time.deltaTime
        );
    }
}
