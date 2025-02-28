using System;
using System.Collections.Generic;
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
                string serverIp = "127.0.0.1"; // IP DEL SERVIDOR 
                int port = 5000;
                try
                {
                    using (TcpClient client = new TcpClient(serverIp, port))
                    using (NetworkStream stream = client.GetStream())
                    {
                        var drives = DriveInfo.GetDrives().Where(d => d.IsReady).ToList();
                        if (drives.Count == 0)
                        {
                            UpdateUI("No se encontró un disco disponible.", "", "");
                            return;
                        }

                        List<ClientClass> diskInfoList = drives.Select(drive => new ClientClass
                        {
                            name_sucursal = "Cochabamba",
                            name_disk = drive.Name,
                            tipo = drive.DriveType.ToString(),
                            full_storage_gb = (int)(drive.TotalSize / (1024 * 1024 * 1024)),
                            available_storage_gb = (int)(drive.AvailableFreeSpace / (1024 * 1024 * 1024)),
                            used_storage_gb = (int)(drive.TotalSize / (1024 * 1024 * 1024)) - (int)(drive.AvailableFreeSpace / (1024 * 1024 * 1024))
                        }).ToList();

                        string jsonData = JsonSerializer.Serialize(diskInfoList);
                        byte[] data = Encoding.UTF8.GetBytes(jsonData);
                        stream.Write(data, 0, data.Length);

                        // Actualizar la interfaz con información ordenada
                        Dispatcher.Invoke(() =>
                        {
                            string details = string.Join("\n\n", diskInfoList.Select(d =>
                                $"Disco: {d.name_disk}\nTipo: {d.tipo}\nTotal: {d.full_storage_gb} GB\nUsado: {d.used_storage_gb} GB\nDisponible: {d.available_storage_gb} GB"));

                            UpdateUI("Información de discos enviada:", jsonData, details);
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
