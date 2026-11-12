using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMovement : MonoBehaviour
{
    private enum EstadoIA
    {
        Patrullando,
        Persiguiendo,
        Buscando,
        InvestigandoRuido,
        InvestigandoCamara,
        RevisandoEscondite
    }

    [Header("Movimiento")]
    public float velocidad = 2f;

    [Header("Patrulla")]
    public Transform patrolPoint1;
    public Transform patrolPoint2;
    public Transform patrolPoint3;
    public Transform patrolPoint4;
    public float distanciaLlegadaPatrulla = 0.2f;
    public float tiempoEsperaPatrulla = 2f;

    [Header("Vision")]
    public float distanciaVision = 10f;
    public float distanciaVisionAgachado = 5f;
    public LayerMask capaParedes;

    [Header("Persecucion")]
    public float tiempoBusqueda = 5f;
    public float distanciaLlegadaUltimaPosicion = 0.3f;

    [Header("Busqueda inteligente")]
public float radioBusqueda = 3f;

[Range(1, 8)]
public int cantidadPuntosBusqueda = 3;

public float tiempoInspeccionPunto = 1.5f;

public int intentosEncontrarPuntoBusqueda = 12;

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

    [Header("Camaras")]
    public float tiempoBusquedaCamara = 5f;

    [Header("Seguridad de movimiento")]
    public float tiempoMaximoSinMovimiento = 3f;
    public float tiempoReintentoRuta = 1f;
    public int maximosReintentosRuta = 2;
    public float distanciaMuestreoNavMesh = 2f;

    private NavMeshAgent agent;
    private Collider2D colliderProfesor;
    private Collider2D colliderJugador;
    private GameManager gameManager;

    private Transform jugador;
    private PlayerMovement playerMovement;

    private readonly List<Transform> puntosPatrulla =
        new List<Transform>();

    private int indicePuntoActual = -1;
    private Transform puntoPatrullaActual;

    private Vector3 ultimaPosicionJugador;
    private Vector3 destinoActual;

    private EstadoIA estadoActual = EstadoIA.Patrullando;

    private float tiempoSinVerJugador;
    // Sistema de búsqueda inteligente

private Vector3 centroBusqueda;

private int puntosBusquedaRestantes;

private float tiempoInspeccionBusqueda;

private bool inspeccionandoBusqueda;
private bool primeraInspeccionBusqueda;

private readonly List<Vector3> posicionesInspeccionadas =
    new List<Vector3>();
    private float tiempoInvestigando;
    private float tiempoBusquedaCamaraActual;
    private float tiempoEsperaActual;

    private float tiempoSinMovimiento;
    private float tiempoReintentoActual;

    private int reintentosRuta;

    private bool esperandoPatrulla;
    private bool juegoTerminado;
    private bool vioEntrarAlEscondite;
    private bool destinoAsignado;
    private bool ruidoAnterior;

    private HidingSpot esconditeRevisando;
    private HidingSpot esconditeCercano;
    private HidingSpot ultimoEsconditeRevisado;

    private float tiempoBloqueoEscondite;

    // =====================================================
    // INICIALIZACION
    // =====================================================

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        colliderProfesor = GetComponent<Collider2D>();

        gameManager = FindAnyObjectByType<GameManager>();

        if (agent == null)
        {
            Debug.LogError("El profesor no tiene NavMeshAgent.");
            enabled = false;
            return;
        }

        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.speed = velocidad;
        agent.stoppingDistance = 0.1f;
        agent.autoBraking = true;
        agent.autoRepath = true;
        agent.isStopped = false;

        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
        {
            Debug.LogError("No se encontró el objeto Player.");
            enabled = false;
            return;
        }

        jugador = playerObject.transform;

        playerMovement =
            playerObject.GetComponent<PlayerMovement>();

        colliderJugador =
            playerObject.GetComponent<Collider2D>();

        if (playerMovement == null)
        {
            Debug.LogError("Player no tiene PlayerMovement.");
            enabled = false;
            return;
        }

        if (capaParedes.value == 0)
            capaParedes = 1 << 1;

        InicializarPatrulla();

        Debug.Log("IA del profesor inicializada correctamente.");
    }

    // =====================================================
    // UPDATE PRINCIPAL
    // =====================================================

    void Update()
    {
        if (juegoTerminado ||
            jugador == null ||
            playerMovement == null ||
            agent == null)
            return;

        if (Time.timeScale == 0f)
            return;

        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning(
                "El profesor dejó de estar sobre el NavMesh."
            );
            return;
        }

        agent.speed = velocidad;

        // Comprobar si el jugador está escondido.

        if (playerMovement.EstaEscondido())
        {
            IgnorarColisionJugador(true);

            if (vioEntrarAlEscondite)
            {
                estadoActual = EstadoIA.Persiguiendo;

                ultimaPosicionJugador = jugador.position;

                ActualizarDestinoSiEsNecesario(
                    jugador.position, 0.15f
                );

                ComprobarCaptura();
                return;
            }

            estadoActual = EstadoIA.Patrullando;

            if (esconditeRevisando != null)
            {
                RevisarEsconditeEnDestino();
                return;
            }

            RevisarEsconditeCercano();
            Patrullar();

            return;
        }

        IgnorarColisionJugador(false);

        // La visión siempre tiene prioridad.

        if (EstaViendoJugador())
        {
            estadoActual = EstadoIA.Persiguiendo;

            esperandoPatrulla = false;
            tiempoSinVerJugador = 0f;

            ultimaPosicionJugador = jugador.position;

            ActualizarDestinoSiEsNecesario(
                jugador.position, 0.15f
            );

            ComprobarCaptura();
            return;
        }

        // Si lo pierde, comienza la búsqueda.

        if (estadoActual == EstadoIA.Persiguiendo)
{
    IniciarBusquedaInteligente(ultimaPosicionJugador);

    Debug.Log(
        "El profesor perdió al jugador. Comienza una búsqueda inteligente."
    );

    return;
}

        // Revisar escondite antes de continuar otras investigaciones.

        if (esconditeRevisando != null)
        {
            RevisarEsconditeEnDestino();
            VigilarMovimiento();
            return;
        }

        // Detectar ruido solamente cuando comienza un nuevo evento.

        bool ruidoActual =
            playerMovement.EsMomentoDeHacerRuido();

        if (ruidoActual && !ruidoAnterior)
        {
            EscucharRuido();
        }

        ruidoAnterior = ruidoActual;

        // Ejecutar comportamiento actual.

        switch (estadoActual)
        {
            case EstadoIA.Patrullando:

                RevisarEsconditeCercano();
                Patrullar();

                break;

            case EstadoIA.Buscando:

                BuscarUltimaPosicion();

                break;

            case EstadoIA.InvestigandoRuido:

                InvestigarRuido();

                break;

            case EstadoIA.InvestigandoCamara:

                InvestigarCamara();

                break;

            case EstadoIA.RevisandoEscondite:

                RevisarEsconditeEnDestino();

                break;
        }

        VigilarMovimiento();
    }

    // =====================================================
    // PATRULLA
    // =====================================================

    void InicializarPatrulla()
    {
        puntosPatrulla.Clear();

        if (patrolPoint1 != null)
            puntosPatrulla.Add(patrolPoint1);

        if (patrolPoint2 != null)
            puntosPatrulla.Add(patrolPoint2);

        if (patrolPoint3 != null)
            puntosPatrulla.Add(patrolPoint3);

        if (patrolPoint4 != null)
            puntosPatrulla.Add(patrolPoint4);

        if (puntosPatrulla.Count == 0)
        {
            Debug.LogError(
                "No hay puntos de patrulla asignados."
            );
            return;
        }

        indicePuntoActual = -1;

        IrAlSiguientePuntoPatrulla();
    }

    void Patrullar()
    {
        if (puntosPatrulla.Count == 0)
            return;

        if (puntoPatrullaActual == null)
        {
            IrAlSiguientePuntoPatrulla();
            return;
        }

        if (esperandoPatrulla)
        {
            tiempoEsperaActual += Time.deltaTime;

            if (tiempoEsperaActual >= tiempoEsperaPatrulla)
            {
                esperandoPatrulla = false;

                IrAlSiguientePuntoPatrulla();
            }

            return;
        }

        if (LlegóAlDestino())
        {
            esperandoPatrulla = true;

            tiempoEsperaActual = 0f;

            agent.isStopped = true;

            Debug.Log(
                "Profesor llegó a " + puntoPatrullaActual.name
            );
        }
    }

    void IrAlSiguientePuntoPatrulla()
    {
        if (puntosPatrulla.Count == 0)
            return;

        int nuevoIndice;

        if (puntosPatrulla.Count == 1)
        {
            nuevoIndice = 0;
        }
        else
        {
            nuevoIndice = Random.Range(
                0, puntosPatrulla.Count
            );

            int intentos = 0;

            while (nuevoIndice == indicePuntoActual &&
                   intentos < 20)
            {
                nuevoIndice = Random.Range(
                    0, puntosPatrulla.Count
                );

                intentos++;
            }
        }

        indicePuntoActual = nuevoIndice;

        puntoPatrullaActual =
            puntosPatrulla[indicePuntoActual];

        esperandoPatrulla = false;
        tiempoEsperaActual = 0f;

        estadoActual = EstadoIA.Patrullando;

        if (AsignarDestino(puntoPatrullaActual.position))
        {
            Debug.Log(
                "Profesor se dirige hacia " +
                puntoPatrullaActual.name
            );
        }
        else
        {
            Debug.LogWarning(
                "No se pudo alcanzar el punto de patrulla."
            );
        }
    }

    // =====================================================
    // SISTEMA DE NAVEGACION
    // =====================================================

    bool AsignarDestino(Vector3 destino)
    {
        return AsignarDestino(destino, true);
    }

    bool AsignarDestino(Vector3 destino, bool nuevoObjetivo)
    {
        if (agent == null || !agent.isOnNavMesh)
            return false;

        NavMeshHit hit;

        if (!NavMesh.SamplePosition(
            destino,
            out hit,
            distanciaMuestreoNavMesh,
            NavMesh.AllAreas))
        {
            Debug.LogWarning(
                "No se encontró NavMesh cerca del destino: " +
                destino
            );

            destinoAsignado = false;
            return false;
        }

        NavMeshPath ruta = new NavMeshPath();

        bool rutaCalculada = NavMesh.CalculatePath(
            agent.nextPosition,
            hit.position,
            agent.areaMask,
            ruta
        );

        if (!rutaCalculada ||
            ruta.status != NavMeshPathStatus.PathComplete)
        {
            Debug.LogWarning(
                "El profesor no encontró una ruta completa."
            );

            destinoAsignado = false;
            return false;
        }

        agent.isStopped = false;

        if (!agent.SetDestination(hit.position))
        {
            Debug.LogWarning(
                "NavMeshAgent rechazó el destino."
            );

            destinoAsignado = false;
            return false;
        }

        destinoActual = hit.position;
        destinoAsignado = true;

        tiempoSinMovimiento = 0f;
        tiempoReintentoActual = 0f;

        if (nuevoObjetivo)
            reintentosRuta = 0;

        return true;
    }

    void ActualizarDestinoSiEsNecesario(
        Vector3 destino,
        float distanciaMinima)
    {
        if (!destinoAsignado ||
            Vector3.Distance(
                destinoActual, destino
            ) > distanciaMinima)
        {
            AsignarDestino(destino);
        }
    }

    bool LlegóAlDestino()
    {
        if (agent == null ||
            !agent.isOnNavMesh ||
            !destinoAsignado ||
            agent.pathPending)
            return false;

        if (!agent.hasPath &&
            Vector2.Distance(
                transform.position,
                destinoActual
            ) > distanciaLlegadaPatrulla)
            return false;

        if (agent.pathStatus != NavMeshPathStatus.PathComplete)
            return false;

        float tolerancia = Mathf.Max(
            distanciaLlegadaPatrulla,
            agent.stoppingDistance + 0.05f
        );

        float distanciaRestante = agent.remainingDistance;

        if (float.IsInfinity(distanciaRestante) ||
            float.IsNaN(distanciaRestante))
            return false;

        return distanciaRestante <= tolerancia;
    }

    // =====================================================
    // CONTROL DE MOVIMIENTO Y RECUPERACION
    // =====================================================

    void VigilarMovimiento()
    {
        if (!destinoAsignado ||
            agent.isStopped ||
            agent.pathPending)
            return;

        if (LlegóAlDestino())
        {
            tiempoSinMovimiento = 0f;
            return;
        }

        if (agent.velocity.magnitude < 0.05f)
        {
            tiempoSinMovimiento += Time.deltaTime;
        }
        else
        {
            tiempoSinMovimiento = 0f;
            tiempoReintentoActual = 0f;
        }

        if (tiempoSinMovimiento >= tiempoMaximoSinMovimiento)
        {
            tiempoReintentoActual += Time.deltaTime;

            if (tiempoReintentoActual >= tiempoReintentoRuta)
            {
                tiempoReintentoActual = 0f;

                ReintentarMovimiento();
            }
        }
    }

    void ReintentarMovimiento()
    {
        if (reintentosRuta < maximosReintentosRuta)
        {
            reintentosRuta++;

            Debug.LogWarning(
                "Profesor bloqueado. Reintentando ruta. Intento: " +
                reintentosRuta
            );

            agent.isStopped = false;
            agent.ResetPath();

            // IMPORTANTE: no reiniciar el contador de reintentos.

            AsignarDestino(destinoActual, false);
        }
        else
        {
            Debug.LogWarning(
                "El profesor abandonó una ruta bloqueada."
            );

            reintentosRuta = 0;
            tiempoSinMovimiento = 0f;
            tiempoReintentoActual = 0f;

            destinoAsignado = false;

            if (estadoActual == EstadoIA.Patrullando)
            {
                IrAlSiguientePuntoPatrulla();
            }
            else if (estadoActual == EstadoIA.Persiguiendo)
            {
                AsignarDestino(jugador.position);
            }
            else
            {
                estadoActual = EstadoIA.Patrullando;

                esconditeRevisando = null;

                IrAlSiguientePuntoPatrulla();
            }
        }
    }

    // =====================================================
    // BUSQUEDA DEL JUGADOR
    // =====================================================

    // =====================================================
