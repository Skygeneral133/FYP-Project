using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class BluetoothInterfacer : MonoBehaviour
{
    public static BluetoothInterfacer Instance { get; private set; }

    // Generic config for one BLE peripheral with up to two characteristics we care about.
    [Serializable]
    public class BleDeviceConfig
    {
        public string deviceName;
        public string serviceUUID;
        public string characteristicAUUID;
        public string characteristicBUUID;

        [NonSerialized] public string address;
        [NonSerialized] public bool addressFound;
        [NonSerialized] public bool isConnected;
        [NonSerialized] public bool subscribedA;
        [NonSerialized] public bool subscribedB;
    }

    // ---- Load Cell ----
    // NOTE: fill in your real characteristic UUIDs below. In the original script
    // weightCharacteristicUUID and ratePourCharacteristicUUID were identical, which
    // means they could never be told apart on the wire.
    private BleDeviceConfig loadCell = new BleDeviceConfig
    {
        deviceName = "LoadCellArduino",
        serviceUUID = "19b10000-e8f2-537e-4f6c-d104768a1214",
        characteristicAUUID = "19b10001-e8f2-537e-4f6c-d104768a1214", // weight
        characteristicBUUID = "19b10002-e8f2-537e-4f6c-d104768a1214", // pour rate  <-- CHANGE ME
    };

    // ---- IMU ----
    // NOTE: this must be the IMU's *actual* advertised BLE name.
    private BleDeviceConfig imu = new BleDeviceConfig
    {
        deviceName = "IMUArduino", // <-- CHANGE ME to the real advertised name
        serviceUUID = "19b20000-e8f2-537e-4f6c-d104768a1214", // <-- CHANGE ME
        characteristicAUUID = "19b20001-e8f2-537e-4f6c-d104768a1214", // pour angle <-- CHANGE ME
        characteristicBUUID = "19b20002-e8f2-537e-4f6c-d104768a1214", // velocity   <-- CHANGE ME
    };

    private List<BleDeviceConfig> devices;
    private Dictionary<string, BleDeviceConfig> devicesByAddress = new Dictionary<string, BleDeviceConfig>();
    private Queue<BleDeviceConfig> connectQueue = new Queue<BleDeviceConfig>();

    private bool _isScanning = false;

    public bool isLoadCellConnected => loadCell.isConnected;
    public bool isIMUConnected => imu.isConnected;

    // Latest values, exposed for other scripts to read.
    public float CurrentWeight { get; private set; }
    public float CurrentPourRate { get; private set; }
    public float CurrentPourAngle { get; private set; }
    public float CurrentVelocity { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate BluetoothInterfacer detected!");
        }
        Instance = this;

        devices = new List<BleDeviceConfig> { loadCell, imu };
    }

    void Start()
    {
        Invoke(nameof(InitializeBLE), 5f);
    }

    // ---------- Step 1: Initialize ----------

    private void InitializeBLE()
    {
        BluetoothLEHardwareInterface.Initialize(true, false, OnInitializeSuccess, OnInitializeError);
    }

    private void OnInitializeSuccess()
    {
        Debug.Log("BLE Initialized Successfully");
        StartScanning();
    }

    private void OnInitializeError(string error)
    {
        Debug.Log("BLE Init Error: " + error);
    }

    // ---------- Step 2: Scan ----------

    private void StartScanning()
    {
        if (_isScanning) return;
        _isScanning = true;

        // Scan ONCE for every service UUID we care about, in a single call.
        string[] serviceUUIDs = devices.Select(d => d.serviceUUID).Distinct().ToArray();
        BluetoothLEHardwareInterface.ScanForPeripheralsWithServices(serviceUUIDs, OnDeviceFound);
    }

    private void OnDeviceFound(string address, string deviceName)
    {
        Debug.Log($"Found Device: {deviceName} Key: {address}");

        if (string.IsNullOrEmpty(deviceName)) return;

        var match = devices.FirstOrDefault(d => !d.addressFound && d.deviceName == deviceName);
        if (match == null) return; // not one of our target devices, or already found

        match.address = address;
        match.addressFound = true;
        devicesByAddress[address] = match;
        connectQueue.Enqueue(match);

        Debug.Log($"Matched '{deviceName}' -> address {address}");

        // Once every device we're looking for has been found, stop scanning and
        // start connecting them one at a time.
        if (devices.All(d => d.addressFound))
        {
            BluetoothLEHardwareInterface.StopScan();
            _isScanning = false;
            ConnectNextInQueue();
        }
    }

    // ---------- Step 3: Connect (one device at a time) ----------
    // Connecting sequentially rather than in parallel avoids race conditions some
    // Android BLE stacks have when two ConnectToPeripheral calls overlap.

    private void ConnectNextInQueue()
    {
        if (connectQueue.Count == 0) return;

        var device = connectQueue.Dequeue();
        Debug.Log($"Connecting to {device.deviceName} @ {device.address}");

        BluetoothLEHardwareInterface.ConnectToPeripheral(
            device.address,
            OnConnected,
            OnServiceDiscovered,
            OnCharacteristicDiscovered,
            OnDisconnected);
    }

    private void OnConnected(string address)
    {
        if (!devicesByAddress.TryGetValue(address, out var device)) return;

        device.isConnected = true;
        Debug.Log($"Connected to: {device.deviceName} ({address})");
    }

    private void OnServiceDiscovered(string address, string service)
    {
        Debug.Log($"Service discovered: {service} on {address}");
    }

    private void OnCharacteristicDiscovered(string address, string service, string characteristic)
    {
        if (!devicesByAddress.TryGetValue(address, out var device)) return;

        Debug.Log($"Characteristic discovered: {characteristic} on {device.deviceName}");

        string c = characteristic.ToLower();

        if (c == device.characteristicAUUID.ToLower())
        {
            BluetoothLEHardwareInterface.SubscribeCharacteristicWithDeviceAddress(
                device.address, device.serviceUUID, device.characteristicAUUID,
                OnSubscribed, OnDataReceived);
        }
        else if (c == device.characteristicBUUID.ToLower())
        {
            BluetoothLEHardwareInterface.SubscribeCharacteristicWithDeviceAddress(
                device.address, device.serviceUUID, device.characteristicBUUID,
                OnSubscribed, OnDataReceived);
        }
    }

    private void OnSubscribed(string address, string characteristic)
    {
        if (!devicesByAddress.TryGetValue(address, out var device)) return;

        string c = characteristic.ToLower();
        if (c == device.characteristicAUUID.ToLower()) device.subscribedA = true;
        else if (c == device.characteristicBUUID.ToLower()) device.subscribedB = true;

        Debug.Log($"Subscribed to {characteristic} on {device.deviceName}");

        // Once both characteristics on this device are subscribed, move on to
        // connecting the next queued device.
        if (device.subscribedA && device.subscribedB)
        {
            ConnectNextInQueue();
        }
    }

    private void OnDisconnected(string address)
    {
        if (devicesByAddress.TryGetValue(address, out var device))
        {
            device.isConnected = false;
            device.subscribedA = false;
            device.subscribedB = false;
            Debug.Log("Disconnected from " + device.deviceName);
        }
    }

    // ---------- Step 4: Parse incoming data ----------

    private void OnDataReceived(string deviceAddress, string characteristicUUID, byte[] bytes)
    {
        if (!devicesByAddress.TryGetValue(deviceAddress, out var device)) return;

        float value = ParseFloat(bytes);
        if (float.IsNaN(value)) return;

        string c = characteristicUUID.ToLower();

        if (device == loadCell)
        {
            if (c == loadCell.characteristicAUUID.ToLower()) CurrentWeight = value;
            else if (c == loadCell.characteristicBUUID.ToLower()) CurrentPourRate = value;
        }
        else if (device == imu)
        {
            if (c == imu.characteristicAUUID.ToLower()) CurrentPourAngle = value;
            else if (c == imu.characteristicBUUID.ToLower()) CurrentVelocity = value;
        }
    }

    private float ParseFloat(byte[] bytes)
    {
        if (bytes == null || bytes.Length < 4)
        {
            Debug.LogWarning("Received malformed data (expected at least 4 bytes).");
            return float.NaN;
        }

        // Assumes the Arduino sends a raw 4-byte float (most common for BLE sensor
        // firmware). If your firmware instead sends an ASCII string like "123.45",
        // comment this block out and uncomment the text-parsing block below.
        try
        {
            byte[] data = bytes;
            if (!BitConverter.IsLittleEndian)
            {
                data = (byte[])bytes.Clone();
                Array.Reverse(data, 0, 4);
            }
            return BitConverter.ToSingle(data, 0);
        }
        catch (Exception e)
        {
            Debug.LogWarning("Failed to parse float bytes: " + e.Message);
            return float.NaN;
        }

        // --- Alternative: text-based parsing ---
        // string s = System.Text.Encoding.UTF8.GetString(bytes);
        // return float.TryParse(s, out float v) ? v : float.NaN;
    }

    // ---------- Cleanup ----------

    void OnDestroy()
    {
        DisconnectAll();
        BluetoothLEHardwareInterface.DeInitialize(OnDeInitialized);
    }

    private void OnDeInitialized()
    {
        Debug.Log("BLE Deinitialized.");
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            Debug.Log("App paused — disconnecting BLE to free peripheral slots.");
            DisconnectAll();
        }
    }

    void OnApplicationQuit()
    {
        DisconnectAll();
        BluetoothLEHardwareInterface.DeInitialize(OnDeInitialized);
    }

    private void DisconnectAll()
    {
        foreach (var device in devices)
        {
            if (device.isConnected)
            {
                BluetoothLEHardwareInterface.DisconnectPeripheral(device.address, OnDisconnected);
            }
        }
    }

    public void DisConnect()
    {
        DisconnectAll();
    }

    public void Connect()
    {
        loadCell.addressFound = false;
        imu.addressFound = false;
        StartScanning();
    }
    
    public void ToggleConnection()
    {
        bool anyConnected = loadCell.isConnected || imu.isConnected;

        if (anyConnected)
        {
            Debug.Log("Toggle: disconnecting...");
            DisconnectAll();
        }
        else
        {
            Debug.Log("Toggle: connecting...");
            Connect();
        }
    }
}