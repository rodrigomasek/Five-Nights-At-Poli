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

private Rigidbody2D rb;
private BoxCollider2D boxCollider;

private Vector2 movimiento;

private float staminaActual;
private bool corriendo;
private bool agachado;

private float alturaColliderNormal;


void Start()
{
    rb = GetComponent<Rigidbody2D>();
    boxCollider = GetComponent<BoxCollider2D>();

    staminaActual = staminaMaxima;

    // Guardamos la altura original del collider
    alturaColliderNormal = boxCollider.size.y;
}


void Update()
{
    // =========================
    // MOVIMIENTO
    // =========================

    movimiento = Vector2.zero;

    if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
        movimiento.y += 1;

    if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
        movimiento.y -= 1;

    if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
        movimiento.x += 1;

    if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
        movimiento.x -= 1;

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

        staminaActual -= consumoStamina * Time.deltaTime;

        if (staminaActual <= 0)
        {
            staminaActual = 0;
            corriendo = false;
        }
    }
    else
    {
        corriendo = false;

        staminaActual += recuperacionStamina * Time.deltaTime;

        if (staminaActual > staminaMaxima)
        {
            staminaActual = staminaMaxima;
        }
    }
}


void FixedUpdate()
{
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
        movimiento * velocidadActual * Time.fixedDeltaTime
    );
}

}
