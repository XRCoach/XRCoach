// In Assets/BLE_Connector/Scripts/BleConnector.cs

using System;
using System.Collections.Generic;
using UnityEngine;

public class BleConnector : MonoBehaviour
{
    public BLE_Device ble_device;

    public string DeviceName = "MyDevice";
    public string ServiceUUID = "180D";
    public string Characteristic = "2A37";

    public bool isScanning = false;
    private bool isConnected = false;

    private float _timer = 0;
    private bool _connected = false;

    void Start()
    {
        ble_device = new BLE_Device(DeviceName, ServiceUUID, Characteristic);
    }

    void Update()
    {
        if (isScanning)
        {
            if (ble_device.IsFound())
            {
                isScanning = false;
                ble_device.Connect();
            }
        }

        if (ble_device.IsConnected() && !_connected)
        {
            _connected = true;
            Debug.Log("Device Connected");
        }


        _timer += Time.deltaTime;
        if (_timer > 2 && ble_device.IsConnected())
        {
            _timer = 0;
            byte[] data = ble_device.GetData();
            if (data != null)
            {
                string s = System.Text.Encoding.UTF8.GetString(data);
                Debug.Log(s);
            }
        }

        if (!ble_device.IsConnected() && _connected)
        {
            Debug.Log("Device Disconnected");
            _connected = false;
        }
    }


    public void Scan()
    {
        isScanning = true;
        ble_device.Scan();
    }
}