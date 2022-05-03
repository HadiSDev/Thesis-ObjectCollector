using UnityEngine;

public class ObjectCollectorAgentBasket : MonoBehaviour
{
    private void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.CompareTag("objective"))
        {
            transform.parent.parent.GetComponent<ObjectCollectorAgent>().OnTriggerObjective(collider.gameObject.GetComponent<ObjectLogic>());
        }
    }
}