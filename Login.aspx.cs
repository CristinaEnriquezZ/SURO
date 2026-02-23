using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SURO2
{
    public partial class Login : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
          

            if (Session["LoginError"] != null)
            {
                lblMensaje.Text = Session["LoginError"].ToString();
                Session.Remove("LoginError");
            }


   
        }

        protected void btnEntrar_Click(object sender, EventArgs e)
        {

            Funciones funciones = new Funciones();
            funciones.ConsultaUsuario(0, txtUser.Text, txtPass.Text, 1);


        }




        protected void btnEnviarCodigo_Click(object sender, EventArgs e)
        {
            string usuario = txtUsuarioRecuperar.Text.Trim();
            if (string.IsNullOrEmpty(usuario))
            {
                lblModalMensaje.Text = "Debes ingresar tu usuario.";
                //updModalRecuperar.Update();
                ScriptManager.RegisterStartupScript(this, this.GetType(), "abrirModal", "$('#modalRecuperar').modal('show');", true);
                return;
            }

            int idUsuario = 0;
            string correo = "";

            try
            {
                Funciones funciones = new Funciones();
                using (SqlConnection con = funciones.ConBD())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_ModificarUsuario", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Operacion", 1);
                        cmd.Parameters.AddWithValue("@UserName", usuario);
                        cmd.Parameters.AddWithValue("@idUsuario", DBNull.Value);
                        cmd.Parameters.AddWithValue("@nuevaPassword", DBNull.Value);

                        if (con.State == ConnectionState.Closed)
                            con.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                idUsuario = reader.GetInt32(reader.GetOrdinal("idUsuario"));
                                correo = reader.GetString(reader.GetOrdinal("Correo"));
                            }
                        }
                    }
                }

                if (idUsuario == 0 || string.IsNullOrEmpty(correo))
                {
                    lblModalMensaje.Text = "Usuario no encontrado o inactivo.";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "abrirModal", "$('#modalRecuperar').modal('show');", true);

                    return;
                }

                string codigo = GenerarCodigo();
                Session["CodigoVerificacion"] = codigo;
                Session["idUsuarioVerificacion"] = idUsuario;
                Session["CorreoVerificacion"] = correo;

                bool enviado = EnviarCodigoPorCorreo(correo, codigo);

                if (enviado)
                {
                    lblModalMensaje.Text = "Se envió un código a tu correo registrado.";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "abrirModal", "$('#modalRecuperar').modal('show');", true);
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "timerCodigo", "iniciarTimerCodigo();", true);

                    //divCodigo.Style["display"] = "block";
                }
                else
                {
                    lblModalMensaje.Text = "No se pudo enviar el código. Comunicate con Sistemas.";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "abrirModal", "$('#modalRecuperar').modal('show');", true);

                }
            }
            catch (Exception ex)
            {
                lblModalMensaje.Text = "Error en la recuperación: " + ex.Message;
            }
        }


        public string GenerarCodigo()
        {
            Random r = new Random();
            return r.Next(100000, 999999).ToString();
        }

        public bool EnviarCodigoPorCorreo(string correo, string codigo)
        {
            try
            {
                // DATOS SMTP, MODIFÍCALOS SEGÚN TU PROVEEDOR
                string smtpServer = "smtps.chihuahua.gob.mx";
                int smtpPort = 587;
                string smtpUser = "soporte.sdr@chihuahua.gob.mx";
                string smtpPass = "FdFaZxJ8";

                // Construir mensaje
                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(smtpUser, "Recuperación SURO");
                mail.To.Add(correo);
                mail.Subject = "Código de verificación - Recuperación de contraseña";
                mail.Body = $"Tu código de verificación es: {codigo}\n\nSi no solicitaste este código, ignora este correo.";
                mail.IsBodyHtml = false;

                // Configuración cliente SMTP
                SmtpClient smtp = new SmtpClient(smtpServer, smtpPort)
                {
                    Credentials = new NetworkCredential(smtpUser, smtpPass),
                    EnableSsl = true
                };

                smtp.Send(mail);
                return true;
            }
            catch (Exception ex)
            {
                // Puedes loguear el error: ex.Message
                System.Diagnostics.Debug.WriteLine($"Error capturado: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");

                return false;
            }
        }


        protected void btnCambiarPass_Click(object sender, EventArgs e)
        {
            // Recuperar datos ingresados por el usuario
            string codigoIngresado = txtCodigo.Text.Trim();
            string nuevaPass = txtNuevaPass.Text.Trim();

            // Recuperar datos guardados en sesión
            string codigoEsperado = Session["CodigoVerificacion"] as string;
            int? idUsuario = Session["idUsuarioVerificacion"] as int?;
            string correo = Session["CorreoVerificacion"] as string;

            // Validaciones básicas
            if (string.IsNullOrEmpty(codigoIngresado) || string.IsNullOrEmpty(nuevaPass))
            {
                lblModalMensaje.Text = "Debes ingresar el código y la nueva contraseña.";
                ScriptManager.RegisterStartupScript(this, this.GetType(), "abrirModal", "$('#modalRecuperar').modal('show');", true);

                return;
            }

            if (codigoEsperado == null || idUsuario == null)
            {
                lblModalMensaje.Text = "Ha ocurrido un error interno. Intenta recuperar la contraseña desde el inicio.";
                ScriptManager.RegisterStartupScript(this, this.GetType(), "abrirModal", "$('#modalRecuperar').modal('show');", true);

                return;
            }

            // Validar código
            if (codigoIngresado != codigoEsperado)
            {
                lblModalMensaje.Text = "El código de verificación es incorrecto.";
                ScriptManager.RegisterStartupScript(this, this.GetType(), "abrirModal", "$('#modalRecuperar').modal('show');", true);

                return;
            }

            // Opcional: puedes agregar validación de fuerza de contraseña aquí
            if (nuevaPass.Length < 6)
            {
                lblModalMensaje.Text = "La contraseña debe tener al menos 6 caracteres.";
                ScriptManager.RegisterStartupScript(this, this.GetType(), "abrirModal", "$('#modalRecuperar').modal('show');", true);

                return;
            }

            // Cambiar contraseña en BD usando tu SP
            try
            {
                Funciones funciones = new Funciones();
                using (SqlConnection con = funciones.ConBD())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_ModificarUsuario", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Operacion", 2);
                        cmd.Parameters.AddWithValue("@UserName", DBNull.Value);
                        cmd.Parameters.AddWithValue("@idUsuario", idUsuario.Value);
                        cmd.Parameters.AddWithValue("@nuevaPassword", nuevaPass); // ¡Pon aquí el hash si usas hash!

                        if (con.State == ConnectionState.Closed)
                            con.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string mensaje = reader[0].ToString();
                                lblModalMensaje.Text = mensaje;
                            }
                            else
                            {
                                lblModalMensaje.Text = "No se pudo cambiar la contraseña.";
                            }
                        }
                    }
                }

                // Limpia la sesión para seguridad
                Session.Remove("CodigoVerificacion");
                Session.Remove("idUsuarioVerificacion");
                Session.Remove("CorreoVerificacion");

                // (Opcional) Limpia los campos del modal
                txtCodigo.Text = "";
                txtNuevaPass.Text = "";

            }
            catch (Exception ex)
            {
                lblModalMensaje.Text = "Error al cambiar la contraseña: " + ex.Message;
                ScriptManager.RegisterStartupScript(this, this.GetType(), "abrirModal", "$('#modalRecuperar').modal('show');", true);
            }
        }

    }
}