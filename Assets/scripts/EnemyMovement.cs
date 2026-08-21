using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    public Transform patrolPoint1;
    public Transform patrolPoint2;
    public float speed = 2f;

    public float detectionRadius = 5f;
    public LayerMask playerLayer;

    private Rigidbody2D rb;
    private Transform objetivoActual;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        objetivoActual = patrolPoint1;
    }

    void FixedUpdate()
    {
        Vector2 direccion = (objetivoActual.position - transform.position).normalized;

        rb.MovePosition(rb.position + direccion * speed * Time.fixedDeltaTime);

        if (Vector2.Distance(transform.position, objetivoActual.position) < 0.1f)
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

        DetectarJugador();
    }

    void DetectarJugador()
    {
        Collider2D jugador = Physics2D.OverlapCircle(
            transform.position,
            detectionRadius,
            playerLayer
        );

        if (jugador != null)
        {
            Debug.Log("¡Jugador detectado!");
        }
    }
}