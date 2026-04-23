using UnityEngine;

[System.Serializable]
public class LimappPhysicsSettings : LimappSettings
{
    public float DefaultContactOffset;
    public float BounceThreshold;
    public int DefaultSolverVelocityIterations;
    public Vector3 Gravity;
    public float SleepThreshold;
    public bool QueriesHitTriggers;
    public bool QueriesHitBackfaces;
    public int DefaultSolverIterations;
    public bool AutoSimulation;
    public bool AutoSyncTransforms;

    public override void SaveSettings()
    {
        DefaultContactOffset = Physics.defaultContactOffset;
        BounceThreshold = Physics.bounceThreshold;
        DefaultSolverVelocityIterations = Physics.defaultSolverVelocityIterations;
        Gravity = Physics.gravity;
        SleepThreshold = Physics.sleepThreshold;
        QueriesHitTriggers = Physics.queriesHitTriggers;
        QueriesHitBackfaces = Physics.queriesHitBackfaces;
        DefaultSolverIterations = Physics.defaultSolverIterations;
        AutoSimulation = Physics.autoSimulation;
        AutoSyncTransforms = Physics.autoSyncTransforms;
    }

    public override void ApplySettings()
    {
        Physics.defaultContactOffset = DefaultContactOffset;
        Physics.bounceThreshold = BounceThreshold;
        Physics.defaultSolverVelocityIterations = DefaultSolverVelocityIterations;
        Physics.gravity = Gravity;
        Physics.sleepThreshold = SleepThreshold;
        Physics.queriesHitTriggers = QueriesHitTriggers;
        Physics.queriesHitBackfaces = QueriesHitBackfaces;
        Physics.defaultSolverIterations = DefaultSolverIterations;
        Physics.autoSimulation = AutoSimulation;
        Physics.autoSyncTransforms = AutoSyncTransforms;
    }
}