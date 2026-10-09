
using UnityEngine;
using UnityEngine.InputSystem;

public class KeyItem : MonoBehaviour
{
    [Header("Interacción")]
    public Key teclaRecoger = Key.E;

    [Header("Objetivos")]
    public ObjectiveManager objectiveManager;

    private bool jugadorCerca = false;
    private PlayerMovement jugador;

    void Update()
    {
        if (!jugadorCerca || jugador == null)
            return;

        if (Keyboard.current[teclaRecoger].wasPressedThisFrame)
        {
            RecogerLlave();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        jugador = other.GetComponent<PlayerMovement>();

        if (jugador != null)
        {
            jugadorCerca = true;

            Debug.Log(
                "Estás cerca de la llave. Pulsa E para recogerla."
            );
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        jugadorCerca = false;
        jugador = null;
    }


    void RecogerLlave()
    {
        jugador.ObtenerLlave();

        Debug.Log("¡Llave recogida!");

        if (objectiveManager != null)
        {
            objectiveManager.CompletarObjetivoLlave();
        }
        else
        {
            Debug.LogWarning(
                "No asignaste el ObjectiveManager en la llave."
            );
        }

        gameObject.SetActive(false);
    }


}
