using DefaultNamespace;
using UnityEngine;

public class BeakerButtonController : MonoBehaviour
{
    private WaterController FindBeakerController()
    {
        WaterController controller =
            FindFirstObjectByType<WaterController>();

        if (controller == null)
        {
            Debug.LogWarning(
                "No tracked beaker has been detected yet."
            );
        }

        return controller;
    }

    public void SelectNormalBeaker()
    {
        FindBeakerController()?.SelectNormalWater();
    }

    public void SelectBoilingBeaker()
    {
        FindBeakerController()?.SelectBoilingWater();
    }

    public void SelectAcidBeaker()
    {
        FindBeakerController()?.SelectAcid();
    }
}