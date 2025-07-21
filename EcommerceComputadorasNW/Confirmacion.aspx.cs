// Asegúrate de tener todas estas directivas al inicio del archivo
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Web;
using System.Web.UI;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace EcommerceComputadorasNW
{
    public partial class Confirmacion : System.Web.UI.Page
    {
        private string connectionString = ConfigurationManager.ConnectionStrings["conexionDB"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (Request.QueryString["pedido"] == null)
                {
                    Response.Redirect("Default.aspx");
                    return;
                }

                int pedID = Convert.ToInt32(Request.QueryString["pedido"]);
                CargarDatosConfirmacion(pedID);
            }
        }

        private void CargarDatosConfirmacion(int pedID)
        {
            DataSet dsPedido = ObtenerDatosPedido(pedID);
            if (dsPedido.Tables.Count < 2 || dsPedido.Tables[0].Rows.Count == 0) return;

            DataRow pedidoInfo = dsPedido.Tables[0].Rows[0];
            DataTable dtDetalles = dsPedido.Tables[1];

            // Llenar información general
            ltlNumeroPedido.Text = pedidoInfo["NumPed"].ToString();
            ltlFechaPedido.Text = Convert.ToDateTime(pedidoInfo["FechPed"]).ToString("dd/MM/yyyy");
            ltlNombreCliente.Text = pedidoInfo["NomFact"].ToString();
            ltlDireccionEnvio.Text = pedidoInfo["DirEnv"].ToString().Replace(", ", "<br/>");

            // Llenar productos
            rptProductosConfirmacion.DataSource = dtDetalles;
            rptProductosConfirmacion.DataBind();

            // Calcular y mostrar totales
            decimal subtotal = 0;
            foreach (DataRow row in dtDetalles.Rows)
            {
                subtotal += Convert.ToDecimal(row["Subtotal"]);
            }
            decimal costoEnvio = Convert.ToDecimal(pedidoInfo["CostoEnvio"]);
            decimal impuestos = subtotal * 0.15m; // ISV 15%
            decimal total = subtotal + costoEnvio + impuestos;

            ltlSubtotal.Text = subtotal.ToString("C");
            ltlEnvio.Text = costoEnvio.ToString("C");
            ltlImpuestos.Text = impuestos.ToString("C");
            ltlTotal.Text = total.ToString("C");
        }

        protected void btnImprimirFactura_Click(object sender, EventArgs e)
        {
            int pedID = Convert.ToInt32(Request.QueryString["pedido"]);
            DataRow facturaInfo = CrearFacturaEnBD(pedID);
            DataSet dsPedido = ObtenerDatosPedido(pedID);

            // Generar y descargar el PDF
            GenerarPdfFactura(facturaInfo, dsPedido);
        }

        private DataRow CrearFacturaEnBD(int pedID)
        {
            // En un sistema real, el CAI, Rango y Número vendrían de un sistema de facturación autorizado por el SAR.
            // Aquí lo simulamos.
            string caiSimulado = "A1B2C3-D4E5F6-G7H8I9-J0K1L2-M3N4O5-P6";
            string rangoAutorizadoSimulado = "000-001-01-00000001 AL 000-001-01-00000100";
            DateTime fechaLimiteSimulada = DateTime.Now.AddMonths(6);

            // Obtener totales
            DataSet ds = ObtenerDatosPedido(pedID);
            DataTable dtDetalles = ds.Tables[1];
            decimal subtotal = 0;
            foreach (DataRow row in dtDetalles.Rows) subtotal += Convert.ToDecimal(row["Subtotal"]);
            decimal impuestos = subtotal * 0.15m;
            decimal total = subtotal + impuestos + Convert.ToDecimal(ds.Tables[0].Rows[0]["CostoEnvio"]);

            string query = @"
                IF NOT EXISTS (SELECT 1 FROM Facturas WHERE PedID = @PedID)
                BEGIN
                    INSERT INTO Facturas (PedID, NumeroFactura, CAI, FechaLimiteEmision, RangoAutorizado, Subtotal, ISV, Total)
                    OUTPUT INSERTED.*
                    VALUES (@PedID, NEXT VALUE FOR SeqNumeroFactura, @CAI, @FechaLimite, @Rango, @Subtotal, @ISV, @Total);
                END
                ELSE
                BEGIN
                    SELECT * FROM Facturas WHERE PedID = @PedID;
                END";

            // NOTA: Necesitarás crear una secuencia en tu BD para el número de factura autoincremental:
            // CREATE SEQUENCE SeqNumeroFactura START WITH 1 INCREMENT BY 1;

            DataTable dtFactura = new DataTable();
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@PedID", pedID);
                    cmd.Parameters.AddWithValue("@CAI", caiSimulado);
                    cmd.Parameters.AddWithValue("@FechaLimite", fechaLimiteSimulada);
                    cmd.Parameters.AddWithValue("@Rango", rangoAutorizadoSimulado);
                    cmd.Parameters.AddWithValue("@Subtotal", subtotal);
                    cmd.Parameters.AddWithValue("@ISV", impuestos);
                    cmd.Parameters.AddWithValue("@Total", total);
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(dtFactura);
                }
            }
            return dtFactura.Rows[0];
        }

        private DataSet ObtenerDatosPedido(int pedID)
        {
            DataSet ds = new DataSet();
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                // Obtener datos del pedido
                string queryPedido = "SELECT * FROM Pedidos WHERE PedID = @PedID";
                SqlDataAdapter daPedido = new SqlDataAdapter(queryPedido, con);
                daPedido.SelectCommand.Parameters.AddWithValue("@PedID", pedID);
                daPedido.Fill(ds, "Pedido");

                // Obtener detalles del pedido
                string queryDetalles = "SELECT *, (CantPro * PreUniPro) AS Subtotal FROM PedidoDetalle WHERE PedID = @PedID";
                SqlDataAdapter daDetalles = new SqlDataAdapter(queryDetalles, con);
                daDetalles.SelectCommand.Parameters.AddWithValue("@PedID", pedID);
                daDetalles.Fill(ds, "Detalles");
            }
            return ds;
        }

        private void GenerarPdfFactura(DataRow facturaInfo, DataSet dsPedido)
        {
            DataRow pedidoInfo = dsPedido.Tables["Pedido"].Rows[0];
            DataTable dtDetalles = dsPedido.Tables["Detalles"];

            using (MemoryStream ms = new MemoryStream())
            {
                Document document = new Document(PageSize.A4, 25, 25, 30, 30);
                PdfWriter writer = PdfWriter.GetInstance(document, ms);
                document.Open();

                // Fuentes
                var titleFont = FontFactory.GetFont("Arial", 18, Font.BOLD);
                var boldFont = FontFactory.GetFont("Arial", 10, Font.BOLD);
                var standardFont = FontFactory.GetFont("Arial", 10, Font.NORMAL);

                // Cabecera
                document.Add(new Paragraph("FACTURA", titleFont) { Alignment = Element.ALIGN_CENTER });
                document.Add(new Paragraph("TechStore S.A.", boldFont));
                document.Add(new Paragraph("Col. Alameda, Tegucigalpa, Honduras", standardFont));
                document.Add(Chunk.NEWLINE);

                // Datos de la factura
                document.Add(new Paragraph($"Factura No.: {facturaInfo["NumeroFactura"]}", boldFont));
                document.Add(new Paragraph($"Fecha Emisión: {Convert.ToDateTime(facturaInfo["FechaEmision"]):dd/MM/yyyy}", standardFont));
                document.Add(new Paragraph($"CAI: {facturaInfo["CAI"]}", standardFont));
                document.Add(new Paragraph($"Rango Autorizado: {facturaInfo["RangoAutorizado"]}", standardFont));
                document.Add(new Paragraph($"Fecha Límite Emisión: {Convert.ToDateTime(facturaInfo["FechaLimiteEmision"]):dd/MM/yyyy}", standardFont));
                document.Add(Chunk.NEWLINE);

                // Datos del cliente
                document.Add(new Paragraph("Cliente:", boldFont));
                document.Add(new Paragraph(pedidoInfo["NomFact"].ToString(), standardFont));
                document.Add(new Paragraph(pedidoInfo["DirFact"].ToString(), standardFont));
                document.Add(Chunk.NEWLINE);

                // Tabla de productos
                PdfPTable table = new PdfPTable(4);
                table.WidthPercentage = 100;
                table.AddCell(new Phrase("Descripción", boldFont));
                table.AddCell(new Phrase("Cantidad", boldFont));
                table.AddCell(new Phrase("Precio Unitario", boldFont));
                table.AddCell(new Phrase("Total", boldFont));

                foreach (DataRow row in dtDetalles.Rows)
                {
                    table.AddCell(new Phrase(row["NomPro"].ToString(), standardFont));
                    table.AddCell(new Phrase(row["CantPro"].ToString(), standardFont));
                    table.AddCell(new Phrase(Convert.ToDecimal(row["PreUniPro"]).ToString("C"), standardFont));
                    table.AddCell(new Phrase(Convert.ToDecimal(row["Subtotal"]).ToString("C"), standardFont));
                }
                document.Add(table);
                document.Add(Chunk.NEWLINE);

                // Totales
                document.Add(new Paragraph($"Subtotal: {Convert.ToDecimal(facturaInfo["Subtotal"]):C}", standardFont) { Alignment = Element.ALIGN_RIGHT });
                document.Add(new Paragraph($"ISV (15%): {Convert.ToDecimal(facturaInfo["ISV"]):C}", standardFont) { Alignment = Element.ALIGN_RIGHT });
                document.Add(new Paragraph($"Total a Pagar: {Convert.ToDecimal(facturaInfo["Total"]):C}", boldFont) { Alignment = Element.ALIGN_RIGHT });

                document.Close();
                writer.Close();

                Response.ContentType = "application/pdf";
                Response.AddHeader("content-disposition", "attachment;filename=Factura-" + facturaInfo["NumeroFactura"] + ".pdf");
                Response.Cache.SetCacheability(HttpCacheability.NoCache);
                Response.BinaryWrite(ms.ToArray());
                Response.End();
            }
        }
    }
}