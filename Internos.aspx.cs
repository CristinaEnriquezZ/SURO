using EzSmb;
using Microsoft.Ajax.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SURO2
{
    public partial class Internos : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                CargaRemitente();
            }
        }


        public void CargaRemitente()
        {
            if (Session["ID"] == null || string.IsNullOrWhiteSpace(Session["ID"].ToString())){
                return;
            }



            try
            {
                Funciones funciones = new Funciones();
                string folioGenerado = string.Empty;
                int idUser = Convert.ToInt32(Session["ID"].ToString());
                using (SqlConnection conn = funciones.ConBD())
                {
                    using (SqlCommand cmd = new SqlCommand("SP_GeneraFolioConsecutivo", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@idUser", idUser);
                        cmd.Parameters.AddWithValue("@SoloConsultar", 1);


                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                lblRemitente.Text = reader["Remitente"].ToString();

                                object extObj = reader["Extension"];
                                int extension = extObj != DBNull.Value ? Convert.ToInt32(extObj) : 0;
                                lblExtension.Text = extension.ToString();

                                lblCorreoRemitente.Text = reader["Correo"].ToString();

                                string folio = reader["FolioOficio"].ToString(); // SDR.00.001.00019/2025

                                int idx = folio.LastIndexOf('.');
                                if (idx > -1 && idx < folio.Length - 1)
                                {
                                    lblOficioPrefijo.Text = folio.Substring(0, idx + 1); // SDR.00.001.
                                    txtOficioEditable.Text = folio.Substring(idx + 1);   // 00019/2025
                                    string folioOriginal = (lblOficioPrefijo.Text ?? "").Trim() + (txtOficioEditable.Text ?? "").Trim();
                                    hfFolioOriginal.Value = folioOriginal;

                                }
                                else
                                {
                                    lblOficioPrefijo.Text = "";
                                    txtOficioEditable.Text = folio;
                                }
                            }
                        }
                    }


                }
            }
            catch (Exception ex)
            {
               
            }
        }

        protected async void btnGuardar_Click(object sender, EventArgs e)
        {
            await GuardaArchivo();
        }

        public async Task GuardaArchivo()
        {
            string folioFinal = string.Empty;

            SqlConnection con = null;
            SqlTransaction transaction = null;

            try
            {
                Funciones conecta = new Funciones();
                con = conecta.ConBD();
                transaction = con.BeginTransaction();

                int idInsertado = 0;
                int orgAds = Convert.ToInt32(Session["OrgAdscrita"]);
                int idUser = Convert.ToInt32(Session["ID"].ToString());

                // 1) Validar archivo PDF (si suben)
                if (ArchivoOficio.HasFile)
                {
                    string extensionArchivo = Path.GetExtension(ArchivoOficio.FileName).ToLower();
                    if (extensionArchivo != ".pdf")
                    {
                        ScriptManager.RegisterStartupScript(this, this.GetType(), "mostrarModal",
                            "mostrarModalConRedireccion('Solo se permiten archivos en formato PDF.');", true);
                        return;
                    }
                }

                // 2) Validar Fecha Oficio (servidor)
                if (!DateTime.TryParse(FechaOficio.Text, out DateTime fechaOficio))
                {
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "mostrarModal",
                        "mostrarModalConRedireccion('Formato de fecha de oficio inválido. Por favor, verifique la fecha.');", true);
                    return;
                }

                // 3) Construir folio desde UI y validar formato
                string prefijo = (lblOficioPrefijo.Text ?? "").Trim();      // SDR.00.001.
                string editable = (txtOficioEditable.Text ?? "").Trim();    // 00019/2025

                var re = new System.Text.RegularExpressions.Regex(@"^\d{5}/\d{4}$");
                if (string.IsNullOrWhiteSpace(prefijo) || !re.IsMatch(editable))
                {
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "folioError",
                        "mostrarModalConRedireccion('El No. Oficio debe tener formato 00000/2026.');", true);
                    return;
                }

                string folioEnPantalla = prefijo + editable;
                string folioSugerido = (hfFolioOriginal.Value ?? "").Trim();
                int? fojasVal = null;

                string fojasTxt = (Fojas.Text ?? "").Trim();
                if (!string.IsNullOrWhiteSpace(fojasTxt))
                {
                    if (!int.TryParse(fojasTxt, out int f) || f < 0)
                    {
                        ScriptManager.RegisterStartupScript(this, this.GetType(), "fojasError",
                            "mostrarModalConRedireccion('Fojas debe ser un número válido.');", true);
                        return;
                    }
                    fojasVal = f; //  puede ser null si no capturaron nada
                }


                bool edito = !string.IsNullOrWhiteSpace(folioSugerido) &&
                             !folioEnPantalla.Equals(folioSugerido, StringComparison.OrdinalIgnoreCase);

                folioFinal = folioEnPantalla;

                // 4) Si NO editó: consumir consecutivo real (incrementa y evita duplicados)
                if (!edito)
                {
                    using (SqlCommand cmdFolio = new SqlCommand("SP_GeneraFolioConsecutivo", con, transaction))
                    {
                        cmdFolio.CommandType = CommandType.StoredProcedure;
                        cmdFolio.Parameters.AddWithValue("@idUser", idUser);
                        cmdFolio.Parameters.AddWithValue("@SoloConsultar", 0); // ✅ consumir

                        using (SqlDataReader rd = cmdFolio.ExecuteReader())
                        {
                            if (rd.Read())
                            {
                                folioFinal = rd["FolioOficio"].ToString();
                            }
                            else
                            {
                                throw new Exception("No se pudo generar el folio oficial.");
                            }
                        }
                    }
                }
                // Si sí editó, NO consumimos nada: folioFinal se queda como lo escribió

                // 5) Insertar oficio
                using (SqlCommand cmd = new SqlCommand("SP_GuardaOficioInterno", con, transaction))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@id", 1);
                    cmd.Parameters.AddWithValue("@Remitente", idUser);
                    cmd.Parameters.AddWithValue("@Asunto", Asunto.Text);
                    cmd.Parameters.AddWithValue("@CreatedBy", idUser);
                    cmd.Parameters.AddWithValue("@CreatedDate", DBNull.Value);
                    cmd.Parameters.AddWithValue("@TipoDoc", ddlDocumento.Text);
                    cmd.Parameters.AddWithValue("@Estatus", 4);
                    cmd.Parameters.AddWithValue("@Active", 1);

                    cmd.Parameters.AddWithValue("@NoOficio", folioFinal); // ✅ aquí va el final

                    cmd.Parameters.AddWithValue("@updateDate", DBNull.Value);
                    cmd.Parameters.AddWithValue("@OrgAdscrita", orgAds);
                    cmd.Parameters.AddWithValue("@accion", "Inserta");

                    cmd.Parameters.Add("@FechaOficio", SqlDbType.Date).Value = fechaOficio.Date;

                    object result = cmd.ExecuteScalar();
                    if (result == null || result == DBNull.Value)
                        throw new Exception("Error al insertar el oficio: ID no retornado.");

                    idInsertado = Convert.ToInt32(result);
                }

                // 6) Bitácora (si ya tienes la tabla)
                // Si aún no existe, comenta este bloque.
                        using (SqlCommand cmdBit = new SqlCommand(@"
                        INSERT INTO dbo.Bitacora_FolioOficio
                        (IdOficio, FolioPropuesto, FolioCapturado, FueModificado, FechaOficio, IdUsuario,Fojas)
                        VALUES
                        (@IdOficio, @FolioPropuesto, @FolioCapturado, @FueModificado, @FechaOficio, @IdUsuario,@Fojas);", con, transaction))
                {
                    bool fueModificado = !string.IsNullOrWhiteSpace(folioSugerido) &&
                                         !folioFinal.Equals(folioSugerido, StringComparison.OrdinalIgnoreCase);

                    cmdBit.Parameters.AddWithValue("@IdOficio", idInsertado);
                    cmdBit.Parameters.AddWithValue("@FolioPropuesto",
                        string.IsNullOrWhiteSpace(folioSugerido) ? folioFinal : folioSugerido);
                    cmdBit.Parameters.AddWithValue("@FolioCapturado", folioFinal);
                    cmdBit.Parameters.AddWithValue("@FueModificado", fueModificado);
                    cmdBit.Parameters.Add("@FechaOficio", SqlDbType.Date).Value = fechaOficio.Date;
                    cmdBit.Parameters.AddWithValue("@IdUsuario", idUser);
                    cmdBit.Parameters.Add("@Fojas", SqlDbType.Int).Value = (object)fojasVal ?? DBNull.Value;

                    cmdBit.ExecuteNonQuery();
                }

                // 7) Guardar archivo + path
                if (ArchivoOficio.HasFile && idInsertado > 0)
                {
                    Stream stream = ArchivoOficio.PostedFile.InputStream;
                    string extension = Path.GetExtension(ArchivoOficio.FileName);
                    string carpetaFisica = @"\\10.18.24.185\Datos_Aplicaciones\SURO\";

                    string noOficioSanitizado = System.Text.RegularExpressions.Regex.Replace(
                        folioFinal, @"[^a-zA-Z0-9\._-]", "_");

                    string fechaFormateada = fechaOficio.ToString("yyyyMMdd");
                    string nombreArchivo = $"{noOficioSanitizado}_{fechaFormateada}_{idInsertado}{extension}";
                    string rutaFinal = Path.Combine(carpetaFisica, nombreArchivo);

                    string rutaArchivoSubido = await SubirArchivoEzSMB(stream, rutaFinal);
                    if (string.IsNullOrEmpty(rutaArchivoSubido))
                        throw new Exception("Error al guardar el archivo en el NAS.");

                    using (SqlCommand cmdRuta = new SqlCommand("SP_GuardaPathInternos", con, transaction))
                    {
                        cmdRuta.CommandType = CommandType.StoredProcedure;
                        cmdRuta.Parameters.AddWithValue("@idOficio", idInsertado);
                        cmdRuta.Parameters.AddWithValue("@path", carpetaFisica + nombreArchivo);
                        cmdRuta.Parameters.AddWithValue("@idUsuario", Session["ID"]);
                        cmdRuta.Parameters.AddWithValue("@accion", "Inserta");
                        cmdRuta.ExecuteNonQuery();
                    }
                }

                // 8) Commit
                transaction.Commit();
                ScriptManager.RegisterStartupScript(this, this.GetType(), "mostrarModal",
                    "mostrarModalConRedireccion('¡Registro guardado con éxito!', 'Externos.aspx');", true);
            }
            catch (Exception ex)
            {
                if (transaction != null) transaction.Rollback();

                string errorMensaje = ex.Message.Replace("'", "\\'");
                ScriptManager.RegisterStartupScript(this, this.GetType(), "mostrarModal",
                    $"mostrarModalConRedireccion('Ocurrió un error al guardar el registro. Detalles: {errorMensaje}');", true);
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