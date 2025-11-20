// In Assets/BLE_Connector/Scripts/BLE_Device.cs

using System;
using System.Collections.Generic;
using UnityEngine;

public class BLE_Device
{
    private AndroidJavaObject ble_android;

    public bool isConnected = false;
    public bool isFound = false;

    private byte[] data;

    public BLE_Device(string DeviceName, string ServiceUUID, string Characteristic)
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            ble_android = new AndroidJavaObject("com.hasindu.ble.BLE_Plugin");
            ble_android.Call("SetBluetoothDevice", DeviceName, ServiceUUID, Characteristic);
        }
    }

    public void Scan()
    {
        if (Application.platform == RuntimePlatform.Android)
            ble_android.Call("Scan");
    }

    public void Connect()
    {
        if (Application.platform == RuntimePlatform.Android)
            ble_android.Call("Connect");
    }

    public byte[] GetData()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            data = ble_android.Call<byte[]>("GetData");
        }
        return data;
    }

    public bool IsConnected()
    {
        if (Application.platform == RuntimePlatform.Android)
            isConnected = ble_android.Call<bool>("IsConnected");
        return isConnected;
    }
    public bool IsFound()
    {
        if (Application.platform == RuntimePlatform.Android)
            isFound = ble_android.Call<bool>("IsFound");
        return isFound;
    }
}