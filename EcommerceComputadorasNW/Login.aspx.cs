using System;
using System.Data.SqlClient;
using System.Web.UI;

namespace EcommerceComputadorasNW
{
    public partial class Login : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
        }

        protected void btnLogin_Click(object sender, EventArgs e)
        {
            string correo = txtEmail.Text.Trim();
            string contrasena = txtPassword.Text.Trim();

            if (string.IsNullOrEmpty(correo) || string.IsNullOrEmpty(contrasena))
            {
                MostrarToast("Por favor, complete todos los campos.", "error");
                return;
            }

            string connectionString = "Data Source=.;Initial Catalog=EcommerceComputadoras;Integrated Security=True";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "SELECT COUNT(*) FROM Usuarios WHERE CorUsu = @correo AND ConUsu = @contrasena AND EstUsu = 1";

                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@correo", correo);
                command.Parameters.AddWithValue("@contrasena", contrasena);

                connection.Open();
                int count = Convert.ToInt32(command.ExecuteScalar());

                if (count == 1)
                {
                    // Obtener el ID del usuario
                    string idQuery = "SELECT UsuID FROM Usuarios WHERE CorUsu = @correo";
                    SqlCommand idCmd = new SqlCommand(idQuery, connection);
                    idCmd.Parameters.AddWithValue("@correo", correo);
                    object idResult = idCmd.ExecuteScalar();

                    if (idResult != null)
                    {
                        int userId = Convert.ToInt32(idResult);
                        Session["UserID"] = userId;  // Guardar UserID en sesión
                    }

                    Session["usuario"] = correo;

                    MostrarToast("¡Inicio de sesión exitoso!", "success");
                    string script = "setTimeout(function(){ window.location = 'Default.aspx'; }, 2000);";
                    ClientScript.RegisterStartupScript(this.GetType(), "redirect", script, true);
                }
                else
                {
                    MostrarToast("Correo o contraseña incorrectos.", "error");
                }
            }
        }

        private void MostrarToast(string mensaje, string tipo)
        {
            string cssClass = tipo == "success" ? "toast-success" : "toast-error";

            string toastHtml = $@"
                <div id='toastMessage' class='toast-container {cssClass} show'>
                    <span>{mensaje}</span>
                    <button class='toast-close' onclick='this.parentElement.style.display=""none"";'>&times;</button>
                </div>";

            litToast.Text = toastHtml;
        }
    }
}
