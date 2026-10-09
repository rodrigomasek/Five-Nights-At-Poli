
using UnityEngine;
using TMPro;

public class ObjectiveManager : MonoBehaviour
{
    [Header("Texto de objetivos")]
    public TMP_Text objectivesText;

    private bool llaveConseguida = false;
    private bool escapado = false;

    void Start()
    {
        ActualizarObjetivos();
    }

    public void CompletarObjetivoLlave()
    {
        if (llaveConseguida)
            return;

        llaveConseguida = true;
        ActualizarObjetivos();

        Debug.Log("Objetivo completado: encontrar la llave.");
    }

    public void CompletarObjetivoEscape()
    {
        if (escapado)
            return;

        escapado = true;
        ActualizarObjetivos();

        Debug.Log("Objetivo completado: escapar.");
    }

    public void ObjetivoEncontrarSalida()
    {
        CompletarObjetivoEscape();
    }

    void ActualizarObjetivos()
    {
        if (objectivesText == null)
        {
            Debug.LogError(
                "ObjectiveManager: no asignaste Objectives Text."
            );
            return;
        }

        string llave = llaveConseguida
            ? "[X] Encontrar la llave"
            : "[ ] Encontrar la llave";

        string escape = escapado
            ? "[X] Escapar por la puerta"
            : "[ ] Escapar por la puerta";

        objectivesText.text =
            "OBJETIVOS\n\n" +
            llave + "\n" +
            escape;
    }
}
