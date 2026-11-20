
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidadCaminar = 5f;
    public float velocidadCorrer = 8f;
    public float velocidadAgachado = 2.5f;

    [Header("Stamina")]
    public float staminaMaxima = 5f;
    public float consumoStamina = 1f;
    public float recuperacionStamina = 0.7f;

    [Header("Agacharse")]
    public float alturaColliderAgachado = 0.6f;

    [Header("Ruido")]
    public float ruidoCaminando = 3f;
    public float ruidoCorriendo = 10f;
    public float ruidoAgachado = 0.5f;

    public float intervaloRuidoCaminando = 0.8f;
    public float intervaloRuidoCorriendo = 0.35f;
    public float intervaloRuidoAgachado = 1.5f;

    [Header("Animaciones")]
    private Animator animator;

    private Vector2 ultimaDireccion = new Vector2(0f, -1f);

    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;

    private Vector2 movimiento;

    public float staminaActual;
    public float StaminaMaxima => staminaMaxima;

    private bool corriendo;
    private bool agachado;

    private float alturaColliderNormal;
    private float temporizadorRuido;

    // =========================
    // VISUAL DEL JUGADOR
    // =========================

    private SpriteRenderer[] spritesDelJugador;

    private void Awake()
    {
        spritesDelJugador =
            GetComponentsInChildren<SpriteRenderer>(true);

        animator =
            GetComponentInChildren<Animator>(true);
    }

    public void OcultarVisual()
    {
        foreach (SpriteRenderer sprite in spritesDelJugador)
        {
            sprite.enabled = false;
        }
    }

    public void MostrarVisual()
    {
        foreach (SpriteRenderer sprite in spritesDelJugador)
        {
            sprite.enabled = true;
        }
    }

    // =========================
    // LLAVE
    // =========================

    private bool tieneLlave = false;

    public void ObtenerLlave()
    {
        tieneLlave = true;

        Debug.Log("El jugador tiene la llave.");
    }

    public bool TieneLlave()
    {
        return tieneLlave;
    }

    // =========================
    // ESCONDITE
    // =========================

    private bool escondido = false;

    private RigidbodyConstraints2D restriccionesNormales;

    public bool EstaEscondido()
    {
        return escondido;
    }

    public void AlternarEscondido()
    {
        escondido = !escondido;

        if (escondido)
        {
            movimiento = Vector2.zero;
            corriendo = false;

            restriccionesNormales = rb.constraints;

            rb.constraints =
                RigidbodyConstraints2D.FreezeAll;

            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

            OcultarVisual();

            Debug.Log(
                "Jugador se escondió y desapareció visualmente."
            );
        }
        else
        {
            rb.constraints = restriccionesNormales;

            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

            MostrarVisual();

            Debug.Log(
                "Jugador salió del escondite y volvió a aparecer."
            );
        }
    }

    // =========================
    // ESTADOS
    // =========================

    public bool EstaAgachado()
    {
        return agachado;
    }

    public bool EstaCorriendo()
    {
        return corriendo;
    }

    // =========================
    // ANIMACIONES
    // =========================

    private void ActualizarAnimacion()
    {
        if (animator == null)
            return;

        if (movimiento.sqrMagnitude > 0.01f)
        {
            ultimaDireccion = movimiento.normalized;
        }

        animator.SetFloat("MoveX", ultimaDireccion.x);
        animator.SetFloat("MoveY", ultimaDireccion.y);

        animator.SetFloat(
            "Speed",
            movimiento.sqrMagnitude
        );
    }

    // =========================
    // RUIDO
    // =========================

    public bool PuedeGenerarRuido()
    {
        if (escondido)
            return false;

        return movimiento != Vector2.zero;
    }

    public float ObtenerNivelRuido()
    {
        if (escondido)
            return 0f;

        if (movimiento == Vector2.zero)
            return 0f;

        if (agachado)
            return ruidoAgachado;

        if (corriendo)
            return ruidoCorriendo;

        return ruidoCaminando;
    }

    public bool EsMomentoDeHacerRuido()
    {
        if (!PuedeGenerarRuido())
            return false;

        temporizadorRuido -= Time.deltaTime;

        float intervaloActual;

        if (agachado)
        {
            intervaloActual =
                intervaloRuidoAgachado;
        }
        else if (corriendo)
        {
            intervaloActual =
                intervaloRuidoCorriendo;
        }
        else
        {
            intervaloActual =
                intervaloRuidoCaminando;
        }

        if (temporizadorRuido <= 0f)
        {
            temporizadorRuido =
                intervaloActual;

            return true;
        }

        return false;
    }

    // =========================
    // INICIO
    // =========================

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        boxCollider = GetComponent<BoxCollider2D>();

        staminaActual = staminaMaxima;

        alturaColliderNormal =
            boxCollider.size.y;

        temporizadorRuido = 0f;

        restriccionesNormales = rb.constraints;

        if (animator != null)
        {
            // Dirección inicial: sur.
            animator.SetFloat("MoveX", 0f);
            animator.SetFloat("MoveY", -1f);
        }
        else
        {
            Debug.LogError(
                "PlayerMovement: no se encontró un Animator en el jugador o sus hijos."
            );
        }
    }

    // =========================
    // UPDATE
    // =========================

    void Update()
    {
        if (escondido)
        {
            movimiento = Vector2.zero;
            corriendo = false;

            return;
        }

        movimiento = Vector2.zero;

        // =========================
        // MOVIMIENTO
        // =========================

        if (Keyboard.current.wKey.isPressed ||
            Keyboard.current.upArrowKey.isPressed)
        {
            movimiento.y += 1;
        }

        if (Keyboard.current.sKey.isPressed ||
            Keyboard.current.downArrowKey.isPressed)
        {
            movimiento.y -= 1;
        }

        if (Keyboard.current.dKey.isPressed ||
            Keyboard.current.rightArrowKey.isPressed)
        {
            movimiento.x += 1;
        }

        if (Keyboard.current.aKey.isPressed ||
            Keyboard.current.leftArrowKey.isPressed)
        {
            movimiento.x -= 1;
        }

        movimiento = movimiento.normalized;

        // =========================
        // ACTUALIZAR ANIMACION
        // =========================

        ActualizarAnimacion();

        // =========================
        // AGACHARSE
        // =========================

        agachado =
            Keyboard.current.ctrlKey.isPressed;

        if (agachado)
        {
            boxCollider.size =
                new Vector2(
                    boxCollider.size.x,
                    alturaColliderAgachado
                );

            boxCollider.offset =
                new Vector2(
                    0f,
                    -(alturaColliderNormal -
                    alturaColliderAgachado) / 2f
                );
        }
        else
        {
            boxCollider.size =
                new Vector2(
                    boxCollider.size.x,
                    alturaColliderNormal
                );

            boxCollider.offset = Vector2.zero;
        }

        // =========================
        // CORRER
        // =========================

        bool quiereCorrer =
            Keyboard.current.shiftKey.isPressed &&
            movimiento != Vector2.zero &&
            !agachado;

        if (quiereCorrer &&
            staminaActual > 0)
        {
            corriendo = true;

            staminaActual -=
                consumoStamina *
                Time.deltaTime;

            if (staminaActual <= 0)
            {
                staminaActual = 0;
                corriendo = false;
            }
        }
        else
        {
            corriendo = false;

            staminaActual +=
                recuperacionStamina *
                Time.deltaTime;

            if (staminaActual > staminaMaxima)
            {
                staminaActual = staminaMaxima;
            }
        }
    }

    // =========================
    // MOVIMIENTO FISICO
    // =========================

    void FixedUpdate()
    {
        if (escondido)
            return;

        float velocidadActual;

        if (agachado)
        {
            velocidadActual =
                velocidadAgachado;
        }
        else if (corriendo)
        {
            velocidadActual =
                velocidadCorrer;
        }
        else
        {
            velocidadActual =
                velocidadCaminar;
        }

        rb.MovePosition(
            rb.position +
            movimiento *
            velocidadActual *
            Time.fixedDeltaTime
        );
    }
}
