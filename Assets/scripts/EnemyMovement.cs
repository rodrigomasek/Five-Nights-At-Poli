
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

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
    public float distanciaVisionAgachado = 5f;
    public LayerMask capaParedes;

    [Header("Persecución")]
    public float tiempoBusqueda = 5f;
    public float distanciaLlegadaUltimaPosicion = 0.3f;

    [Header("Oido")]
    public float distanciaEscucha = 10f;
    public float distanciaEscuchaCorriendo = 15f;
    public float distanciaEscuchaAgachado = 2f;

    [Header("Investigacion de ruido")]
    public float tiempoInvestigandoRuido = 4f;

    [Header("Escondite")]
    public float distanciaParaAtraparlo = 0.8f;

    private NavMeshAgent agent;
    private Collider2D colliderProfesor;
    private Collider2D colliderJugador;

    private Transform jugador;
    private PlayerMovement playerMovement;

    private Transform puntoPatrullaActual;

    private Vector3 ultimaPosicionJugador;

    private float tiempoSinVerJugador;
    private float tiempoInvestigando;

    private bool persiguiendo = false;
    private bool buscando = false;
    private bool investigandoRuido = false;

    // Indica si el profesor vio al jugador entrar al escondite
    private bool vioEntrarAlEscondite = false;

    // =========================================
    // INICIO
    // =========================================

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        colliderProfesor = GetComponent<Collider2D>();

        agent.updateRotation = false;
        agent.updateUpAxis = false;

        agent.speed = velocidad;
        agent.stoppingDistance = 0.1f;

        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            jugador = playerObject.transform;

            playerMovement =
                playerObject.GetComponent<PlayerMovement>();

            colliderJugador =
                playerObject.GetComponent<Collider2D>();
        }
        else
        {
            Debug.LogError(
                "No se encontró un objeto con el Tag 'Player'."
            );

            return;
        }

        if (capaParedes.value == 0)
        {
            capaParedes = 1 << 1;
        }

        if (patrolPoint1 != null)
        {
            puntoPatrullaActual = patrolPoint1;

            agent.SetDestination(
                puntoPatrullaActual.position
            );
        }
        else
        {
            Debug.LogWarning(
                "No asignaste Patrol Point 1."
            );
        }
    }

    // =========================================
    // UPDATE
    // =========================================

    void Update()
    {
        if (jugador == null ||
            playerMovement == null)
        {
            return;
        }

        // =====================================
        // JUGADOR ESCONDIDO
        // =====================================

        if (playerMovement.EstaEscondido())
        {
            // Evitamos que el profesor empuje al jugador
            if (colliderProfesor != null &&
                colliderJugador != null)
            {
                Physics2D.IgnoreCollision(
                    colliderProfesor,
                    colliderJugador,
                    true
                );
            }

            // =================================
            // EL PROFESOR VIO ENTRAR AL JUGADOR
            // =================================

            if (vioEntrarAlEscondite)
            {
                agent.SetDestination(
                    jugador.position
                );

                float distancia =
                    Vector2.Distance(
                        transform.position,
                        jugador.position
                    );

                // El profesor llegó al escondite
                if (distancia <= distanciaParaAtraparlo)
                {
                    AtraparJugador();
                }

                return;
            }

            // =================================
            // NO VIO ENTRAR AL JUGADOR
            // =================================

            persiguiendo = false;
            buscando = false;
            investigandoRuido = false;

            tiempoSinVerJugador = 0f;
            tiempoInvestigando = 0f;

            Patrullar();

            return;
        }

        // =====================================
        // JUGADOR NO ESTÁ ESCONDIDO
        // =====================================

        if (colliderProfesor != null &&
            colliderJugador != null)
        {
            Physics2D.IgnoreCollision(
                colliderProfesor,
                colliderJugador,
                false
            );
        }

        // =====================================
        // VISION
        // =====================================

        bool puedeVerJugador =
            EstaViendoJugador();

        if (puedeVerJugador)
        {
            persiguiendo = true;
            buscando = false;
            investigandoRuido = false;

            tiempoSinVerJugador = 0f;

            ultimaPosicionJugador =
                jugador.position;

            agent.SetDestination(
                jugador.position
            );

            return;
        }

        // =====================================
        // RUIDO
        // =====================================

        if (playerMovement.EsMomentoDeHacerRuido())
        {
            EscucharRuido();
        }

        // =====================================
        // PERSECUCION → BUSQUEDA
        // =====================================

        if (persiguiendo)
        {
            persiguiendo = false;
            buscando = true;

            tiempoSinVerJugador = 0f;

            agent.SetDestination(
                ultimaPosicionJugador
            );

            return;
        }

        // =====================================
        // INVESTIGAR RUIDO
        // =====================================

        if (investigandoRuido)
        {
            tiempoInvestigando +=
                Time.deltaTime;

            if (!agent.pathPending &&
                agent.remainingDistance <=
                distanciaLlegadaUltimaPosicion)
            {
                if (tiempoInvestigando >=
                    tiempoInvestigandoRuido)
                {
                    investigandoRuido = false;

                    IrAlSiguientePuntoPatrulla();
                }
            }

            return;
        }

        // =====================================
        // BUSCAR JUGADOR
        // =====================================

        if (buscando)
        {
            tiempoSinVerJugador +=
                Time.deltaTime;

            if (!agent.pathPending &&
                agent.remainingDistance <=
                distanciaLlegadaUltimaPosicion)
            {
                if (tiempoSinVerJugador >=
                    tiempoBusqueda)
                {
                    buscando = false;

                    IrAlSiguientePuntoPatrulla();
                }
            }

            return;
        }

        // =====================================
        // PATRULLAR
        // =====================================

        Patrullar();
    }

    // =========================================
    // JUGADOR ENTRA AL ESCONDITE
    // =========================================

    public void JugadorEntroAlEscondite()
    {
        // Comprobamos si el profesor lo estaba viendo
        if (EstaViendoJugador())
        {
            vioEntrarAlEscondite = true;

            ultimaPosicionJugador =
                jugador.position;

            persiguiendo = false;
            buscando = false;
            investigandoRuido = false;

            agent.SetDestination(
                ultimaPosicionJugador
            );

            Debug.Log(
                "El profesor vio al jugador entrar al escondite."
            );
        }
        else
        {
            vioEntrarAlEscondite = false;

            Debug.Log(
                "El profesor no vio al jugador entrar al escondite."
            );
        }
    }

    // =========================================
    // JUGADOR SALE DEL ESCONDITE
    // =========================================

    public void JugadorSalioDelEscondite()
    {
        vioEntrarAlEscondite = false;

        if (colliderProfesor != null &&
            colliderJugador != null)
        {
            Physics2D.IgnoreCollision(
                colliderProfesor,
                colliderJugador,
                false
            );
        }

        Debug.Log(
            "El jugador salió del escondite."
        );
    }

    // =========================================
    // ¿ESTÁ VIENDO AL JUGADOR?
    // =========================================

    public bool EstaViendoJugador()
    {
        if (jugador == null ||
            playerMovement == null)
        {
            return false;
        }

        if (playerMovement.EstaEscondido())
            return false;

        Vector2 origen =
            transform.position;

        Vector2 direccion =
            ((Vector2)jugador.position -
            origen).normalized;

        float distancia =
            Vector2.Distance(
                origen,
                jugador.position
            );

        float rangoActual;

        if (playerMovement.EstaAgachado())
        {
            rangoActual =
                distanciaVisionAgachado;
        }
        else
        {
            rangoActual =
                distanciaVision;
        }

        if (distancia > rangoActual)
            return false;

        RaycastHit2D impacto =
            Physics2D.Raycast(
                origen,
                direccion,
                distancia,
                capaParedes
            );

        return impacto.collider == null;
    }

    // =========================================
    // ATRAPAR AL JUGADOR
    // =========================================

    public void AtraparJugador()
    {
        Debug.Log(
            "¡El profesor atrapó al jugador!"
        );

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }

    // =========================================
    // ESCUCHAR RUIDO
    // =========================================

    void EscucharRuido()
    {
        if (playerMovement.EstaEscondido())
            return;

        float distancia =
            Vector2.Distance(
                transform.position,
                jugador.position
            );

        float rangoEscucha;

        if (playerMovement.EstaAgachado())
        {
            rangoEscucha =
                distanciaEscuchaAgachado;
        }
        else if (playerMovement.EstaCorriendo())
        {
            rangoEscucha =
                distanciaEscuchaCorriendo;
        }
        else
        {
            rangoEscucha =
                distanciaEscucha;
        }

        if (distancia > rangoEscucha)
            return;

        ultimaPosicionJugador =
            jugador.position;

        investigandoRuido = true;

        persiguiendo = false;
        buscando = false;

        tiempoInvestigando = 0f;

        agent.SetDestination(
            ultimaPosicionJugador
        );
    }

    // =========================================
    // PATRULLA
    // =========================================

    void Patrullar()
    {
        if (puntoPatrullaActual == null)
            return;

        if (!agent.pathPending &&
            agent.remainingDistance <=
            distanciaLlegadaPatrulla)
        {
            IrAlSiguientePuntoPatrulla();
        }
    }

    // =========================================
    // CAMBIAR PUNTO DE PATRULLA
    // =========================================

    void IrAlSiguientePuntoPatrulla()
    {
        if (patrolPoint1 == null ||
            patrolPoint2 == null)
        {
            return;
        }

        if (puntoPatrullaActual ==
            patrolPoint1)
        {
            puntoPatrullaActual =
                patrolPoint2;
        }
        else
        {
            puntoPatrullaActual =
                patrolPoint1;
        }

        agent.SetDestination(
            puntoPatrullaActual.position
        );
    }

    // =========================================
    // GIZMOS
    // =========================================

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
