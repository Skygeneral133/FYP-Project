using UnityEngine;
using System;

public class BluetoothInterfacer : MonoBehaviour
{
    public static BluetoothInterfacer Instance { get; private set; }

    void Awake()
    {    
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate BluetoothInterfacer detected!");
        }
        Instance = this;
    }
    
    // Replace with your target device's specific UUIDs if known
    private string targetDeviceName = "LoadCellArduino";
    private string serviceUUID = "19b10000-e8f2-537e-4f6c-d104768a1214";
    private string characteristicUUID = "19b10001-e8f2-537e-4f6c-d104768a1214";

    private string _deviceAddress;
    private bool _isScanning = false;
    public bool _isConnected = false;

    // The latest weight value read from the load cell, exposed for other
    // scripts to read (e.g. GetComponent<BluetoothInterfacer>().CurrentWeight)
    public float CurrentWeight { get; private set; }

    void Start()
    {
        Invoke(nameof(InitializeBLE), 5f);
    }

    private void InitializeBLE()
    {
        BluetoothLEHardwareInterface.Initialize(true, false, OnInitializeSuccess, OnInitializeError);
    }

    // ---------- Step 1: Initialize ----------

    private void OnInitializeSuccess()
    {
        Debug.Log("BLE Initialized Successfully");
        StartScanning();
    }

    private void OnInitializeError(string error)
    {
        Debug.LogError("BLE Init Error: " + error);
    }

    // ---------- Step 2: Scan ----------

    private void StartScanning()
    {
        if (_isScanning) return;
        _isScanning = true;

        // Filtering by serviceUUID (instead of null) makes scanning faster
        // and is required for reliable background scanning on iOS.
        BluetoothLEHardwareInterface.ScanForPeripheralsWithServices(
            new string[] { serviceUUID }, OnDeviceFound);
    }

    private void OnDeviceFound(string address, string name)
    {
        Debug.Log($"Found Device: {name} Key: {address}");

        if (name != null && name.Contains(targetDeviceName))
        {
            _deviceAddress = address;
            BluetoothLEHardwareInterface.StopScan();
            _isScanning = false;
            ConnectToDevice();
        }
    }

    // ---------- Step 3: Connect ----------

    private void ConnectToDevice()
    {
        Debug.Log("Connecting to " + _deviceAddress);
        BluetoothLEHardwareInterface.ConnectToPeripheral(
            _deviceAddress,
            OnConnected,
            OnServiceDiscovered,        // can be null-safe no-op, kept for logging
            OnCharacteristicDiscovered, // can be null-safe no-op, kept for logging
            OnDisconnected);
    }

    private void OnConnected(string address)
    {
        _isConnected = true;
        Debug.Log("Connected to: " + address);
        // Don't subscribe here anymore — wait for discovery confirmation instead
    }

    private void OnServiceDiscovered(string address, string service)
    {
        Debug.Log($"Service discovered: {service}");
    }

    private void OnCharacteristicDiscovered(string address, string service, string characteristic)
    {
        Debug.Log($"Characteristic discovered: {characteristic}");

        if (characteristic.ToLower() == characteristicUUID.ToLower())
        {
            Debug.Log("Target characteristic found — subscribing now.");
            SubscribeToCharacteristic();
        }
    }

    private void OnDisconnected(string address)
    {
        Debug.Log("Disconnected from peripheral");
        _isConnected = false;
    }

    // ---------- Step 4: Subscribe ----------

    private void SubscribeToCharacteristic()
    {
        BluetoothLEHardwareInterface.SubscribeCharacteristicWithDeviceAddress(
            _deviceAddress, serviceUUID, characteristicUUID,
            OnSubscribed, OnDataReceived);
    }

    private void OnSubscribed(string address, string characteristic)
    {
        Debug.Log("Subscribed to characteristic: " + characteristic);
    }

    private void OnDataReceived(string deviceAddress, string characteristicUUID, byte[] bytes)
    {
        ProcessIncomingData(bytes);
    }

    // ---------- Step 5: Parse ----------

    private void ProcessIncomingData(byte[] bytes)
    {
        if (bytes == null || bytes.Length < 4)
        {
            Debug.LogWarning("Received malformed data (expected 4-byte float).");
            return;
        }
        
        Debug.Log($"Raw bytes ({bytes.Length}): {BitConverter.ToString(bytes)}");
        Debug.Log($"BitConverter.IsLittleEndian: {BitConverter.IsLittleEndian}");
        string weightStr = System.Text.Encoding.UTF8.GetString(bytes);
        if (float.TryParse(weightStr, out float parsedWeight))
        {
            CurrentWeight = parsedWeight;
            Debug.Log("Weight received as float in : " + CurrentWeight);
        }
        else
        {
            Debug.LogWarning("Failed to parse weight string: " + weightStr);
        }
        
    }

    // ---------- Cleanup ----------

    void OnDestroy()
    {
        if (_isConnected)
        {
            BluetoothLEHardwareInterface.DisconnectPeripheral(_deviceAddress, OnDisconnected);
        }
        BluetoothLEHardwareInterface.DeInitialize(OnDeInitialized);
    }

    private void OnDeInitialized()
    {
        Debug.Log("BLE Deinitialized.");
    }
    
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && _isConnected)
        {
            Debug.Log("App paused — disconnecting BLE to free the peripheral slot.");
            BluetoothLEHardwareInterface.DisconnectPeripheral(_deviceAddress, OnDisconnected);
        }
    }

    void OnApplicationQuit()
    {
        if (_isConnected)
        {
            BluetoothLEHardwareInterface.DisconnectPeripheral(_deviceAddress, OnDisconnected);
        }
        BluetoothLEHardwareInterface.DeInitialize(OnDeInitialized);
    }
    
    public void disConnect()
    {
        if (_isConnected)
        {
            Debug.Log("DIsconnecting...");
            BluetoothLEHardwareInterface.DisconnectPeripheral(_deviceAddress, OnDisconnected);
            
        }
        BluetoothLEHardwareInterface.DeInitialize(OnDeInitialized);
    }
}