// BUSQUEDA INTELIGENTE
// =====================================================

void IniciarBusquedaInteligente(Vector3 posicion)
{
    estadoActual = EstadoIA.Buscando;

    centroBusqueda = posicion;

    puntosBusquedaRestantes =
        Mathf.Max(1, cantidadPuntosBusqueda);

    tiempoInspeccionBusqueda = 0f;

    inspeccionandoBusqueda = false;
    primeraInspeccionBusqueda = true;

    posicionesInspeccionadas.Clear();

    posicionesInspeccionadas.Add(posicion);

    tiempoSinMovimiento = 0f;

    esperandoPatrulla = false;

    if (!AsignarDestino(centroBusqueda))
    {
        Debug.LogWarning(
            "No se pudo alcanzar la última posición del jugador. " +
            "Buscando una posición cercana."
        );

        if (!AsignarSiguientePuntoBusqueda())
        {
            FinalizarBusquedaInteligente();
        }
    }
}

void BuscarUltimaPosicion()
{
    // Si está inspeccionando una zona, espera.

    if (inspeccionandoBusqueda)
    {
        tiempoInspeccionBusqueda += Time.deltaTime;

        float tiempoNecesario = primeraInspeccionBusqueda
            ? tiempoBusqueda
            : tiempoInspeccionPunto;

        if (tiempoInspeccionBusqueda < tiempoNecesario)
            return;

        inspeccionandoBusqueda = false;

        primeraInspeccionBusqueda = false;

        tiempoInspeccionBusqueda = 0f;

        // Todavía quedan zonas por revisar.

        if (puntosBusquedaRestantes > 0)
        {
            puntosBusquedaRestantes--;

            if (AsignarSiguientePuntoBusqueda())
                return;
        }

        // No encontró al jugador.

        FinalizarBusquedaInteligente();

        return;
    }

    // Comprobar llegada al destino.

    if (LlegóAlDestino())
    {
        agent.isStopped = true;

        inspeccionandoBusqueda = true;

        tiempoInspeccionBusqueda = 0f;

        Debug.Log(
            "Profesor está inspeccionando una zona de búsqueda."
        );
    }
}

