
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Game Over")]
    public GameObject gameOverPanel;

    private bool juegoTerminado = false;

    void Start()
    {
        gameOverPanel.SetActive(false);

        Time.timeScale = 1f;
    }

    public void GameOver()
    {
        if (juegoTerminado)
            return;

        juegoTerminado = true;

        gameOverPanel.SetActive(true);

        Time.timeScale = 0f;

        Debug.Log("GAME OVER");
    }

    public void ReiniciarJuego()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().name
        );
    }
}

