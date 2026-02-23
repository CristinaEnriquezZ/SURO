
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using static ClosedXML.Excel.XLPredefinedFormat;
using DateTime = System.DateTime;


namespace SURO2
{
    public partial class Visualizacion : System.Web.UI.Page
    {



        protected Literal litNoOficio;
        protected Literal litFechaOficio;
        protected HtmlGenericControl phOficioPrincipalPdfIframe; // PlaceHolder para el PDF del oficio principal
        protected HtmlIframe iframeOficioPrincipal; // El iframe del oficio principal
        protected HyperLink hlDescargaOficioPrincipal;
        protected Oficio oficioActual;
        public System.Web.UI.WebControls.GridView MiGridViewOficios
        {
            get { return gvOficios; }
        }

        public System.Web.UI.UpdatePanel MiUpdatePanelOficios
        {
            get { return UpdatePanel1; }
        }
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                

                gvOficios.RowCreated += gvOficios_RowCreated;
                CargarOficios();


            }
        }

     
      
        protected string GetFileNameFromPath(object path)
        {
            if (path == DBNull.Value || string.IsNullOrEmpty(path.ToString()))
            {
                return "N/A";
            }
            try
            {
                return Path.GetFileName(path.ToString());
            }
            catch
            {
                return "Archivo Adjunto";
            }
        }

        /// <summary>
        /// Método de ayuda para formatear una fecha a un string legible.
        /// </summary>
        protected string FormatDate(object date)
        {
            // El Convert.ToDateTime(date) ya debería manejar DBNull si se usa correctamente
            // Pero el check previo en dr.Read() ayuda.
            System.DateTime dt = Convert.ToDateTime(date);
            return dt.ToString("dd/MM/yyyy");
        }

      

   

        public void CargarOficios()
        {
            string filtro = ddlEstatus.SelectedValue?.Trim();

            if (Session["ID"] == null)
            {
                Response.Redirect("Login.aspx");
                return;
            }

            int idUsuario = Convert.ToInt32(Session["ID"]);
            string tipoOrg = Session["TipoOrg"]?.ToString();
            int OrgAdscrita = Convert.ToInt32(Session["OrgAdscrita"]);

            Funciones funciones = new Funciones();
            using (SqlConnection conn = funciones.ConBD())
            using (SqlCommand cmd = new SqlCommand("SP_MuestraOficiosxUsuario", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@IdUsuario", SqlDbType.Int).Value = idUsuario;
                cmd.Parameters.Add("@tipoOrg", SqlDbType.Int).Value = tipoOrg;
                cmd.Parameters.Add("@OrgAdscrita", SqlDbType.Int).Value = OrgAdscrita;

                if (!string.IsNullOrEmpty(filtro))
                    cmd.Parameters.Add("@Estatus", SqlDbType.NVarChar, 50).Value = filtro;
                else
                    cmd.Parameters.Add("@Estatus", SqlDbType.NVarChar, 50).Value = DBNull.Value;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                gvOficios.DataSource = dt;
                gvOficios.DataBind();
                if (UpdatePanel1 != null && ScriptManager.GetCurrent(this)?.IsInAsyncPostBack == true)
                    UpdatePanel1.Update();
            }
           
        }
        //Creo el evento RowDataBound para darle color a los estatus de los oficios
        protected void gvOficios_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                string estatusSemaforo = DataBinder.Eval(e.Row.DataItem, "EstatusSemaforo").ToString();

                // Aplicar estilos según el valor del semáforo
                switch (estatusSemaforo)
                {
                    case "Rojo":
                        e.Row.BackColor = ColorTranslator.FromHtml("#FFC7C7");
                        // Puedes cambiar el color de la fuente para mejor legibilidad
                        //e.Row.ForeColor = System.Drawing.Color.White;
                        break;
                    case "Naranja":
                        // Aquí, el color naranja es un poco más complicado, puedes usar un color RGB o el nombre
                        e.Row.BackColor = ColorTranslator.FromHtml("#FFD6A5");
                        break;
                    case "Amarillo":
                        e.Row.BackColor = ColorTranslator.FromHtml("#FFECB3");
                        break;
                    default:
                        // Para 'No Aplica' o cualquier otro caso, no apliques un estilo especial
                        break;
                }



                Label lblEstatus = (Label)e.Row.FindControl("lblEstatus");
                HyperLink btnEditar = (HyperLink)e.Row.FindControl("btnEditar");
                LinkButton btnVer = (LinkButton)e.Row.FindControl("btnVerDetalles");
                HtmlGenericControl divAcciones = (HtmlGenericControl)e.Row.FindControl("divAcciones");




                if (lblEstatus != null)
                {
                    string estatus = lblEstatus.Text.ToLower();

                    switch (estatus)
                    {
                        case "recibido":
                            lblEstatus.CssClass = "estatus-recibido"; //morado
                            break;
                        case "concluido":
                            lblEstatus.CssClass = "estatus-concluido"; //verde
                            break;
                        case "turnado":
                            lblEstatus.CssClass = "estatus-turnado"; //amarillo
                            break;
                        case "pendiente":
                            lblEstatus.CssClass = "estatus-pendiente"; //rojo
                            break;
                        case "capturado":
                            lblEstatus.CssClass = "estatus-capturado"; //azul un poco más fuerte
                            break;
                        case "en proceso":
                            lblEstatus.CssClass = "estatus-EnProceso"; //azul cielo
                            break;
                    }
                    // Oculta el botón de editar si el estatus es concluido o turnado
                    if (btnEditar != null)
                    {
                        if (estatus == "concluido" || estatus == "turnado" || estatus == "en proceso")
                        {
                            btnEditar.Visible = false;



                        }
                        else
                        {
                            string id = DataBinder.Eval(e.Row.DataItem, "ID").ToString();
                            btnEditar.NavigateUrl = "EditarOficio.aspx?ID=" + id; //mando el ID del oficio a la página de edición en la URL
                            btnEditar.Visible = true;
                        }
                    }
                }
                bool verVisible = btnVer != null && btnVer.Visible;
                bool editarVisible = btnEditar != null && btnEditar.Visible;


                //Lógica para asignar la clase de alineación de los botones Editar y Ver
                if (divAcciones != null)
                {
                    if (verVisible && editarVisible)
                    {
                        divAcciones.Attributes["class"] = "d-flex gap-1 justify-content-center";
                    }
                    else
                    {
                        divAcciones.Attributes["class"] = "d-flex gap-1 justify-content-start";
                    }
                }

            }
        }




        protected void btnBuscar_Click(object sender, EventArgs e)
        {
            //Busco oficio en el gridview

            string busqueda = txtBuscarMultiple.Text.Trim();
            int idUsuario = Convert.ToInt32(Session["ID"]);

            //Si está vacío: recarga todo y sal
            if (string.IsNullOrWhiteSpace(busqueda))
            {
                CargarOficios();
                if (UpdatePanel1 != null) UpdatePanel1.Update();
                return;
            }
            int TipoOrg = Convert.ToInt32(Session["TipoOrg"].ToString());
            int OrgAds = Convert.ToInt32(Session["OrgAdscrita"].ToString());
            Funciones funciones = new Funciones();

            using (SqlConnection conn = funciones.ConBD())
            using (SqlCommand cmd = new SqlCommand("SP_ObtenerOficioPorID", conn))
            {

                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@busqueda", busqueda);
                cmd.Parameters.AddWithValue("@TipoOrg", TipoOrg);
                cmd.Parameters.AddWithValue("@OrgAds", OrgAds);


                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                gvOficios.DataSource = dt;
                gvOficios.DataBind();
            }

        }

        private Oficio ObtenerOficioParaModal(int idOficio)
        {
            Oficio oficio = null;
            string tipoOrg = Session["TipoOrg"]?.ToString();
            int OrgAdscrita = Convert.ToInt32(Session["OrgAdscrita"]);


            Funciones func = new Funciones();
            using (SqlConnection con = func.ConBD())
            {
                SqlCommand cmd = new SqlCommand("SP_MuestraOficioModal", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@id", idOficio);
                cmd.Parameters.AddWithValue("@tipoOrg", tipoOrg);
                cmd.Parameters.AddWithValue("@OrgAdscrita", OrgAdscrita);

                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    oficio = new Oficio
                    {

                        FolioCaptura = reader["FolioCaptura"].ToString(),
                        FolioOficio = reader["FolioOficio"].ToString(),
                        NumeroOficio = reader["NumeroOficio"].ToString(),
                        Remitente = reader["Remitente"].ToString(),
                        LugarRemitente = reader["LugarRemitente"].ToString(),
                        Telefono = reader["Telefono"].ToString(),
                        Correo = reader["Correo"] != DBNull.Value ? reader["Correo"].ToString() : string.Empty,
                        MunicipioRemitente = reader["MunicipioRemitente"].ToString(),
                        TipoDocumento = reader["TipoDocumento"].ToString(),
                        Estatus = reader["Estatus"].ToString(),
                        NivelAtencion = reader["NivelAtencion"].ToString(),
                        RutaPDF = reader["RutaPDF"].ToString(),
                        Conocimiento = reader["Conocimiento"] != DBNull.Value ? Convert.ToBoolean(reader["Conocimiento"]) : false,
                        // Evita error de conversión si viene nulo
                        FechaMaxAtencion = reader["FechaMaxAtencion"] != DBNull.Value
                            ? (DateTime?)Convert.ToDateTime(reader["FechaMaxAtencion"])
                            : null,

                        Asunto = reader["Asunto"].ToString(),
                        FechaOficio = Convert.ToDateTime(reader["FechaOficio"]).ToShortDateString()
                    };
                }
                reader.Close();
            }



            return oficio;


        }



        protected void gvOficios_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            Funciones fn = new Funciones();

            if (e.CommandName == "VerDetalles")
            {
                int tipoOrg = Convert.ToInt32(Session["TipoOrg"]);
                
                int id = Convert.ToInt32(e.CommandArgument);
                Oficio oficio = ObtenerOficioParaModal(id);


                if (oficio.Estatus == "Turnado")
                {

                    int orgAdscrita = Convert.ToInt32(Session["OrgAdscrita"]);
                    Funciones funciones = new Funciones();
                    using (SqlConnection conn = funciones.ConBD())
                    using (SqlCommand cmd = new SqlCommand("sp_MarcarOficioVistoPorUsuario", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@IdOficio", id);
                        cmd.Parameters.AddWithValue("@TipoOrg", tipoOrg);
                        cmd.Parameters.AddWithValue("@OrgAdscrita", orgAdscrita);
                        cmd.ExecuteNonQuery();
                    }
                    oficio = ObtenerOficioParaModal(id);
                    CargarOficios();
                    if (UpdatePanel1 != null) UpdatePanel1.Update();
                }



                ConsultoDestinosTurnados(id); // Llama al método para consultar destinos turnados
                HttpContext.Current.Session["IDOficio"] = id;
                SiteMaster masterPage = this.Master as SiteMaster;
                if (masterPage == null) return;

                if (oficio != null && !string.IsNullOrEmpty(oficio.RutaPDF) && masterPage != null)
                {
                    HttpContext.Current.Session["NumeroOficio"] = oficio.NumeroOficio;

                    if (masterPage.MiHfEstatus != null)
                    {
                        masterPage.MiHfEstatus.Value = oficio.Estatus;
                    }
                    int idUsuarioActual = Convert.ToInt32(Session["ID"]); //obtengo el id del usuario actual
                    int idOrgAdscrita = Convert.ToInt32(Session["OrgAdscrita"]); //obtengo la organizacion adscrita del usuario
                                                                                 //List<int> idsOrganizacionesTurnadas = ObtenerOrganizacionesTurnadasParaOficio(oficio); 
                                                                                 // Evaluar el estatus del oficio para seguimiento
                    bool estatusPermiteSeguimiento = (oficio.Estatus == "Turnado" || oficio.Estatus == "En Proceso" || oficio.Estatus == "Pendiente" || oficio.Estatus == "Recibido");
                    //bool paraConocimiento = oficio.Conocimiento;


                    CargarNotasDeConclusion(id); // Cargar las notas de conclusión del oficio actual
                    using (SqlConnection conn = fn.ConBD())


                    {
                        using (SqlCommand cmd = new SqlCommand("SP_ConsultaSeguimiento", conn))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@IdOficio", id);

                            SiteMaster sitemaster = new SiteMaster();
                            oficioActual = new Oficio { Id = id.ToString() }; // Inicializa el objeto oficioActual
                            SqlDataReader drSeguimiento = cmd.ExecuteReader();


                            while (drSeguimiento.Read())
                            {
                                Seguimiento s = new Seguimiento();
                                s.ID = Convert.ToInt32(drSeguimiento["ID"]);
                                s.FechaSeguimiento = drSeguimiento["Fecha"] != DBNull.Value ? Convert.ToDateTime(drSeguimiento["Fecha"]) : (System.DateTime?)null;
                                s.NotasSeguimiento = drSeguimiento["NotasSeguimiento"] != DBNull.Value ? drSeguimiento["NotasSeguimiento"].ToString() : string.Empty;
                                string rutaDB = drSeguimiento["RutaArchivo"] != DBNull.Value ? drSeguimiento["RutaArchivo"].ToString() : string.Empty;
                                if (string.IsNullOrWhiteSpace(rutaDB) || rutaDB.Equals("No se agregó un archivo adjunto", StringComparison.OrdinalIgnoreCase))
                                {
                                    s.RutaArchivoSeguimiento = "No se agregó un archivo adjunto";
                                }
                                else
                                {
                                    s.RutaArchivoSeguimiento = rutaDB;
                                }

                                //s.RutaArchivoSeguimiento = drSeguimiento["RutaArchivo"] != DBNull.Value ? drSeguimiento["RutaArchivo"].ToString() : string.Empty;
                                s.IdUsuario = drSeguimiento["IdUsuario"] != DBNull.Value ? drSeguimiento["IdUsuario"].ToString() : string.Empty;

                                oficioActual.Seguimientos.Add(s); // Agrega el seguimiento a la lista del oficio


                            }
                            drSeguimiento.Close();



                            // Lógica para mostrar / ocultar el botón de Seguimiento
                            if (masterPage.MibtnMostrarSeguimiento != null && masterPage.MiSeccionSeguimiento != null)
                            {
                                masterPage.MibtnMostrarSeguimiento.Visible = false;
                                masterPage.MiSeccionSeguimiento.Style["display"] = "none";

                                //Condiciones para mostrar el botón de "Agregar Seguimiento"
                                //1. No es para conocimiento
                                //2. La organización del usuario NO debe de ser un "area" osea del tipo 5
                                //3. El estatus del oficio permite seguimiento 
                                //!paraConocimiento && 
                                if (estatusPermiteSeguimiento)
                                {
                                    masterPage.MibtnMostrarSeguimiento.Visible = true;
                                }

                            }




                            // --- Lógica para el botón "Turnar Documento" y sus controles relacionados ---

                            bool allControlsAreAccessible =
                    masterPage.MiBotonDesdeMaster != null && // Botón "Turnar Documento"
                    masterPage.MiSeccionTurnar != null &&  // Sección con los controles para turnar
                    masterPage.MiFechatxt != null &&   // TextBox de fecha
                                                       //masterPage.MiLblFechaAtencionHtml != null &&
                    masterPage.MiLitConocimiento != null && // Literal "Para conocimiento"
                    masterPage.MiLitFechaConocimiento != null; // Literal "Fecha Máxima Atención"

                            if (allControlsAreAccessible)
                            {

                                //Oculto todos los controles y sus literales por defecto
                                masterPage.MiBotonDesdeMaster.Visible = false;
                                masterPage.MiSeccionTurnar.Visible = false;
                                masterPage.MiFechatxt.Visible = false;



                                masterPage.MiLitConocimiento.Visible = true; // Siempre visible
                                masterPage.MiLitConocimiento.Text = "Para conocimiento: " + (oficio.Conocimiento ? "Sí" : "No");



                                masterPage.MiLitFechaConocimiento.Visible = true;

                                if (oficio.FechaMaxAtencion.HasValue)
                                {
                                    string FechaMax = oficio.FechaMaxAtencion.Value.ToString("dd/MM/yyyy");

                                    if (oficio.FechaMaxAtencion.HasValue && FechaMax != "01/01/0001")
                                    {
                                        masterPage.MiLitFechaConocimiento.Text = "Fecha de Atención: " + oficio.FechaMaxAtencion.Value.ToString("dd/MM/yyyy");
                                    }
                                    else
                                    {
                                        masterPage.MiLitFechaConocimiento.Text = "Fecha de Atención: No especificada";
                                    }
                                }



                                if (oficio.Estatus == "Turnado") // Si el oficio ya está turnado
                                {

                                    int semiConcluido = 0;
                                    //Verifico si el area ya lo concluyó
                                    semiConcluido = fn.VerificaConlusion(idOrgAdscrita, idUsuarioActual, id);


                                    if (semiConcluido != 0)
                                    {

                                        masterPage.MiSeccionMensajeConc.Style["display"] = "block";
                                        masterPage.MiBotonDesdeMaster.Style["display"] = "none";
                                        masterPage.MiSeccionNoTurno.Style["display"] = "none";
                                        masterPage.MiSeccionSeguimiento.Style["display"] = "none";
                                        masterPage.MibtnMostrarSeguimiento.Style["display"] = "none";

                                    }
                                    else
                                    {
                                        masterPage.MiSeccionMensajeConc.Style["display"] = "none";
                                        masterPage.MibtnMostrarSeguimiento.Visible = true;
                                        masterPage.MiBotonDesdeMaster.Visible = true;
                                        masterPage.MiSeccionTurnar.Visible = true;
                                        masterPage.MibtnConcluir.Visible = true;
                                        masterPage.MibtnConcluir.Style["display"] = "block";
                                        masterPage.MiBotonDesdeMaster.Style["display"] = "block";
                                        masterPage.MibtnMostrarSeguimiento.Style["display"] = "block";
                                        masterPage.MiSeccionTurnar.Style["display"] = "none";
                                        //AQUI TENGO QUE AGREGAR EL BOTON DE CONCLUIR
                                    }


                                }
                                else // Si el oficio NO está turnado (por ejemplo, "Capturado", "Pendiente", etc.)
                                {
                                    if (oficio.Estatus == "Capturado" || oficio.Estatus == "En Proceso")
                                    {
                                        masterPage.MiBotonDesdeMaster.Visible = true;  // Muestra el botón "Turnar Documento"                                                                        // Inicialmente, la sección de turno debe estar oculta hasta que se haga clic en btnMostrarTurno
                                        masterPage.MiSeccionTurnar.Visible = true;
                                        masterPage.MiFechatxt.Visible = false;

                                        masterPage.MibtnConcluir.Style["display"] = "block";
                                        masterPage.MiBotonDesdeMaster.Style["display"] = "block";
                                        masterPage.MibtnMostrarSeguimiento.Style["display"] = "block";
                                        masterPage.MiSeccionTurnar.Style["display"] = "none";

                                        masterPage.CargarDdlPrincipalTurnado(); // Carga el dropdown de direcciones turnadas
                                    }

                                }

                                //si el oficio está concluido ya no debe mostrar el botón de concluir, ni agregar seguimiento ni turnar
                                if (oficio.Estatus == "Concluido")
                                {
                                    masterPage.MiBotonDesdeMaster.Style["display"] = "none";
                                    masterPage.MibtnConcluir.Style["display"] = "none";
                                    masterPage.MibtnMostrarSeguimiento.Style["display"] = "none";
                                    CargarNotasDeConclusion(id);
                                }

                                if (oficio.Estatus == "Recibido" && tipoOrg != 5)
                                {
                                    masterPage.MiBotonDesdeMaster.Style["display"] = "block";
                                    masterPage.MibtnConcluir.Style["display"] = "block";
                                    masterPage.MibtnMostrarSeguimiento.Style["display"] = "block";
                                    masterPage.MiBotonDesdeMaster.Visible = true;  // Muestra el botón "Turnar Documento"                                                                        // Inicialmente, la sección de turno debe estar oculta hasta que se haga clic en btnMostrarTurno
                                    masterPage.MiSeccionTurnar.Visible = true;

                                }
                                //Primero obtengo la direcion del usuario actual   
                                DataTable dtDepartamentos = new DataTable();
                                int idDireccionDelUsuario = 0;

                                if (idOrgAdscrita > 0)
                                {
                                    idDireccionDelUsuario = fn.ObtenerDireccionUsuario(idOrgAdscrita, idUsuarioActual);
                                }

                                //Uso el IDDireccion para llenar el dropdown de Departamentos
                                if (idDireccionDelUsuario > 0)
                                {
                                    dtDepartamentos = fn.ObtenerDepartamentosParaTurno(idDireccionDelUsuario);
                                }




                                if (masterPage.MiUpContenidoTurnar != null)
                                {
                                    masterPage.MiUpContenidoTurnar.Update();
                                }
                                string rutaParaPDF = "";
                                string rutaCompletaDesdeDB = oficio.RutaPDF;
                                //obtengo el nombre del archivo desde la ruta completa
                                string nombreArchivo = Path.GetFileName(rutaCompletaDesdeDB);

                                //construyo la url que llama al handler registrado en web.config
                                rutaParaPDF = ResolveClientUrl($"~/VisualizadorPDF.axd?archivo={nombreArchivo}");

                                //armo el script
                                    try
                                    {
                                        string script = $@"
                $('#pdfFrame').attr('src', '{rutaParaPDF}');
                $('#fichaFolioCaptura').text('{oficio.FolioCaptura}');
                $('#fichaNumeroOficio').text('{oficio.NumeroOficio}');
                $('#fichaFolioSecretario').text('{oficio.FolioOficio}');
                $('#fichaRemitente').text('{oficio.Remitente}');
                $('#fichaLugarRemitente').text('{oficio.LugarRemitente}');
                $('#fichaTelefono').text('{oficio.Telefono}');
";
                                        if (!string.IsNullOrEmpty(oficio.Correo))
                                        {
                                            script += $"$('#fichaCorreo').text('{oficio.Correo}');";
                                        }
                                        script += $@"
                $('#fichaMunicipio').text('{oficio.MunicipioRemitente}');
                $('#fichaNivelAtencion').text('{oficio.NivelAtencion}');
                $('#fichaTipoDocumento').text('{oficio.TipoDocumento}');
                actualizarIconoEstatus('{oficio.Estatus}');
                $('#fichaAsunto').text('{oficio.Asunto}');
                $('#fichaFechaOficio').text('{oficio.FechaOficio}');
                $('#pdfModal').modal('show');
                ////setTimeout(controlarVisibilidadBotonTurnar, 200);

             ";
                                        ScriptManager.RegisterStartupScript(this, this.GetType(), "AbrirModal", script, true);
                                    }
                                    catch (Exception ex)
                                    {
                                        // Manejo del error
                                        string mensajeError = "No se pudo cargar la información del oficio. Por favor, inténtelo de nuevo más tarde.";
                                        string mensajeEscapado = mensajeError.Replace("'", "\\'");
                                        string scriptError = $"mostrarAlertaEnModal('{mensajeEscapado}');";
                                        ScriptManager.RegisterStartupScript(this, GetType(), "mostrarError", scriptError, true);
                                    }
                            }
                          


                        }
                        RenderizarSeguimientos();
                    }

                }
            }
        }
        private List<int> ObtenerOrganizacionesTurnadasParaOficio(int idOficio)
        {
            List<int> idsTurnadas = new List<int>();
            Funciones funciones = new Funciones(); // Asumiendo que 'Funciones' es tu clase de utilidades de DB

            // Modifica esta consulta para que coincida con tu tabla real de turnos.
            // Asumo una tabla 'OficiosTurnados' con columnas 'IDOficio' y 'IDOrganizacionDestino'.
            string query = "SELECT DISTINCT IDOrganizacionDestino FROM OficiosTurnados WHERE IDOficio = @IDOficio";

            using (SqlConnection conn = funciones.ConBD())
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@IDOficio", idOficio);
                    try
                    {
                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                idsTurnadas.Add(Convert.ToInt32(reader["IDOrganizacionDestino"]));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Loguea el error o manéjalo apropiadamente
                        System.Diagnostics.Debug.WriteLine("Error al obtener organizaciones turnadas: " + ex.Message);
                    }
                }
            }
            return idsTurnadas;
        }

        protected void gvOficios_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvOficios.PageIndex = e.NewPageIndex;
            CargarOficios();
        }

        protected void ddlEstatus_SelectedIndexChanged(object sender, EventArgs e)
        {
            CargarOficios(); // Se ejecutará sin recargar la página gracias al UpdatePanel
        }

        private void ConsultoDestinosTurnados(int idOficio)
        {
            SiteMaster masterPage = this.Master as SiteMaster;
            Funciones fn = new Funciones();
            using (SqlConnection conn = fn.ConBD())
            {
                using (SqlCommand cmd = new SqlCommand("SP_MuestroTurnados", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@idOficio", idOficio);

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        StringBuilder sbTurnados = new StringBuilder();
                        sbTurnados.Append("<ul class='list-unstyled turnado-items-list'>");

                        bool hayTurnados = false;
                        string concluidoPor = string.Empty; // Variable para guardar el nombre de quién concluyó

                        // Bucle para agregar los destinos turnados (badges)
                        while (dr.Read())
                        {
                            hayTurnados = true;
                            string destinoConsolidado = dr["DestinoConsolidado"] != DBNull.Value ? dr["DestinoConsolidado"].ToString() : "Destino no especificado";

                            string fechaTurnado = dr["FechaTurnado"] != DBNull.Value ? Convert.ToDateTime(dr["FechaTurnado"]).ToString("dd/MM/yyyy") : "Fecha de turnado no disponible";
                            // Almacenamos el nombre de "Concluido por" si existe.
                            // Asumimos que es el mismo para todas las filas.
                            if (string.IsNullOrEmpty(concluidoPor))
                            {
                                concluidoPor = dr["ConcluidoPor"] != DBNull.Value ? dr["ConcluidoPor"].ToString() : "";
                            }

                            sbTurnados.Append($@"
                        <li class='turnado-item-list-item d-flex justify-content-between align-items-center'>
                            <span class='turnado-badge turnado-badge-direccion'>
                                {Server.HtmlEncode(destinoConsolidado)}
                            </span>
                            <span class='turnado-fecha text-muted small'>
                               Fecha de Turno: {fechaTurnado}
                            </span>                           
                        </li>");
                        }

                        if (!hayTurnados)
                        {
                            sbTurnados.Append("<li class='text-muted'>Este oficio no ha sido turnado a ninguna organización específica.</li>");
                        }

                        // Generar el HTML para mostrar quién lo concluyó, pero solo UNA VEZ, fuera del bucle.
                        if (!string.IsNullOrEmpty(concluidoPor))
                        {
                            sbTurnados.Append($@"
                        <div class='concluido-info-container mt-2'>
                            <i class='fas fa-check-circle text-success me-1'></i>
                            <span class='text-success'>Concluido por:</span>
                            <span>{Server.HtmlEncode(concluidoPor)}</span>
                        </div>");
                        }

                        sbTurnados.Append("</ul>");
                        if (masterPage.MiSeccionTurnadosDiv != null)
                        {
                            masterPage.MiSeccionTurnadosDiv.InnerHtml = sbTurnados.ToString();
                        }
                    }
                }
            }
        }
        private void RenderizarSeguimientos()
        {
            SiteMaster master = this.Master as SiteMaster;
            StringBuilder htmlSeguimientos = new StringBuilder();

            if (master == null || master.ContenidoSeguimientosLiteral == null)
            {
                return;
            }

            try
            {
                // Verifica si hay seguimientos en la lista de tu objeto oficioActual
                if (oficioActual != null && oficioActual.Seguimientos != null && oficioActual.Seguimientos.Count > 0)
                {
                    foreach (Seguimiento s in oficioActual.Seguimientos)
                    {
                        // Construye el HTML para cada "tarjetita" de seguimiento
                        htmlSeguimientos.Append($@" 
                <div class='card seguimiento-card'>
                    <div class='card-body'>
                        <h5 class='card-title'>Seguimiento</h5>
                        <h6 class='card-subtitle mb-2 text-muted'>Fecha: {FormatDate(s.FechaSeguimiento)}</h6>
                        <p class='card-text'>Notas de seguimiento: {Server.HtmlEncode(s.NotasSeguimiento)}</p>
                ");

                        // Si hay una ruta de archivo PDF para este seguimiento, añade el iframe y el botón de descarga
                        if (!string.IsNullOrEmpty(s.RutaArchivoSeguimiento) &&
                        !s.RutaArchivoSeguimiento.Equals("No se agregó un archivo adjunto", StringComparison.OrdinalIgnoreCase))
                        {
                            // 1. Obtén solo el nombre del archivo de la ruta completa.
                            string nombreArchivo = Path.GetFileName(s.RutaArchivoSeguimiento);
                            // 2. Codifica el nombre del archivo para que la URL sea segura.
                            string nombreArchivoCodificado = Server.UrlEncode(nombreArchivo);
                            // 3. Construye la URL para el manejador.
                            string urlHandler = "VisualizadorSeguimientos.axd?archivo=" + nombreArchivoCodificado;

                            htmlSeguimientos.Append($@"
                    <h6 class='mt-3'>Archivo Adjunto:</h6>
                    <div class='pdf-viewer-container-sm'>
                        <iframe src='{urlHandler}'
                                 width='100%'
                                 height='300px'
                                 frameborder='0'></iframe>
                    </div>
                    <a href='{urlHandler}' target='_blank' class='btn btn-secondary btn-sm mt-2'>
                        <i class='fas fa-download'></i> Descargar PDF de Seguimiento
                    </a>
                ");
                        }
                        else if (s.RutaArchivoSeguimiento.Equals("No se agregó un archivo adjunto", StringComparison.OrdinalIgnoreCase))
                        {
                            htmlSeguimientos.Append("<p class='text-info mt-3'>No se agregó un archivo adjunto a este seguimiento.</p>");

                        }

                            // Cierra la tarjetita
                            htmlSeguimientos.Append($@"
                    </div>
                </div>
                <br />
                ");
                    }
                }
                else
                {
                    // Puedes agregar un mensaje para el usuario si no hay seguimientos
                    htmlSeguimientos.Append("<p>No se encontraron seguimientos para este oficio.</p>");
                }

                // --- Este es el paso final para renderizar! ---
                master.ContenidoSeguimientosLiteral.Text = htmlSeguimientos.ToString();
            }
            catch (Exception ex)
            {
                // Solución CS1061: Usar System.Diagnostics.Trace en vez de TraceContext
                System.Diagnostics.Trace.WriteLine($"Error inesperado en RenderizarSeguimientos: {ex}");

                // Opcionalmente, muestra un mensaje de error amigable al usuario
                master.ContenidoSeguimientosLiteral.Text = "<p class='text-danger'>Ha ocurrido un error al cargar los seguimientos. Por favor, inténtelo de nuevo.</p>";
            }
        }
        protected void gvOficios_RowCreated(object sender, GridViewRowEventArgs e)
        {
            // Oculta la primera celda (columna ID) del encabezado y de las filas
            if (e.Row.RowType == DataControlRowType.Header ||
                e.Row.RowType == DataControlRowType.DataRow)
            {
                // Asegúrate de que haya columnas
                if (e.Row.Cells.Count > 0)
                {
                    e.Row.Cells[0].Visible = false; // Oculta la primera celda
                }
            }
        }
        private void CargarNotasDeConclusion(int idOficio)
        {
            SiteMaster master = this.Master as SiteMaster;
            StringBuilder sb = new StringBuilder();
            int idOrgAdscrita = Convert.ToInt32(Session["OrgAdscrita"]);
            Funciones fn = new Funciones();
            using (SqlConnection conn = fn.ConBD())
            {
                using (SqlCommand cmd = new SqlCommand("SP_ConsultaConclusion", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@IdOficio", idOficio);
                    cmd.Parameters.AddWithValue("@IdOrgAds", idOrgAdscrita);


                    SqlDataReader dr = cmd.ExecuteReader();
                    if (dr.HasRows)
                    {
                        // Construir el contenedor si hay notas
                        sb.AppendLine("<div class='card-deck row'>");
                        while (dr.Read())
                        {
                            string observacion = dr["Observacion"].ToString();
                            System.DateTime fechaConclusion = (System.DateTime)dr["FechaConclusion"];
                            string Nombre = dr["Nombre"].ToString();

                            // Construir la "tarjetita" para cada nota
                            sb.AppendLine("<div class='col-sm-6 col-md-4 mb-3 d-flex align-items-stretch'>");
                            sb.AppendLine("  <div class='card border-success w-100'>");
                            sb.AppendLine("    <div class='card-body d-flex flex-column'>");
                            sb.AppendLine("      <div class='d-flex align-items-center mb-2'>");
                            sb.AppendLine("        <i class='fa fa-check-circle fa-2x text-success mr-2'></i>");
                            sb.AppendLine($"        <p class='card-text font-weight-bold mb-0'>Conclusión</p>");

                            
                            sb.AppendLine($"        <span class='text-muted small ml-auto'>Concluido Por: {Nombre}</span>");

                            sb.AppendLine("      </div>");
                            sb.AppendLine($"      <p class='card-text'>Observación: {observacion}</p>");
                            sb.AppendLine($"      <small class='text-muted mt-auto'>Fecha de conclusión: {fechaConclusion.ToString("dd/MM/yyyy")}</small>");
                            sb.AppendLine("    </div>");
                            sb.AppendLine("  </div>");
                            sb.AppendLine("</div>");
                        }
                        sb.AppendLine("</div>");
                    }
                    else
                    {
                        // Mensaje si no hay notas de conclusión
                        sb.AppendLine("<p>No se han registrado notas de conclusión para este oficio.</p>");
                    }


                        dr.Close();
                }
            }
            // Asignar el HTML construido al Literal
            master.ContenidoConcluidoLiteral.Text = sb.ToString();


        }
       
      

    }
}



