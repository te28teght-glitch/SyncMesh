using System;
using Android.App;
using Android.OS;
using Android.Widget;
using Android.Content.PM;
using SyncMesh.Bluetooth.Server;
using SyncMesh.Bluetooth.Client;

namespace SyncMesh
{
    [Activity(Label = "", MainLauncher = true, Theme = "@android:style/Theme.NoTitleBar")]
    public class MainActivity : Activity
    {
        private BluetoothServer? bluetoothServer;
        private BluetoothClient? bluetoothClient;
        private EditText? macAddressInput;
        private EditText? messageInput;
        private TextView? logTextView;
        private Button? connectButton;
        private Button? sendButton;
        private Button? startServerButton;
        private const int RequestCode = 1001;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            
            // Создаём вертикальный макет
            var layout = new LinearLayout(this);
            layout.Orientation = Orientation.Vertical;
            layout.SetPadding(30, 100, 30, 30);

            // ===== Кнопка запуска сервера =====
            startServerButton = new Button(this);
            startServerButton.Text = "Запустить сервер";
            startServerButton.Enabled = false;
            startServerButton.Click += (sender, e) =>
            {
                bluetoothServer = new BluetoothServer();
                bluetoothServer.Start();
                AddLog("Сервер запущен, ждём подключений...");
            };
            layout.AddView(startServerButton);

            // ===== Разделитель =====
            var separator = new TextView(this);
            separator.Text = "---------------------";
            separator.SetPadding(0, 20, 0, 20);
            layout.AddView(separator);

            // ===== Поле для MAC-адреса =====
            var macLabel = new TextView(this);
            macLabel.Text = "MAC-адрес сервера:";
            layout.AddView(macLabel);
            
            macAddressInput = new EditText(this);
            macAddressInput.Hint = "например: AA:BB:CC:DD:EE:FF";
            layout.AddView(macAddressInput);

            // ===== Кнопка подключения =====
            connectButton = new Button(this);
            connectButton.Text = "Подключиться к серверу";
            connectButton.Click += (sender, e) =>
            {
                var mac = macAddressInput?.Text;
                if (string.IsNullOrWhiteSpace(mac))
                {
                    AddLog("Введите MAC-адрес");
                    return;
                }
                
                bluetoothClient = new BluetoothClient();
                
                bluetoothClient.OnConnected += () =>
                {
                    RunOnUiThread(() => AddLog("Подключено к серверу!"));
                };
                
                bluetoothClient.OnMessageReceived += (message) =>
                {
                    RunOnUiThread(() => AddLog($"Получено: {message}"));
                };
                
                bluetoothClient.OnDisconnected += () =>
                {
                    RunOnUiThread(() => AddLog("Отключено от сервера"));
                };
                
                bluetoothClient.Connect(mac);
                AddLog("Подключение...");
            };
            layout.AddView(connectButton);

            // ===== Поле для сообщения =====
            var messageLabel = new TextView(this);
            messageLabel.Text = "Сообщение:";
            layout.AddView(messageLabel);
            
            messageInput = new EditText(this);
            messageInput.Hint = "Введите текст";
            layout.AddView(messageInput);

            // ===== Кнопка отправки =====
            sendButton = new Button(this);
            sendButton.Text = "Отправить";
            sendButton.Click += (sender, e) =>
            {
                var text = messageInput?.Text;
                if (string.IsNullOrWhiteSpace(text))
                {
                    AddLog("Введите сообщение");
                    return;
                }
                
                if (bluetoothClient != null && bluetoothClient.IsConnected)
                {
                    bluetoothClient.SendMessage(text);
                    AddLog($"Отправлено: {text}");
                    if (messageInput != null) messageInput.Text = "";
                }
                else
                {
                    AddLog("Не подключено к серверу");
                }
            };
            layout.AddView(sendButton);

            // ===== Поле для логов =====
            logTextView = new TextView(this);
            logTextView.Text = "=== Логи ===\n";
            logTextView.SetTextSize(Android.Util.ComplexUnitType.Dip, 12);
            layout.AddView(logTextView);

            SetContentView(layout);

            // Запрашиваем разрешения
            RequestPermissions(new string[] 
            { 
                "android.permission.BLUETOOTH_SCAN",
                "android.permission.BLUETOOTH_CONNECT",
                "android.permission.BLUETOOTH",
                "android.permission.BLUETOOTH_ADMIN",
                "android.permission.ACCESS_FINE_LOCATION"
            }, RequestCode);
        }

        private void AddLog(string message)
        {
            RunOnUiThread(() =>
            {
                if (logTextView != null)
                {
                    logTextView.Text += $"{DateTime.Now:HH:mm:ss} - {message}\n";
                }
            });
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
                    if (startServerButton != null) startServerButton.Enabled = true;
                    if (connectButton != null) connectButton.Enabled = true;
                    AddLog("Разрешения получены");
                }
                else
                {
                    if (startServerButton != null) startServerButton.Enabled = false;
                    if (connectButton != null) connectButton.Enabled = false;
                    AddLog("Разрешения не получены. Bluetooth не будет работать");
                }
            }
        }
    }
}