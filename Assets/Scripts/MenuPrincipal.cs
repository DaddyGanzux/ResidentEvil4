using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPrincipal : MonoBehaviour
{
    // Carga la escena indicada por su nombre en el Inspector o evento
    public void CargarEscena(string SampleScene)
    {
        SceneManager.LoadScene(SampleScene);
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