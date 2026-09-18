
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
    public float distanciaVisionAgachado = 5f;
    public LayerMask capaParedes;

    [Header("Persecución")]
    public float tiempoBusqueda = 5f;
    public float distanciaLlegadaUltimaPosicion = 0.3f;

    [Header("Captura")]
    public float distanciaParaAtraparlo = 0.8f;

    [Header("Oido")]
    public float distanciaEscucha = 10f;
    public float distanciaEscuchaCorriendo = 15f;
    public float distanciaEscuchaAgachado = 2f;

    [Header("Investigacion de ruido")]
    public float tiempoInvestigandoRuido = 4f;

    [Header("Revision de escondites")]
    [Range(0f, 100f)]
    public float probabilidadRevisarEscondite = 30f;

    public float distanciaRevisionEscondite = 2f;


    private NavMeshAgent agent;

    private Collider2D colliderProfesor;
    private Collider2D colliderJugador;

    private GameManager gameManager;

    private Transform jugador;
    private PlayerMovement playerMovement;

    private Transform puntoPatrullaActual;

    private Vector3 ultimaPosicionJugador;

    private float tiempoSinVerJugador;
    private float tiempoInvestigando;

    private bool persiguiendo = false;
    private bool buscando = false;
    private bool investigandoRuido = false;

    // El profesor vio al jugador entrar al escondite.
    private bool vioEntrarAlEscondite = false;

    // Escondite que el profesor decidió revisar.
    private HidingSpot esconditeRevisando;

    // Último escondite detectado cerca del profesor.
    private HidingSpot esconditeCercano;


    // =========================================
    // INICIO
    // =========================================

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        colliderProfesor =
            GetComponent<Collider2D>();

        gameManager =
            FindFirstObjectByType<GameManager>();

        if (gameManager == null)
        {
            Debug.LogError(
                "No se encontró ningún GameManager en la escena."
            );
        }

        agent.updateRotation = false;
        agent.updateUpAxis = false;

        agent.speed = velocidad;
        agent.stoppingDistance = 0.1f;

        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            jugador =
                playerObject.transform;

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
            puntoPatrullaActual =
                patrolPoint1;

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
        // ESTÁ REVISANDO UN ESCONDITE
        // =====================================

        if (esconditeRevisando != null)
        {
            RevisarEsconditeEnDestino();

            return;
        }


        // =====================================
        // JUGADOR ESCONDIDO
        // =====================================

        if (playerMovement.EstaEscondido())
        {
            // Evitamos que el profesor empuje al jugador.
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
            // EL PROFESOR VIO AL JUGADOR ENTRAR
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

                if (distancia <= distanciaParaAtraparlo)
                {
                    AtraparJugador();
                }

                return;
            }


            // =================================
            // NO LO VIO ENTRAR
            // =================================

            persiguiendo = false;
            buscando = false;
            investigandoRuido = false;

            tiempoSinVerJugador = 0f;
            tiempoInvestigando = 0f;


            // =================================
            // IMPORTANTE:
            // AUNQUE EL JUGADOR ESTÉ ESCONDIDO,
            // EL PROFESOR PUEDE REVISAR ESCONDITE.
            // =================================

            RevisarEsconditeCercano();

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


            // =================================
            // CAPTURA DURANTE PERSECUCIÓN
            // =================================

            float distancia =
                Vector2.Distance(
                    transform.position,
                    jugador.position
                );

            if (distancia <= distanciaParaAtraparlo)
            {
                AtraparJugador();

                return;
            }

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
        // PERSECUCIÓN → BÚSQUEDA
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
        // REVISAR ESCONDITE CERCANO
        // =====================================

        RevisarEsconditeCercano();


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
        // IMPORTANTE:
        // HidingSpot llama a esta función
        // ANTES de esconder al jugador.

        if (EstaViendoJugador())
        {
            vioEntrarAlEscondite = true;

            ultimaPosicionJugador =
                jugador.position;

            persiguiendo = false;
            buscando = false;
            investigandoRuido = false;

            esconditeRevisando = null;
            esconditeCercano = null;

            agent.SetDestination(
                jugador.position
            );

            Debug.Log(
                "¡El profesor vio al jugador entrar al escondite!"
            );
        }
        else
        {
            vioEntrarAlEscondite = false;

            Debug.Log(
                "El profesor NO vio al jugador entrar al escondite."
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

        esconditeRevisando = null;
        esconditeCercano = null;

        Debug.Log(
            "El jugador salió del escondite."
        );
    }


    // =========================================
    // DETECTAR ESCONDITE CERCANO
    // =========================================

    void RevisarEsconditeCercano()
    {
        Collider2D[] colliders =
            Physics2D.OverlapCircleAll(
                transform.position,
                distanciaRevisionEscondite
            );

        HidingSpot esconditeEncontrado = null;


        foreach (Collider2D collider in colliders)
        {
            HidingSpot escondite =
                collider.GetComponent<HidingSpot>();

            if (escondite != null)
            {
                esconditeEncontrado =
                    escondite;

                break;
            }
        }


        // No hay escondite cerca.
        if (esconditeEncontrado == null)
        {
            esconditeCercano = null;

            return;
        }


        // Ya pasó por este escondite.
        if (esconditeCercano ==
            esconditeEncontrado)
        {
            return;
        }


        esconditeCercano =
            esconditeEncontrado;


        Debug.Log(
            "El profesor pasó cerca de un escondite."
        );


        // =====================================
        // PROBABILIDAD
        // =====================================

        float resultado =
            Random.Range(
                0f,
                100f
            );


        if (resultado <=
            probabilidadRevisarEscondite)
        {
            Debug.Log(
                "El profesor decidió revisar el escondite."
            );

            esconditeRevisando =
                esconditeEncontrado;

            agent.SetDestination(
                esconditeRevisando.transform.position
            );

            return;
        }


        Debug.Log(
            "El profesor decidió no revisar el escondite."
        );
    }


    // =========================================
    // LLEGÓ AL ESCONDITE
    // =========================================

    void RevisarEsconditeEnDestino()
    {
        if (agent.pathPending)
            return;


        if (agent.remainingDistance >
            distanciaLlegadaUltimaPosicion)
        {
            return;
        }


        Debug.Log(
            "El profesor llegó al escondite para revisarlo."
        );


        // =====================================
        // ¿ESTÁ EL JUGADOR EN ESTE ESCONDITE?
        // =====================================

        if (esconditeRevisando.HayJugadorEscondido())
        {
            Debug.Log(
                "¡El profesor encontró al jugador escondido!"
            );

            AtraparJugador();

            return;
        }


        // =====================================
        // ESCONDITE VACÍO
        // =====================================

        Debug.Log(
            "El profesor revisó el escondite y estaba vacío."
        );

        esconditeRevisando = null;
        esconditeCercano = null;

        IrAlSiguientePuntoPatrulla();
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
        {
            return false;
        }


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
        {
            return false;
        }


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


        if (gameManager != null)
        {
            gameManager.GameOver();
        }
        else
        {
            Debug.LogError(
                "No se encontró el GameManager."
            );
        }
    }


    // =========================================
    // ESCUCHAR RUIDO
    // =========================================

    void EscucharRuido()
    {
        if (playerMovement.EstaEscondido())
        {
            return;
        }


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
        {
            return;
        }


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
        {
            return;
        }


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
        {
            return;
        }


        Gizmos.DrawLine(
            transform.position,
            jugador.position
        );
    }
}
