using UnityEngine;

public class TriggerMiedoSpawner : MonoBehaviour
{
    [Header("Prefabs a Instanciar")]
    [SerializeField] private GameObject objeto1;
    [SerializeField] private GameObject objeto2;

    

    private void OnTriggerEnter(Collider other)
    {
        // Usar CompareTag es lo más optimizado en Unity
        if (other.CompareTag("Miedo"))
        {
            objeto1.SetActive(true);
            objeto2.SetActive(true);



        }
    }

   
}