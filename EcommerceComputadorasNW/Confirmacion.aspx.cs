// Asegúrate de tener todas estas directivas al inicio del archivo
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.draw;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Web;
using System.Web.UI;

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
                Document document = new Document(PageSize.A4, 40, 40, 40, 40);
                PdfWriter writer = PdfWriter.GetInstance(document, ms);
                document.Open();

                // Fuentes estilizadas
                var titleFont = FontFactory.GetFont("Helvetica", 20, Font.BOLD, BaseColor.DARK_GRAY);
                var boldFont = FontFactory.GetFont("Helvetica", 11, Font.BOLD);
                var standardFont = FontFactory.GetFont("Helvetica", 10, Font.NORMAL);
                var whiteBold = FontFactory.GetFont("Helvetica", 10, Font.BOLD, BaseColor.WHITE);

                // Título centrado
                Paragraph title = new Paragraph("FACTURA", titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 15f
                };
                document.Add(title);

                // Datos de empresa
                document.Add(new Paragraph("TechStore S.A.", boldFont));
                document.Add(new Paragraph("Col. Alameda, Tegucigalpa, Honduras", standardFont));
                document.Add(new Paragraph("Tel: +504 2222-2222 | techstore@correo.com", standardFont));
                document.Add(Chunk.NEWLINE);

                // Línea separadora
                LineSeparator line = new LineSeparator(1f, 100f, BaseColor.LIGHT_GRAY, Element.ALIGN_CENTER, -2);
                document.Add(new Chunk(line));
                document.Add(Chunk.NEWLINE);

                // Info de Factura
                PdfPTable facturaTable = new PdfPTable(2);
                facturaTable.WidthPercentage = 100;
                facturaTable.SetWidths(new float[] { 30, 70 });

                facturaTable.AddCell(Cell("Factura No.:", boldFont));
                facturaTable.AddCell(Cell($"{facturaInfo["NumeroFactura"]}", standardFont));

                facturaTable.AddCell(Cell("Fecha Emisión:", boldFont));
                facturaTable.AddCell(Cell($"{Convert.ToDateTime(facturaInfo["FechaEmision"]):dd/MM/yyyy}", standardFont));

                facturaTable.AddCell(Cell("CAI:", boldFont));
                facturaTable.AddCell(Cell(facturaInfo["CAI"].ToString(), standardFont));

                facturaTable.AddCell(Cell("Rango Autorizado:", boldFont));
                facturaTable.AddCell(Cell(facturaInfo["RangoAutorizado"].ToString(), standardFont));

                facturaTable.AddCell(Cell("Fecha Límite Emisión:", boldFont));
                facturaTable.AddCell(Cell($"{Convert.ToDateTime(facturaInfo["FechaLimiteEmision"]):dd/MM/yyyy}", standardFont));

                document.Add(facturaTable);
                document.Add(Chunk.NEWLINE);

                // Datos del Cliente
                Paragraph clienteHeader = new Paragraph("Datos del Cliente", boldFont)
                {
                    SpacingBefore = 10f,
                    SpacingAfter = 5f
                };
                document.Add(clienteHeader);

                document.Add(new Paragraph(pedidoInfo["NomFact"].ToString(), standardFont));
                document.Add(new Paragraph(pedidoInfo["DirFact"].ToString(), standardFont));
                document.Add(Chunk.NEWLINE);

                // Tabla de Productos
                PdfPTable productosTable = new PdfPTable(4);
                productosTable.WidthPercentage = 100;
                productosTable.SetWidths(new float[] { 50, 15, 17, 18 });

                // Header con fondo
                BaseColor headerColor = new BaseColor(99, 102, 241);
                productosTable.AddCell(HeaderCell("Descripción", whiteBold, headerColor));
                productosTable.AddCell(HeaderCell("Cantidad", whiteBold, headerColor));
                productosTable.AddCell(HeaderCell("Precio Unitario", whiteBold, headerColor));
                productosTable.AddCell(HeaderCell("Total", whiteBold, headerColor));

                foreach (DataRow row in dtDetalles.Rows)
                {
                    productosTable.AddCell(Cell(row["NomPro"].ToString(), standardFont));
                    productosTable.AddCell(Cell(row["CantPro"].ToString(), standardFont, Element.ALIGN_CENTER));
                    productosTable.AddCell(Cell(Convert.ToDecimal(row["PreUniPro"]).ToString("C"), standardFont, Element.ALIGN_RIGHT));
                    productosTable.AddCell(Cell(Convert.ToDecimal(row["Subtotal"]).ToString("C"), standardFont, Element.ALIGN_RIGHT));
                }

                document.Add(productosTable);
                document.Add(Chunk.NEWLINE);

                // Totales
                PdfPTable totales = new PdfPTable(2);
                totales.WidthPercentage = 40;
                totales.HorizontalAlignment = Element.ALIGN_RIGHT;
                totales.SetWidths(new float[] { 50, 50 });

                totales.AddCell(Cell("Subtotal:", boldFont, Element.ALIGN_LEFT, BaseColor.WHITE, noBorder: true));
                totales.AddCell(Cell(Convert.ToDecimal(facturaInfo["Subtotal"]).ToString("C"), standardFont, Element.ALIGN_RIGHT, BaseColor.WHITE, noBorder: true));

                totales.AddCell(Cell("ISV (15%):", boldFont, Element.ALIGN_LEFT, BaseColor.WHITE, noBorder: true));
                totales.AddCell(Cell(Convert.ToDecimal(facturaInfo["ISV"]).ToString("C"), standardFont, Element.ALIGN_RIGHT, BaseColor.WHITE, noBorder: true));

                totales.AddCell(Cell("Total a Pagar:", boldFont, Element.ALIGN_LEFT, BaseColor.WHITE, noBorder: true));
                totales.AddCell(Cell(Convert.ToDecimal(facturaInfo["Total"]).ToString("C"), boldFont, Element.ALIGN_RIGHT, BaseColor.WHITE, noBorder: true));

                document.Add(totales);

                // Finalizar documento
                document.Close();
                writer.Close();

                Response.ContentType = "application/pdf";
                Response.AddHeader("content-disposition", "attachment;filename=Factura-" + facturaInfo["NumeroFactura"] + ".pdf");
                Response.Cache.SetCacheability(HttpCacheability.NoCache);
                Response.BinaryWrite(ms.ToArray());
                Response.End();
            }
        }

        // Helper para celdas normales
        private PdfPCell Cell(string text, Font font, int align = Element.ALIGN_LEFT, BaseColor bg = null, bool noBorder = false)
        {
            var cell = new PdfPCell(new Phrase(text, font))
            {
                Padding = 5f,
                HorizontalAlignment = align,
                VerticalAlignment = Element.ALIGN_MIDDLE,
                Border = noBorder ? Rectangle.NO_BORDER : Rectangle.BOX
            };
            if (bg != null) cell.BackgroundColor = bg;
            return cell;
        }

        // Helper para encabezado
        private PdfPCell HeaderCell(string text, Font font, BaseColor bg)
        {
            var cell = new PdfPCell(new Phrase(text, font))
            {
                BackgroundColor = bg,
                Padding = 6f,
                HorizontalAlignment = Element.ALIGN_CENTER,
                VerticalAlignment = Element.ALIGN_MIDDLE
            };
            return cell;
        }

    }
}
