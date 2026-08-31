using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class BluetoothInterfacer : MonoBehaviour
{
    public static BluetoothInterfacer Instance { get; private set; }

    // Generic config for one BLE peripheral.
    // characteristicAUUID = the single NOTIFY characteristic that streams
    //   "value1,value2" as ASCII text (e.g. "45.20,12.50").
    // characteristicBUUID = a WRITE-ONLY command characteristic (e.g. "RESET",
    //   "TARE"). It is never subscribed to and never produces sensor data.
    [Serializable]
    public class BleDeviceConfig
    {
        public string deviceName;
        public string serviceUUID;
        public string characteristicAUUID; // notify: "v1,v2"
        public string characteristicBUUID; // write-only: command

        [NonSerialized] public string address;
        [NonSerialized] public bool addressFound;
        [NonSerialized] public bool isConnected;
        [NonSerialized] public bool subscribedA;
    }

    // ---- Load Cell ----
    private BleDeviceConfig loadCell = new BleDeviceConfig
    {
        deviceName = "LoadCellArduino",
        serviceUUID = "19b10000-e8f2-537e-4f6c-d104768a1214",
        characteristicAUUID = "19b10001-e8f2-537e-4f6c-d104768a1214", // weight,pouringRate
        characteristicBUUID = "19b10002-e8f2-537e-4f6c-d104768a1214", // TARE command
    };

    // ---- IMU ----
    private BleDeviceConfig imu = new BleDeviceConfig
    {
        deviceName = "IMUArduino",
        serviceUUID = "19b10000-e8f2-537e-4f6c-d104768a1214",
        characteristicAUUID = "19b10001-e8f2-537e-4f6c-d104768a1214", // angle,angularVelocity
        characteristicBUUID = "19b10002-e8f2-537e-4f6c-d104768a1214", // RESET command
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

        if (devices.All(d => d.addressFound))
        {
            BluetoothLEHardwareInterface.StopScan();
            _isScanning = false;
            ConnectNextInQueue();
        }
    }

    // ---------- Step 3: Connect (one device at a time) ----------

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

        // Only subscribe to the data (A) characteristic. B is write-only —
        // discovering it is enough; we don't subscribe to it, since the
        // firmware never sends notifications on it.
        if (c == device.characteristicAUUID.ToLower())
        {
            BluetoothLEHardwareInterface.SubscribeCharacteristicWithDeviceAddress(
                device.address, device.serviceUUID, device.characteristicAUUID,
                OnSubscribed, OnDataReceived);
        }
    }

    private void OnSubscribed(string address, string characteristic)
    {
        if (!devicesByAddress.TryGetValue(address, out var device)) return;

        string c = characteristic.ToLower();
        if (c == device.characteristicAUUID.ToLower()) device.subscribedA = true;

        Debug.Log($"Subscribed to {characteristic} on {device.deviceName}");

        if (device.subscribedA)
        {
            StartCoroutine(ConnectNextInQueueDelayed(0.75f));
        }
    }

    private System.Collections.IEnumerator ConnectNextInQueueDelayed(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        ConnectNextInQueue();
    }
    private void OnDisconnected(string address)
    {
        if (devicesByAddress.TryGetValue(address, out var device))
        {
            device.isConnected = false;
            device.subscribedA = false;
            Debug.Log("Disconnected from " + device.deviceName);
        }
    }

    // ---------- Step 4: Parse incoming data ----------
    // Firmware sends ASCII text "value1,value2" per notification, e.g.
    // "45.20,12.50" (IMU: angle,angularVelocity) or "120.50,18.30"
    // (load cell: weight,pouringRate) — matching the web reference implementation.
    private void OnDataReceived(string deviceAddress, string characteristicUUID, byte[] bytes)
    {
        Debug.Log($"[RAW] len={(bytes?.Length ?? -1)} text=\"{(bytes != null ? System.Text.Encoding.UTF8.GetString(bytes) : "null")}\"");

        if (!devicesByAddress.TryGetValue(deviceAddress, out var device))
        {
            Debug.LogWarning($"[LOOKUP FAIL] No device registered for address {deviceAddress}. Known addresses: {string.Join(", ", devicesByAddress.Keys)}");
            return;
        }

        string c = characteristicUUID.ToLower();
        if (c != device.characteristicAUUID.ToLower())
        {
            Debug.LogWarning($"[UUID MISMATCH] Got {c}, expected {device.characteristicAUUID.ToLower()} for {device.deviceName}");
            return;
        }

        if (!TryParseCsvPair(bytes, out float value1, out float value2))
        {
            Debug.LogWarning($"Malformed data from {device.deviceName}: {BytesToDebugString(bytes)}");
            return;
        }

        if (device == loadCell)
        {
            CurrentWeight = value1;
            CurrentPourRate = value2;
            Debug.Log($"[OK] LoadCell -> weight={CurrentWeight} rate={CurrentPourRate}");
        }
        else if (device == imu)
        {
            CurrentPourAngle = value1;
            CurrentVelocity = value2;
            Debug.Log($"[OK] IMU -> angle={CurrentPourAngle} vel={CurrentVelocity}");
        }
    }

    private bool TryParseCsvPair(byte[] bytes, out float value1, out float value2)
    {
        value1 = float.NaN;
        value2 = float.NaN;

        if (bytes == null || bytes.Length == 0) return false;

        string raw;
        try
        {
            raw = Encoding.UTF8.GetString(bytes).Trim();
        }
        catch (Exception e)
        {
            Debug.LogWarning("Failed to decode BLE text data: " + e.Message);
            return false;
        }

        string[] parts = raw.Split(',');
        if (parts.Length != 2) return false;

        bool ok1 = float.TryParse(parts[0], out value1);
        bool ok2 = float.TryParse(parts[1], out value2);

        return ok1 && ok2 && !float.IsNaN(value1) && !float.IsNaN(value2);
    }

    private string BytesToDebugString(byte[] bytes)
    {
        if (bytes == null) return "<null>";
        try { return Encoding.UTF8.GetString(bytes); }
        catch { return BitConverter.ToString(bytes); }
    }

    // ---------- Step 5: Write commands (RESET / TARE) ----------
    // Mirrors resetIMU() / tareLoadCell() from the web reference: writes an
    // ASCII command string to the write-only characteristic B.

    public void ResetIMU()
    {
        SendCommand(imu, "RESET");
    }

    public void TareLoadCell()
    {
        SendCommand(loadCell, "TARE");
    }

    private void SendCommand(BleDeviceConfig device, string command)
    {
        if (!device.isConnected)
        {
            Debug.LogWarning($"Cannot send '{command}' — {device.deviceName} is not connected.");
            return;
        }

        byte[] payload = Encoding.UTF8.GetBytes(command);

        BluetoothLEHardwareInterface.WriteCharacteristic(
            device.address,
            device.serviceUUID,
            device.characteristicBUUID,
            payload,
            payload.Length,
            false, // withoutResponse — set true/false to match your firmware's expectations
            (characteristic) => Debug.Log($"Sent '{command}' to {device.deviceName}"));
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