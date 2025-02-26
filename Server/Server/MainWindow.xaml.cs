using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Server
{
    //Canon
    public partial class MainWindow : Window
    {
        private Dictionary<string, StackPanel> sucursalPanels = new Dictionary<string, StackPanel>();
        private Dictionary<string, DateTime> lastReportTime = new Dictionary<string, DateTime>();
        private const int timeoutSeconds = 10;

        public List<string> sucursal_names = new List<string>()
        {
            "Cochabamba", "La Paz", "Tarija",
            "Santa Cruz", "Pando", "Beni",
            "Chuquisaca", "Potosi", "Oruro"
        };

        public MainWindow()
        {
            InitializeComponent();
            InitializeUI();
            Thread serverThread = new Thread(Main);
            serverThread.IsBackground = true;
            serverThread.Start();
            StartMonitoring();
        }

        void InitializeUI()
        {
            StorageGrid.Children.Clear();
            StorageGrid.Columns = 3;

            foreach (var sucursal in sucursal_names)
            {
                Border border = new Border
                {
                    Margin = new Thickness(10),
                    Width = 200,
                    Height = 200, //150
                    BorderBrush = new SolidColorBrush(Color.FromArgb(255, 66, 165, 245)),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(10),
                    CornerRadius = new CornerRadius(15),
                    Background = new SolidColorBrush(Color.FromArgb(255, 43, 58, 74))
                };

                var shadowEffect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 315,
                    ShadowDepth = 5,
                    Opacity = 0.5
                };
                border.Effect = shadowEffect;

                StackPanel panel = new StackPanel
                {
                    Background = new SolidColorBrush(Color.FromArgb(255, 43, 58, 74))
                };

                TextBlock title = new TextBlock { Text = sucursal, FontWeight = FontWeights.Bold, FontSize = 16, TextAlignment = TextAlignment.Center };
                TextBlock status = new TextBlock { Text = "No reporta", Foreground = Brushes.Red, FontSize = 14, TextAlignment = TextAlignment.Center };
                panel.Children.Add(title);
                panel.Children.Add(status);

                border.Child = panel;

                sucursalPanels[sucursal] = panel;
                lastReportTime[sucursal] = DateTime.MinValue;
                StorageGrid.Children.Add(border);
            }
        }

        void Main()
        {
            int port = 5000;
            TcpListener server = new TcpListener(IPAddress.Any, port);
            server.Start();

            Dispatcher.Invoke(() =>
            {
                lblInfo.Content = $"Servidor escuchando en el puerto {port}...";
            });

            while (true)
            {
                TcpClient client = server.AcceptTcpClient();
                Thread thread = new Thread(() => HandleClient(client));
                thread.Start();
            }
        }

        void HandleClient(TcpClient client)
        {
            try
            {
                using (client)
                using (NetworkStream stream = client.GetStream())
                {
                    byte[] buffer = new byte[2048];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    List<ClientClass> diskInfoList = JsonSerializer.Deserialize<List<ClientClass>>(receivedData);

                    foreach (var diskInfo in diskInfoList)
                    {
                        Dispatcher.Invoke(() => UpdateUI(diskInfo));
                    }
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => lblInfo.Content = $"Error en cliente: {ex.Message} desde servidor");
            }
        }

        void UpdateUI(ClientClass diskInfo)
        {
            if (sucursalPanels.ContainsKey(diskInfo.name_sucursal))
            {
                StackPanel panel = sucursalPanels[diskInfo.name_sucursal];

                if (!panel.Children.OfType<TextBlock>().Any(tb => tb.Text == diskInfo.name_sucursal))
                {
                    panel.Children.Clear();
                    panel.Children.Add(new TextBlock
                    {
                        Text = diskInfo.name_sucursal,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.White,
                        FontSize = 16,
                        TextAlignment = TextAlignment.Center
                    });
                }

                SolidColorBrush c_color = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF42A5F5"));

                panel.Children.Add(new TextBlock { Text = $"Disco: {diskInfo.name_disk}", FontSize = 14, Foreground = c_color, TextAlignment = TextAlignment.Center });
                panel.Children.Add(new TextBlock { Text = $"Tipo: {diskInfo.tipo}", Foreground = Brushes.LightGray });
                panel.Children.Add(new TextBlock { Text = $"{diskInfo.full_storage_gb} GB Total", Foreground = Brushes.LightGray });
                panel.Children.Add(new TextBlock { Text = $"{diskInfo.used_storage_gb} GB Uso", Foreground = Brushes.LightGray });
                panel.Children.Add(new TextBlock { Text = $"{diskInfo.available_storage_gb} GB Libre", Foreground = Brushes.LightGray, Margin = new Thickness(0, 0, 0, 5) });

                //lastReportTime[diskInfo.name_sucursal] = DateTime.Now;

                //ProgressBar progressBar = new ProgressBar
                //{
                //    Value = (double)diskInfo.available_storage_gb / diskInfo.full_storage_gb * 100,
                //    Height = 10,
                //    Foreground = diskInfo.available_storage_gb > 100 ? Brushes.Green : Brushes.Red
                //};
                //panel.Children.Add(progressBar);
                //panel.Children.Add(CreatePieChart(diskInfo.used_storage_gb, diskInfo.available_storage_gb, diskInfo.full_storage_gb));


                PieChart pieChart = new PieChart
                {
                    Width = 25,
                    Height = 25,
                    Series = new SeriesCollection
            {
                new PieSeries
                {
                    Title = "Uso",
                    Values = new ChartValues<double> { diskInfo.used_storage_gb },
                    Fill = Brushes.Red
                },
                new PieSeries
                {
                    Title = "Libre",
                    Values = new ChartValues<double> { diskInfo.available_storage_gb },
                    Fill = Brushes.Green
                }
            }
                };

                panel.Children.Add(pieChart);
                lastReportTime[diskInfo.name_sucursal] = DateTime.Now;

                UpdateResume();
            }
        }

        void StartMonitoring()
        {
            Thread monitorThread = new Thread(() =>
            {
                while (true)
                {
                    Thread.Sleep(5000);
                    Dispatcher.Invoke(() => CheckDisconnections());
                }
            });
            monitorThread.IsBackground = true;
            monitorThread.Start();
        }

        void CheckDisconnections()
        {
            DateTime now = DateTime.Now;
            foreach (var sucursal in sucursal_names)
            {
                if ((now - lastReportTime[sucursal]).TotalSeconds > timeoutSeconds)
                {
                    if (sucursalPanels.ContainsKey(sucursal))
                    {
                        StackPanel panel = sucursalPanels[sucursal];
                        panel.Children.Clear();
                        panel.Children.Add(new TextBlock { Text = sucursal, FontWeight = FontWeights.Bold, FontSize = 16, TextAlignment = TextAlignment.Center });
                        panel.Children.Add(new TextBlock { Text = "No reporta", Foreground = Brushes.Red, FontSize = 14, TextAlignment = TextAlignment.Center });
                    }
                }
            }
        }

        void UpdateResume()
        {
            int totalGB = 0;
            int totalUsado = 0;
            int totalLibre = 0;
            int reportados = 0;

            foreach (var panel in sucursalPanels.Values)
            {
                var texts = panel.Children.OfType<TextBlock>().ToList();
                if (texts.Count >= 5)
                {
                    foreach (var text in texts)
                    {
                        if (text.Text.Contains("GB Total")) totalGB += int.Parse(text.Text.Split(' ')[0]);
                        if (text.Text.Contains("GB Uso")) totalUsado += int.Parse(text.Text.Split(' ')[0]);
                        if (text.Text.Contains("GB Libre")) totalLibre += int.Parse(text.Text.Split(' ')[0]);
                    }
                    reportados++;
                }
            }

            lblStorage.Content = $"Total: {totalGB / 1024.0:F1} TB - Usado: {totalUsado} GB - Libre: {totalLibre} GB";
            lblReport.Content = $"Reportaron {reportados} de {sucursalPanels.Count}";
        }

        private void btmMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
                WindowState = WindowState.Normal;
            else
                WindowState = WindowState.Maximized;
        }

        private void btnMinimizar_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}
