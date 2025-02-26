using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Client
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Thread hilo = new Thread(listenner);
            hilo.IsBackground = true;
            hilo.Start();
        }

        void listenner()
        {
            while (true)
            {
                string serverIp = "192.168.0.101"; // IP DEL SERVIDOR 
                int port = 5000;
                try
                {
                    using (TcpClient client = new TcpClient(serverIp, port))
                    using (NetworkStream stream = client.GetStream())
                    {
                        DriveInfo drive = DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady);
                        if (drive == null)
                        {
                            UpdateUI("No se encontró un disco disponible.", "", "");
                            return;
                        }

                        int full_storage = (int)(drive.TotalSize / (1024 * 1024 * 1024));
                        int available_storage_gb = (int)(drive.AvailableFreeSpace / (1024 * 1024 * 1024));
                        int used_storage_gb = full_storage - available_storage_gb;

                        ClientClass diskInfo = new ClientClass
                        {
                            name_sucursal = "Cochabamba",
                            name_disk = drive.Name,
                            tipo = drive.DriveType.ToString(),
                            full_storage_gb = full_storage,
                            available_storage_gb = available_storage_gb,
                            used_storage_gb = used_storage_gb
                        };

                        string jsonData = JsonSerializer.Serialize(diskInfo);
                        byte[] data = Encoding.UTF8.GetBytes(jsonData);
                        stream.Write(data, 0, data.Length);

                        // Actualizar la interfaz con información ordenada
                        Dispatcher.Invoke(() =>
                        {
                            UpdateUI("Información del disco enviada:", jsonData, $"Disco: {drive.Name}\nTipo: {drive.DriveType}\nEspacio Total: {full_storage} GB\nEspacio Usado: {used_storage_gb} GB\nEspacio Disponible: {available_storage_gb} GB");
                        });
                    }
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        UpdateUI($"Error en el cliente: {ex.Message}", "", "");
                    });
                }
                Thread.Sleep(5000);
            }
        }

        // Método para actualizar la interfaz de usuario
        private void UpdateUI(string title, string json, string details)
        {
       
            lblDetails.Content = details;
            lblInfo.Content = "";

            Storyboard updateAnimation = (Storyboard)this.Resources["UpdateAnimation"];
         
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
                WindowState = WindowState.Normal;
            else
                WindowState = WindowState.Maximized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}
