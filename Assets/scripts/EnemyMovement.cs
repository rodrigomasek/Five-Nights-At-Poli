using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    public Transform patrolPoint1;
    public Transform patrolPoint2;
    public float speed = 2f;

    public float detectionRadius = 5f;
    public LayerMask playerLayer;
    public LayerMask wallLayer;

    public float tiempoDeBusqueda = 3f;

    private Rigidbody2D rb;
    private Transform objetivoActual;
    private Transform jugadorDetectado;

    private Vector2 ultimaPosicionJugador;
    private bool buscandoJugador = false;
    private float tiempoBusqueda = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        objetivoActual = patrolPoint1;
    }

    void FixedUpdate()
    {
        DetectarJugador();

        Vector2 direccion;

        // El profesor está viendo al jugador
        if (jugadorDetectado != null)
        {
            buscandoJugador = false;

            ultimaPosicionJugador = jugadorDetectado.position;

            direccion = (
                (Vector2)jugadorDetectado.position - rb.position
            ).normalized;
        }

        // El profesor perdió al jugador y va a su última posición
        else if (buscandoJugador)
        {
            direccion = (
                ultimaPosicionJugador - rb.position
            ).normalized;

            float distancia = Vector2.Distance(
                rb.position,
                ultimaPosicionJugador
            );

            if (distancia < 0.2f)
            {
                tiempoBusqueda -= Time.fixedDeltaTime;

                if (tiempoBusqueda <= 0f)
                {
                    buscandoJugador = false;
                }
            }
        }

        // El profesor está patrullando
        else
        {
            direccion = (
                (Vector2)objetivoActual.position - rb.position
            ).normalized;

            if (Vector2.Distance(
                rb.position,
                objetivoActual.position
            ) < 0.1f)
            {
                if (objetivoActual == patrolPoint1)
                {
                    objetivoActual = patrolPoint2;
                }
                else
                {
                    objetivoActual = patrolPoint1;
                }
            }
        }

        Vector2 nuevaPosicion =
            rb.position + direccion * speed * Time.fixedDeltaTime;

        rb.MovePosition(nuevaPosicion);
    }

    void DetectarJugador()
    {
        Collider2D jugador = Physics2D.OverlapCircle(
            rb.position,
            detectionRadius,
            playerLayer
        );

        if (jugador != null)
        {
            RaycastHit2D obstaculo = Physics2D.Linecast(
                rb.position,
                jugador.transform.position,
                wallLayer
            );

            // No hay pared entre el profesor y el jugador
            if (obstaculo.collider == null)
            {
                jugadorDetectado = jugador.transform;

                ultimaPosicionJugador = jugador.transform.position;

                Debug.Log("¡Jugador visible!");
            }
            else
            {
                // Si estaba persiguiendo y ahora una pared lo tapa
                if (jugadorDetectado != null)
                {
                    jugadorDetectado = null;
                    buscandoJugador = true;
                    tiempoBusqueda = tiempoDeBusqueda;

                    Debug.Log("¡Perdí de vista al jugador!");
                }
            }
        }
        else
        {
            // El jugador salió del radio de detección
            if (jugadorDetectado != null)
            {
                jugadorDetectado = null;
                buscandoJugador = true;
                tiempoBusqueda = tiempoDeBusqueda;

                Debug.Log("¡Perdí de vista al jugador!");
            }
        }
    }
}