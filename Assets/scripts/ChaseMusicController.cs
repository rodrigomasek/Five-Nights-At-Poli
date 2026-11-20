
using UnityEngine;

public class ChaseMusicController : MonoBehaviour
{
    [Header("Audios")]
    public AudioSource musicaAmbiente;
    public AudioSource musicaPersecucion;

    [Header("Configuracion")]
    [Min(0.1f)]
    public float duracionTransicion = 1.5f;

    [Min(0f)]
    public float tiempoMantenerPersecucion = 10f;

    [Range(0f, 1f)]
    public float volumenAmbiente = 0.5f;

    [Range(0f, 1f)]
    public float volumenPersecucion = 0.3f;

    [Header("Profesor")]
    public EnemyMovement profesor;

    private bool persecucionActiva = false;

    private float tiempoDesdeUltimaVision = 0f;

    private float volumenAmbienteActual = 0f;
    private float volumenPersecucionActual = 0f;

    void Start()
    {
        // Buscar al profesor automáticamente.
        if (profesor == null)
        {
            profesor = FindAnyObjectByType<EnemyMovement>();
        }

        // Comprobar música ambiental.
        if (musicaAmbiente == null)
        {
            Debug.LogError(
                "ChaseMusicController: falta asignar la musica ambiental."
            );
        }

        // Comprobar música de persecución.
        if (musicaPersecucion == null)
        {
            Debug.LogError(
                "ChaseMusicController: falta asignar la musica de persecucion."
            );
        }

        // Configurar música ambiental.
        if (musicaAmbiente != null)
        {
            musicaAmbiente.playOnAwake = false;
            musicaAmbiente.loop = true;
            musicaAmbiente.spatialBlend = 0f;

            musicaAmbiente.volume = 0f;
            musicaAmbiente.Play();

            volumenAmbienteActual = 0f;
        }

        // Configurar música de persecución.
        if (musicaPersecucion != null)
        {
            musicaPersecucion.playOnAwake = false;
            musicaPersecucion.loop = true;
            musicaPersecucion.spatialBlend = 0f;

            musicaPersecucion.volume = 0f;

            volumenPersecucionActual = 0f;
        }

        persecucionActiva = false;

        Debug.Log("Sistema de musica iniciado.");
    }

    void Update()
    {
        if (profesor == null)
            return;

        // Detectar si el profesor ve al jugador.
        if (profesor.EstaViendoJugador())
        {
            tiempoDesdeUltimaVision = 0f;

            ActivarPersecucion();
        }
        else
        {
            tiempoDesdeUltimaVision += Time.deltaTime;

            if (tiempoDesdeUltimaVision >= tiempoMantenerPersecucion)
            {
                DesactivarPersecucion();
            }
        }

        ActualizarVolumenes();
    }

    public void ActivarPersecucion()
    {
        if (persecucionActiva)
            return;

        persecucionActiva = true;

        Debug.Log("Musica de persecucion activada.");
    }

    public void DesactivarPersecucion()
    {
        if (!persecucionActiva)
            return;

        persecucionActiva = false;

        Debug.Log("Musica ambiental restaurada.");
    }

    void ActualizarVolumenes()
    {
        float velocidadTransicion =
            Time.deltaTime / duracionTransicion;

        float volumenAmbienteObjetivo =
            persecucionActiva ? 0f : volumenAmbiente;

        float volumenPersecucionObjetivo =
            persecucionActiva ? volumenPersecucion : 0f;

        // Transición gradual del ambiente.
        volumenAmbienteActual = Mathf.MoveTowards(
            volumenAmbienteActual,
            volumenAmbienteObjetivo,
            velocidadTransicion
        );

        // Transición gradual de la persecución.
        volumenPersecucionActual = Mathf.MoveTowards(
            volumenPersecucionActual,
            volumenPersecucionObjetivo,
            velocidadTransicion
        );

        if (musicaAmbiente != null)
        {
            musicaAmbiente.volume = volumenAmbienteActual;
        }

        if (musicaPersecucion != null)
        {
            musicaPersecucion.volume = volumenPersecucionActual;
        }
    }
}
