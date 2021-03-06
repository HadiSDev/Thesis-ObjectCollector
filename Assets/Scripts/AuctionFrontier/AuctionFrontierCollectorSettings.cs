using System;
using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
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
    private int resetCounter;
    private int m_Counter;
    
    public int sampleSize;
    public string fileName;
    public string directory;
    public int maxTimestep;
    public int timestep;
    
    private DateTime m_StartTime;
    private TimeSpan m_ElapsedTime;
    
    // Visualized variables
    [HideInInspector] public int totalScore;

    public Text elapsedTime;
    public Text scoreText;

    StatsRecorder m_Recorder;
    [HideInInspector]
    public float totalCollected;

    public bool MaxStepReached()
    {
        return timestep >= maxTimestep;
    }

    public void Awake()
    {
        agents = GameObject.FindGameObjectsWithTag("agent").AsEnumerable().Where(a => a.layer == 0).ToArray();
        Academy.Instance.OnEnvironmentReset += EnvironmentReset;
        m_Recorder = Academy.Instance.StatsRecorder;
        
        StatisticsWriter.NumAgents = agents.Length;
        StatisticsWriter.FileName = fileName;
        StatisticsWriter.WriteDirectory = directory;
        StatisticsWriter.IsEvaluating = m_Is_evaluating;
        
        // Speed up play time
        Time.timeScale = 100;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;    
    }

    private void EnvironmentReset()
    {
        Debug.Log("Reset environment...");
        GridTracking.GridTrackingReset();
        if (m_Is_evaluating == false || m_Counter < sampleSize)
        {
            // Reset map
            listArea = FindObjectsOfType<AuctionFrontierCollectorArea>();
            foreach (var fa in listArea)
            {
                fa.ResetObjectiveArea(agents);
            }
            AuctionFrontierUtil.FINISHED = false;
            ClearObjects(GameObject.FindGameObjectsWithTag("obstacle"));
        
            m_StartTime = DateTime.Now;
            totalScore = 0;
            totalCollected = 0;
            m_Counter++;
        }
        else if(m_Is_evaluating && m_Counter >= sampleSize) 
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#endif
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
        timestep++;
        // Actively listen if episode is finished
        if (GameObject.FindGameObjectsWithTag("objective").Length == 0 && GridTracking.GridWorldComplete() && AuctionFrontierUtil.FINISHED ||timestep >= maxTimestep)
        {
            EnvironmentReset();
            AuctionFrontierUtil.CLEAR();
            timestep = 0;
        }
        
        m_ElapsedTime = DateTime.Now - m_StartTime;
        scoreText.text = $"Score: {totalScore}";
        elapsedTime.text = m_ElapsedTime.ToString();
        
        foreach (var area in listArea)
        {
            foreach (var agent in agents)
            {
                area.UpdateGridWorld(agent);
                //GridTracking.SetValueFromWorldPos(agent.transform.position, 1);
                //var pos = GridTracking.GetCellFromWorldCoord(agent.transform.position);
            }
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
    
