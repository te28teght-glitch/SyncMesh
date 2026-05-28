using System;
using Android.App;
using Android.OS;
using Android.Widget;
using Android.Content.PM;
using SyncMesh.Bluetooth.Server;

namespace SyncMesh
{
    [Activity(Label = "SyncMesh", MainLauncher = true)]
    public class MainActivity : Activity
    {
        private BluetoothServer bluetoothServer;
        private Button startButton;
        private const int RequestCode = 1001;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            startButton = new Button(this);
            startButton.Text = "Запустить сервер";
            startButton.Enabled = false;

            startButton.Click += (sender, e) =>
            {
                bluetoothServer = new BluetoothServer();
                bluetoothServer.Start();
                Toast.MakeText(this, "Сервер запущен", ToastLength.Short).Show();
            };

            SetContentView(startButton);

            // Запрашиваем разрешения при запуске
            RequestPermissions(new string[] 
            { 
                "android.permission.BLUETOOTH_SCAN",
                "android.permission.BLUETOOTH_CONNECT" 
            }, RequestCode);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            
            if (requestCode == RequestCode)
            {
                bool allGranted = true;
                foreach (var result in grantResults)
                {
                    if (result != Permission.Granted)
                    {
                        allGranted = false;
                        break;
                    }
                }
                
                if (allGranted)
                {
                    startButton.Enabled = true;
                    Toast.MakeText(this, "Разрешения получены", ToastLength.Short).Show();
                }
                else
                {
                    startButton.Enabled = false;
                    Toast.MakeText(this, "Разрешения не получены", ToastLength.Long).Show();
                }
            }
        }
    }
}