bool AsignarSiguientePuntoBusqueda()
{
    for (
        int i = 0;
        i < intentosEncontrarPuntoBusqueda;
        i++
    )
    {
        // Elegir una posición aleatoria alrededor
        // de la última ubicación conocida.

        Vector2 desplazamiento =
            Random.insideUnitCircle * radioBusqueda;

        Vector3 candidato = centroBusqueda +
            new Vector3(
                desplazamiento.x,
                desplazamiento.y,
                0f
            );

        NavMeshHit hit;

        if (!NavMesh.SamplePosition(
            candidato,
            out hit,
            distanciaMuestreoNavMesh,
            NavMesh.AllAreas))
        {
            continue;
        }

        // Evitar revisar repetidamente el mismo lugar.

        bool repetido = false;

        foreach (Vector3 posicion in posicionesInspeccionadas)
        {
            if (Vector3.Distance(
                posicion,
                hit.position
            ) < 0.8f)
            {
                repetido = true;
                break;
            }
        }

        if (repetido)
            continue;

        // Comprobar que realmente pueda llegar.

        if (AsignarDestino(hit.position))
        {
            posicionesInspeccionadas.Add(hit.position);

            Debug.Log(
                "Profesor investigará otra zona cercana: " +
                hit.position
            );

            return true;
        }
    }

    Debug.LogWarning(
        "No se encontraron más puntos de búsqueda alcanzables."
    );

    return false;
}

