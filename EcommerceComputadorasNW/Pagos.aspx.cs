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
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // Validamos que exista un PedID en la sesión
                if (Session["PedID"] == null)
                {
                    // Si no hay ID de pedido, redirigimos al inicio o al carrito
                    Response.Redirect("Default.aspx");
                    return;
                }

                int pedID = Convert.ToInt32(Session["PedID"]);
                CargarResumenPedido(pedID);
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