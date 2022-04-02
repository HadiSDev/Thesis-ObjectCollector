using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Interfaces;
using UnityEngine;
using UnityEngine.UI;
using Unity.MLAgents;
using CsvHelper;
using Statistics;

public class ObjectCollectorSettings : MonoBehaviour
{
    private Agent[] agents;
    [HideInInspector] 
    public ObjectCollectorArea[] listArea;
    
    public bool m_Is_evaluating;
    // Statistics
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
    private bool firstReset = true;
    [HideInInspector]
    public float totalCollected;
    
    [HideInInspector]
    public SimpleMultiAgentGroup m_AgentGroup;
    
    [Header("Max Environment Steps")] public int MaxEnvironmentSteps = 6000;
    private int m_ResetTimer;
    
    private void Start()
    {
        m_AgentGroup = new SimpleMultiAgentGroup();
        foreach (var item in agents)
        {
            // Add to team manager
            m_AgentGroup.RegisterAgent(item);
        }
    }

    void FixedUpdate()
    {
        m_ResetTimer += 1;
        if (m_ResetTimer >= MaxEnvironmentSteps && MaxEnvironmentSteps > 0)
        {
            m_AgentGroup.GroupEpisodeInterrupted();
            EnvironmentReset();
        }
    }

    public void Awake()
    {
        agents = FindObjectsOfType<Agent>();
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
        //Reset counter
        m_ResetTimer = 0;
        if (resetCounter % agents.Length == 0)
        {
            if (m_Is_evaluating == false || m_Counter < sampleSize)
            {
                // Reset map
                listArea = FindObjectsOfType<ObjectCollectorArea>();
                foreach (var fa in listArea)
                {
                    fa.ResetObjectiveArea(agents);
                    
                    foreach (var agent in FindObjectsOfType<Agent>())
                    {
                        m_AgentGroup.RegisterAgent(agent);
                    }
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

        // Send stats via SideChannel so that they'll appear in TensorBoard.
        // These values get averaged every summary_frequency steps, so we don't
        // need to send every Update() call.
        if ((Time.frameCount % 100) == 0)
        {
            m_Recorder.Add("TotalScore", totalScore);
        }
    }
    
}
    