void FinalizarBusquedaInteligente()
{
    Debug.Log(
        "El profesor terminó de inspeccionar la zona. " +
        "Retoma su patrulla."
    );

    inspeccionandoBusqueda = false;

    posicionesInspeccionadas.Clear();

    estadoActual = EstadoIA.Patrullando;

    destinoAsignado = false;

    IrAlSiguientePuntoPatrulla();
}

    // =====================================================
    // CAMARAS
    // =====================================================

    public void DetectadoPorCamara(Vector3 posicionDetectada)
    {
        if (juegoTerminado ||
            jugador == null ||
            playerMovement == null)
            return;

        if (playerMovement.EstaEscondido())
            return;

        if (estadoActual == EstadoIA.Persiguiendo ||
            EstaViendoJugador())
            return;

        if (esconditeRevisando != null)
            return;

        bool nuevaInvestigacion =
            estadoActual != EstadoIA.InvestigandoCamara;

        ultimaPosicionJugador = posicionDetectada;

        estadoActual = EstadoIA.InvestigandoCamara;

        esperandoPatrulla = false;

        if (nuevaInvestigacion)
        {
            tiempoBusquedaCamaraActual = 0f;

            AsignarDestino(posicionDetectada);

            Debug.Log(
                "Profesor recibió una detección de cámara."
            );
        }
        else
        {
            ActualizarDestinoSiEsNecesario(
                posicionDetectada, 1f
            );
        }
    }

    void InvestigarCamara()
    {
        if (LlegóAlDestino())
        {
            tiempoBusquedaCamaraActual += Time.deltaTime;

            if (tiempoBusquedaCamaraActual >= tiempoBusquedaCamara)
            {
                Debug.Log(
                    "Profesor terminó de investigar la cámara."
                );

                estadoActual = EstadoIA.Patrullando;

                IrAlSiguientePuntoPatrulla();
            }
        }
    }

    // =====================================================
    // ESCONDITES
    // =====================================================

    public void JugadorEntroAlEscondite()
    {
        if (EstaViendoJugador())
        {
            vioEntrarAlEscondite = true;

            ultimaPosicionJugador = jugador.position;

            estadoActual = EstadoIA.Persiguiendo;

            esperandoPatrulla = false;

            esconditeRevisando = null;
            esconditeCercano = null;

            AsignarDestino(jugador.position);

            Debug.Log(
                "¡El profesor vio al jugador entrar al escondite!"
            );
        }
        else
        {
            vioEntrarAlEscondite = false;

            Debug.Log(
                "El profesor no vio al jugador entrar."
            );
        }
    }

    public void JugadorSalioDelEscondite()
    {
        vioEntrarAlEscondite = false;

        IgnorarColisionJugador(false);

        esconditeRevisando = null;
        esconditeCercano = null;

        Debug.Log(
            "El jugador salió del escondite."
        );
    }

    void IgnorarColisionJugador(bool ignorar)
    {
        if (colliderProfesor != null &&
            colliderJugador != null)
        {
            Physics2D.IgnoreCollision(
                colliderProfesor,
                colliderJugador,
                ignorar
            );
        }
    }

    void RevisarEsconditeCercano()
    {
        if (esconditeRevisando != null)
            return;

        if (Time.time < tiempoBloqueoEscondite)
            return;

        Collider2D[] colliders =
            Physics2D.OverlapCircleAll(
                transform.position,
                distanciaRevisionEscondite
            );

        HidingSpot encontrado = null;

        foreach (Collider2D col in colliders)
        {
            HidingSpot spot =
                col.GetComponentInParent<HidingSpot>();

            if (spot != null)
            {
                encontrado = spot;
                break;
            }
        }

        if (encontrado == null)
        {
            esconditeCercano = null;
            return;
        }

        if (esconditeCercano == encontrado)
            return;

        if (ultimoEsconditeRevisado == encontrado)
            return;

        esconditeCercano = encontrado;

        float resultado = Random.Range(0f, 100f);

        if (resultado <= probabilidadRevisarEscondite)
        {
            Debug.Log(
                "Profesor decidió revisar un escondite."
            );

            esconditeRevisando = encontrado;

            ultimoEsconditeRevisado = encontrado;

            estadoActual = EstadoIA.RevisandoEscondite;

            esperandoPatrulla = false;

            if (!AsignarDestino(encontrado.transform.position))
            {
                Debug.LogWarning(
                    "No se pudo alcanzar el escondite."
                );

                esconditeRevisando = null;

                estadoActual = EstadoIA.Patrullando;
            }
        }
        else
        {
            Debug.Log(
                "Profesor pasó cerca del escondite y no lo revisó."
            );
        }
    }

    void RevisarEsconditeEnDestino()
    {
        if (esconditeRevisando == null)
        {
            estadoActual = EstadoIA.Patrullando;

            IrAlSiguientePuntoPatrulla();

            return;
        }

        if (!LlegóAlDestino())
            return;

        Debug.Log(
            "Profesor llegó al escondite."
        );

        if (esconditeRevisando.HayJugadorEscondido())
        {
            Debug.Log(
                "¡El profesor encontró al jugador!"
            );

            AtraparJugador();

            return;
        }

        Debug.Log(
            "El escondite estaba vacío."
        );

        esconditeRevisando = null;
        esconditeCercano = null;

        tiempoBloqueoEscondite = Time.time + 2f;

        estadoActual = EstadoIA.Patrullando;

        IrAlSiguientePuntoPatrulla();
    }

    // =====================================================
    // VISION
    // =====================================================

    public bool EstaViendoJugador()
    {
        if (jugador == null ||
            playerMovement == null)
            return false;

        if (playerMovement.EstaEscondido())
            return false;

        Vector2 origen = transform.position;

        Vector2 direccion =
            ((Vector2)jugador.position - origen).normalized;

        float distancia = Vector2.Distance(
            origen,
            jugador.position
        );

        float rangoActual =
            playerMovement.EstaAgachado()
            ? distanciaVisionAgachado
            : distanciaVision;

        if (distancia > rangoActual)
            return false;

        RaycastHit2D impacto = Physics2D.Raycast(
            origen,
            direccion,
            distancia,
            capaParedes
        );

        return impacto.collider == null;
    }

    // =====================================================
    // CAPTURA
    // =====================================================

    void ComprobarCaptura()
    {
        float distancia = Vector2.Distance(
            transform.position,
            jugador.position
        );

        if (distancia <= distanciaParaAtraparlo)
            AtraparJugador();
    }

    public void AtraparJugador()
    {
        if (juegoTerminado)
            return;

        juegoTerminado = true;

        agent.isStopped = true;

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
                "No se encontró GameManager."
            );
        }
    }

    // =====================================================
    // SONIDO
    // =====================================================

    void EscucharRuido()
    {
        if (playerMovement.EstaEscondido())
            return;

        if (estadoActual == EstadoIA.Persiguiendo)
            return;

        if (esconditeRevisando != null)
            return;

        float distancia = Vector2.Distance(
            transform.position,
            jugador.position
        );

        float rangoEscucha;

        if (playerMovement.EstaAgachado())
        {
            rangoEscucha = distanciaEscuchaAgachado;
        }
        else if (playerMovement.EstaCorriendo())
        {
            rangoEscucha = distanciaEscuchaCorriendo;
        }
        else
        {
            rangoEscucha = distanciaEscucha;
        }

        if (distancia > rangoEscucha)
            return;

        ultimaPosicionJugador = jugador.position;

        estadoActual = EstadoIA.InvestigandoRuido;

        esperandoPatrulla = false;

        tiempoInvestigando = 0f;

        if (AsignarDestino(ultimaPosicionJugador))
        {
            Debug.Log(
                "Profesor escuchó un ruido y va a investigarlo."
            );
        }
    }

    void InvestigarRuido()
    {
        if (LlegóAlDestino())
        {
            tiempoInvestigando += Time.deltaTime;

            if (tiempoInvestigando >= tiempoInvestigandoRuido)
            {
                Debug.Log(
                    "Profesor terminó de investigar el ruido."
                );

                estadoActual = EstadoIA.Patrullando;

                IrAlSiguientePuntoPatrulla();
            }
        }
    }

    // =====================================================
    // GIZMOS
    // =====================================================

    void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(
            transform.position,
            distanciaRevisionEscondite
        );

        if (jugador != null)
        {
            Gizmos.DrawLine(
                transform.position,
                jugador.position
            );
        }

        if (destinoAsignado)
        {
            Gizmos.DrawWireSphere(
                destinoActual,
                0.2f
            );
        }
    }
}