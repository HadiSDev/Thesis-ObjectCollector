using MBaske.Sensors.Grid;

namespace CustomDetectableObjects
{
    public class DetectableVisibleObject : DetectableGameObject
    {
        public bool isTargeted;
        float IsTargeted() => isTargeted ? 1 : 0;
        
        public bool isDetected;
        float isDetectedObservable() => isDetected ? 1 : 0;
        
        public bool isNotDetected;
        float isNotDetectedObservable() => isNotDetected ? 1 : 0;

        public override void AddObservables()
        {
            Observables.Add("Detected", isDetectedObservable);
            Observables.Add("NotDetected", isNotDetectedObservable);
        }
    }
}