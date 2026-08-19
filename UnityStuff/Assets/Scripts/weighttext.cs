using TMPro;
using UnityEngine;

public class weighttext : MonoBehaviour
{
    public BluetoothInterfacer BluetoothInterface;
    public float weight;
    public bool isConnected;
    public TextMeshProUGUI text;
    public TextMeshProUGUI conText;
        
    void Start()
    {
        BluetoothInterface = BluetoothInterfacer.Instance;
    }

    // Update is called once per frame
    void Update()
    {
        weight = BluetoothInterface.CurrentWeight;
        isConnected = BluetoothInterface._isConnected;
        text.text = weight.ToString("F2");
        conText.text = isConnected.ToString();
    }
}
