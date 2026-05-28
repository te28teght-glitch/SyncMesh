using Android.Bluetooth;
using Java.Util;

namespace SyncMesh.Bluetooth.Server
{
    public class BluetoothServer
    {
        private BluetoothAdapter bluetoothAdapter;
        private BluetoothServerSocket serverSocket;
        private bool isRunning;
        private Thread acceptThread;
        public BluetoothServer()
        {
            bluetoothAdapter = BluetoothAdapter.DefaultAdapter;    
        } 
        public void Start()
        {
            if (bluetoothAdapter == null)
            {
                Android.Util.Log.Error("BluetoothServer", "Телефон не поддерживает Bluetooth");
                return;
            }
            if (!bluetoothAdapter.IsEnabled)
            {
                Android.Util.Log.Error("BluetoothServer", "Bluetooth выключен");
                return;
            }
            try
            {
                var uuid = Java.Util.UUID.FromString("550e8400-e29b-41d4-a716-446655440000");
                serverSocket = bluetoothAdapter.ListenUsingInsecureRfcommWithServiceRecord("SyncMeshHub",uuid);
                isRunning = true;
                acceptThread = new Thread(AcceptLoop);
                acceptThread.Start();
                Android.Util.Log.Info("BluetoothServer", "Сервер запущен, ждём подключения...");
            }
            catch (Exception ex)
            {
                Android.Util.Log.Error("BluetoothServer", $"Ошибка: {ex.Message}");
            }
        }
        private void AcceptLoop()
        {
            while (isRunning)
            {
                try
                {
                    var clientSocket = serverSocket.Accept();
                    if (clientSocket != null)
                    {
                        Android.Util.Log.Info("Bluetooth","Клиент подключился!");
                    }
                }
                catch (Exception ex)
                {
                    if (isRunning)
                    {
                        Android.Util.Log.Error("BluetoothServer", $"Ошибка Accept: {ex.Message}");
                    }
                }
            }
        }
        public void Stop()
        {
            isRunning = false;
            try
            {
                serverSocket?.Close();
            }
            catch (Exception ex)
            {
                Android.Util.Log.Error("BluetoothServer", $"Ошибка остановки: {ex.Message}");
            }
        }
    }
}