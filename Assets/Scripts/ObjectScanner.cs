using CustomDetectableObjects;
using UnityEngine;

public class ObjectScanner : MonoBehaviour
{
    private ObjectCollectorAgent m_Agent { get; set; }
    private void Start()
    {
        m_Agent = GetComponentInParent<ObjectCollectorAgent>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("objective"))
        {
            var objective = other.GetComponent<DetectableVisibleObject>();

            if (objective.isDetected == false)
            {
                m_Agent.m_ObjectCollectorArea.m_exploredObjectives.Add(objective.gameObject);
                objective.isDetected = true; 
            }
        }
    }
}
