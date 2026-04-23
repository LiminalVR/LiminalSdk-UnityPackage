using UnityEngine;

[System.Serializable]
public class LimappPhysics2DSettings : LimappSettings
{
    public float MaxRotationSpeed;
    public float MaxTranslationSpeed;
    public float MaxAngularCorrection;
    public float MaxLinearCorrection;
    public float VelocityThreshold;

    public bool CallbacksOnDisable;
    public float DefaultContactOffset;
    public bool ChangeStopsCallbacks;
    public bool QueriesStartInColliders;
    public bool QueriesHitTriggers;
    public Vector2 Gravity;
    public int PositionIterations;
    public bool AutoSyncTransforms;
    public float BaumgarteScale;
    public float TimeToSleep;
    public int VelocityIterations;
    public float LinearSleepTolerance;
    public float AngularSleepTolerance;
    public bool AlwaysShowColliders;
    public bool ShowColliderSleep;
    public bool ShowColliderContacts;
    public bool ShowColliderAABB;
    public float ContactArrowScale;
    public Color ColliderAwakeColor;
    public Color ColliderAsleepColor;
    public Color ColliderContactColor;
    public Color ColliderAABBColor;
    public SimulationMode2D SimulationMode;

    public override void SaveSettings()
    {
        return;
        MaxRotationSpeed = Physics2D.maxRotationSpeed;
        MaxTranslationSpeed = Physics2D.maxTranslationSpeed;
        MaxAngularCorrection = Physics2D.maxAngularCorrection;
        MaxLinearCorrection = Physics2D.maxLinearCorrection;
        VelocityThreshold = Physics2D.velocityThreshold;
        CallbacksOnDisable = Physics2D.callbacksOnDisable;
        DefaultContactOffset = Physics2D.defaultContactOffset;
        QueriesStartInColliders = Physics2D.queriesStartInColliders;
        QueriesHitTriggers = Physics2D.queriesHitTriggers;
        Gravity = Physics2D.gravity;
        PositionIterations = Physics2D.positionIterations;
        AutoSyncTransforms = Physics2D.autoSyncTransforms;
        BaumgarteScale = Physics2D.baumgarteScale;
        TimeToSleep = Physics2D.timeToSleep;
        VelocityIterations = Physics2D.velocityIterations;
        LinearSleepTolerance = Physics2D.linearSleepTolerance;
        AngularSleepTolerance = Physics2D.angularSleepTolerance;
        SimulationMode = Physics2D.simulationMode;
    }

    public override void ApplySettings()
    {
        return;
        Physics2D.maxRotationSpeed = MaxRotationSpeed;
        Physics2D.maxTranslationSpeed = MaxTranslationSpeed;
        Physics2D.maxAngularCorrection = MaxAngularCorrection;
        Physics2D.maxLinearCorrection = MaxLinearCorrection;
        Physics2D.velocityThreshold = VelocityThreshold;
        Physics2D.callbacksOnDisable = CallbacksOnDisable;
        Physics2D.defaultContactOffset = DefaultContactOffset;
        Physics2D.queriesStartInColliders = QueriesStartInColliders;
        Physics2D.queriesHitTriggers = QueriesHitTriggers;
        Physics2D.gravity = Gravity;
        Physics2D.positionIterations = PositionIterations;
        Physics2D.autoSyncTransforms = AutoSyncTransforms;
        Physics2D.baumgarteScale = BaumgarteScale;
        Physics2D.timeToSleep = TimeToSleep;
        Physics2D.velocityIterations = VelocityIterations;
        Physics2D.linearSleepTolerance = LinearSleepTolerance;
        Physics2D.angularSleepTolerance = AngularSleepTolerance;
        Physics2D.simulationMode = SimulationMode;
    }
}