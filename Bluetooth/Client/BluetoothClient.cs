using System;
using System.Text;
using System.Threading;
using Android.Bluetooth;
using Android.Util;

namespace SyncMesh.Bluetooth.Client
{
    public class BluetoothClient
    {
        private BluetoothAdapter bluetoothAdapter;
        private BluetoothSocket socket;
        private bool isConnected;
        private Thread readThread;
        private static readonly string MY_UUID = "550e8400-e29b-41d4-a716-446655440000";
        public bool IsConnected => isConnected;
        public event Action<string> OnMessageReceived;
        public event Action OnConnected;
        public event Action OnDisconnected;

        public BluetoothClient()
        {
            bluetoothAdapter = BluetoothAdapter.DefaultAdapter;
        }
        public void Connect(string deviceAddress)
        {
            if (bluetoothAdapter == null)
            {
                Log.Error("BluetoothClient", "Телефон не поддерживает Bluetooth");
                return;
            }
            
            try
            {
                var device = bluetoothAdapter.GetRemoteDevice(deviceAddress);
                socket = device.CreateRfcommSocketToServiceRecord(Java.Util.UUID.FromString(MY_UUID));
                socket.Connect();
                
                isConnected = true;
                OnConnected?.Invoke();
                
                readThread = new Thread(ReadLoop);
                readThread.Start();
                
                Log.Info("BluetoothClient", "Подключено к " + deviceAddress);
            }
            catch (Exception ex)
            {
                Log.Error("BluetoothClient", $"Ошибка подключения: {ex.Message}");
            }
        }
        public void SendMessage(string message)
        {
            if (!isConnected || socket == null)
            {
                Log.Warn("BluetoothClient", "Не подключено, сообщение не отправлено");
                return;
            }
            
            try
            {
                var bytes = Encoding.UTF8.GetBytes(message);
                socket.OutputStream.Write(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                Log.Error("BluetoothClient", $"Ошибка отправки: {ex.Message}");
            }
        }
        private void ReadLoop()
        {
            var buffer = new byte[1024];
            
            while (isConnected)
            {
                try
                {
                    var bytesRead = socket.InputStream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        OnMessageReceived?.Invoke(message);
                    }
                }
                catch (Exception ex)
                {
                    if (isConnected)
                    {
                        Log.Error("BluetoothClient", $"Ошибка чтения: {ex.Message}");
                        Disconnect();
                    }
                }
            }
        }
        public void Disconnect()
        {
            isConnected = false;
            try
            {
                socket?.Close();
            }
            catch (Exception ex)
            {
                Log.Error("BluetoothClient", $"Ошибка отключения: {ex.Message}");
            }
            OnDisconnected?.Invoke();
        }
    }
}