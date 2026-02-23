using EzSmb;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SURO2
{
    public partial class EditarOficio : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (Request.QueryString["ID"] != null)
                {
                    Municipios.DataBind();
                    ddlDocumento.DataBind();
                    int id = int.Parse(Request.QueryString["ID"]);
                    CargarDatosOficio(id);
                }
            }
        }
        private void CargarDatosOficio(int id)
        {
            Funciones funciones = new Funciones();
            SqlConnection con = funciones.ConBD();
            using (SqlCommand cmd = new SqlCommand("SP_MuestraOficioEditar", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@id", id);


                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {

                    Remitente.Text = reader["Remitente"]?.ToString();
                    Lugar.Text = reader["LugarRemitente"]?.ToString();

                    ListItem itemMunicipio = Municipios.Items.FindByValue(reader["MunicipioRemitente"]?.ToString());

                    if (itemMunicipio != null)
                    {
                        Municipios.ClearSelection();
                        itemMunicipio.Selected = true;
                    }
                    else
                    {
                        // Si el municipio no se encuentra en la lista, mostrar un mensaje de error
                        string script = @"mostrarModalConRedireccion('El municipio del folio seleccionado no se encuentra en la lista de municipios disponibles.', 'Visualizacion.aspx');";
                        ScriptManager.RegisterStartupScript(this, this.GetType(), "mostrarModal", script, true);
                    }

                    ListItem itemTipoDoc = ddlDocumento.Items.FindByValue(reader["TipoDocumento"]?.ToString());
                    if (itemTipoDoc != null)
                    {
                        ddlDocumento.ClearSelection();
                        itemTipoDoc.Selected = true;
                    }
                    else
                    {
                        // Si el tipo de documento no se encuentra en la lista, mostrar un mensaje de error
                        string script = @"mostrarModalConRedireccion('El tipo de documento del folio seleccionado no se encuentra en la lista de Tipo de Documentos disponibles.', 'Visualizacion.aspx');";
                        ScriptManager.RegisterStartupScript(this, this.GetType(), "mostrarModal", script, true);
                    }

                    Folio.Text = reader["FolioCaptura"]?.ToString();
                    NoOficio.Text = reader["NumeroOficio"]?.ToString();
                    Asunto.Text = reader["Asunto"]?.ToString();

                    if (reader["FechaOficio"] != DBNull.Value)
                    {
                        DateTime fecha = Convert.ToDateTime(reader["FechaOficio"]);
                        FechaOficio.Text = fecha.ToString("yyyy-MM-dd"); // formato compatible con <input type="date">
                    }
                    else
                    {
                        FechaOficio.Text = "";
                    }

                }
                else
                {
                    string script = @"
                    document.getElementById('modalMensajeContenido').innerText = 'No se encontró el oficio con el ID proporcionado.';
                    $('#modalMensaje').modal('show');
                    ";

                    ScriptManager.RegisterStartupScript(this, this.GetType(), "mostrarModal", script, true);
                }
            }
        }

        protected async void btnGuardar_Click(object sender, EventArgs e)
        {


            await GuardaArchivo();
        }


        public async Task GuardaArchivo()
        {

            Funciones funciones = new Funciones();
            string idFolio = Request.QueryString["ID"];
            SqlTransaction transaccion = null;
            string rutaVirtual = "";
            string rutaAnterior = "";

            try
            {
               

                using (SqlConnection con1 = funciones.ConBD())
                {
                    transaccion = con1.BeginTransaction(); //inicia la transacción

                    if (ArchivoOficio.HasFile)
                    {
                        // Validar que sea PDF
                        string extension = Path.GetExtension(ArchivoOficio.FileName).ToLower();
                        if (extension != ".pdf")
                        {
                            string scriptError = @"mostrarModalConRedireccion('Solo se permiten archivos PDF.', 'Editar.aspx');";
                            ScriptManager.RegisterStartupScript(this, this.GetType(), "mostrarModal", scriptError, true);
                            return;
                        }

                        // Obtener ruta anterior
                        using (SqlCommand cmdGetPath = new SqlCommand("SP_GuardaPath", con1, transaccion))
                        {
                            cmdGetPath.CommandType = CommandType.StoredProcedure;
                            cmdGetPath.Parameters.AddWithValue("@accion", "ObtengoPath");
                            cmdGetPath.Parameters.AddWithValue("@idOficio", idFolio);
                            cmdGetPath.Parameters.AddWithValue("@idUsuario", Session["ID"]);
                            cmdGetPath.Parameters.AddWithValue("@path", "");

                            object resultado = cmdGetPath.ExecuteScalar();
                            if (resultado != null && resultado != DBNull.Value)
                            {
                                //obtengo ruta anterior
                                rutaAnterior = resultado.ToString();

                                // Eliminar archivo anterior si existe
                                if (!string.IsNullOrEmpty(rutaAnterior))
                                {
                                    string rutaFisicaAnterior = Server.MapPath(rutaAnterior);
                                    if (File.Exists(rutaFisicaAnterior))
                                    {
                                        File.Delete(rutaFisicaAnterior);
                                    }
                                }
                            }
                        }



                        // Guardar nuevo archivo
                        extension = Path.GetExtension(ArchivoOficio.FileName).ToLower();
                        if (extension != ".pdf")
                        {
                            string scriptPDF = @"mostrarModalConRedireccion('Solo se permiten archivos en formato .pdf', 'EditarOficio.aspx');";
                            ScriptManager.RegisterStartupScript(this, this.GetType(), "mostrarModal", scriptPDF, true);
                        }

                        string noOficioSanitizado = System.Text.RegularExpressions.Regex.Replace(NoOficio.Text.Trim(), @"[^a-zA-Z0-9\._-]", "_");
                        string folioSanitizado = System.Text.RegularExpressions.Regex.Replace(Folio.Text.Trim(), @"[^a-zA-Z0-9\._-]", "_");
                        string fechaFormateada = DateTime.Parse(FechaOficio.Text).ToString("yyyyMMdd");

                        string carpetaFisica = @"\\10.18.24.185\Datos_Aplicaciones\SURO\";

                        string nombreArchivo = noOficioSanitizado + "_" + folioSanitizado + "_" + fechaFormateada + "_" + idFolio + extension;
                        string rutaFinal = Path.Combine(carpetaFisica, nombreArchivo);

                        Stream stream = ArchivoOficio.PostedFile.InputStream;
                        string rutaArchivoSubido = await SubirArchivoEzSMB(stream, rutaFinal);

                        if (string.IsNullOrEmpty(rutaArchivoSubido))
                        {
                            string scriptNAS = @"mostrarModalConRedireccion('Error al guardar el nuevo archivo en el NAS.', 'EditarOficio.aspx');";
                            ScriptManager.RegisterStartupScript(this, this.GetType(), "mostrarModal", scriptNAS, true);
                        }


                        rutaVirtual = rutaFinal;

                    }
           
                    
                    else
                    {
                        // Si no se subió archivo, obtener la ruta anterior para no sobrescribirla con null
                        using (SqlCommand cmdGetPath = new SqlCommand("SP_GuardaPath", con1, transaccion))
                        {
                            cmdGetPath.CommandType = CommandType.StoredProcedure;
                            cmdGetPath.Parameters.AddWithValue("@accion", "ObtengoPath");
                            cmdGetPath.Parameters.AddWithValue("@idOficio", idFolio);
                            cmdGetPath.Parameters.AddWithValue("@idUsuario", Session["ID"]);
                            cmdGetPath.Parameters.AddWithValue("@path", "");

                            object resultado = cmdGetPath.ExecuteScalar();
                            if (resultado != null && resultado != DBNull.Value)
                            {
                                rutaVirtual = resultado.ToString(); // reutilizar ruta anterior
                            }
                        }
                    }
                    DateTime fechaOficio;
                    string formatoFecha = "yyyy-MM-dd";

                    bool conversionExitosa = DateTime.TryParseExact(FechaOficio.Text, formatoFecha, null, System.Globalization.DateTimeStyles.None, out fechaOficio);


                    int idUsuario = Convert.ToInt32(Session["ID"]);
                    // Guardar cambios del oficio (con o sin nuevo archivo)

                    if (!conversionExitosa)
                    {

                        string scriptPDF = @"mostrarModalConRedireccion('Formato de fecha de oficio inválido.', 'EditarOficio.aspx');";
                        ScriptManager.RegisterStartupScript(this, this.GetType(), "mostrarModal", scriptPDF, true);
                    }
                        using (SqlCommand cmdRuta = new SqlCommand("SP_ModificaOficio", con1, transaccion))
                        {
                            cmdRuta.CommandType = CommandType.StoredProcedure;
                            cmdRuta.Parameters.AddWithValue("@idOficio", Convert.ToInt32(idFolio));
                            cmdRuta.Parameters.AddWithValue("@path", rutaVirtual);
                            cmdRuta.Parameters.AddWithValue("@idUsuario", idUsuario);
                            cmdRuta.Parameters.AddWithValue("@Folio", Convert.ToInt32(Folio.Text));
                            cmdRuta.Parameters.AddWithValue("@Remitente", Remitente.Text);
                            cmdRuta.Parameters.AddWithValue("@LugarRemitente", Lugar.Text);
                            cmdRuta.Parameters.AddWithValue("@Asunto", Asunto.Text);
                            cmdRuta.Parameters.AddWithValue("@Municipio", Convert.ToInt32(Municipios.Text));
                            cmdRuta.Parameters.AddWithValue("@TipoDoc", Convert.ToInt32(ddlDocumento.Text));
                            cmdRuta.Parameters.AddWithValue("@NoOficio", NoOficio.Text);
                            cmdRuta.Parameters.AddWithValue("@FolioOficio", Folio.Text);
                            cmdRuta.Parameters.AddWithValue("@FechaOficio", fechaOficio);

                            cmdRuta.ExecuteNonQuery();

                        }

                    transaccion.Commit();
                        // Mostrar mensaje final
                    string script = @"mostrarModalConRedireccion('¡Registro guardado con éxito!', 'Visualizacion.aspx');";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "mostrarModal", script, true);
                }
                
               
            }
            catch (Exception ex)
            {
                if(transaccion != null)
                {
                    transaccion.Rollback();
                }
                string mensajeError = $"Ocurrió un error al guardar el registro: {ex.Message}";
                string mensajeEscapado = mensajeError.Replace("'", "\\'");

                string script = $"mostrarModalConRedireccion('{mensajeEscapado}', 'Visualizacion.aspx');";
                ScriptManager.RegisterStartupScript(this, this.GetType(), "mostrarModal", script, true);
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
