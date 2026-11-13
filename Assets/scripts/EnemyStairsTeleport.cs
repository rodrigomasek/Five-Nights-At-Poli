using UnityEngine;
using UnityEngine.AI;

public class EnemyStairsTeleport : MonoBehaviour
{
    [Header("Destinos")]
    public Transform puntoPiso1;
    public Transform puntoPiso2;

    private NavMeshAgent agente;
    private bool estaEnEscalera = false;

    void Awake()
    {
        agente = GetComponent<NavMeshAgent>();

        if (agente == null)
        {
            Debug.LogError("No se encontró NavMeshAgent.");
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Stairs"))
            return;

        if (estaEnEscalera)
            return;

        estaEnEscalera = true;

        Teletransportar();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Stairs"))
        {
            estaEnEscalera = false;
        }
    }

    void Teletransportar()
    {
        if (agente == null)
            return;

        Transform destino = null;

        float distanciaPiso1 = Vector2.Distance(
            transform.position,
            puntoPiso1.position
        );

        float distanciaPiso2 = Vector2.Distance(
            transform.position,
            puntoPiso2.position
        );

        destino = distanciaPiso1 < distanciaPiso2
            ? puntoPiso2
            : puntoPiso1;

        agente.ResetPath();

        bool resultado = agente.Warp(destino.position);

        if (resultado)
        {
            Debug.Log("Profesor cambió de piso.");
        }
        else
        {
            Debug.LogError(
                "No se pudo teletransportar al profesor. " +
                "Comprobá que el destino esté sobre el NavMesh."
            );
        }
    }
}