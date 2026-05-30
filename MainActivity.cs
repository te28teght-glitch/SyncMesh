using System;
using Android.App;
using Android.OS;
using Android.Widget;
using Android.Content.PM;
using Android.Content;
using Android.Bluetooth;
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
        private TextView? statusTextView;
        private Button? connectButton;
        private Button? sendButton;
        private Button? startServerButton;
        private Button? clearLogsButton;
        private const int RequestCode = 1001;
        private const string SavedMacKey = "last_mac_address";
        private ISharedPreferences? sharedPreferences;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            
            // Загружаем сохранённые настройки
            sharedPreferences = GetSharedPreferences("SyncMeshPrefs", FileCreationMode.Private);
            
            // Загружаем разметку из XML
            SetContentView(Resource.Layout.main_layout);
            
            // Находим все элементы управления по их ID
            startServerButton = FindViewById<Button>(Resource.Id.startServerButton);
            connectButton = FindViewById<Button>(Resource.Id.connectButton);
            sendButton = FindViewById<Button>(Resource.Id.sendButton);
            clearLogsButton = FindViewById<Button>(Resource.Id.clearLogsButton);
            macAddressInput = FindViewById<EditText>(Resource.Id.macAddressInput);
            messageInput = FindViewById<EditText>(Resource.Id.messageInput);
            logTextView = FindViewById<TextView>(Resource.Id.logTextView);
            statusTextView = FindViewById<TextView>(Resource.Id.statusTextView);
            
            // Загружаем сохранённый MAC-адрес (если есть)
            var savedMac = sharedPreferences?.GetString(SavedMacKey, "");
            if (!string.IsNullOrEmpty(savedMac) && macAddressInput != null)
            {
                macAddressInput.Text = savedMac;
                AddLog($"Загружен сохранённый MAC: {savedMac}");
            }
            
            // Настраиваем обработчики событий
            SetupEventHandlers();
            
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
        
        private void SetupEventHandlers()
        {
            // Кнопка запуска сервера
            if (startServerButton != null)
            {
                startServerButton.Enabled = false;
                startServerButton.Click += (sender, e) =>
                {
                    bluetoothServer = new BluetoothServer();
                    bluetoothServer.Start();
                    AddLog("Сервер запущен, ждём подключений...");
                    UpdateStatus("сервер активен", true);
                };
            }
            
            // Кнопка подключения к серверу
            if (connectButton != null)
            {
                connectButton.Click += (sender, e) =>
                {
                    var mac = macAddressInput?.Text;
                    if (string.IsNullOrWhiteSpace(mac))
                    {
                        AddLog("Введите MAC-адрес");
                        return;
                    }
                    
                    // Сохраняем введённый MAC
                    var editor = sharedPreferences?.Edit();
                    editor?.PutString(SavedMacKey, mac);
                    editor?.Apply();
                    
                    bluetoothClient = new BluetoothClient();
                    
                    bluetoothClient.OnConnected += () =>
                    {
                        RunOnUiThread(() => {
                            AddLog("Подключено к серверу!");
                            UpdateStatus("клиент подключён", true);
                        });
                    };
                    
                    bluetoothClient.OnMessageReceived += (message) =>
                    {
                        RunOnUiThread(() => AddLog($"📩 Получено: {message}"));
                    };
                    
                    bluetoothClient.OnDisconnected += () =>
                    {
                        RunOnUiThread(() => {
                            AddLog("Отключено от сервера");
                            UpdateStatus("ожидание", false);
                        });
                    };
                    
                    bluetoothClient.Connect(mac);
                    AddLog("Подключение...");
                };
            }
            
            // Кнопка отправки сообщения
            if (sendButton != null)
            {
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
                        AddLog($"📤 Отправлено: {text}");
                        if (messageInput != null) messageInput.Text = "";
                    }
                    else if (bluetoothServer != null)
                    {
                        AddLog("Режим сервера: отправка клиентам пока не реализована");
                    }
                    else
                    {
                        AddLog("Не подключено к серверу");
                    }
                };
            }
            
            // Кнопка очистки логов
            if (clearLogsButton != null)
            {
                clearLogsButton.Click += (sender, e) =>
                {
                    if (logTextView != null)
                    {
                        logTextView.Text = "=== Логи ===\n";
                        AddLog("Логи очищены");
                    }
                };
            }
        }
        
        private void UpdateStatus(string status, bool isActive)
        {
            RunOnUiThread(() =>
            {
                if (statusTextView != null)
                {
                    var icon = isActive ? "🟢" : "⚪";
                    statusTextView.Text = $"{icon} Статус: {status}";
                }
            });
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
        
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Android.Content.PM.Permission[] grantResults)
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
                    AddLog("✅ Разрешения получены");
                    UpdateStatus("разрешения получены", false);
                }
                else
                {
                    if (startServerButton != null) startServerButton.Enabled = false;
                    if (connectButton != null) connectButton.Enabled = false;
                    AddLog("❌ Разрешения не получены. Bluetooth не будет работать");
                    UpdateStatus("нет разрешений", false);
                }
            }
        }
    }
}