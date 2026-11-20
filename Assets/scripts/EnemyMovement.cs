
using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public enum PisoEscuela
{
    Piso1,
    Piso2
}

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
    public float velocidadPatrullaje = 2f;
    public float velocidadDeteccion = 4f;

    [Header("Patrulla por piso")]
    public List<Transform> puntosPatrullaPiso1 = new List<Transform>();
    public List<Transform> puntosPatrullaPiso2 = new List<Transform>();
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

    [Header("Cambio de piso y recuperacion")]
    public PisoEscuela pisoInicial = PisoEscuela.Piso1;
    public float distanciaMaximaJugador = 20f;
    public float distanciaMinimaRecuperacion = 8f;
    public float distanciaMaximaRecuperacion = 12f;
    public float demoraCambioPiso = 3f;
    public float radioValidacionDestinoPiso = 0.75f;
    public float intervaloComprobacionDistancia = 0.5f;
    public int intentosRecuperacionDistancia = 25;

    private PisoEscuela pisoProfesor;
    private PisoEscuela pisoJugador;
    private bool cambiandoPiso;
    private float proximaComprobacionDistancia;
    private Coroutine rutinaCambioPiso;

    private NavMeshAgent agent;
    private Collider2D colliderProfesor;
    private Collider2D colliderJugador;
    private GameManager gameManager;

    private Transform jugador;
    private PlayerMovement playerMovement;

    private readonly List<Transform> puntosPatrullaActivos =
        new List<Transform>();

    private int indicePuntoActual = -1;
    private Transform puntoPatrullaActual;

    private Vector3 ultimaPosicionJugador;
    private Vector3 destinoActual;

    private EstadoIA estadoActual = EstadoIA.Patrullando;

    private float tiempoSinVerJugador;

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

        pisoProfesor = pisoInicial;
        pisoJugador = pisoInicial;

        gameManager = FindAnyObjectByType<GameManager>();

        if (agent == null)
        {
            Debug.LogError("El profesor no tiene NavMeshAgent.");
            enabled = false;
            return;
        }

        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.speed = velocidadPatrullaje;
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
    // LISTAS DE PATRULLA POR PISO
    // =====================================================

    void ActualizarPuntosPatrullaDelPiso()
    {
        puntosPatrullaActivos.Clear();

        List<Transform> listaOrigen =
            pisoProfesor == PisoEscuela.Piso1
            ? puntosPatrullaPiso1
            : puntosPatrullaPiso2;

        foreach (Transform punto in listaOrigen)
        {
            if (punto != null)
                puntosPatrullaActivos.Add(punto);
        }

        indicePuntoActual = -1;
        puntoPatrullaActual = null;

        Debug.Log(
            "Lista de patrulla actualizada. Piso: " +
            pisoProfesor +
            ". Puntos disponibles: " +
            puntosPatrullaActivos.Count
        );

        if (puntosPatrullaActivos.Count == 0)
        {
            Debug.LogWarning(
                "No hay puntos de patrulla configurados para " +
                pisoProfesor
            );
        }
    }

    void InicializarPatrulla()
    {
        ActualizarPuntosPatrullaDelPiso();

        if (puntosPatrullaActivos.Count > 0)
            IrAlSiguientePuntoPatrulla();
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

        if (cambiandoPiso)
            return;

        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning(
                "El profesor dejó de estar sobre el NavMesh."
            );
            return;
        }

        agent.speed = estadoActual == EstadoIA.Persiguiendo
    ? velocidadDeteccion
    : velocidadPatrullaje;

        if (ComprobarRecuperacionDistancia())
            return;

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

        if (estadoActual == EstadoIA.Persiguiendo)
        {
            IniciarBusquedaInteligente(ultimaPosicionJugador);

            Debug.Log(
                "El profesor perdió al jugador. " +
                "Comienza una búsqueda inteligente."
            );

            return;
        }

        if (esconditeRevisando != null)
        {
            RevisarEsconditeEnDestino();
            VigilarMovimiento();
            return;
        }

        bool ruidoActual =
            playerMovement.EsMomentoDeHacerRuido();

        if (ruidoActual && !ruidoAnterior)
            EscucharRuido();

        ruidoAnterior = ruidoActual;

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
    // CAMBIO DE PISO Y RECUPERACION POR DISTANCIA
    // =====================================================

    // Se conserva la firma de tres parámetros para que
    // otros scripts que ya la utilizan sigan siendo compatibles.
    // El profesor cambia de piso siempre que el jugador
    // se encuentre en un piso diferente.
    public void NotificarCambioPisoJugador(
        PisoEscuela pisoDestino,
        Transform destinoProfesor,
        bool profesorLoVeia)
    {
        pisoJugador = pisoDestino;

        if (cambiandoPiso || pisoProfesor == pisoDestino)
            return;

        if (destinoProfesor == null)
        {
            Debug.LogError(
                "TeleportZone no tiene asignado Destino Profesor."
            );
            return;
        }

        if (rutinaCambioPiso != null)
            StopCoroutine(rutinaCambioPiso);

        rutinaCambioPiso = StartCoroutine(
            CambiarPisoConDemora(pisoDestino, destinoProfesor)
        );
    }

    IEnumerator CambiarPisoConDemora(
        PisoEscuela pisoDestino,
        Transform destinoProfesor)
    {
        if (agent == null || !agent.isOnNavMesh)
        {
            Debug.LogError(
                "No se puede iniciar el cambio de piso: " +
                "el profesor no está sobre un NavMesh."
            );

            rutinaCambioPiso = null;
            yield break;
        }

        EstadoIA estadoAnterior = estadoActual;
        Vector3 destinoAnterior = destinoActual;
        bool teniaDestinoAnterior = destinoAsignado;

        cambiandoPiso = true;

        agent.isStopped = true;
        agent.ResetPath();
        destinoAsignado = false;

        SpriteRenderer[] renderers =
            GetComponentsInChildren<SpriteRenderer>(true);

        bool[] renderersEstabanActivos =
            new bool[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            renderersEstabanActivos[i] = renderers[i].enabled;
            renderers[i].enabled = false;
        }

        bool colliderEstabaActivo =
            colliderProfesor != null && colliderProfesor.enabled;

        if (colliderProfesor != null)
            colliderProfesor.enabled = false;

        yield return new WaitForSeconds(demoraCambioPiso);

        if (destinoProfesor == null ||
            agent == null ||
            !agent.enabled)
        {
            Debug.LogError(
                "Se canceló el cambio de piso: " +
                "falta el destino o el NavMeshAgent."
            );

            RestaurarVisibilidadProfesor(
                renderers,
                renderersEstabanActivos,
                colliderEstabaActivo
            );

            if (agent != null)
                agent.isStopped = false;

            RestaurarComportamientoAnterior(
                estadoAnterior,
                destinoAnterior,
                teniaDestinoAnterior
            );

            cambiandoPiso = false;
            rutinaCambioPiso = null;
            yield break;
        }

        NavMeshQueryFilter filtro = new NavMeshQueryFilter
        {
            agentTypeID = agent.agentTypeID,
            areaMask = agent.areaMask
        };

        NavMeshHit hit;

        bool encontroNavMesh = NavMesh.SamplePosition(
            destinoProfesor.position,
            out hit,
            radioValidacionDestinoPiso,
            filtro
        );

        if (!encontroNavMesh ||
            Vector3.Distance(
                hit.position,
                destinoProfesor.position
            ) > radioValidacionDestinoPiso)
        {
            Debug.LogError(
                "El destino del profesor no está sobre el NavMesh. " +
                "Revisá Destino Profesor en TeleportZone."
            );

            RestaurarVisibilidadProfesor(
                renderers,
                renderersEstabanActivos,
                colliderEstabaActivo
            );

            agent.isStopped = false;

            RestaurarComportamientoAnterior(
                estadoAnterior,
                destinoAnterior,
                teniaDestinoAnterior
            );

            cambiandoPiso = false;
            rutinaCambioPiso = null;
            yield break;
        }

        bool trasladoCorrecto = agent.Warp(hit.position);

        if (!trasladoCorrecto)
        {
            Debug.LogError(
                "NavMeshAgent.Warp no pudo trasladar al profesor."
            );

            RestaurarVisibilidadProfesor(
                renderers,
                renderersEstabanActivos,
                colliderEstabaActivo
            );

            agent.isStopped = false;

            RestaurarComportamientoAnterior(
                estadoAnterior,
                destinoAnterior,
                teniaDestinoAnterior
            );

            cambiandoPiso = false;
            rutinaCambioPiso = null;
            yield break;
        }

        pisoProfesor = pisoDestino;

        // Cambiar la lista de patrulla al nuevo piso.
        ActualizarPuntosPatrullaDelPiso();

        agent.ResetPath();
        agent.isStopped = false;

        RestaurarVisibilidadProfesor(
            renderers,
            renderersEstabanActivos,
            colliderEstabaActivo
        );

        ultimaPosicionJugador = jugador.position;
        esperandoPatrulla = false;
        esconditeRevisando = null;
        esconditeCercano = null;
        destinoAsignado = false;

        if (!playerMovement.EstaEscondido())
        {
            estadoActual = EstadoIA.Persiguiendo;
            AsignarDestino(jugador.position);
        }
        else
        {
            estadoActual = EstadoIA.Buscando;
            IniciarBusquedaInteligente(ultimaPosicionJugador);
        }

        Debug.Log(
            "Profesor cambió correctamente al " + pisoDestino +
            ". Ahora tiene " + puntosPatrullaActivos.Count +
            " puntos de patrulla disponibles."
        );

        cambiandoPiso = false;
        rutinaCambioPiso = null;
    }

    void RestaurarComportamientoAnterior(
        EstadoIA estadoAnterior,
        Vector3 destinoAnterior,
        bool teniaDestinoAnterior)
    {
        estadoActual = estadoAnterior;
        destinoAsignado = false;

        if (teniaDestinoAnterior)
            AsignarDestino(destinoAnterior, false);
    }

    void RestaurarVisibilidadProfesor(
        SpriteRenderer[] renderers,
        bool[] estadosOriginales,
        bool colliderEstabaActivo)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].enabled = estadosOriginales[i];
        }

        if (colliderProfesor != null)
            colliderProfesor.enabled = colliderEstabaActivo;
    }

    bool ComprobarRecuperacionDistancia()
    {
        if (cambiandoPiso ||
            playerMovement.EstaEscondido() ||
            pisoProfesor != pisoJugador ||
            Time.time < proximaComprobacionDistancia)
            return false;

        proximaComprobacionDistancia =
            Time.time + intervaloComprobacionDistancia;

        float distancia = Vector2.Distance(
            transform.position,
            jugador.position
        );

        if (distancia <= distanciaMaximaJugador)
            return false;

        if (!agent.isOnNavMesh)
            return false;

        NavMeshQueryFilter filtro = new NavMeshQueryFilter
        {
            agentTypeID = agent.agentTypeID,
            areaMask = agent.areaMask
        };

        for (int i = 0; i < intentosRecuperacionDistancia; i++)
        {
            Vector2 direccion =
                Random.insideUnitCircle.normalized;

            float radio = Random.Range(
                distanciaMinimaRecuperacion,
                distanciaMaximaRecuperacion
            );

            Vector3 candidato = jugador.position +
                new Vector3(
                    direccion.x * radio,
                    direccion.y * radio,
                    0f
                );

            NavMeshHit hit;

            if (!NavMesh.SamplePosition(
                candidato,
                out hit,
                radioValidacionDestinoPiso,
                filtro))
                continue;

            float distanciaAlJugador = Vector2.Distance(
                hit.position,
                jugador.position
            );

            if (distanciaAlJugador < distanciaMinimaRecuperacion ||
                distanciaAlJugador > distanciaMaximaRecuperacion)
                continue;

            if (!agent.Warp(hit.position))
                continue;

            agent.ResetPath();
            agent.isStopped = false;

            destinoAsignado = false;
            tiempoSinMovimiento = 0f;
            tiempoReintentoActual = 0f;

            ultimaPosicionJugador = jugador.position;
            estadoActual = EstadoIA.Persiguiendo;
            esperandoPatrulla = false;

            AsignarDestino(jugador.position);

            Debug.Log(
                "Profesor recuperado por distancia. Nueva distancia: " +
                distanciaAlJugador.ToString("F1")
            );

            return true;
        }

        Debug.LogWarning(
            "El profesor superó la distancia máxima, pero no " +
            "encontró una posición válida de recuperación."
        );

        return false;
    }

    // =====================================================
    // PATRULLA
    // =====================================================

    void Patrullar()
    {
        if (puntosPatrullaActivos.Count == 0)
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
        if (puntosPatrullaActivos.Count == 0)
            return;

        int nuevoIndice;

        if (puntosPatrullaActivos.Count == 1)
        {
            nuevoIndice = 0;
        }
        else
        {
            nuevoIndice = Random.Range(
                0,
                puntosPatrullaActivos.Count
            );

            int intentos = 0;

            while (nuevoIndice == indicePuntoActual &&
                   intentos < 20)
            {
                nuevoIndice = Random.Range(
                    0,
                    puntosPatrullaActivos.Count
                );

                intentos++;
            }
        }

        indicePuntoActual = nuevoIndice;

        puntoPatrullaActual =
            puntosPatrullaActivos[indicePuntoActual];

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
                "No se pudo alcanzar el punto de patrulla: " +
                puntoPatrullaActual.name
            );

            puntoPatrullaActual = null;
            destinoAsignado = false;
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
                destinoActual,
                destino
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
                puntoPatrullaActual = null;
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
                puntoPatrullaActual = null;
                IrAlSiguientePuntoPatrulla();
            }
        }
    }

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
                FinalizarBusquedaInteligente();
        }
    }

    void BuscarUltimaPosicion()
    {
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

            if (puntosBusquedaRestantes > 0)
            {
                puntosBusquedaRestantes--;

                if (AsignarSiguientePuntoBusqueda())
                    return;
            }

            FinalizarBusquedaInteligente();
            return;
        }

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
                continue;

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
        puntoPatrullaActual = null;

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
                puntoPatrullaActual = null;

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
            puntoPatrullaActual = null;

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
        puntoPatrullaActual = null;

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
                puntoPatrullaActual = null;

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
