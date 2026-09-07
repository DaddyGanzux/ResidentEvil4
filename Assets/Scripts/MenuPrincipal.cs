using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPrincipal : MonoBehaviour
{

// Opción B: Exponerla al Inspector para modificarla en Unity
    [SerializeField] private string escenaACargar = "Play";

    public void CargarEscena()
    {
        SceneManager.LoadScene(escenaACargar);
    }

    // Cierra la aplicación
    public void SalirDelJuego()
    {
        Debug.Log("Saliendo del juego...");

        Application.Quit();

        // Permite probar el botón Salir desde el propio Editor de Unity
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}