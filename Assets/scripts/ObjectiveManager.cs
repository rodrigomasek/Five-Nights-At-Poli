using UnityEngine;
using TMPro;

public class ObjectiveManager : MonoBehaviour
{
    [Header("Interfaz")]
    public TMP_Text objectivesText;

    [Header("Rescate")]
    public int estudiantesNecesarios = 2;

    private bool llaveConseguida = false;
    private int estudiantesRescatados = 0;
    private bool escapado = false;

    private const string VERDE = "#79D99A";
    private const string ROJO = "#D66B6B";
    private const string BLANCO = "#E6E9EF";
    private const string GRIS = "#929BAA";

    void Start()
    {
        if (objectivesText == null)
        {
            Debug.LogError("No asignaste Objectives Text.");
            return;
        }

        objectivesText.richText = true;
        ActualizarObjetivos();
    }

    public void CompletarObjetivoLlave()
    {
        if (llaveConseguida)
            return;

        llaveConseguida = true;
        ActualizarObjetivos();

        Debug.Log("Llave conseguida.");
    }

    public void CompletarObjetivoRescatarNPC()
    {
        if (estudiantesRescatados >= estudiantesNecesarios)
            return;

        estudiantesRescatados++;

        ActualizarObjetivos();

        Debug.Log(
            "Estudiantes rescatados: " +
            estudiantesRescatados + "/" +
            estudiantesNecesarios
        );
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
            return;

        objectivesText.text =
            "<size=110%><b><color=" + BLANCO +
            ">MISSION LOG</color></b></size>" +
            "  <size=75%><color=" + GRIS +
            ">01</color></size>\n" +

            "<size=80%><color=" + GRIS +
            ">OBJETIVOS DE SUPERVIVENCIA</color></size>\n" +

            "<color=#46505E>━━━━━━━━━━━━━━━━━━━━</color>\n\n" +

            CrearObjetivo(
                "01",
                "ENCONTRAR LA LLAVE",
                llaveConseguida
            ) +

            "\n\n" +

            CrearObjetivo(
                "02",
                "RESCATAR ESTUDIANTES",
                estudiantesRescatados >= estudiantesNecesarios
            ) +

            "\n" +

            "      <size=80%><color=" + GRIS +
            ">PROGRESO</color></size>" +

            "  <b><color=" + BLANCO + ">" +
            estudiantesRescatados + "/" +
            estudiantesNecesarios +
            "</color></b>\n" +

            "      " + CrearBarraProgreso() +

            "\n\n" +

            CrearObjetivo(
                "03",
                "ESCAPAR DE LA ESCUELA",
                escapado
            );
    }

    string CrearObjetivo(
        string numero,
        string nombre,
        bool completado
    )
    {
        string color = completado ? VERDE : BLANCO;

        string estado = completado
            ? "<color=" + VERDE + ">COMPLETADO</color>"
            : "<color=" + ROJO + ">PENDIENTE</color>";

        return
            "<color=" + GRIS + ">[" + numero + "]</color>  " +
            "<color=" + color + "><b>" + nombre + "</b></color>\n" +
            "       <size=75%>" + estado + "</size>";
    }

    string CrearBarraProgreso()
    {
        int total = 16;

        int completados = Mathf.RoundToInt(
            (float)estudiantesRescatados /
            Mathf.Max(1, estudiantesNecesarios) * total
        );

        completados = Mathf.Clamp(completados, 0, total);

        string barra = "";

        for (int i = 0; i < total; i++)
        {
            barra += i < completados
                ? "<color=" + VERDE + ">■</color>"
                : "<color=#46505E>■</color>";
        }

        return barra;
    }
}