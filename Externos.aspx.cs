using EzSmb;
using Microsoft.AspNetCore.Http;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Optimization;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;

namespace SURO2
{
    public partial class Externos : System.Web.UI.Page
    {
       

        // Define la carpeta donde se guardarán temporalmente los archivos
        private const string TempUploadFolder = "~/Uploads/Temp/";
        // Define la carpeta donde se guardarán los archivos finales
        private const string FinalUploadFolder = "~/Uploads/Oficios/";
        protected void Page_Load(object sender, EventArgs e)
        {

            if (!IsPostBack)
            {
               

            }
        }

        protected async void btnGuardar_Click(object sender, EventArgs e)
        {
          await GuardaArchivo();
        }

        public async Task GuardaArchivo()
        {
            SqlConnection con = null;
            SqlTransaction transaction = null;
            try
            {
                Funciones conecta = new Funciones();
                con = conecta.ConBD();
               
                transaction = con.BeginTransaction();
                int idInsertado = 0;

                // 1. Validar si hay un archivo y si es PDF
                if (ArchivoOficio.HasFile)
                {
                    string extensionArchivo = Path.GetExtension(ArchivoOficio.FileName).ToLower();
                    if (extensionArchivo != ".pdf")
                    {
                        string script1 = @"mostrarModalConRedireccion('Solo se permiten archivos en formato PDF.');";
                        ScriptManager.RegisterStartupScript(this, this.GetType(), "mostrarModal", script1, true);
                        return;
                    }
                }

                // 2. Insertar oficio
                using (SqlCommand cmd = new SqlCommand("SP_GuardaOficio", con, transaction))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@id", 1);
                    cmd.Parameters.AddWithValue("@Folio", Folio.Text);
                    cmd.Parameters.AddWithValue("@Remitente", Remitente.Text);
                    cmd.Parameters.AddWithValue("@Telefono", Telefono.Text);
                    cmd.Parameters.AddWithValue("@Lugar", Lugar.Text);
                    cmd.Parameters.AddWithValue("@Asunto", Asunto.Text);
                    cmd.Parameters.AddWithValue("@CreatedBy", Session["ID"]);
                    cmd.Parameters.AddWithValue("@CreatedDate", "");
                    cmd.Parameters.AddWithValue("@Municipio", Municipios.Text);
                    cmd.Parameters.AddWithValue("@NivelAtencion", 5);
                    cmd.Parameters.AddWithValue("@TipoDoc", ddlDocumento.Text);
                    cmd.Parameters.AddWithValue("@Estatus", 4);
                    cmd.Parameters.AddWithValue("@Active", true);
                    cmd.Parameters.AddWithValue("@NoOficio", NoOficio.Text);
                    cmd.Parameters.AddWithValue("@FolioOficio", Folio.Text);
                    cmd.Parameters.AddWithValue("@updateDate", "");
                    cmd.Parameters.AddWithValue("@Correo", string.IsNullOrEmpty(Correo.Text) ? "" : Correo.Text);
                    cmd.Parameters.AddWithValue("@otro", string.IsNullOrEmpty(otroMunicipioInput.Text) ? "" : otroMunicipioInput.Text);

                    DateTime fechaOficio;
                    if (DateTime.TryParse(FechaOficio.Text, out fechaOficio))
                    {
                        cmd.Parameters.AddWithValue("@FechaOficio", FechaOficio.Text);
                    }
                    else
                    {
                        string scriptFechaError = @"mostrarModalConRedireccion('Formato de fecha de oficio inválido. Por favor, verifique la fecha.');";
                        ScriptManager.RegisterStartupScript(this, this.GetType(), "mostrarModal", scriptFechaError, true);
                        return;
                    }
                    cmd.Parameters.AddWithValue("@accion", "Inserta");

                    object result = cmd.ExecuteScalar();
                    if (result == null || result == DBNull.Value)
                    {
                        throw new Exception("Error al insertar el oficio: ID no retornado.");
                    }
                    idInsertado = Convert.ToInt32(result);
                }

                // 3. Guardar archivo si existe y el oficio se insertó correctamente
                if (ArchivoOficio.HasFile && idInsertado > 0)
                {
                    Stream stream = ArchivoOficio.PostedFile.InputStream;
                    string extension = Path.GetExtension(ArchivoOficio.FileName);
                    string carpetaFisica = @"\\10.18.24.185\Datos_Aplicaciones\SURO\";
                    string noOficioSanitizado = System.Text.RegularExpressions.Regex.Replace(NoOficio.Text.Trim(), @"[^a-zA-Z0-9\._-]", "_");
                    string folioSanitizado = System.Text.RegularExpressions.Regex.Replace(Folio.Text.Trim(), @"[^a-zA-Z0-9\._-]", "_");
                    string fechaFormateada = DateTime.Parse(FechaOficio.Text).ToString("yyyyMMdd");
                    string nombreArchivo = $"{noOficioSanitizado}_{folioSanitizado}_{fechaFormateada}_{idInsertado}{extension}";
                    string rutaFinal = Path.Combine(carpetaFisica, nombreArchivo);

                    try
                    {
                        string rutaArchivoSubido = await SubirArchivoEzSMB(stream, rutaFinal);
                        if (string.IsNullOrEmpty(rutaArchivoSubido))
                        {
                            throw new Exception("Error al guardar el archivo en el NAS.");
                        }

                        using (SqlCommand cmdRuta = new SqlCommand("SP_GuardaPath", con, transaction))
                        {
                            cmdRuta.CommandType = CommandType.StoredProcedure;
                            cmdRuta.Parameters.AddWithValue("@idOficio", idInsertado);
                            cmdRuta.Parameters.AddWithValue("@path", carpetaFisica + nombreArchivo);
                            cmdRuta.Parameters.AddWithValue("@idUsuario", Session["ID"]);
                            cmdRuta.Parameters.AddWithValue("@accion", "Inserta");
                            cmdRuta.ExecuteNonQuery();
                        }

                        // Si todo sale bien, confirmamos la transacción
                        transaction.Commit();
                        string scriptMessage = @"mostrarModalConRedireccion('¡Registro y archivo guardados con éxito!', 'Externos.aspx');";
                        ScriptManager.RegisterStartupScript(this, this.GetType(), "mostrarModal", scriptMessage, true);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback(); // Revertimos los cambios si falla la subida o la inserción del path
                        string errorMensaje = ex.Message.Replace("'", "\\'");
                        string script2 = $"mostrarModalConRedireccion('Ocurrió un error al guardar el archivo o su ruta. Detalles: {errorMensaje}');";
                        ScriptManager.RegisterStartupScript(this, this.GetType(), "mostrarModal", script2, true);
                    }
                }
                else // Caso donde no se sube un archivo
                {
                    transaction.Commit(); // Confirmamos solo la inserción del oficio
                    string scriptMessage = @"mostrarModalConRedireccion('¡Registro guardado con éxito!', 'Externos.aspx');";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "mostrarModal", scriptMessage, true);
                }
            }
            catch (Exception ex)
            {
                // Manejo de errores para la inserción del oficio
                if (transaction != null)
                {
                    transaction.Rollback();
                }
                string errorMensaje = ex.Message.Replace("'", "\\'");
                string script2 = $"mostrarModalConRedireccion('Ocurrió un error al guardar el registro. Detalles: {errorMensaje}');";
                ScriptManager.RegisterStartupScript(this, this.GetType(), "mostrarModal", script2, true);
            }
            finally
            {
                if (con != null && con.State == ConnectionState.Open)
                {
                    con.Close();
                    con.Dispose();
                }
            }
        }

        public async Task<string> SubirArchivoEzSMB(Stream fileStream, string rutaFinal)
        {
            // 1. Validar la entrada de forma temprana.
            if (fileStream == null || fileStream.Length == 0)
            {
                return string.Empty;
            }

            try
            {
                // 2. Separar la ruta para obtener la carpeta y el nombre del archivo.
                string remotePath = Path.GetDirectoryName(rutaFinal);
                string fileName = Path.GetFileName(rutaFinal);

                // 3. Credenciales de acceso al NAS.
                const string user = "WebUser";
                const string password = "Usuarioweb1";

                // 4. Obtener el nodo de la carpeta remota de forma segura.
                // Asegúrate de que el path sea el adecuado para EzSMB (ej. "\\IP\CarpetaCompartida").
                var folder = await Node.GetNode(remotePath, user, password);


                // 5. Usar directamente el Stream que ya recibiste.
                await folder.Write(fileStream, fileName);

                return rutaFinal;
              
            }
            catch (Exception ex)
            {
                // Manejo de la excepción de forma más específica y útil.
                Console.WriteLine($"Error al subir el archivo: {ex.Message}");
                return string.Empty;
            }
        }

     
    }
}

