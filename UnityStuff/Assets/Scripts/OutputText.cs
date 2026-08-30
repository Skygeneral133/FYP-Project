using System;using TMPro;
using UnityEngine;

public class OutputText : MonoBehaviour
{
    public BluetoothInterfacer BluetoothInterface;
    public float numToDisplay;
    public bool isDeviceConnected;
    public TextMeshProUGUI text;
    public TextMeshProUGUI connectionText;
    public ThingToOutput whatsToOutput;

    void Start()
    {
        BluetoothInterface = BluetoothInterfacer.Instance;
    }

    void Update()
    {
        switch (whatsToOutput)
        {
            case ThingToOutput.Weight:
                numToDisplay = BluetoothInterface.CurrentWeight;
                isDeviceConnected = BluetoothInterface.isLoadCellConnected;
                break;
            case ThingToOutput.PouringRate:
                numToDisplay = BluetoothInterface.CurrentPourRate;
                isDeviceConnected = BluetoothInterface.isLoadCellConnected;
                break;
            case ThingToOutput.YPouringAngle:
                numToDisplay = BluetoothInterface.CurrentPourAngle;
                isDeviceConnected = BluetoothInterface.isIMUConnected;
                break;
            case ThingToOutput.YAngularVelocity:
                numToDisplay = BluetoothInterface.CurrentVelocity;
                isDeviceConnected = BluetoothInterface.isIMUConnected;
                break;
        }

        text.text = numToDisplay.ToString("F2");
        connectionText.text = isDeviceConnected.ToString();
    }
}

public enum ThingToOutput
{
    Weight,
    PouringRate,
    YPouringAngle,
    YAngularVelocity
}
