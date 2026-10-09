
using UnityEngine;

public class SecurityCamera : MonoBehaviour
{
    [Header("Detección")]
    public float distanciaVision = 8f;
    public float anguloVision = 90f;
    public LayerMask capaParedes;

    [Header("Dirección")]
    public Vector2 direccionVision = Vector2.right;

    [Header("Estado")]
    public bool jugadorDetectado;

    private Transform jugador;
    private PlayerMovement playerMovement;
    private EnemyMovement profesor;

    void Start()
    {
        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            jugador = playerObject.transform;

            playerMovement =
                playerObject.GetComponent<PlayerMovement>();
        }
        else
        {
            Debug.LogError(
                "SecurityCamera: no se encontró al jugador."
            );
        }

        profesor =
            FindAnyObjectByType<EnemyMovement>();

        if (profesor == null)
        {
            Debug.LogError(
                "SecurityCamera: no se encontró al profesor."
            );
        }

        if (capaParedes.value == 0)
        {
            capaParedes = 1 << 1;
        }

        direccionVision = direccionVision.normalized;
    }

    void Update()
    {
        if (jugador == null ||
            playerMovement == null)
        {
            jugadorDetectado = false;
            return;
        }

        jugadorDetectado = PuedeDetectarJugador();

        if (jugadorDetectado && profesor != null)
        {
            profesor.DetectadoPorCamara(
                jugador.position
            );
        }
    }

    bool PuedeDetectarJugador()
    {
        if (playerMovement.EstaEscondido())
            return false;

        Vector2 origen = transform.position;

        Vector2 haciaJugador =
            (Vector2)jugador.position - origen;

        float distancia = haciaJugador.magnitude;

        if (distancia > distanciaVision)
            return false;

        if (distancia <= 0.001f)
            return true;

        float angulo = Vector2.Angle(
            direccionVision,
            haciaJugador.normalized
        );

        if (angulo > anguloVision / 2f)
            return false;

        RaycastHit2D impacto = Physics2D.Raycast(
            origen,
            haciaJugador.normalized,
            distancia,
            capaParedes
        );

        return impacto.collider == null;
    }

    void OnDrawGizmos()
    {
        Vector2 direccion = direccionVision.normalized;

        if (direccion == Vector2.zero)
            direccion = Vector2.right;

        float mitadAngulo = anguloVision / 2f;

        Vector3 extremoIzquierdo =
            Quaternion.Euler(0, 0, mitadAngulo) *
            direccion;

        Vector3 extremoDerecho =
            Quaternion.Euler(0, 0, -mitadAngulo) *
            direccion;

        Gizmos.color = jugadorDetectado
            ? Color.red
            : Color.yellow;

        Gizmos.DrawRay(
            transform.position,
            extremoIzquierdo * distanciaVision
        );

        Gizmos.DrawRay(
            transform.position,
            extremoDerecho * distanciaVision
        );

        Gizmos.DrawWireSphere(
            transform.position,
            0.15f
        );
    }
}
