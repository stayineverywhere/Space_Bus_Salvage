using UnityEngine;

public class Survivor : MonoBehaviour
{
    public bool isRescued = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!isRescued && other.CompareTag("Player"))
        {
            Rescued();
        }
    }

    private void Rescued()
    {
        isRescued = true;
        Debug.Log("A survivor has been rescued!");
        if (ContractManager.Instance != null)
        {
            ContractManager.Instance.RescueSurvivor();
        }
        gameObject.SetActive(false); // Remove from scene
    }
}
