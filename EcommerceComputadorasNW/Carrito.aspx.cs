using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web.UI;

namespace EcommerceComputadorasNW
{
    public partial class Carrito : System.Web.UI.Page
    {
        private string connectionString = ConfigurationManager.ConnectionStrings["conexionDB"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            CargarCarrito();

            string script = @"
            document.addEventListener('DOMContentLoaded', function () {
                const shippingOptions = document.querySelectorAll('input[name$=""shipping""]');
                shippingOptions.forEach(radio => {
                    radio.addEventListener('change', updateSummary);
                });
                updateSummary();
            });

            function updateSummary() {
                const subtotalSpan = document.getElementById('" + subtotal.ClientID + @"');
                let subtotalValue = parseFloat(subtotalSpan.innerText.replace(/[^0-9.-]+/g, ''));

                const selectedShipping = document.querySelector('input[name$=""shipping""]:checked');
                let shippingValue = 0;
                if (selectedShipping) {
                    const priceSpan = selectedShipping.nextElementSibling.querySelector('.shipping-price');
                    shippingValue = parseFloat(priceSpan.innerText.replace(/[^0-9.-]+/g, ''));
                }

                const discountSpan = document.getElementById('" + discount.ClientID + @"');
                let discountValue = parseFloat(discountSpan.innerText.replace(/[^0-9.-]+/g, ''));

                const totalValue = subtotalValue + shippingValue + discountValue;

                document.getElementById('" + shipping.ClientID + @"').innerText = '$' + shippingValue.toFixed(2);
                document.getElementById('" + total.ClientID + @"').innerText = '$' + totalValue.toFixed(2);
            }";

            ScriptManager.RegisterStartupScript(this, this.GetType(), "UpdateSummaryScript", script, true);
        }

        private void CargarCarrito()
        {
            if (Session["Carrito"] != null && ((DataTable)Session["Carrito"]).Rows.Count > 0)
            {
                DataTable carrito = (DataTable)Session["Carrito"];
                rptCarrito.DataSource = carrito;
                rptCarrito.DataBind();

                rptCarrito.Visible = true;
                pnlCarritoVacio.Visible = false;
                pnlCartActions.Visible = true;
                pnlResumen.Visible = true;
                btnPagar.Visible = true;

                decimal subtotalValue = carrito.AsEnumerable().Sum(r => Convert.ToDecimal(r["Subtotal"]));
                decimal shippingValue = 0.00m;
                decimal discountValue = 0.00m;
                decimal totalValue = subtotalValue + shippingValue - discountValue;

                subtotal.InnerText = subtotalValue.ToString("C");
                shipping.InnerText = shippingValue.ToString("C");
                discount.InnerText = "-" + discountValue.ToString("C");
                total.InnerText = totalValue.ToString("C");
            }
            else
            {
                rptCarrito.Visible = false;
                pnlCarritoVacio.Visible = true;
                pnlCartActions.Visible = false;
                pnlResumen.Visible = false;
                btnPagar.Visible = false;
            }
        }

        protected void btnVaciarCarrito_Click(object sender, EventArgs e)
        {
            Session["Carrito"] = null;

            rptCarrito.Visible = false;
            pnlCarritoVacio.Visible = true;
            pnlCartActions.Visible = false;
            pnlResumen.Visible = false;
            btnPagar.Visible = false;

            subtotal.InnerText = "$0.00";
            discount.InnerText = "-$0.00";
            shipping.InnerText = "$0.00";
            total.InnerText = "$0.00";

            var badgeControl = this.Master.FindControl("cartCountBadge") as System.Web.UI.HtmlControls.HtmlGenericControl;
            if (badgeControl != null)
            {
                string script = $"updateCartBadge(0, '{badgeControl.ClientID}');";
                ScriptManager.RegisterStartupScript(this, this.GetType(), "cartCleared", script, true);
            }
        }

        protected void btnPagar_Click(object sender, EventArgs e)
        {
            if (Session["Carrito"] == null || ((DataTable)Session["Carrito"]).Rows.Count == 0)
            {
                Response.Write("No hay productos en el carrito.");
                return;
            }

            string correoUsuario = Session["usuario"]?.ToString();
            if (string.IsNullOrEmpty(correoUsuario))
            {
                Response.Write("Debe iniciar sesión para completar la compra.");
                return;
            }

            DataTable carrito = (DataTable)Session["Carrito"];

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                using (SqlTransaction trans = con.BeginTransaction())
                {
                    try
                    {
                        // Obtener ID del usuario a partir del correo
                        int usuarioID = 0;
                        using (SqlCommand cmdUsu = new SqlCommand("SELECT UsuID FROM Usuarios WHERE CorUsu = @correo", con, trans))
                        {
                            cmdUsu.Parameters.AddWithValue("@correo", correoUsuario);
                            object result = cmdUsu.ExecuteScalar();
                            if (result != null)
                                usuarioID = Convert.ToInt32(result);
                        }

                        if (usuarioID == 0)
                        {
                            throw new Exception("Usuario no encontrado.");
                        }

                        // Insertar carrito
                        int carritoID;
                        using (SqlCommand cmd = new SqlCommand(@"
                            INSERT INTO Carrito (UsuID, FechCre, EstCar)
                            OUTPUT INSERTED.CarID
                            VALUES (@UsuID, GETDATE(), 1)", con, trans))
                        {
                            cmd.Parameters.AddWithValue("@UsuID", usuarioID);
                            carritoID = (int)cmd.ExecuteScalar();
                        }

                        // Insertar detalles
                        foreach (DataRow row in carrito.Rows)
                        {
                            using (SqlCommand cmdDet = new SqlCommand(@"
                                INSERT INTO CarritoDetalle (CarID, ProID, CantPro, PrecUni, FechAg)
                                VALUES (@CarID, @ProID, @CantPro, @PrecUni, GETDATE())", con, trans))
                            {
                                cmdDet.Parameters.AddWithValue("@CarID", carritoID);
                                cmdDet.Parameters.AddWithValue("@ProID", row["ProID"]);
                                cmdDet.Parameters.AddWithValue("@CantPro", row["Cantidad"]);
                                cmdDet.Parameters.AddWithValue("@PrecUni", row["Precio"]);
                                cmdDet.ExecuteNonQuery();
                            }
                        }

                        trans.Commit();

                        Session["Carrito"] = null;

                        rptCarrito.Visible = false;
                        pnlCarritoVacio.Visible = true;
                        pnlCartActions.Visible = false;
                        pnlResumen.Visible = false;
                        btnPagar.Visible = false;

                        subtotal.InnerText = "$0.00";
                        discount.InnerText = "-$0.00";
                        shipping.InnerText = "$0.00";
                        total.InnerText = "$0.00";

                        var badgeControl = this.Master.FindControl("cartCountBadge") as System.Web.UI.HtmlControls.HtmlGenericControl;
                        if (badgeControl != null)
                        {
                            string script = $"updateCartBadge(0, '{badgeControl.ClientID}');";
                            ScriptManager.RegisterStartupScript(this, this.GetType(), "cartReset", script, true);
                        }

                        Response.Redirect("Pedido.aspx");
                    }
                    catch (Exception ex)
                    {
                        trans.Rollback();
                        Response.Write("Error al procesar el pago: " + ex.Message);
                    }
                }
            }
        }
    }
}
