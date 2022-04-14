using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Unity.MLAgents;
using Statistics;

public class AuctionFrontierCollectorSettings : MonoBehaviour
{
    public GameObject[] agents;
    [HideInInspector] public AuctionFrontierCollectorArea[] listArea;
    
    // Statistics
    public bool m_Is_evaluating;
    private List<Stats> m_Records = new List<Stats>();
    private int resetCounter;
    private int m_Counter = 1;
    public int sampleSize;
    public string fileName;
    public string directory;
    
    private DateTime m_StartTime;
    private TimeSpan m_ElapsedTime;
    
    // Visualized variables
    [HideInInspector] public int totalScore;

    public Text elapsedTime;
    public Text scoreText;

    StatsRecorder m_Recorder;
    [HideInInspector]
    public float totalCollected;

    public void Awake()
    {
        agents = GameObject.FindGameObjectsWithTag("agent").AsEnumerable().Where(a => a.layer == 0).ToArray();
        Academy.Instance.OnEnvironmentReset += EnvironmentReset;
        m_Recorder = Academy.Instance.StatsRecorder;
        
        StatisticsWriter.NumAgents = agents.Length;
        StatisticsWriter.FileName = fileName;
        StatisticsWriter.WriteDirectory = directory;
        StatisticsWriter.IsEvaluating = m_Is_evaluating;
    }

    public void EnvironmentReset()
    {
        resetCounter++;
        if (resetCounter % agents.Length == 0)
        {
            if (m_Is_evaluating == false || m_Counter < sampleSize)
            {
                // Reset map
                listArea = FindObjectsOfType<AuctionFrontierCollectorArea>();
                foreach (var fa in listArea)
                {
                    fa.ResetObjectiveArea(agents);
                }
                ClearObjects(GameObject.FindGameObjectsWithTag("obstacle"));
            
                m_StartTime = DateTime.Now;
                totalScore = 0;
                totalCollected = 0;
                m_Counter++;
            }
            else if(m_Is_evaluating && m_Counter == sampleSize) // To avoid multiple writes when all agents call method EnvironmentReset (GreedyAgent)
            {
                m_Counter++;
                StatisticsWriter.plotResults();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.ExitPlaymode();
#endif
            }
        }
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
        m_ElapsedTime = DateTime.Now - m_StartTime;
        scoreText.text = $"Score: {totalScore}";
        elapsedTime.text = m_ElapsedTime.Minutes.ToString();

        var coords = agents.Select(a => (a.transform.position + new Vector3(50, 0, 50), a.transform.rotation));
        

        foreach (var area in listArea)
        {

            foreach (var agent in agents)
            {
                area.UpdateGridWorld(agent);
            }
            
            
            /*
            foreach (var pos in coords)
            {
                var curr_val = area.GetGridWorldValue(pos.Item1);
                
                //TODO: Decide if it should keep increment or not
                area.SetGridWorldValue(pos.Item1, ++curr_val);
            }
            */
        }
        
        // Send stats via SideChannel so that they'll appear in TensorBoard.
        // These values get averaged every summary_frequency steps, so we don't
        // need to send every Update() call.
        if ((Time.frameCount % 100) == 0)
        {
            m_Recorder.Add("TotalScore", totalScore);
        }
    }
    
    
}
    
