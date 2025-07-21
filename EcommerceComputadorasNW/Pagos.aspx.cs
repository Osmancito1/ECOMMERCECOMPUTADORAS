using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace EcommerceComputadorasNW
{
    public partial class Pagos : System.Web.UI.Page
    {
        private string connectionString = ConfigurationManager.ConnectionStrings["conexionDB"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (Session["PedID"] == null || Session["usuario"] == null)
                {
                    Response.Redirect("Default.aspx");
                    return;
                }

                int pedID = Convert.ToInt32(Session["PedID"]);
                string correoUsuario = Session["usuario"].ToString();

                CargarResumenPedido(pedID);
                CargarDireccionFacturacion(pedID);
                CargarTarjetasGuardadas(correoUsuario);
            }
        }
        private void CargarDireccionFacturacion(int pedID)
        {
            string direccion = "";
            string query = "SELECT DirFact FROM Pedidos WHERE PedID = @PedID";
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@PedID", pedID);
                    con.Open();
                    object result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        direccion = result.ToString().Replace(", ", "<br>");
                    }
                }
            }
            ltlDireccionFacturacion.Text = $"<p>{direccion}</p>";
        }

        private void CargarTarjetasGuardadas(string correo)
        {
            int usuID = ObtenerUsuarioID(correo);
            if (usuID == 0) return;

            DataTable dtTarjetas = new DataTable();
            string query = "SELECT TarjetaID, TipoTarjeta, NombreTitular, UltimosCuatro, FechaExp FROM Tarjetas WHERE UsuID = @UsuID";
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@UsuID", usuID);
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(dtTarjetas);
                }
            }
            rptTarjetasGuardadas.DataSource = dtTarjetas;
            rptTarjetasGuardadas.DataBind();
        }

        protected void btnProcesarPago_Click(object sender, EventArgs e)
        {
            int pedID = Convert.ToInt32(Session["PedID"]);
            decimal montoTotal = decimal.Parse(lblTotalPago.Text, System.Globalization.NumberStyles.Currency);

            // Simulación de pago
            bool pagoExitoso = true; // En un caso real, aquí se llamaría a la pasarela de pago

            if (pagoExitoso)
            {
                // Si se usó una nueva tarjeta y se marcó para guardar
                string opcionTarjeta = Request.Form["savedCard"];
                if (opcionTarjeta == "new" && chkSaveCard.Checked)
                {
                    GuardarTarjeta();
                }

                RegistrarPago(pedID, montoTotal, "Tarjeta");
                ActualizarEstadoPedido(pedID, "Pagado");

                // Limpiar sesiones y redirigir a confirmación
                Session.Remove("PedID");
                Session.Remove("Carrito"); // Limpiar carrito de la sesión
                Response.Redirect("Confirmacion.aspx?pedido=" + pedID);
            }
            else
            {
                RegistrarPago(pedID, montoTotal, "Tarjeta", "Rechazado");
                ActualizarEstadoPedido(pedID, "Error de pago");
                // Mostrar mensaje de error
            }
        }

        private void GuardarTarjeta()
        {
            string correo = Session["usuario"].ToString();
            int usuID = ObtenerUsuarioID(correo);
            if (usuID == 0) return;

            string numeroTarjeta = txtCardNumber.Text.Replace(" ", "");
            string ultimosCuatro = numeroTarjeta.Length > 4 ? numeroTarjeta.Substring(numeroTarjeta.Length - 4) : numeroTarjeta;

            // Lógica simple para determinar el tipo de tarjeta
            string tipoTarjeta = "card";
            if (numeroTarjeta.StartsWith("4")) tipoTarjeta = "visa";
            else if (numeroTarjeta.StartsWith("5")) tipoTarjeta = "mastercard";

            string query = @"INSERT INTO Tarjetas (UsuID, TipoTarjeta, NombreTitular, UltimosCuatro, FechaExp)
                             VALUES (@UsuID, @TipoTarjeta, @NombreTitular, @UltimosCuatro, @FechaExp)";

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@UsuID", usuID);
                    cmd.Parameters.AddWithValue("@TipoTarjeta", tipoTarjeta);
                    cmd.Parameters.AddWithValue("@NombreTitular", txtCardName.Text);
                    cmd.Parameters.AddWithValue("@UltimosCuatro", ultimosCuatro);
                    cmd.Parameters.AddWithValue("@FechaExp", txtExpiryDate.Text);
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void RegistrarPago(int pedID, decimal monto, string metodo, string estado = "Aprobado")
        {
            string query = @"INSERT INTO Pagos (PedID, Monto, MetodoPago, EstadoPago, TransaccionID)
                             VALUES (@PedID, @Monto, @MetodoPago, @EstadoPago, @TransaccionID)";
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@PedID", pedID);
                    cmd.Parameters.AddWithValue("@Monto", monto);
                    cmd.Parameters.AddWithValue("@MetodoPago", metodo);
                    cmd.Parameters.AddWithValue("@EstadoPago", estado);
                    cmd.Parameters.AddWithValue("@TransaccionID", Guid.NewGuid().ToString()); // ID de transacción simulado
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void ActualizarEstadoPedido(int pedID, string nuevoEstado)
        {
            string query = "UPDATE Pedidos SET EstPed = @Estado WHERE PedID = @PedID";
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Estado", nuevoEstado);
                    cmd.Parameters.AddWithValue("@PedID", pedID);
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private int ObtenerUsuarioID(string correo)
        {
            string query = "SELECT UsuID FROM Usuarios WHERE CorUsu = @Correo";
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Correo", correo);
                    con.Open();
                    object result = cmd.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : 0;
                }
            }
        }
        private void CargarResumenPedido(int pedID)
        {
            try
            {
                DataTable dtDetalles = ObtenerDetallesDelPedido(pedID);
                if (dtDetalles.Rows.Count == 0)
                {
                    // Manejar el caso de que el pedido no tenga detalles
                    return;
                }

                // Enlazar los productos al Repeater
                rptProductosPago.DataSource = dtDetalles;
                rptProductosPago.DataBind();

                // Calcular totales
                decimal subtotal = dtDetalles.AsEnumerable().Sum(row => row.Field<decimal>("Subtotal"));
                decimal costoEnvio = ObtenerCostoEnvio(pedID); // Obtener costo de envío desde la tabla Pedidos
                decimal impuestos = subtotal * 0.16m; // O el cálculo que prefieras
                decimal total = subtotal + costoEnvio + impuestos;

                // Mostrar totales en los Literal
                lblSubtotalPago.Text = subtotal.ToString("C");
                lblEnvioPago.Text = costoEnvio.ToString("C");
                lblImpuestosPago.Text = impuestos.ToString("C");
                lblTotalPago.Text = total.ToString("C");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error al cargar el resumen del pedido: " + ex.Message);
                // Aquí podrías mostrar un mensaje de error al usuario
            }
        }

        private DataTable ObtenerDetallesDelPedido(int pedID)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["conexionDB"].ConnectionString;
            DataTable dt = new DataTable();
            string query = @"SELECT NomPro, CantPro, PreUniPro, (CantPro * PreUniPro) AS Subtotal 
                     FROM PedidoDetalle 
                     WHERE PedID = @PedID";

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@PedID", pedID);
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(dt);
                }
            }
            return dt;
        }

        private decimal ObtenerCostoEnvio(int pedID)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["conexionDB"].ConnectionString;
            decimal costoEnvio = 0m;
            string query = "SELECT CostoEnvio FROM Pedidos WHERE PedID = @PedID";

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@PedID", pedID);
                    con.Open();
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        costoEnvio = Convert.ToDecimal(result);
                    }
                }
            }
            return costoEnvio;
        }
    }
}