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

public class ObjectCollectorSettings : MonoBehaviour
{
    public GameObject[] agents;
    [HideInInspector] public ObjectCollectorArea[] listArea;
    
    public bool m_Is_evaluating;
    // Statistics
    private List<Stats> m_Records = new List<Stats>();
    private int resetCounter;
    private string BASE_DIRECTORY = "./Assets/Scripts/Statistics/";
    private int m_Counter = -1;
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
    
    public void Awake()
    {
        agents = GameObject.FindGameObjectsWithTag("agent").AsEnumerable().Where(a => a.layer == 0).ToArray();
        Academy.Instance.OnEnvironmentReset += EnvironmentReset;
        m_Recorder = Academy.Instance.StatsRecorder;
    }

    public void EnvironmentReset()
    {
        resetCounter++;
        if (resetCounter % agents.Length == 0)
        {
            if (m_Is_evaluating == false || m_Counter < sampleSize)
            {
                if (!firstReset && m_Is_evaluating)
                {
                    AppendStatToRecordList();
                }
                else
                {
                    firstReset = false;
                }
            
                // Reset map
                listArea = FindObjectsOfType<ObjectCollectorArea>();
                foreach (var fa in listArea)
                {
                    fa.ResetObjectiveArea(agents);
                }
                ClearObjects(GameObject.FindGameObjectsWithTag("obstacle"));
            
                m_StartTime = DateTime.Now;
                totalScore = 0;
                m_Counter++;
            }
            else if(m_Is_evaluating && m_Counter == sampleSize) // To avoid multiple writes when all agents call method EnvironmentReset (GreedyAgent)
            {
                Directory.CreateDirectory(BASE_DIRECTORY + directory);
                AppendStatToRecordList();
                using (var writer = new StreamWriter($"./Assets/Scripts/Statistics/{directory}/{fileName}.csv"))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(m_Records);
                    csv.Flush();
                    csv.Dispose();
                }
                m_Counter++;
#if UNITY_EDITOR
                UnityEditor.EditorApplication.ExitPlaymode();
#endif
            }
        }
    }
    
    private void AppendStatToRecordList()
    {
        var record = new Stats(m_Counter, m_ElapsedTime.Seconds.ToString());
        record.ComputeAgentSpecificStats(agents);
        m_Records.Add(record);
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

    public class Stats
    {
        public int Id { get; set; }
        public int NumOfAgents { get; set; }
        
        public string CompletionTime { get; set; }
        public float MinReward { get; set; } 
        public float AvgReward { get; set; }
        public float MaxReward { get; set; }
        
        public float MinAgentStep { get; set; }
        public float AvgAgentStep { get; set; }
        public float MaxAgentStep { get; set; }

        public float MinDistTravelled { get; set; }
        public float AvgDistTravelled { get; set; }
        public float MaxDistTravelled { get; set; }

        public Stats(int id, string completionTime)
        {
            Id = id;
            CompletionTime = completionTime;
        }

        public void ComputeAgentSpecificStats(GameObject[] agents)
        {
            var distances = agents.Select(a => a.GetComponent<IStats>().GetAgentCumulativeDistance());
            var rewards = agents.Select(a => a.GetComponent<IStats>().GetAgentCumulativeReward());
            var stepCounts = agents.Select(a => a.GetComponent<IStats>().GetAgentStepCount());

            NumOfAgents = agents.Length;
            MinDistTravelled = distances.Min();
            AvgDistTravelled = distances.Average();
            MaxDistTravelled = distances.Max();
            
            MinReward = rewards.Min();
            AvgReward = rewards.Average();
            MaxReward = rewards.Max();
            
            MinAgentStep = stepCounts.Min();
            AvgAgentStep = (int) stepCounts.Average();
            MaxAgentStep = stepCounts.Max();
        }
        
        public double StandardDeviation(IEnumerable<double> sequence)
        {
            double result = 0;
            if (sequence.Any())
            {
                double average = sequence.Average();
                double sum = sequence.Sum(d => Math.Pow(d - average, 2));
                result = Math.Sqrt((sum) / sequence.Count());
            }
            
            return result;
        }
    }
}
    
