    using Microsoft.Win32;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SqlClient;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Media;
    namespace practica2
    {
        /// <summary>
        /// Lógica de interacción para MainWindow.xaml
        /// </summary>
        public partial class MainWindow : Window
        {
            private DataHandler dataHandler = new DataHandler();
            private Dictionary<string, int> empleadosDict = new Dictionary<string, int>();


            public MainWindow()
            {
                InitializeComponent();
                CargarEmpleados();
            }
            // Cargar empleados en el ComboBox
            private void CargarEmpleados()
            {
                cbEmpleados.Items.Clear();
                List<Tuple<int, string>> empleados = dataHandler.ObtenerEmpleados();

                foreach (var emp in empleados)
                {
                    cbEmpleados.Items.Add(emp.Item2);
                    empleadosDict[emp.Item2] = emp.Item1;
                }
            }
            private void btnCargarDatos_Click(object sender, RoutedEventArgs e)
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Filter = "Archivos de texto (*.txt)|*.txt"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    ProcesarArchivo(openFileDialog.FileName);
                }
            }

            private void ProcesarArchivo(string filePath)
            {
                try
                {
                    var lines = File.ReadAllLines(filePath);
                    List<string[]> datosProcesados = new List<string[]>();

                    if (lines.Length < 3)
                    {
                        MessageBox.Show("El archivo no tiene suficientes líneas para procesar.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    for (int i = 2; i < lines.Length - 1; i++) // Ignorar primera, segunda y última línea
                    {
                        string[] valores = lines[i].Split(',');

                        if (valores.Length < 10) // Validar cantidad mínima de columnas
                        {
                            MessageBox.Show($"Error en la línea {i + 1}: datos insuficientes.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        datosProcesados.Add(valores);
                    }

                    GuardarDatosEnSQL(datosProcesados);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al leer el archivo: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            private void btnEscogerDatos_Click(object sender, RoutedEventArgs e)
            {

            }

        private void btnBuscar_Click(object sender, RoutedEventArgs e)
        {
            if (cbEmpleados.SelectedItem == null)
            {
                MessageBox.Show("Seleccione un empleado", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string nombreEmpleado = cbEmpleados.SelectedItem.ToString();
            int empleadoID = empleadosDict[nombreEmpleado];

            List<RegistroAsistencia> registros = dataHandler.ObtenerAsistenciaPorEmpleado(empleadoID);

            // Agrupar entradas y salidas por fecha
            var registrosAgrupados = registros.GroupBy(r => r.FechaHora.Date)
                .Select(g => new
                {
                    Fecha = g.Key,
                    Entrada1 = g.Where(r => r.Tipo == "Entrada").Select(r => r.FechaHora.ToString("HH:mm")).FirstOrDefault(),
                    Salida1 = g.Where(r => r.Tipo == "Salida").Select(r => r.FechaHora.ToString("HH:mm")).FirstOrDefault(),
                    Entrada2 = g.Where(r => r.Tipo == "Entrada").Skip(1).Select(r => r.FechaHora.ToString("HH:mm")).FirstOrDefault(),
                    Salida2 = g.Where(r => r.Tipo == "Salida").Skip(1).Select(r => r.FechaHora.ToString("HH:mm")).FirstOrDefault(),
                }).ToList();

            dgDatos.ItemsSource = registrosAgrupados;
        }

        private void dgDatos_SelectionChanged(object sender, SelectionChangedEventArgs e)
            {   
                
            }

            private void GuardarDatosEnSQL(List<string[]> datos)
            {
                try
                {
                    // Definimos la variable para guardar el ID
                    int empleadoID = 0;

                    foreach (var fila in datos)
                    {
                        try
                        {
                            // Cuando la fila tenga la marca "ID" (según tu formato)
                            if (fila[0].Trim() == "ID")
                            {
                                // En lugar de parsear fila[1] como ID,
                                // usas directamente Nombre y Departamento de la línea
                                string nombre = fila[4].Trim();
                                string departamento = fila[8].Trim();

                                if (!string.IsNullOrEmpty(nombre) && !string.IsNullOrEmpty(departamento))
                                {
                                    // dataHandler.InsertEmpleado(...) te devuelve el ID de BD
                                    empleadoID = dataHandler.InsertEmpleado(nombre, departamento);

                                    // A partir de aquí, empleadoID es el verdadero ID en la BD
                                }
                            }
                            else if (fila[0].Trim().Contains("/")) // Procesar registros de asistencia
                            {
                                // Asegurar que ya obtuviste un empleadoID válido
                                if (empleadoID == 0) continue;

                                int i = 0;
                                while (i < fila.Length)
                                {
                                    string valor = fila[i].Trim();

                                    // Verificamos si valor es una fecha/hora
                                    if (DateTime.TryParseExact(
                                        valor,
                                        "dd/MM/yyyy HH:mm",
                                        CultureInfo.InvariantCulture,
                                        DateTimeStyles.None,
                                        out DateTime fechaHora))
                                    {
                                        // Valor por defecto
                                        string tipo = "Entrada";

                                        // Revisar siguiente columna para ver si "Entrada" o "Salida"
                                        if (i + 1 < fila.Length)
                                        {
                                            string posibleTipo = fila[i + 1].Trim();
                                            if (posibleTipo.Equals("Entrada", StringComparison.OrdinalIgnoreCase) ||
                                                posibleTipo.Equals("Salida", StringComparison.OrdinalIgnoreCase))
                                            {
                                                tipo = posibleTipo;
                                                i++;
                                            }
                                            else if (string.IsNullOrEmpty(posibleTipo))
                                            {
                                                // Revisar dos columnas adelante
                                                if (i + 2 < fila.Length)
                                                {
                                                    string posibleTipo2 = fila[i + 2].Trim();
                                                    if (posibleTipo2.Equals("Entrada", StringComparison.OrdinalIgnoreCase) ||
                                                        posibleTipo2.Equals("Salida", StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        tipo = posibleTipo2;
                                                        i += 2;
                                                    }
                                                }
                                            }
                                        }

                                        // Insertamos el registro de asistencia con el ID de la BD
                                        dataHandler.InsertRegistroAsistencia(empleadoID, fechaHora, tipo);
                                    }

                                    i++;
                                }
                            }
                            else if (fila[0].Trim() == "Entrada")
                            {
                                // Asegurar que hay un empleado asignado
                                if (empleadoID == 0) continue;

                                string totalEntradas = fila[1].Trim();
                                string totalSalidas = fila[4].Trim();
                                string tiempoTotal = fila[6].Trim();

                                // Ejemplo: concatenas índices 9 y 10
                                string horasTotales = fila[9].Trim() + ";" + fila[10].Trim();

                                // Insertar horas, totales, etc.
                                dataHandler.InsertHorasTrabajadas(empleadoID, tiempoTotal, horasTotales);
                                dataHandler.InsertTotalesAsistencia(empleadoID, totalEntradas, totalSalidas);
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error en la línea: {string.Join(",", fila)}\n{ex.Message}", "Error", MessageBoxButton.OK,MessageBoxImage.Error);
                        }
                    }

                    MessageBox.Show("Datos importados con éxito", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al guardar los datos en la base: {ex.Message}","Error",MessageBoxButton.OK,MessageBoxImage.Error);
                }
            }

        }





    }
