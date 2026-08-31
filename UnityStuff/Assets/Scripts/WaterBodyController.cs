using UnityEngine;

[ExecuteAlways]
public class WaterBodyController : MonoBehaviour
{
    [Header("Water Object")]
    [SerializeField] private Transform waterBody;

    [Header("Volume")]
    [Range(0f, 100f)]
    [SerializeField] private float currentVolumeMl = 0f;

    [SerializeField, Min(0.01f)]
    private float maximumVolumeMl = 100f;

    [Header("Beaker Measurements")]
    [SerializeField] private float bottomY = 0f;

    [SerializeField, Min(0.001f)]
    private float fullWaterHeight = 1f;

    public float CurrentVolumeMl => currentVolumeMl;
    public float MaximumVolumeMl => maximumVolumeMl;
    public float FillAmount => maximumVolumeMl > 0f
        ? Mathf.Clamp01(currentVolumeMl / maximumVolumeMl)
        : 0f;

    private void Awake()
    {
        UpdateWater();
    }

    private void OnValidate()
    {
        UpdateWater();
    }

    /// <summary>
    /// Receives the volume calculated from the Bluetooth load-cell reading by
    /// SplashController and updates the visible water body.
    /// </summary>
    public void SetVolume(float volumeMl)
    {
        currentVolumeMl = Mathf.Clamp(
            volumeMl,
            0f,
            maximumVolumeMl
        );

        UpdateWater();
    }

    private void UpdateWater()
    {
        if (waterBody == null)
        {
            return;
        }

        float fillAmount = Mathf.Clamp01(
            currentVolumeMl / maximumVolumeMl
        );

        waterBody.gameObject.SetActive(fillAmount > 0f);

        if (fillAmount <= 0f)
        {
            return;
        }

        float waterHeight = fullWaterHeight * fillAmount;

        Vector3 scale = waterBody.localScale;
        scale.y = waterHeight / 2f;
        waterBody.localScale = scale;

        Vector3 position = waterBody.localPosition;
        position.y = bottomY + (waterHeight / 2f);
        waterBody.localPosition = position;
    }
}