using UnityEngine;

public class SplashController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WaterBodyController waterController;
    [Tooltip("Small, contained splash used below the rim.")]
    [SerializeField] private ParticleSystem splashParticles;
    [Tooltip("Large splash allowed to leave the beaker near full capacity.")]
    [SerializeField] private ParticleSystem overflowSplashParticles;

    [SerializeField] private Transform splashOrigin;
    [SerializeField] private Transform overflowSplashOrigin;
    [SerializeField] private Transform waterBody;
    [SerializeField] private BluetoothInterfacer bluetoothInterface;

    [Header("Load Cell Conversion")]
    [Tooltip("The load-cell reading when the empty beaker is on the scale.")]
    [SerializeField] private float tareWeightGrams = 0f;

    [Tooltip("Water is approximately 1 gram per millilitre.")]
    [SerializeField, Min(0.001f)] private float liquidDensityGramsPerMl = 1f;

    [Tooltip("Use the pouring-rate value sent by the load-cell Arduino for splash detection.")]
    [SerializeField] private bool useArduinoPourRate = true;

    [Header("Small Contained Splash")]
    [SerializeField, Min(0f)] private float minimumSmallIncreaseMl = 0.5f;
    [SerializeField, Min(0f)] private float smallSplashThresholdMlPerSecond = 3f;
    [SerializeField, Min(0f)] private float smallSplashCooldown = 0.15f;
    [SerializeField, Min(0.01f)] private float smallSplashFullIntensityIncreaseMl = 5f;
    [SerializeField, Min(1)] private int minimumSmallParticleCount = 3;
    [SerializeField, Min(1)] private int maximumSmallParticleCount = 15;

    [Header("Large Overflow Splash")]
    [SerializeField, Range(0f, 1f)] private float overflowFillThreshold = 0.9f;
    [SerializeField, Min(0f)] private float minimumSuddenIncreaseMl = 10f;
    [SerializeField, Min(0f)] private float splashThresholdMlPerSecond = 40f;
    [SerializeField, Min(0f)] private float splashResetRateMlPerSecond = 10f;
    [SerializeField, Min(0f)] private float splashCooldown = 0.5f;
    [SerializeField, Min(1)] private int overflowParticleCount = 50;

    private bool splashArmed = true;
    
    [Header("Testing")]
    [SerializeField] private bool useTestWeight = false;

    [Range(0f, 100f)]
    [SerializeField] private float testWeightGrams = 0f;

    private float previousVolume;
    private float previousTime;
    private float lastSmallSplashTime = -10f;
    private float lastSplashTime = -10f;
    private bool hasPreviousReading;

    private void Start()
    {
        ResolveBluetoothInterface();
    }

    private void OnEnable()
    {
        previousVolume = 0f;
        previousTime = Time.time;
        lastSmallSplashTime = -10f;
        lastSplashTime = -10f;
        hasPreviousReading = false;
        splashArmed = true;
    }

    private void Update()
    {
        if (useTestWeight)
        {
            ProcessLoadCellReading(testWeightGrams, float.NaN);
            return;
        }

        ResolveBluetoothInterface();

        if (bluetoothInterface != null && bluetoothInterface.isLoadCellConnected)
        {
            ProcessLoadCellReading(
                bluetoothInterface.CurrentWeight,
                bluetoothInterface.CurrentPourRate
            );
        }
    }

    private void ResolveBluetoothInterface()
    {
        if (bluetoothInterface == null)
        {
            bluetoothInterface = BluetoothInterfacer.Instance;
        }
    }
    
    [ContextMenu("Test Small Splash Effect")]
    private void TestSmallSplashEffect()
    {
        UpdateSplashPosition();
        TriggerSmallSplash(minimumSmallIncreaseMl);
    }

    [ContextMenu("Test Overflow Splash Effect")]
    private void TestOverflowSplashEffect()
    {
        UpdateSplashPosition();
        TriggerOverflowSplash();
    }

    public void ProcessLoadCellReading(float weightGrams)
    {
        ProcessLoadCellReading(weightGrams, float.NaN);
    }

    public void ProcessLoadCellReading(
        float weightGrams,
        float reportedPourRateGramsPerSecond
    )
    {
        if (waterController == null)
        {
            Debug.LogWarning("waterController is null");
            return;
        }
            
        float netWeightGrams = Mathf.Max(0f, weightGrams - tareWeightGrams);
        float volumeMl = netWeightGrams / liquidDensityGramsPerMl;

        // The water level always updates.
        waterController.SetVolume(volumeMl);
        UpdateSplashPosition();

        float currentTime = Time.time;

        // The first reading only establishes the starting value.
        if (!hasPreviousReading)
        {
            previousVolume = volumeMl;
            previousTime = currentTime;
            hasPreviousReading = true;
            return;
        }

        float elapsedTime = currentTime - previousTime;

        if (elapsedTime <= 0f)
        {
            return;
        }

        float volumeIncrease = volumeMl - previousVolume;
        float calculatedIncreaseRate = volumeIncrease / elapsedTime;

        bool hasArduinoPourRate =
            useArduinoPourRate &&
            !float.IsNaN(reportedPourRateGramsPerSecond) &&
            !float.IsInfinity(reportedPourRateGramsPerSecond);

        float increaseRate = hasArduinoPourRate
            ? Mathf.Max(0f, reportedPourRateGramsPerSecond) /
              liquidDensityGramsPerMl
            : calculatedIncreaseRate;
        float fillAmount = waterController.FillAmount;

        // Allow another splash after the increase becomes stable again.
        if (increaseRate < splashResetRateMlPerSecond)
        {
            splashArmed = true;
        }

        bool smallIncreaseDetected =
            volumeIncrease >= minimumSmallIncreaseMl &&
            increaseRate >= smallSplashThresholdMlPerSecond;

        bool smallCooldownFinished =
            currentTime - lastSmallSplashTime >= smallSplashCooldown;

        bool largeIncreaseDetected =
            volumeIncrease >= minimumSuddenIncreaseMl;

        bool largeIncreaseFastEnough =
            increaseRate >= splashThresholdMlPerSecond;

        bool overflowCooldownFinished =
            currentTime - lastSplashTime >= splashCooldown;

        bool containsWater = volumeMl > 0f;
        bool nearTop = fillAmount >= overflowFillThreshold;

        bool shouldOverflow =
            splashArmed &&
            largeIncreaseDetected &&
            largeIncreaseFastEnough &&
            overflowCooldownFinished &&
            nearTop &&
            containsWater;

        if (shouldOverflow)
        {
            TriggerOverflowSplash();
            splashArmed = false;
        }
        else if (smallIncreaseDetected &&
            smallCooldownFinished &&
            containsWater)
        {
            TriggerSmallSplash(volumeIncrease);
        }

        previousVolume = volumeMl;
        previousTime = currentTime;
    }

    private void UpdateSplashPosition()
    {
        if (splashOrigin == null || waterBody == null)
        {
            return;
        }

        Vector3 position = splashOrigin.localPosition;

        // A Unity cylinder's top is one local Y scale above its centre.
        position.y =
            waterBody.localPosition.y + waterBody.localScale.y;

        splashOrigin.localPosition = position;

        if (overflowSplashOrigin != null)
        {
            Vector3 overflowPosition = overflowSplashOrigin.localPosition;
            overflowPosition.y = position.y;
            overflowSplashOrigin.localPosition = overflowPosition;
        }
    }

    private void TriggerSmallSplash(float volumeIncreaseMl)
    {
        if (splashParticles == null)
        {
            return;
        }

        float intensity = Mathf.InverseLerp(
            minimumSmallIncreaseMl,
            smallSplashFullIntensityIncreaseMl,
            volumeIncreaseMl
        );

        int particleCount = Mathf.RoundToInt(
            Mathf.Lerp(
                minimumSmallParticleCount,
                maximumSmallParticleCount,
                intensity
            )
        );

        splashParticles.Emit(particleCount);
        lastSmallSplashTime = Time.time;
    }

    private void TriggerOverflowSplash()
    {
        if (overflowSplashParticles == null)
        {
            Debug.LogWarning("Overflow Splash Particles is not assigned.");
            return;
        }

        overflowSplashParticles.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );

        overflowSplashParticles.Emit(overflowParticleCount);
        lastSplashTime = Time.time;
    }
}
