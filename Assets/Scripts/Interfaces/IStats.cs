namespace Interfaces
{
    public interface IStats
    {
        void ResetStats();
        float GetAgentCumulativeDistance();
        float GetAgentCumulativeReward();
        int GetAgentStepCount();
    }
}