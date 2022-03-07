using UnityEngine;
using UnityEngine.UI;
using Unity.MLAgents;

public class ObjectCollectorSettings : MonoBehaviour
{
    [HideInInspector]
    public GameObject[] agents;
    [HideInInspector]
    public ObjectCollectorArea[] listArea;

    public int totalScore;
    public Text scoreText;

    StatsRecorder m_Recorder;

    public void Awake()
    {
        Academy.Instance.OnEnvironmentReset += EnvironmentReset;
        m_Recorder = Academy.Instance.StatsRecorder;
    }

    public void EnvironmentReset()
    {
        ClearObjects(GameObject.FindGameObjectsWithTag("objective"));
        ClearObjects(GameObject.FindGameObjectsWithTag("obstacle"));

        agents = GameObject.FindGameObjectsWithTag("agent");
        listArea = FindObjectsOfType<ObjectCollectorArea>();
        foreach (var fa in listArea)
        {
            fa.ResetObjectiveArea(agents);
        }

        totalScore = 0;
    }

    void ClearObjects(GameObject[] objects)
    {
        foreach (var objective in objects)
        {
            Destroy(objective);
        }
    }

    public void Update()
    {
        scoreText.text = $"Score: {totalScore}";

        // Send stats via SideChannel so that they'll appear in TensorBoard.
        // These values get averaged every summary_frequency steps, so we don't
        // need to send every Update() call.
        if ((Time.frameCount % 100) == 0)
        {
            m_Recorder.Add("TotalScore", totalScore);
        }
    }
}
