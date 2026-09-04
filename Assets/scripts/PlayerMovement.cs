
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

    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;

    private Vector2 movimiento;

    private float staminaActual;
    private bool corriendo;
    private bool agachado;

    private float alturaColliderNormal;
    private float temporizadorRuido;

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

            // Guardamos las restricciones normales
            restriccionesNormales = rb.constraints;

            // Bloqueamos completamente al jugador
            rb.constraints = RigidbodyConstraints2D.FreezeAll;

            // Evitamos que conserve velocidad
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

            Debug.Log("Jugador se escondió.");
        }
        else
        {
            // Restauramos las restricciones anteriores
            rb.constraints = restriccionesNormales;

            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

            Debug.Log("Jugador salió del escondite.");
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
            intervaloActual = intervaloRuidoAgachado;
        }
        else if (corriendo)
        {
            intervaloActual = intervaloRuidoCorriendo;
        }
        else
        {
            intervaloActual = intervaloRuidoCaminando;
        }

        if (temporizadorRuido <= 0f)
        {
            temporizadorRuido = intervaloActual;
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

        alturaColliderNormal = boxCollider.size.y;

        temporizadorRuido = 0f;

        restriccionesNormales = rb.constraints;
    }

    // =========================
    // UPDATE
    // =========================

    void Update()
    {
        // Si está escondido, no procesa movimiento
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
        // AGACHARSE
        // =========================

        agachado = Keyboard.current.ctrlKey.isPressed;

        if (agachado)
        {
            boxCollider.size = new Vector2(
                boxCollider.size.x,
                alturaColliderAgachado
            );

            boxCollider.offset = new Vector2(
                0f,
                -(alturaColliderNormal - alturaColliderAgachado) / 2f
            );
        }
        else
        {
            boxCollider.size = new Vector2(
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

        if (quiereCorrer && staminaActual > 0)
        {
            corriendo = true;

            staminaActual -=
                consumoStamina * Time.deltaTime;

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
                recuperacionStamina * Time.deltaTime;

            if (staminaActual > staminaMaxima)
            {
                staminaActual = staminaMaxima;
            }
        }
    }

    // =========================
    // MOVIMIENTO FÍSICO
    // =========================

    void FixedUpdate()
    {
        if (escondido)
            return;

        float velocidadActual;

        if (agachado)
        {
            velocidadActual = velocidadAgachado;
        }
        else if (corriendo)
        {
            velocidadActual = velocidadCorrer;
        }
        else
        {
            velocidadActual = velocidadCaminar;
        }

        rb.MovePosition(
            rb.position +
            movimiento *
            velocidadActual *
            Time.fixedDeltaTime
        );
    }
}
