using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Web.Services.Description;
using System.Web.UI;
using System.Web.UI.WebControls;
using WebGrease.Activities;

namespace EcommerceComputadorasNW
{
    public partial class Pedido : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (Session["usuario"] == null)
                {
                    Response.Redirect("Login.aspx");
                }
                else
                {
                    Debug.WriteLine($"Iniciando carga de pedido para: {Session["usuario"]}");
                    CargarDatosUsuario();
                    CargarResumenPedido();
                }
            }
        }

        private void CargarDatosUsuario()
        {
            string correoUsuario = Session["usuario"].ToString();
            string connectionString = ConfigurationManager.ConnectionStrings["conexionDB"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = @"SELECT NomUsu, CorUsu, TelUsu 
                               FROM Usuarios 
                               WHERE CorUsu = @Correo AND EstUsu = 1";

                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@Correo", correoUsuario);

                try
                {
                    connection.Open();
                    SqlDataReader reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        string[] nombres = reader["NomUsu"].ToString().Split(' ');
                        txtFirstName.Text = nombres.Length > 0 ? nombres[0] : "";
                        txtLastName.Text = nombres.Length > 1 ? nombres[1] : "";
                        txtEmail.Text = reader["CorUsu"].ToString();
                        txtPhone.Text = reader["TelUsu"].ToString();
                    }
                }
                catch (Exception ex)
                {
                    MostrarError("Error al cargar datos del usuario: " + ex.Message);
                    Debug.WriteLine($"Error en CargarDatosUsuario: {ex}");
                }
            }
        }

        private void MostrarError(string v)
        {
            lblError.Visible = true;
            lblError.Text = mensaje;
        }

        private void CargarResumenPedido()
        {
            string correoUsuario = Session["usuario"].ToString();
            string connectionString = ConfigurationManager.ConnectionStrings["conexionDB"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    Debug.WriteLine("Conexión a BD establecida");

                    int usuID = ObtenerUsuarioID(connection, correoUsuario);
                    if (usuID == 0)
                    {
                        MostrarError("Usuario no encontrado");
                        Debug.WriteLine("Usuario no encontrado");
                        return;
                    }

                    Debug.WriteLine($"Usuario ID encontrado: {usuID}");

                    // Verificar si hay productos en cualquier carrito
                    if (!ExistenProductosEnCarritos(connection, usuID))
                    {
                        MostrarError("No tienes productos en tu carrito");
                        Debug.WriteLine("No se encontraron productos en ningún carrito");
                        return;
                    }

                    int carID = ObtenerCarritoConProductos(connection, usuID);
                    if (carID == 0)
                    {
                        MostrarError("No tienes productos en tu carrito");
                        return;
                    }

                    Debug.WriteLine($"Carrito ID a usar: {carID}");

                    DataTable dtProductos = ObtenerProductosCarrito(connection, carID);
                    if (dtProductos.Rows.Count == 0)
                    {
                        MostrarError("Tu carrito está vacío");
                        return;
                    }

                    CalcularYMostrarTotales(dtProductos);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"ERROR: {ex}");
                    MostrarError("Error al cargar el carrito. Por favor intenta nuevamente.");
                }
            }
        }

        private bool ExistenProductosEnCarritos(SqlConnection connection, int usuID)
        {
            string query = @"SELECT COUNT(*) 
                           FROM CarritoDetalle cd
                           INNER JOIN Carrito c ON cd.CarID = c.CarID
                           WHERE c.UsuID = @UsuID";

            SqlCommand cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@UsuID", usuID);
            int count = Convert.ToInt32(cmd.ExecuteScalar());

            Debug.WriteLine($"Total de productos en todos los carritos: {count}");
            return count > 0;
        }

        private int ObtenerCarritoConProductos(SqlConnection connection, int usuID)
        {
            // 1. Buscar el carrito más reciente que tenga productos
            string query = @"SELECT TOP 1 c.CarID
                           FROM Carrito c
                           INNER JOIN CarritoDetalle cd ON c.CarID = cd.CarID
                           WHERE c.UsuID = @UsuID
                           ORDER BY c.FechCre DESC, c.CarID DESC";

            SqlCommand cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@UsuID", usuID);
            object result = cmd.ExecuteScalar();

            if (result != null)
            {
                int carID = Convert.ToInt32(result);
                Debug.WriteLine($"Carrito con productos encontrado: {carID}");
                return carID;
            }

            Debug.WriteLine("No se encontró ningún carrito con productos");
            return 0;
        }

        private int ObtenerUsuarioID(SqlConnection connection, string correo)
        {
            string query = "SELECT UsuID FROM Usuarios WHERE CorUsu = @Correo AND EstUsu = 1";
            SqlCommand cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Correo", correo);
            object result = cmd.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        private DataTable ObtenerProductosCarrito(SqlConnection connection, int carID)
        {
            Debug.WriteLine($"Obteniendo productos para carrito ID: {carID}");

            string query = @"SELECT 
                           p.NomPro, 
                           cd.CantPro, 
                           cd.PrecUni, 
                           (cd.CantPro * cd.PrecUni) AS Subtotal
                           FROM CarritoDetalle cd
                           INNER JOIN Productos p ON cd.ProID = p.ProID
                           WHERE cd.CarID = @CarID
                           ORDER BY cd.FechAg DESC";

            SqlCommand cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@CarID", carID);

            try
            {
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                Debug.WriteLine($"Productos encontrados: {dt.Rows.Count}");
                return dt;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al obtener productos: {ex.Message}");
                return new DataTable();
            }
        }

        private void CalcularYMostrarTotales(DataTable dtProductos)
        {
            decimal subtotal = 0;
            foreach (DataRow row in dtProductos.Rows)
            {
                subtotal += Convert.ToDecimal(row["Subtotal"]);
            }

            decimal envio = 0;
            decimal impuestos = subtotal * 0.16m;
            decimal total = subtotal + envio + impuestos;

            rptProductos.DataSource = dtProductos;
            rptProductos.DataBind();
            lblSubtotal.Text = subtotal.ToString("C");
            lblEnvio.Text = envio.ToString("C");
            lblImpuestos.Text = impuestos.ToString("C");
            lblTotal.Text = total.ToString("C");

            Debug.WriteLine($"Totales calculados - Subtotal: {subtotal}, Total: {total}");
        }

        // Resto de tus métodos (btnAplicarPromo_Click, btnVolver_Click, etc.)...
    }
}