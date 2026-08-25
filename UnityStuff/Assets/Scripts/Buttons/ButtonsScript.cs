using System;
using System.Collections;
using DefaultNamespace;
using UnityEngine;

public class ButtonsScript : MonoBehaviour
{
    public GameObject BoilingWaterPrefab;
    public GameObject WaterPrefab;
    public GameObject HydrochloricAcidPrefab;
    private WaterController WaterControllerInstance;
    private GameObject BeakerPrefab;
    private bool foundBeaker = false;

    private void Update()
    {
        if (foundBeaker) return;

        BeakerPrefab = GameObject.Find("100mlbeakerprefab1");

        if (BeakerPrefab != null)
        {
            WaterControllerInstance = BeakerPrefab.GetComponent<WaterController>();
            foundBeaker = true;
            Debug.Log("Beaker found and WaterController cached.");
        }
    }

    public void BoilingWaterPress()
    {
        WaterControllerInstance.SpawnLiquid(BoilingWaterPrefab);
    }
    
    public void WaterPress()
    {
        WaterControllerInstance.SpawnLiquid(WaterPrefab);
    }

    public void HydrochloricAcidPress()
    {
        WaterControllerInstance.SpawnLiquid(HydrochloricAcidPrefab);
    }
}
