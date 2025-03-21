using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace practica2
{
    class DataHandler
    {

        private string connectionString = "Data Source=localhost;Initial Catalog=ControlAsistencia;User ID=sa;Password=univalle";


        public int InsertEmpleado(string nombre, string departamento)
        {
            // Consulta para:
            // 1) Buscar si ya existe un empleado con el mismo Nombre
            // 2) Si no existe, insertar nuevo
            // 3) Devolver el ID (nuevo o existente)
            string query = @"
                                        DECLARE @ExistingId INT;

                                        SELECT @ExistingId = ID
                                        FROM Empleados
                                        WHERE Nombre = @Nombre;

                                        IF @ExistingId IS NULL
                                        BEGIN
                                            INSERT INTO Empleados (Nombre, Departamento)
                                            VALUES (@Nombre, @Departamento);

                                            -- Obtener el ID recién insertado
                                            SET @ExistingId = SCOPE_IDENTITY();
                                        END

                                        SELECT @ExistingId;
                                    ";

            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Nombre", nombre);
                command.Parameters.AddWithValue("@Departamento", departamento);

                connection.Open();

                // ExecuteScalar() devuelve el valor de la primera columna
                // de la primera fila del resultado (en este caso, @ExistingId)
                object result = command.ExecuteScalar();

                // Convertir a int
                return Convert.ToInt32(result);
            }
        }

        public int InsertRegistroAsistencia(int empleadoID, DateTime fechaHora, string tipo)
        {
            string query = "INSERT INTO RegistrosAsistencia (EmpleadoID, FechaHora, Tipo) VALUES (@EmpleadoID, @FechaHora, @Tipo)";

            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@EmpleadoID", empleadoID);
                command.Parameters.AddWithValue("@FechaHora", fechaHora);
                command.Parameters.AddWithValue("@Tipo", tipo);

                connection.Open();
                return command.ExecuteNonQuery();
            }
        }

        public int InsertHorasTrabajadas(int empleadoID, string tiempoTotal, string horasTotales)
        {
            string query = "INSERT INTO HorasTrabajadas (EmpleadoID, TiempoTotal, HorasTotales) VALUES (@EmpleadoID, @TiempoTotal, @HorasTotales)";

            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@EmpleadoID", empleadoID);
                command.Parameters.AddWithValue("@TiempoTotal", tiempoTotal);
                command.Parameters.AddWithValue("@HorasTotales", horasTotales);

                connection.Open();
                return command.ExecuteNonQuery();
            }
        }

        public int InsertTotalesAsistencia(int empleadoID, string totalEntrada, string totalSalida)
        {
            string query = "INSERT INTO TotalesAsistencia (EmpleadoID, TotalEntrada, TotalSalida) VALUES (@EmpleadoID, @TotalEntrada, @TotalSalida)";

            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@EmpleadoID", empleadoID);
                command.Parameters.AddWithValue("@TotalEntrada", totalEntrada);
                command.Parameters.AddWithValue("@TotalSalida", totalSalida);

                connection.Open();
                return command.ExecuteNonQuery();
            }
        }
        // Método para obtener la lista de empleados (ID y Nombre)
        public List<Tuple<int, string>> ObtenerEmpleados()
        {
            List<Tuple<int, string>> empleados = new List<Tuple<int, string>>();

            string query = "SELECT ID, Nombre FROM Empleados";

            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int id = reader.GetInt32(0);
                        string nombre = reader.GetString(1);
                        empleados.Add(new Tuple<int, string>(id, nombre));
                    }
                }
            }
            return empleados;
        }

        // Método para obtener registros de asistencia de un empleado específico
        public List<RegistroAsistencia> ObtenerAsistenciaPorEmpleado(int empleadoID)
        {
            List<RegistroAsistencia> registros = new List<RegistroAsistencia>();

            string query = @"
                SELECT FechaHora, Tipo
                FROM RegistrosAsistencia
                WHERE EmpleadoID = @EmpleadoID
                ORDER BY FechaHora";

            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@EmpleadoID", empleadoID);
                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        DateTime fechaHora = reader.GetDateTime(0);
                        string tipo = reader.GetString(1);
                        registros.Add(new RegistroAsistencia { FechaHora = fechaHora, Tipo = tipo });
                    }
                }
            }
            return registros;
        }
    }
    public class RegistroAsistencia
    {
        public DateTime FechaHora { get; set; }
        public string Tipo { get; set; }
    }
}
