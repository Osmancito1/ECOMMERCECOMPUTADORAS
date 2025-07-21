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
                    CargarDepartamentosEnDropDown();
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
                   
                    Debug.WriteLine($"Error en CargarDatosUsuario: {ex}");
                }
            }
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
                       
                        Debug.WriteLine("Usuario no encontrado");
                        return;
                    }

                    Debug.WriteLine($"Usuario ID encontrado: {usuID}");

                    // Verificar si hay productos en cualquier carrito
                    if (!ExistenProductosEnCarritos(connection, usuID))
                    {
                       
                        Debug.WriteLine("No se encontraron productos en ningún carrito");
                        return;
                    }

                    int carID = ObtenerCarritoConProductos(connection, usuID);
                    if (carID == 0)
                    {
                        
                        return;
                    }

                    Debug.WriteLine($"Carrito ID a usar: {carID}");

                    DataTable dtProductos = ObtenerProductosCarrito(connection, carID);
                    if (dtProductos.Rows.Count == 0)
                    {
                        
                        return;
                    }

                    CalcularYMostrarTotales(dtProductos);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"ERROR: {ex}");
                    
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

        private int ObtenerCarritoConProductos(SqlConnection connection, int usuID, SqlTransaction transaction = null)
        {
            string query = @"SELECT TOP 1 c.CarID
                     FROM Carrito c
                     INNER JOIN CarritoDetalle cd ON c.CarID = cd.CarID
                     WHERE c.UsuID = @UsuID
                     ORDER BY c.FechCre DESC, c.CarID DESC";
            using (SqlCommand cmd = new SqlCommand(query, connection))
            {
                if (transaction != null)
                {
                    cmd.Transaction = transaction;
                }
                cmd.Parameters.AddWithValue("@UsuID", usuID);
                object result = cmd.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : 0;
            }
        }
        private int ObtenerUsuarioID(SqlConnection connection, string correo, SqlTransaction transaction = null)
        {
            string query = "SELECT UsuID FROM Usuarios WHERE CorUsu = @Correo AND EstUsu = 1";
            using (SqlCommand cmd = new SqlCommand(query, connection))
            {
                if (transaction != null)
                {
                    cmd.Transaction = transaction;
                }
                cmd.Parameters.AddWithValue("@Correo", correo);
                object result = cmd.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : 0;
            }
        }

        private DataTable ObtenerProductosCarrito(SqlConnection connection, int carID, SqlTransaction transaction = null)
        {
            string query = @"SELECT 
                        p.ProID, p.NomPro, p.ImaPro, cd.CantPro, cd.PrecUni, 
                        (cd.CantPro * cd.PrecUni) AS Subtotal
                     FROM CarritoDetalle cd
                     INNER JOIN Productos p ON cd.ProID = p.ProID
                     WHERE cd.CarID = @CarID
                     ORDER BY cd.FechAg DESC";
            using (SqlCommand cmd = new SqlCommand(query, connection))
            {
                if (transaction != null)
                {
                    cmd.Transaction = transaction;
                }
                cmd.Parameters.AddWithValue("@CarID", carID);
                DataTable dt = new DataTable();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(dt);
                return dt;
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

        protected void btnAplicarPromo_Click(object sender, EventArgs e)
        {
            // Lógica para validar y aplicar el código de promoción.
            // Por ejemplo:
            string codigo = txtPromoCode.Text.Trim();
            if (!string.IsNullOrEmpty(codigo))
            {
                // Aquí deberías validar el código contra la base de datos.
                lblMensajePromo.Visible = true;
                lblMensajePromo.Text = $"Código '{codigo}' aplicado (lógica pendiente).";
                // Una vez aplicado, deberías volver a calcular los totales.
            }
            else
            {
                lblMensajePromo.Visible = true;
                lblMensajePromo.Text = "Por favor, ingresa un código.";
            }
        }

        protected void btnVolver_Click(object sender, EventArgs e)
        {
            // Redirige al usuario de vuelta a la página del carrito.
            Response.Redirect("Carrito.aspx");
        }

        protected void btnContinuar_Click(object sender, EventArgs e)
        {
            // 1. OBTENER DATOS DEL FORMULARIO Y SESIÓN
            string correoUsuario = Session["usuario"].ToString();
            string nomFact = txtFirstName.Text + " " + txtLastName.Text;
            string telFact = txtPhone.Text;
            string corFact = txtEmail.Text;

            // Concatenar la dirección completa
            string dirFact = string.Format("{0}, {1}, {2}, {3}",
                txtAddress.Text,
                ddlCity.SelectedItem.Text,
                ddlState.SelectedItem.Text,
                txtZipCode.Text);

            string connectionString = ConfigurationManager.ConnectionStrings["conexionDB"].ConnectionString;
            int nuevoPedID = 0;

            // 2. INICIAR CONEXIÓN Y TRANSACCIÓN
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                SqlTransaction trans = con.BeginTransaction();

                try
                {
                    // OBTENER UsuID
                    int usuID = ObtenerUsuarioID(con, correoUsuario, trans);
                    if (usuID == 0) throw new Exception("Usuario no encontrado.");

                    // 3. INSERTAR EL REGISTRO MAESTRO EN LA TABLA [Pedidos]
                    string queryPedido = @"INSERT INTO Pedidos 
                                    (NumPed, UsuID, NomFact, DirFact, TelFact, CorFact, NomEnv, DirEnv, TelEnv)
                                    OUTPUT INSERTED.PedID
                                    VALUES 
                                    (@NumPed, @UsuID, @NomFact, @DirFact, @TelFact, @CorFact, @NomEnv, @DirEnv, @TelEnv)";

                    using (SqlCommand cmdPedido = new SqlCommand(queryPedido, con, trans))
                    {
                        cmdPedido.Parameters.AddWithValue("@NumPed", Guid.NewGuid().ToString()); // Un número de pedido único
                        cmdPedido.Parameters.AddWithValue("@UsuID", usuID);
                        cmdPedido.Parameters.AddWithValue("@NomFact", nomFact);
                        cmdPedido.Parameters.AddWithValue("@DirFact", dirFact);
                        cmdPedido.Parameters.AddWithValue("@TelFact", telFact);
                        cmdPedido.Parameters.AddWithValue("@CorFact", corFact);
                        // Asumimos que la dirección de envío es la misma que la de facturación
                        cmdPedido.Parameters.AddWithValue("@NomEnv", nomFact);
                        cmdPedido.Parameters.AddWithValue("@DirEnv", dirFact);
                        cmdPedido.Parameters.AddWithValue("@TelEnv", telFact);

                        // Ejecutamos y obtenemos el ID del nuevo pedido
                        nuevoPedID = (int)cmdPedido.ExecuteScalar();
                    }

                    // 4. OBTENER LOS PRODUCTOS DEL CARRITO PARA LOS DETALLES
                    int carID = ObtenerCarritoConProductos(con, usuID, trans);
                    if (carID == 0) throw new Exception("No se encontró un carrito con productos.");

                    DataTable dtProductos = ObtenerProductosCarrito(con, carID, trans);
                    if (dtProductos.Rows.Count == 0) throw new Exception("El carrito está vacío.");

                    // 5. INSERTAR CADA PRODUCTO EN LA TABLA [PedidoDetalle]
                    foreach (DataRow row in dtProductos.Rows)
                    {
                        string queryDetalle = @"INSERT INTO PedidoDetalle 
                                        (PedID, ProID, NomPro, CantPro, PreUniPro)
                                        VALUES
                                        (@PedID, @ProID, @NomPro, @CantPro, @PreUniPro)";

                        using (SqlCommand cmdDetalle = new SqlCommand(queryDetalle, con, trans))
                        {
                            cmdDetalle.Parameters.AddWithValue("@PedID", nuevoPedID);
                            cmdDetalle.Parameters.AddWithValue("@ProID", Convert.ToInt32(row["ProID"]));
                            cmdDetalle.Parameters.AddWithValue("@NomPro", row["NomPro"].ToString());
                            cmdDetalle.Parameters.AddWithValue("@CantPro", Convert.ToInt32(row["CantPro"]));
                            cmdDetalle.Parameters.AddWithValue("@PreUniPro", Convert.ToDecimal(row["PrecUni"]));
                            cmdDetalle.ExecuteNonQuery();
                        }
                    }

                    // 6. SI TODO FUE EXITOSO, CONFIRMAR LA TRANSACCIÓN
                    trans.Commit();

                    // 7. GUARDAR EL ID DEL NUEVO PEDIDO Y REDIRIGIR
                    Session["PedID"] = nuevoPedID; // Guardamos el ID para usarlo en la página de pago
                    Response.Redirect("Pagos.aspx", false);
                    Context.ApplicationInstance.CompleteRequest();
                }
                catch (Exception ex)
                {
                    // EN CASO DE ERROR, REVERTIR TODO
                    trans.Rollback();
                   
                    Debug.WriteLine("Error en btnContinuar_Click: " + ex.ToString());
                }
            }
        }

        private void CargarDepartamentosEnDropDown()
        {
            DataTable dtDepartamentos = ObtenerDepartamentos();
            ddlState.DataSource = dtDepartamentos;

            // La columna que verá el usuario
            ddlState.DataTextField = "NomDep";

            // El valor interno que usarás (el ID)
            ddlState.DataValueField = "DepID";
            ddlState.DataBind();

            // Inserta un item por defecto al inicio de la lista
            ddlState.Items.Insert(0, new ListItem("Seleccionar departamento...", ""));
        }

        private DataTable ObtenerDepartamentos()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["conexionDB"].ConnectionString;
            DataTable dt = new DataTable();

            // Consulta SQL para traer los departamentos ordenados por nombre
            string query = "SELECT DepID, NomDep FROM Departamentos ORDER BY NomDep";

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    try
                    {
                        con.Open();
                        SqlDataAdapter da = new SqlDataAdapter(cmd);
                        da.Fill(dt);
                    }
                    catch (Exception ex)
                    {
                        // Manejo básico de errores, puedes mejorarlo según tus necesidades
                        Debug.WriteLine("Error al obtener departamentos: " + ex.Message);
                    }
                }
            }
            return dt;
        }

        private void CargarCiudadesEnDropDown(int depID)
        {
            // Limpia las ciudades anteriores
            ddlCity.Items.Clear();

            DataTable dtCiudades = ObtenerCiudadesPorDepartamento(depID);
            ddlCity.DataSource = dtCiudades;
            ddlCity.DataTextField = "NomCiu";
            ddlCity.DataValueField = "CiuID";
            ddlCity.DataBind();

            ddlCity.Items.Insert(0, new ListItem("Seleccionar ciudad...", ""));
        }

        private DataTable ObtenerCiudadesPorDepartamento(int depID)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["conexionDB"].ConnectionString;
            DataTable dt = new DataTable();
            // Consulta SQL para traer solo las ciudades del departamento seleccionado
            string query = "SELECT CiuID, NomCiu FROM Ciudades WHERE DepID = @DepID ORDER BY NomCiu";

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    // Agregamos el parámetro del ID del departamento
                    cmd.Parameters.AddWithValue("@DepID", depID);
                    try
                    {
                        con.Open();
                        SqlDataAdapter da = new SqlDataAdapter(cmd);
                        da.Fill(dt);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Error al obtener ciudades: " + ex.Message);
                    }
                }
            }
            return dt;
        }
        protected void ddlState_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(ddlState.SelectedValue))
            {
                int depID = Convert.ToInt32(ddlState.SelectedValue);

                // Carga las ciudades (lógica existente)
                CargarCiudadesEnDropDown(depID);
                ddlCity.Enabled = true;

                // --- CÓDIGO AÑADIDO ---
                // Obtiene y asigna el código postal
                string codigoPostal = ObtenerCodigoPostalPorDepartamento(depID);
                txtZipCode.Text = codigoPostal;
            }
            else
            {
                // Limpia las ciudades (lógica existente)
                ddlCity.Items.Clear();
                ddlCity.Enabled = false;

                // --- CÓDIGO AÑADIDO ---
                // Limpia también el código postal
                txtZipCode.Text = "";
            }
        }

        private string ObtenerCodigoPostalPorDepartamento(int depID)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["conexionDB"].ConnectionString;
            string codigoPostal = "";
            string query = "SELECT CodigoPostal FROM Departamentos WHERE DepID = @DepID";

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@DepID", depID);
                    try
                    {
                        con.Open();
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            codigoPostal = result.ToString();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Error al obtener código postal: " + ex.Message);
                    }
                }
            }
            return codigoPostal;
        }
    }
}