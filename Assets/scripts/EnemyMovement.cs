using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
[Header("Movimiento")]
public float velocidad = 2f;

[Header("Patrulla")]
public Transform patrolPoint1;
public Transform patrolPoint2;
public float distanciaLlegadaPatrulla = 0.2f;

[Header("Vision")]
public float distanciaVision = 10f;
public LayerMask capaParedes;

[Header("Persecución")]
public float tiempoBusqueda = 5f;
public float distanciaLlegadaUltimaPosicion = 0.3f;

private NavMeshAgent agent;
private Transform jugador;

private Transform puntoPatrullaActual;

private Vector3 ultimaPosicionJugador;
private float tiempoSinVerJugador;

private bool persiguiendo = false;
private bool buscando = false;


void Start()
{
    // Obtener el NavMeshAgent
    agent = GetComponent<NavMeshAgent>();

    // Configuración necesaria para un juego 2D
    agent.updateRotation = false;
    agent.updateUpAxis = false;

    // Velocidad del profesor
    agent.speed = velocidad;

    // Evita que se detenga demasiado lejos
    agent.stoppingDistance = 0.1f;

    // Buscar al Player por Tag
    GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

    if (playerObject != null)
    {
        jugador = playerObject.transform;
    }
    else
    {
        Debug.LogError("No se encontró un objeto con el Tag 'Player'.");
        return;
    }

    // Si no asignamos las paredes desde el Inspector,
    // automáticamente usa Layer 1.
    if (capaParedes.value == 0)
    {
        capaParedes = 1 << 1;
    }

    // Empezar patrullando hacia el punto 1
    if (patrolPoint1 != null)
    {
        puntoPatrullaActual = patrolPoint1;
        agent.SetDestination(puntoPatrullaActual.position);
    }
    else
    {
        Debug.LogWarning("No asignaste Patrol Point 1.");
    }
}


void Update()
{
    if (jugador == null)
        return;

    bool puedeVerJugador = PuedeVerJugador();

    // =====================================================
    // ESTADO 1: VE AL JUGADOR
    // =====================================================

    if (puedeVerJugador)
    {
        persiguiendo = true;
        buscando = false;

        tiempoSinVerJugador = 0f;

        // Guardamos constantemente la última posición conocida
        ultimaPosicionJugador = jugador.position;

        // Lo persigue
        agent.SetDestination(jugador.position);

        return;
    }


    // =====================================================
    // ESTADO 2: PERDIÓ AL JUGADOR
    // =====================================================

    if (persiguiendo)
    {
        persiguiendo = false;
        buscando = true;

        tiempoSinVerJugador = 0f;

        // Va hacia donde vio al jugador por última vez
        agent.SetDestination(ultimaPosicionJugador);

        return;
    }


    // =====================================================
    // ESTADO 3: BUSCANDO
    // =====================================================

    if (buscando)
    {
        tiempoSinVerJugador += Time.deltaTime;

        // Si llegó a la última posición
        if (!agent.pathPending &&
            agent.remainingDistance <= distanciaLlegadaUltimaPosicion)
        {
            // Espera/busca durante unos segundos
            if (tiempoSinVerJugador >= tiempoBusqueda)
            {
                buscando = false;

                // Volver a patrullar
                IrAlSiguientePuntoPatrulla();
            }
        }

        return;
    }


    // =====================================================
    // ESTADO 4: PATRULLANDO
    // =====================================================

    Patrullar();
}


// =========================================================
// VISION
// =========================================================

bool PuedeVerJugador()
{
    Vector2 origen = transform.position;

    Vector2 direccion =
        ((Vector2)jugador.position - origen).normalized;

    float distancia =
        Vector2.Distance(origen, jugador.position);

    // Está fuera del rango de visión
    if (distancia > distanciaVision)
        return false;

    // Raycast que solamente detecta paredes
    RaycastHit2D impacto = Physics2D.Raycast(
        origen,
        direccion,
        distancia,
        capaParedes
    );

    // Si no chocó con una pared, puede verlo
    return impacto.collider == null;
}


// =========================================================
// PATRULLA
// =========================================================

void Patrullar()
{
    if (puntoPatrullaActual == null)
        return;

    if (!agent.pathPending &&
        agent.remainingDistance <= distanciaLlegadaPatrulla)
    {
        IrAlSiguientePuntoPatrulla();
    }
}


void IrAlSiguientePuntoPatrulla()
{
    if (patrolPoint1 == null || patrolPoint2 == null)
        return;

    if (puntoPatrullaActual == patrolPoint1)
    {
        puntoPatrullaActual = patrolPoint2;
    }
    else
    {
        puntoPatrullaActual = patrolPoint1;
    }

    agent.SetDestination(puntoPatrullaActual.position);
}


// =========================================================
// DEBUG: DIBUJAR LINEA DE VISION
// =========================================================

void OnDrawGizmos()
{
    if (jugador == null)
        return;

    Gizmos.DrawLine(
        transform.position,
        jugador.position
    );
}

}
