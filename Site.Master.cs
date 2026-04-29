using EzSmb;
using Microsoft.Ajax.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;


namespace SURO2
{

    public partial class SiteMaster : MasterPage
    {
        public Literal ContenidoConcluidoLiteral
        {
            get { return LitNotasConclusion; }
        }
        public Literal ContenidoSeguimientosLiteral
        {
            get { return LitSeguimientosHistorial; }
        }
        public DropDownList MiDdlDireccion // Este es el nombre de la propiedad pública
        {
            get { return ddlDireccion1; } // 'DdlDireccion' es el ID de tu control en Site.Master.
                                          // Si tu ID es 'ddlDireccion1', entonces sería 'return ddlDireccion1;'
        }
        public HtmlGenericControl MiSeccionMensajeConc { get { return mensajeConlusion; } }
        public Button MibtnConcluir { get { return btnConcluir; } }
        public HtmlGenericControl MiSeccionTurnadosDiv { get { return divDestinosTurnadosList; } }
        public HtmlGenericControl MiSeccionNoTurno { get { return noTurno; } }
        public HtmlGenericControl MiSeccionTurnar { get { return seccionTurnar; } }

        public HtmlGenericControl DivFolioCaptura { get { return divFolioCaptura; } }
        public HtmlGenericControl DivFolioSecretario { get { return divFolioSecretario; } }
        public HtmlGenericControl DivLugarRemitente { get { return divLugarRemitente; } }
        public HtmlGenericControl DivMunicipio { get { return divMunicipio; } }
        public HtmlGenericControl DivNivelAtencion { get { return divNivelAtencion; } }

        public System.Web.UI.WebControls.HiddenField MiDestinosAgregados { get { return this.FindControl("destinosAgregados") as System.Web.UI.WebControls.HiddenField; } }

        public DropDownList MiDdlArea { get { return ddlArea1; } }
        public DropDownList MiDdlDepartamento { get { return ddlDepartamento1; } }
        public HiddenField MiHfTipoOrgDestino { get { return hfTipoOrgDestino; } }
        public HtmlGenericControl MiSeccionSeguimiento { get { return seccionSeguimiento; } }

        public Button MibtnMostrarSeguimiento { get { return btnMostrarSeguimiento; } } //hago publico y accesible el boton de mostrar seguimiento

        //public CheckBox MiChkActivo { get { return chkActivo; } } // ID="chkActivo"
        public Literal MiFechatxt { get { return FechaAtencion; } } // ID="FechaAtencion"

        // Literales para mostrar los valores

        public Literal MiLitConocimiento { get { return litConocimiento; } } // ID="litConocimiento"
        public Literal MiLitFechaConocimiento { get { return litFechaConocimiento; } } // ID="litFechaConocimiento"
        /* public HtmlGenericControl MiLblFechaAtencionHtml { get { return lblFechaAtencion; } }*/ // Si lblFechaAtencion es un label con runat="server"
        public System.Web.UI.WebControls.Button MiBotonDesdeMaster { get { return this.FindControl("btnMostrarTurno") as System.Web.UI.WebControls.Button; } }
        public System.Web.UI.WebControls.HiddenField MiDivTurnadoA { get { return this.FindControl("divTurnadoA") as System.Web.UI.WebControls.HiddenField; } }
        public System.Web.UI.UpdatePanel MiUpContenidoTurnar { get { return this.FindControl("upContenidoTurnar") as System.Web.UI.UpdatePanel; } }
        //pnlDestinos1
        public System.Web.UI.UpdatePanel PanelDestinos { get { return this.FindControl("pnlDestinos1") as System.Web.UI.UpdatePanel; } }

        public System.Web.UI.WebControls.HiddenField MiHfEstatus { get { return this.FindControl("hfEstatus") as System.Web.UI.WebControls.HiddenField; } }
        //upContenidoTurnar

        public List<string> DestinosSeleccionadosMaster
        {
            get
            {
                if (ViewState["MasterPageDestinos"] == null)
                {
                    ViewState["MasterPageDestinos"] = new List<string>();
                }
                return (List<string>)ViewState["MasterPageDestinos"];
            }
            set
            {
                ViewState["MasterPageDestinos"] = value;
            }
        }


        public void ControlarVisibilidadInicialModal(string estatusOficio)
        {

            bool shouldShowBtnAndChk = !(estatusOficio.ToLower() == "concluido" || estatusOficio.ToLower() == "turnado");

            // Llama a la función JavaScript para ocultar/mostrar el botón y el checkbox
            ScriptManager.RegisterStartupScript(this, this.GetType(), "ToggleBtnChk", $"setupVisibilityBtnAndCheckbox({shouldShowBtnAndChk.ToString().ToLower()});", true);

            // Asegúrate de que la sección de turnar (dropdowns, etc.) esté OCULTA al inicio
            //if (MiSeccionTurnar != null)
            //{
            //    ScriptManager.RegisterStartupScript(this, this.GetType(), "HideTurnarSectionInitial", $"document.getElementById('{MiSeccionTurnar.ClientID}').style.display = 'none';", true);
            //}
        }

        protected void Page_Load(object sender, EventArgs e)
        {






            string currentPath = Request.Url.AbsolutePath.ToLower();

            // Ocultar elementos si estás en la página de login
            if (currentPath.Contains("/login"))
            {
                if (accordionSidebar != null) accordionSidebar.Visible = false;
                if (topbar != null) topbar.Visible = false;
                if (footer != null) footer.Visible = false;

                // Agrega clase para estilo de login
                Page.ClientScript.RegisterStartupScript(this.GetType(), "loginClass",
                    "<script>document.body.classList.add('login-page');</script>", false);

                return;
            }

            // Verificación de sesión: si no hay usuario, redirige a login
            if (Session["TipoUser"] == null || Session["Usuario"] == null)
            {
                Response.Redirect("Login.aspx");
                return;
            }

            if (!IsPostBack)
            {
                if (EsPaginaInternos())
                {
                    // Modo internos
                    ModoInterno = true;
                    lblUsuario.Text = Session["Usuario"].ToString();
                    lblIDUsuario.Text = Session["ID"]?.ToString();
                    hfTipoOrg.Value = Session["TipoOrg"]?.ToString();
                    string tipoUsuario = Session["TipoUser"].ToString();
                }
                else
                {
                    // Modo externos
                    ModoInterno = false;

                    if (hfTipoOrgDestino != null)
                    {
                        hfTipoOrgDestino.Value = "";
                    }


                    if (MiSeccionTurnar != null)
                    {
                        MiSeccionTurnar.Visible = true;
                        MiSeccionTurnar.Style["display"] = "block";

                        CargarDdlPrincipalTurnado();
                    }
                    if (MiDivTurnadoA != null)
                    {
                        MiDivTurnadoA.Visible = false;
                    }
                    if (MiDestinosAgregados != null)
                    {
                        MiDestinosAgregados.Visible = false;
                    }


                    //hfDestinos.Value = "[]"; // Inicializa vacío
                    // Mostrar nombre del usuario
                    lblUsuario.Text = Session["Usuario"].ToString();
                    lblIDUsuario.Text = Session["ID"]?.ToString();
                    hfTipoOrg.Value = Session["TipoOrg"]?.ToString();
                    string tipoUsuario = Session["TipoUser"].ToString();

                    // Asignar visibilidad de menús según el tipo de usuario
                    switch (tipoUsuario)
                    {
                        case "SU":
                        case "A":
                        case "AS":
                            Resumen.Visible = true;
                            Captura.Visible = true;
                            CapturaExternos.Visible = true;
                            CapturaInternos.Visible = true;
                            VisualizacionExternos.Visible = true;
                            VisualizacionInternos.Visible = true;
                            Reportes.Visible = true;
                            break;

                        case "CR":
                            Resumen.Visible = true;
                            Captura.Visible = true;
                            CapturaExternos.Visible = true;
                            CapturaInternos.Visible = true;
                            VisualizacionExternos.Visible = true;
                            VisualizacionInternos.Visible = true;
                            Reportes.Visible = false;
                            break;
                        case "EmA":
                            Resumen.Visible = true;
                            Captura.Visible = true;
                            CapturaExternos.Visible = false;
                            CapturaInternos.Visible = true;
                            VisualizacionExternos.Visible = true;
                            VisualizacionInternos.Visible = true;
                            Reportes.Visible = false;
                            break;
                        case "Visual":
                            Resumen.Visible = true;                          
                            Visualizacion.Visible = true;
                            VisualizacionExternos.Visible = true;
                            VisualizacionInternos.Visible = true;
                            Captura.Visible = true;
                            CapturaExternos.Visible = false;
                            CapturaInternos.Visible = true;
                            Reportes.Visible = true;
                            

                            break;

                        default:
                            // En caso de tipo inválido
                            Response.Redirect("Login.aspx");
                            break;
                    }
                }
            }
        }

        public bool ModoInterno
        {
            get => ViewState["ModoInterno"] != null && (bool)ViewState["ModoInterno"];
            set => ViewState["ModoInterno"] = value;
        }

        private bool EsPaginaInternos()
        {
            string currentPage = Path.GetFileName(Request.Url.AbsolutePath).ToLower();
            return currentPage.Contains("visualizacioninternos"); // en minúsculas
        }

        public void CargarDdlPrincipalTurnado()
        {
            if (EsPaginaInternos())
            {
                CargarDropdownsInternos();
            }
            else
            {
                //SOLO PARA EXTERNOS
                if (MiDdlDireccion != null)
                {
                    int idOficio = Convert.ToInt32(Session["IDOficio"]);
                    int idOrg = 0;
                    if (Session["TipoOrg"] != null)
                    {
                        idOrg = Convert.ToInt32(Session["TipoOrg"]);
                    }
                    int idUser = Convert.ToInt32(Session["ID"]?.ToString());
                    int idOrgAds = Convert.ToInt32(Session["OrgAdscrita"]?.ToString());
                    Funciones fn = new Funciones();
                    DataTable dtOrganizaciones = new DataTable();
                    int idDireccion = fn.ObtenerDireccionUsuario(idOrgAds, idUser);

                    if (idOrg == 1 || idOrg == 2)
                    {
                        dtOrganizaciones = fn.ObtenerDireccionesParaTurno(idDireccion);
                        divDireccion.Visible = true;
                        MiDdlDireccion.DataSource = dtOrganizaciones;
                        MiDdlDireccion.DataTextField = "Nombre";
                        MiDdlDireccion.DataValueField = "ID";
                        MiDdlDireccion.DataBind();

                        MiDdlDireccion.Items.Insert(0, new ListItem("-- Seleccione Dirección --", "0"));
                    }
                    if (idOrg == 3)
                    {
                        dtOrganizaciones = fn.ObtenerDepartamentosParaTurno(idDireccion);
                        if (dtOrganizaciones.Rows.Count > 0)
                        {
                            // MiSeccionTurnar.Visible = true;
                            btnMostrarSeguimiento.Style["display"] = "true";
                            btnMostrarTurno.Style["display"] = "true";
                            divDepartamento1.Visible = true;
                            MiDdlDepartamento.DataSource = dtOrganizaciones;
                            MiDdlDepartamento.DataTextField = "Nombre";
                            MiDdlDepartamento.DataValueField = "ID";
                            MiDdlDepartamento.DataBind();

                            MiDdlDepartamento.Items.Insert(0, new ListItem("-- Seleccione Departamento --", "0"));
                        }
                        else
                        {
                            btnMostrarTurno.Style["display"] = "none";
                            noTurno.Style["display"] = "block";
                            btnMostrarSeguimiento.Style["display"] = "none";
                        }
                    }
                    if (idOrg == 4)
                    {
                        //obtengo el departamento para despues obtener el area
                        int idDepartamento = fn.ObtenerDepartamentoUsuario(idOrgAds);
                        dtOrganizaciones = fn.ObtenerAreasParaTurno(idDepartamento);

                        if (dtOrganizaciones.Rows.Count > 0)
                        {
                            divArea1.Visible = true;
                            MiDdlArea.DataSource = dtOrganizaciones;
                            MiDdlArea.DataTextField = "Nombre";
                            MiDdlArea.DataValueField = "ID";
                            MiDdlArea.DataBind();

                            MiDdlArea.Items.Insert(0, new ListItem("-- Seleccione Área --", "0"));
                        }
                        else
                        {
                            btnMostrarTurno.Style["display"] = "none";
                            noTurno.Style["display"] = "block";
                            btnMostrarSeguimiento.Style["display"] = "none";

                        }

                    }
                    if (idOrg == 5)
                    {

                        btnMostrarTurno.Style["display"] = "none";






                    }
                    if (MiUpContenidoTurnar != null)
                    {
                        MiUpContenidoTurnar.Update();
                    }
                }
            }
        }


        public void CargarDropdownsInternos()
        {
            Funciones fn = new Funciones();
            DataTable dtDirecciones = fn.ObtenerTodasLasDirecciones();

            divDireccion.Visible = true;
            MiDdlDireccion.DataSource = dtDirecciones;
            MiDdlDireccion.DataTextField = "Nombre";
            MiDdlDireccion.DataValueField = "ID";
            MiDdlDireccion.DataBind();
            MiDdlDireccion.Items.Insert(0, new ListItem("-- Seleccione Dirección --", "0"));

            // Limpia los demás
            MiDdlDepartamento.Items.Clear();
            MiDdlDepartamento.Items.Insert(0, new ListItem("-- Seleccione Departamento --", "0"));
            MiDdlArea.Items.Clear();
            MiDdlArea.Items.Insert(0, new ListItem("-- Seleccione Área --", "0"));

            divDepartamento1.Visible = false;
            divArea1.Visible = false;

            if (MiUpContenidoTurnar != null)
                MiUpContenidoTurnar.Update();
        }




        public void MostrarModalTurnar()
        {
            string tipoUsuario = Session["TipoUser"].ToString();


            string cleanupScript = "ocultarSeccionTurnar();";
            ScriptManager.RegisterStartupScript(this, this.GetType(), "resetModalTurnar", cleanupScript, true);



            // 3. Maneja la lógica específica del tipo de usuario para el botón "Turnar".
            if (tipoUsuario == "SU" || tipoUsuario == "A" || tipoUsuario == "AS")
            {
                int tipoOrg = Convert.ToInt32(Session["TipoOrg"] ?? "0");
                hfTipoOrg.Value = tipoOrg.ToString();


            }
            else // Para usuarios que no pueden "turnar"
            {
                // Si el tipo de usuario no tiene permiso para turnar, oculta completamente el botón "Turnar".
                // Usar Visible = false es apropiado aquí ya que no quieres que se renderice en absoluto.
                btnMostrarTurno.Visible = false;
                // Además, asegúrate explícitamente de que los otros botones de acción estén ocultos si no lo están ya.
                btnAgregarDestino.Style["display"] = "none";
                btnCancelarTurno.Style["display"] = "none";
                btnConfirmarTurno.Style["display"] = "none";
            }

            // 4. Finalmente, muestra el modal de Bootstrap.
            ScriptManager.RegisterStartupScript(this, this.GetType(), "pdfModal", "$('#pdfModal').modal('show');", true);
        }
        private void PrepararDropdownsTurnado()
        {
            int tipoOrg = Convert.ToInt32(Session["TipoOrg"] ?? "0");
            hfTipoOrg.Value = tipoOrg.ToString();
            int tipoOrgAdscrita = Convert.ToInt32(Session["OrgAdscrita"]);

            divDireccion.Visible = false;
            divDepartamento1.Visible = false;
            divArea1.Visible = false;



            switch (tipoOrg)
            {
                case 1: // Dirección para secretaría
                case 2: // Dirección para subsecretaría
                        // Solo mostrar el div de Dirección
                    divDireccion.Visible = true;

                    // Cargar datos del DDL de Dirección
                    try
                    {
                        CargarDdlPrincipalTurnado();
                    }
                    catch (Exception ex)
                    {
                        // Manejo de errores
                        Console.WriteLine($"Error al cargar direcciones: {ex.Message}");
                    }

                    break;

                case 3: // Si es una Dirección la que turna, solo puede turnar a Departamentos
                        // Solo mostrar el div de Departamento
                    divDepartamento1.Visible = true;
                    try
                    {
                        CargarDdlPrincipalTurnado();
                    }
                    catch (Exception ex)
                    {
                        // Manejo de errores
                        Console.WriteLine($"Error al cargar departamentos: {ex.Message}");
                    }

                    break;

                case 4: // Si es un Departamento, solo puede turnar a un Área
                        // Solo mostrar el div de Área
                    try
                    {
                        CargarDdlPrincipalTurnado();
                    }
                    catch (Exception ex)
                    {
                        // Manejo de errores
                        Console.WriteLine($"Error al cargar áreas: {ex.Message}");
                    }
                    break;

                case 5: // Si es un Área, no puede turnar a nada
                        // En este caso, como todos los divs se ocultaron al inicio de esta función,
                        // no se necesita hacer nada más, y ningún dropdown se mostrará.
                    break;

            }
            upContenidoTurnar.Update();
        }




        protected void ddlDepartamento1_SelectedIndexChanged(object sender, EventArgs e)
        {
            //SOLO PARA OFICIOS INTERNOS 
            if (EsPaginaInternos())
            {
                Funciones fn = new Funciones();

                if (int.TryParse(ddlDepartamento1.SelectedValue, out int idDep) && idDep > 0)
                {
                    // Cargar las áreas dependientes
                    DataTable dtAreas = fn.ObtenerAreasParaTurno(idDep);
                    divArea1.Visible = true;

                    ddlArea1.DataSource = dtAreas;
                    ddlArea1.DataTextField = "Nombre";
                    ddlArea1.DataValueField = "ID";
                    ddlArea1.DataBind();
                    ddlArea1.Items.Insert(0, new ListItem("-- Seleccione Área --", "0"));
                }
                else
                {
                    divArea1.Visible = false;
                }


                // Asegura que la sección de turnar y los botones sigan visibles
                seccionTurnar.Style["display"] = "block";
                lblTurnadoA.Style["display"] = "block";
                btnAgregarDestino.Style["display"] = "block";
                btnCancelarTurno.Style["display"] = "block";
                btnConfirmarTurno.Style["display"] = "block";

                // Actualiza el UpdatePanel sin cerrar la vista
                if (MiUpContenidoTurnar != null)
                    MiUpContenidoTurnar.Update();

                //  Reabre el modal si se cierra al hacer postback
                ScriptManager.RegisterStartupScript(this, this.GetType(), "ReabrirModalTurnar", "$('#pdfModal').modal('show');", true);

                return; // Detén aquí si es modo interno
            }


            // Ocultar el div de área por defecto al cambiar el departamento.
            // Se mostrará solo si hay áreas válidas para la nueva selección.
            divArea1.Visible = false;
            seccionTurnar.Style["display"] = "block";
            lblTurnadoA.Style["display"] = "block";
            btnAgregarDestino.Style["display"] = "block";
            btnCancelarTurno.Style["display"] = "block";
            btnConfirmarTurno.Style["display"] = "block";

            int idSeleccionado = 0;
            SiteMaster masterPage = this.Master as SiteMaster;
            if (masterPage == null)
            {
                masterPage = this as SiteMaster;
                if (masterPage == null) return;
            }



            if (int.TryParse(ddlDepartamento1.SelectedValue, out idSeleccionado) && idSeleccionado > 0)
            {
                Funciones fn = new Funciones();
                int idTipoOrgDestino = fn.ObtenerTipoOrgDesdeTablaSimple(idSeleccionado, "Departamento");

                //Guardo el Tipo Organización del destino en un HiddenField para usarlo después
                masterPage.MiHfTipoOrgDestino.Value = idTipoOrgDestino.ToString();


            }
            else
            {
                masterPage.MiHfTipoOrgDestino.Value = "0"; // Si no se selecciona una dirección válida, resetea el HiddenField a 0
                                                           // Si se selecciona "--Seleccione--" en ddlDireccion1, asegúrate de ocultar y limpiar
                                                           // los dropdowns de Departamento y Área.

                //  ddlDepartamento1.Items.Clear();
                divArea1.Style["display"] = "none";
                //ddlArea1.Items.Clear();

            }
            masterPage.MiUpContenidoTurnar.Update(); // Actualiza el UpdatePanel para reflejar los cambios en la UI

        }

        public DateTime? FechaMaxAtencion { get; set; }
        protected void btnConfirmarTurno_Click(object sender, EventArgs e)
        {
            int tipoOrg = 0; int.TryParse(hfTipoOrg.Value, out tipoOrg);
             
        int idDocumento = 0;
            string destinosJson = hfDestinos.Value;
            int idUsuario = Convert.ToInt32(Session["ID"]);
            int idOrg = Convert.ToInt32(Session["TipoOrg"]);
            int orgAds = Convert.ToInt32(Session["OrgAdscrita"]);

            if (HttpContext.Current.Session["IDOficio"] == null ||
                !int.TryParse(HttpContext.Current.Session["IDOficio"].ToString(), out idDocumento) || idDocumento <= 0)
            {
                ScriptManager.RegisterStartupScript(this.Page, this.GetType(), "alert", "mostrarAlertaEnModal('Error: No se ha seleccionado un oficio válido para turnar.');", true);
                return;
            }

            if (string.IsNullOrWhiteSpace(destinosJson) || destinosJson == "[]")
            {
                ScriptManager.RegisterStartupScript(this.Page, this.GetType(), "alert", "mostrarAlertaEnModal('No hay destinos seleccionados para turnar.');", true);
                return;
            }

            List<DestinoTurnado> destinos;
            try
            {
                destinos = JsonConvert.DeserializeObject<List<DestinoTurnado>>(destinosJson);
            }
            catch
            {
                ScriptManager.RegisterStartupScript(this.Page, this.GetType(), "alert", "mostrarAlertaEnModal('Error al procesar los destinos seleccionados.');", true);
                return;
            }

            // 🔹 Interno si estás en VisualizacionInternos.aspx (ajusta si usas otra bandera)
            bool esInterno = Request.Url.AbsolutePath.ToLower().Contains("visualizacioninternos");

            Funciones func = new Funciones();
            using (SqlConnection con = func.ConBD())
            using (SqlCommand cmd = new SqlCommand("SP_TurnarOficio", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                try
                {


                    if (esInterno)
                    {
                        // Consolidar: obtener IDs de cada nivel
                        int? idDir = null, idDep = null, idArea = null;

                        var dDir = destinos.FirstOrDefault(d => d.Tipo == "2");
                        var dDep = destinos.FirstOrDefault(d => d.Tipo == "3");
                        var dArea = destinos.FirstOrDefault(d => d.Tipo == "4");

                        if (dDir != null && int.TryParse(dDir.Id, out int tmpDir)) idDir = tmpDir;
                        if (dDep != null && int.TryParse(dDep.Id, out int tmpDep)) idDep = tmpDep;
                        if (dArea != null && int.TryParse(dArea.Id, out int tmpArea)) idArea = tmpArea;

                        // ParaConocimiento: si cualquiera está marcado
                        bool paraConocimiento = destinos.Any(x => x.Conocimiento);

                        // FechaMax: toma la más próxima si mandas varias (o la primera disponible)
                        DateTime? fechaMax = destinos
                            .Where(x => x.FechaMaxAtencion != DateTime.MinValue)
                            .Select(x => (DateTime?)x.FechaMaxAtencion)
                            .OrderBy(x => x)
                            .FirstOrDefault();

                        // Detectar "Otro" (Dirección=12)
                        bool esOtro = (idDir.HasValue && idDir.Value == 12);

                        // Si es "Otro", forzamos a que NO se mande depto/área (por si acaso)
                        if (esOtro)
                        {
                            idDep = null;
                            idArea = null;
                        }

                        // IdTipoOrg final: el más específico disponible (Área>Depto>Dirección)
                        int idTipoOrgDestinoFinal = idArea.HasValue ? 4 : (idDep.HasValue ? 3 : 2);

                        // IdDestino final (si tu SP lo ocupa): usa el más específico también
                        int? idDestinoFinal = idArea ?? idDep ?? idDir;

                        // Texto externo (solo si Dirección=12)
                        string destinatarioExterno = null;
                        if (esOtro)
                        {
                            destinatarioExterno = (txtTextoDireccion12.Text ?? "").Trim();

                            if (string.IsNullOrWhiteSpace(destinatarioExterno))
                            {
                                ScriptManager.RegisterStartupScript(
                                    this.Page, this.GetType(), "alert",
                                    "mostrarAlertaEnModal('Escribe el destinatario en el campo \"Otro\".');", true);
                                return;
                            }
                        }

                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@IdOficio", idDocumento);
                        cmd.Parameters.AddWithValue("@IdDestino", (object)idDestinoFinal ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@IdDireccion", (object)idDir ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@IdDepartamento", (object)idDep ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@IdArea", (object)idArea ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@IdTipoOrg", idTipoOrgDestinoFinal);
                        cmd.Parameters.Add("@FechaMax", SqlDbType.Date).Value =
                        fechaMax.HasValue ? (object)fechaMax.Value.Date : DBNull.Value;
                        cmd.Parameters.AddWithValue("@Conocimiento", paraConocimiento);
                        cmd.Parameters.AddWithValue("@Estatus", 2);
                        cmd.Parameters.AddWithValue("@IdUser", idUsuario);
                        cmd.Parameters.AddWithValue("@IdOrgDestino", idTipoOrgDestinoFinal); 
                        cmd.Parameters.AddWithValue("@OrgAdscrita", orgAds);
                        cmd.Parameters.AddWithValue("@EsInterno", 1);
                        cmd.Parameters.Add("@DestinatarioExterno", SqlDbType.NVarChar, 300).Value =
                        esOtro ? (object)destinatarioExterno : DBNull.Value;
                        cmd.ExecuteNonQuery();
                    }
                    else
                    {
                        // EXTERNOS: uno por destino (igual que antes), pero ahora mandamos EsInterno = 0
                        foreach (var destino in destinos)
                        {
                            if (!int.TryParse(destino.Id, out int idDestinoNumerico)) continue;
                            if (!int.TryParse(destino.Tipo, out int idOrgDestino)) continue;

                            cmd.Parameters.Clear();
                            cmd.Parameters.AddWithValue("@IdOficio", idDocumento);
                            cmd.Parameters.AddWithValue("@IdDestino", idDestinoNumerico);
                            cmd.Parameters.AddWithValue("@IdDireccion", DBNull.Value);
                            cmd.Parameters.AddWithValue("@IdDepartamento", DBNull.Value);
                            cmd.Parameters.AddWithValue("@IdArea", DBNull.Value);
                            cmd.Parameters.AddWithValue("@IdTipoOrg", idOrg);
                            cmd.Parameters.Add("@FechaMax", SqlDbType.Date).Value =
                             (destino.FechaMaxAtencion.HasValue)
                                 ? (object)destino.FechaMaxAtencion.Value.Date
                                 : DBNull.Value;
                            cmd.Parameters.AddWithValue("@Conocimiento", destino.Conocimiento);
                            cmd.Parameters.AddWithValue("@Estatus", 2);
                            cmd.Parameters.AddWithValue("@IdUser", idUsuario);
                            cmd.Parameters.AddWithValue("@IdOrgDestino", idOrgDestino);
                            cmd.Parameters.AddWithValue("@OrgAdscrita", orgAds);
                            cmd.Parameters.AddWithValue("@EsInterno", 0); // 🔹 importante
                            cmd.Parameters.Add("@DestinatarioExterno", SqlDbType.NVarChar, 300).Value = DBNull.Value;


                            cmd.ExecuteNonQuery();
                        }
                    }

                    // refrescos y limpieza como ya lo hacías…
                    Visualizacion contentPage = this.Page as Visualizacion;
                    if (contentPage != null && contentPage.MiGridViewOficios != null && contentPage.MiUpdatePanelOficios != null)
                    {
                        contentPage.CargarOficios();
                        contentPage.MiUpdatePanelOficios.Update();
                    }

                    ScriptManager.RegisterStartupScript(this.Page, this.Page.GetType(), "showSuccessModal", "mostrarModalConRedireccion('Destinos turnados correctamente.', '');", true);
                    hfDestinos.Value = "[]";
                    ScriptManager.RegisterStartupScript(this.Page, this.GetType(), "cleanupTurnadoUI", "cargarDestinosDesdeHiddenField();", true);
                    HttpContext.Current.Session.Remove("IDOficio");
                    FechaAtencion.Text = string.Empty;
                }
                catch (Exception ex)
                {
                    ScriptManager.RegisterStartupScript(this.Page, this.GetType(), "errorAlert", "mostrarAlertaEnModal('Error al guardar los destinos: " + ex.Message.Replace("'", "\\'") + "');", true);
                }
            }
        }




        protected void btnMostrarTurno_Click(object sender, EventArgs e)
        {
            //SiteMaster masterPage = this.Master as SiteMaster;

            //if (masterPage == null)

            //    masterPage = this as SiteMaster;

            //// ScriptManager.RegisterStartupScript(this, this.GetType(), "HideBtnChkOnTurn", "setupVisibilityBtnAndCheckbox(false);", true);
            //if (masterPage.MiBotonDesdeMaster != null)
            //{
            //    masterPage.MiBotonDesdeMaster.Visible = true;

            //}


            //if (masterPage.MiSeccionTurnar != null)
            //{
            //    masterPage.MiSeccionTurnar.Visible = true;
            //}

            //if (masterPage.MiDivTurnadoA != null)
            //{
            //    MiDivTurnadoA.Visible = true;
            //}
            //if (masterPage.MiDestinosAgregados != null)
            //{
            //    MiDestinosAgregados.Visible = true;
            //}

            PrepararDropdownsTurnado();




        }

        protected void ddlDireccion1_SelectedIndexChanged1(object sender, EventArgs e)
        {
            if (EsPaginaInternos())
            {
                // Lógica para internos si es necesario
                Funciones fn = new Funciones();

                // 🔹 Verifica que se haya seleccionado una dirección válida
                if (int.TryParse(ddlDireccion1.SelectedValue, out int idDireccion) && idDireccion > 0)
                {
                    // Llenar Departamentos según la Dirección
                    DataTable dtDepartamentos = fn.ObtenerDepartamentosParaTurno(idDireccion);
                    divDepartamento1.Visible = true;
                    ddlDepartamento1.DataSource = dtDepartamentos;
                    ddlDepartamento1.DataTextField = "Nombre";
                    ddlDepartamento1.DataValueField = "ID";
                    ddlDepartamento1.DataBind();
                    ddlDepartamento1.Items.Insert(0, new ListItem("-- Seleccione Departamento --", "0"));

                    // Reinicia el dropdown de Áreas
                    divArea1.Visible = false;
                    ddlArea1.Items.Clear();
                    ddlArea1.Items.Insert(0, new ListItem("-- Seleccione Área --", "0"));
                }
                else
                {
                    divDepartamento1.Visible = false;
                    divArea1.Visible = false;
                }

                //Mantiene visible la sección de turnar después del postback
                seccionTurnar.Style["display"] = "block";
                lblTurnadoA.Style["display"] = "block";
                btnAgregarDestino.Style["display"] = "block";
                btnCancelarTurno.Style["display"] = "block";
                btnConfirmarTurno.Style["display"] = "block";

                // Actualiza el UpdatePanel
                if (MiUpContenidoTurnar != null)
                    MiUpContenidoTurnar.Update();

                return; // Aquí termina si es interno
            }


            // Si NO es página interna (oficios externos)
            divDepartamento1.Style["display"] = "none";
            //ddlDepartamento1.Items.Clear(); // Limpia los elementos existentes
            divArea1.Style["display"] = "none";
            //ddlArea1.Items.Clear(); // Limpia los elementos existentes

            // Asegura que la sección de turnar siga visible
            divDireccion.Style["display"] = "block";
            seccionTurnar.Style["display"] = "block";
            lblTurnadoA.Style["display"] = "block";
            btnAgregarDestino.Style["display"] = "block";
            btnCancelarTurno.Style["display"] = "block";
            btnConfirmarTurno.Style["display"] = "block";
            int idSeleccionado = 0;
            SiteMaster masterPage = this.Master as SiteMaster;
            if (masterPage == null)
            {
                masterPage = this as SiteMaster;
                if (masterPage == null) return;
            }
            // Solo procede si se ha seleccionado una dirección válida (no "--Seleccione--")
            if (int.TryParse(ddlDireccion1.SelectedValue, out idSeleccionado) && idSeleccionado > 0)
            {
                Funciones fn = new Funciones();
                int idTipoOrgDestino = fn.ObtenerTipoOrgDesdeTablaSimple(idSeleccionado, "Direccion");

                if (MiHfTipoOrgDestino != null)
                {
                    //Guardo el Tipo Organización del destino en un HiddenField para usarlo después
                    MiHfTipoOrgDestino.Value = idTipoOrgDestino.ToString();
                }




            }
            else
            {
                if (MiHfTipoOrgDestino != null)
                {
                    masterPage.MiHfTipoOrgDestino.Value = "";
                }
                // masterPage.MiHfTipoOrgDestino.Value = "0"; // Si no se selecciona una dirección válida, resetea el HiddenField a 0
                // Si se selecciona "--Seleccione--" en ddlDireccion1, asegúrate de ocultar y limpiar
                // los dropdowns de Departamento y Área.
                divDepartamento1.Style["display"] = "none";
                //  ddlDepartamento1.Items.Clear();
                divArea1.Style["display"] = "none";
                //ddlArea1.Items.Clear();

            }
            masterPage.MiUpContenidoTurnar.Update(); // Actualiza el UpdatePanel para reflejar los cambios en la UI

        }

        protected void btnGuardarSeguimiento_Click(object sender, EventArgs e)
        {
            int idUsuario = Convert.ToInt32(Session["ID"]);

            string notasSegu = txtSeguimiento.Text.Trim();
            HttpPostedFile postedFile = fuArchivoPdf.PostedFile;

            //obtengo el idOficio desde la variable de sesión
            int idOficio = 0;
            if (HttpContext.Current.Session["IDOficio"] != null)
            {
                idOficio = Convert.ToInt32(HttpContext.Current.Session["IDOficio"]);
            }
            else
            {


                ScriptManager.RegisterStartupScript(this, this.GetType(), "ErrorIDUsuario", "mostrarAlertaEnModal('Error: No se pudo obtener el ID del oficio. La sesión pudo haber expirado.');", true);
                return;
            }

            int IDtipoOrg = 0;
            if (HttpContext.Current.Session["OrgAdscrita"] != null)
            {
                IDtipoOrg = Convert.ToInt32(HttpContext.Current.Session["OrgAdscrita"]);
            }
            else
            {
                ScriptManager.RegisterStartupScript(this, this.GetType(), "ErrorIDOrg", "mostrarAlertaEnModal('Error: No se pudo obtener el ID de la organización del usuario.');", true);
                return;
            }

            if (string.IsNullOrEmpty(notasSegu) && postedFile.ContentLength == 0)
            {
                ScriptManager.RegisterStartupScript(this, this.GetType(), "Validacion", "mostrarAlertaEnModal('Error: Por favor, ingresa notas de seguimiento o adjunta un archivo PDF.');", true);
                return;
            }

            string fechaHoraActual = DateTime.Now.ToString("yyyy-MM-dd_HH:mm:ss");
            string rutaArchivoAdjunto = null;


            if (postedFile.ContentLength > 0)
            {
                //valido que solo se puedan subir archivos PDF
                if (postedFile.ContentType != "application/pdf")
                {
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "ValidacionPDF", "mostrarAlertaEnModal('Error: Solo se permiten archivos PDF.');", true);
                    return;

                }
                // valido que el tamaño no supere los 20 MB
                if (postedFile.ContentLength > (20 * 1024 * 1024))
                {
                    ScriptManager.RegisterStartupScript(
                        this,
                        this.GetType(),
                        "ValidacionPDF",
                        "mostrarAlertaEnModal('Error: El archivo no debe superar los 5 MB.');",
                        true
                    );
                    return;
                }

                try
                {
                    //Defino donde se guardarán los archivos
                    //string uploadFolder = Server.MapPath("~/ArchivosSeguimiento/");

                    //Genera un nombre de archivo para el documento
                    string nombreOriginalArchivo = Path.GetFileName(postedFile.FileName);
                    string extension = Path.GetExtension(nombreOriginalArchivo);
                    string nombreNuevoBase = $"Seguimiento_{idOficio}_Usuario_{idUsuario}_{fechaHoraActual}";
                    string nombreUnico = SanitizeFileName(nombreNuevoBase + extension);

                    string rutaNAS = @"\\10.18.24.185\Datos_Aplicaciones\SURO\Seguimiento\";
                    string rutaFinalNAS = Path.Combine(rutaNAS, nombreUnico);

                    // Llama al método asíncrono y espera a que termine
                    string rutaSubida = SubirArchivoEnNAS(postedFile.InputStream, rutaFinalNAS);
                    // Verifica si la subida fue exitosa
                    if (!string.IsNullOrEmpty(rutaSubida))
                    {
                        rutaArchivoAdjunto = rutaSubida; // Asigna la ruta final del NAS
                    }
                    else
                    {
                        ScriptManager.RegisterStartupScript(this, this.GetType(), "ErrorSubida", "mostrarAlertaEnModal('Error: No se pudo subir el archivo al NAS.');", true);
                        return;
                    }

                    // Guarda la ruta relativa en la base de datos
                    // rutaArchivoAdjunto = $"~/ArchivosSeguimiento/{nombreUnico}"; en local
                }
                catch (Exception ex)
                {
                    string mensajeError = $"Error al subir el archivo: {ex.Message}";
                    string mensajeEscapado = mensajeError.Replace("'", "\\'");
                    string script = $"mostrarAlertaEnModal('{mensajeEscapado}');";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "ErrorSubida", script, true);

                    return;

                }


            }
            Funciones func = new Funciones();
            try
            {
                using (SqlConnection con = func.ConBD())
                {
                    using (SqlCommand cmd = new SqlCommand("SP_GuardaSeguimiento", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@idOficio", idOficio);
                        cmd.Parameters.AddWithValue("@NotasSeguimiento", notasSegu);
                        object rutaValor = DBNull.Value;
                        if (!string.IsNullOrEmpty(rutaArchivoAdjunto))
                        {
                            rutaValor = rutaArchivoAdjunto;
                        }
                        cmd.Parameters.AddWithValue("@RutaArchivoAdjunto", rutaValor);
                        cmd.Parameters.AddWithValue("@IdUsuarioRegistra", idUsuario);
                        cmd.Parameters.AddWithValue("@IdTipoOrg", IDtipoOrg);
                        cmd.ExecuteNonQuery();


                    }
                }
                //Registro éxitoso
                string script = @"mostrarModalConRedireccion('Éxito al guardar el seguimiento del Oficio.', 'Visualizacion.aspx');";
                ScriptManager.RegisterStartupScript(this, this.GetType(), "mostrarModal", script, true);

                seccionSeguimiento.Style["display"] = "none";
                btnMostrarSeguimiento.Visible = true;
                txtSeguimiento.Text = string.Empty;
                upContenidoTurnar.Update();

            }
            catch (SqlException sqlEx)
            {
                // Captura excepciones específicas de SQL
                string mensajeError = $"Error en la base de datos: {sqlEx.Message}";
                string mensajeEscapado = mensajeError.Replace("'", "\\'");

                // Puedes loggear el sqlEx.Message y sqlEx.Number (código de error SQL)
                ScriptManager.RegisterStartupScript(this, this.GetType(), "ErrorDB", $"mostrarAlertaEnModal('{mensajeEscapado}');", true);
            }
            catch (Exception ex) // Captura otras excepciones generales
            {
                string mensajeError = $"Error al guardar el seguimiento: {ex.Message}";
                string mensajeEscapado = mensajeError.Replace("'", "\\'");

                ScriptManager.RegisterStartupScript(this, this.GetType(), "ErrorGeneral", $"mostrarAlertaEnModal('{mensajeEscapado}');", true);
            }

        }

        // Método síncrono que puedes llamar desde el SiteMaster.
        public string SubirArchivoEnNAS(Stream fileStream, string rutaFinal)
        {
            // Llama al método asíncrono y espera a que la tarea termine de forma síncrona.
            // .GetAwaiter().GetResult() es una forma de esperar de forma síncrona.
            // Puedes también usar .Result o .Wait()
            try
            {
                return SubirArchivoEzSMB(fileStream, rutaFinal).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                // El manejo de excepciones debe estar aquí para capturar cualquier error
                // que ocurra durante la ejecución de la tarea.
                Console.WriteLine($"Error al subir el archivo de forma síncrona: {ex.Message}");
                // Opcional: relanzar la excepción para que sea manejada por el SiteMaster
                throw new Exception("Hubo un error al subir el archivo.", ex);
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
                var folder = await Node.GetNode(remotePath, user, password).ConfigureAwait(false);


                // 5. Usar directamente el Stream que ya recibiste.
                await folder.Write(fileStream, fileName).ConfigureAwait(false);

                return rutaFinal;

            }
            catch (Exception ex)
            {
                // Log más completo para diagnóstico de fallas intermitentes con NAS.
                System.Diagnostics.Trace.TraceError(
                    $"[NAS_UPLOAD_ERROR] Ruta: {rutaFinal}. Mensaje: {ex.Message}. Detalle: {ex}");
                return string.Empty;
            }
        }

        private string SanitizeFileName(string fileName)
        {
            string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"[{0}]+", invalidChars);
            return System.Text.RegularExpressions.Regex.Replace(fileName, invalidRegStr, "_");
        }



        protected void btnGuardaTermino_Click(object sender, EventArgs e)
        {

        }

        protected void ddlArea1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int idSeleccionado = 0;

            //  Si es página interna (turnado escalonado)
            if (EsPaginaInternos())
            {
                // Mantén visibles los tres niveles
                divDireccion.Visible = true;
                divDepartamento1.Visible = true;
                divArea1.Visible = true;

                // Mantén visible la sección de turnar y los controles
                seccionTurnar.Style["display"] = "block";
                lblTurnadoA.Style["display"] = "block";
                btnAgregarDestino.Style["display"] = "block";
                btnCancelarTurno.Style["display"] = "block";
                btnConfirmarTurno.Style["display"] = "block";

                // Si seleccionó un área válida
                if (int.TryParse(ddlArea1.SelectedValue, out idSeleccionado) && idSeleccionado > 0)
                {
                    Funciones fn = new Funciones();
                    int idTipoOrgDestino = fn.ObtenerTipoOrgDesdeTablaSimple(idSeleccionado, "Area");

                    //  Como estás en el master, accede directo
                    MiHfTipoOrgDestino.Value = idTipoOrgDestino.ToString();
                }
                else
                {
                    MiHfTipoOrgDestino.Value = "0";
                }

                // Actualiza el UpdatePanel para reflejar cambios
                if (MiUpContenidoTurnar != null)
                    MiUpContenidoTurnar.Update();

                // Reabre el modal si se cerró con el postback
                ScriptManager.RegisterStartupScript(this, this.GetType(), "MantenerModalTurnarAbiertoArea",
                    "$('#pdfModal').modal('show');", true);

                return; //  Termina aquí si es internos
            }

            //  Si NO es página interna (oficios externos)
            divDireccion.Visible = false;
            divDepartamento1.Visible = false;

            seccionTurnar.Style["display"] = "block";
            lblTurnadoA.Style["display"] = "block";
            btnAgregarDestino.Style["display"] = "block";
            btnCancelarTurno.Style["display"] = "block";
            btnConfirmarTurno.Style["display"] = "block";

            if (int.TryParse(ddlArea1.SelectedValue, out idSeleccionado) && idSeleccionado > 0)
            {
                Funciones fn = new Funciones();
                int idTipoOrgDestino = fn.ObtenerTipoOrgDesdeTablaSimple(idSeleccionado, "Area");
                MiHfTipoOrgDestino.Value = idTipoOrgDestino.ToString();
            }
            else
            {
                MiHfTipoOrgDestino.Value = "0";
            }

            if (MiUpContenidoTurnar != null)
                MiUpContenidoTurnar.Update();

            ScriptManager.RegisterStartupScript(this, this.GetType(), "MantenerModalTurnarAbiertoAreaExt",
                "$('#pdfModal').modal('show');", true);
        }

        protected void btnGuardaTermino_Click1(object sender, EventArgs e)
        {
            string observaciones;
            int idOficio = 0;
            int idOrgUsuario = 0;
            int idUsuario = 0;

            if (Session["ID"] != null)
            {
                idUsuario = Convert.ToInt32(Session["ID"]);
            }
            else
            {
                ScriptManager.RegisterStartupScript(this, this.GetType(), "ErrorIDUsuario", "mostrarAlertaEnModal('No se puede obtener el ID del usuario.');", true);
                return;

            }

            if (Session["IDOficio"] != null)
            {
                idOficio = Convert.ToInt32(Session["IDOficio"]);
            }
            else
            {
                ScriptManager.RegisterStartupScript(this, this.GetType(), "ErrorIDOficio", "mostrarAlertaEnModal('No se puede obtener el ID del oficio seleccionado.');", true);
                return;
            }

            if (Session["OrgAdscrita"] != null)
            {
                idOrgUsuario = Convert.ToInt32(Session["OrgAdscrita"]);
            }
            else
            {
                ScriptManager.RegisterStartupScript(this, this.GetType(), "ErrorOrgAdscritaUser", "mostrarAlertaEnModal('No se puede obtener el ID de la organización del Usuario.');", true);
                return;

            }

            observaciones = txtObservacion.Text.Trim();
            if (idOficio > 0 && idOrgUsuario > 0)
            {

                try
                {
                    Funciones fn = new Funciones();

                    using (SqlConnection conn = fn.ConBD())
                    {
                        using (SqlCommand cmd = new SqlCommand("SP_GuardaFinalizacionturno", conn))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
                            cmd.Parameters.AddWithValue("@IdOficio", idOficio);
                            cmd.Parameters.AddWithValue("@IdOrgAds", idOrgUsuario);
                            cmd.Parameters.AddWithValue("@Observacion", string.IsNullOrEmpty(observaciones) ? (object)DBNull.Value : observaciones);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    ScriptManager.RegisterStartupScript(this.Page, this.Page.GetType(), "ExitoUltimoTurnado", "mostrarModalConRedireccion('Observaciones guardadas y turno finalizado.', '');", true);
                    noTurno.Style["display"] = "none";
                    //Accedo al gridview de oficios en visualizacion para refrescar el updatePanel
                    Visualizacion contentPage = this.Page as Visualizacion;
                    var paginaInternos = this.Page as SURO2.VisualizacionInternos;
                    if (paginaInternos != null)
                    {
                        paginaInternos.CargarOficios();                 // vuelve a consultar el SP_MuestraOficiosInternos_xUsuario
                        if (paginaInternos.MiUpdatePanelOficios != null)
                            paginaInternos.MiUpdatePanelOficios.Update();  // refresca visualmente la grid dentro del UpdatePanel
                    }
                    if (contentPage != null && contentPage.MiGridViewOficios != null && contentPage.MiUpdatePanelOficios != null)
                    {
                        contentPage.CargarOficios();
                        contentPage.MiUpdatePanelOficios.Update();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("ADVERTENCIA: No se pudo acceder al GridView o UpdatePanel en la página de contenido (Visualizacion.aspx).");
                    }
                }
                catch (Exception ex)
                {
                    string errorMessage = "Error: No se puede guardar la finalización del turno. Detalles: " + ex.Message;
                    string escapedMessage = errorMessage.Replace("'", "\\'");
                    string script = $"mostrarAlertaEnModal('{escapedMessage}');";

                    ScriptManager.RegisterStartupScript(this, this.GetType(), "ErrorAlFinalizarTurnado", script, true);
                    return;

                }
            }
            else
            {
                ScriptManager.RegisterStartupScript(this.Page, this.Page.GetType(), "ErrorUltimoTurnado2", "mostrarAlertaEnModal('Error: Datos insuficientes para procesamiento.'  + , '');", true);

            }
        }


        protected void btnDescargar_Click(object sender, EventArgs e)
        {
            DateTime? fechaInicio = null;
            DateTime? fechaFin = null;
            int? estatus = null;

            // Obtener datos de sesión
            int idUsuario = 0, tipoOrg = 1, orgAdscrita = 0;
            if (Session["ID"] != null)
                int.TryParse(Session["ID"].ToString(), out idUsuario);
            if (Session["TipoOrg"] != null)
                int.TryParse(Session["TipoOrg"].ToString(), out tipoOrg);
            if (Session["OrgAdscrita"] != null)
                int.TryParse(Session["OrgAdscrita"].ToString(), out orgAdscrita);

            // Detecta cuál botón hizo el llamado
            var btn = sender as Button;
            string btnID = btn?.ID ?? ((LinkButton)sender).ID;

            if (btnID == "lnkDescargarGeneral") // Botón General
            {
                // No necesitas filtrar nada, genera el general
            }
            else if (btnID == "btnDescargarFecha") // Modal Por Fecha
            {
                DateTime temp;
                if (DateTime.TryParse(txtFechaInicio.Text, out temp)) fechaInicio = temp;
                if (DateTime.TryParse(txtFechaFin.Text, out temp)) fechaFin = temp;
            }
            else if (btnID == "btonDescargaEstatus") // Modal Por Estatus
            {
                int tempEstatus;
                if (!string.IsNullOrWhiteSpace(ddlEstatus.SelectedValue) && int.TryParse(ddlEstatus.SelectedValue, out tempEstatus))
                    estatus = tempEstatus;
            }

            Reportes reportes = new Reportes();
            byte[] archivo = reportes.GenerarReporteExcelOficios(idUsuario, tipoOrg, orgAdscrita, fechaInicio, fechaFin, estatus);

            Response.Clear();
            Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            Response.AddHeader("content-disposition", "attachment; filename=Reporte_Oficios.xlsx");
            Response.BinaryWrite(archivo);
            Response.End();
        }

        protected void btnDescargarPendientes_Click(object sender, EventArgs e)
        {
            int? idUsuario = null, tipoOrg = null, orgAdscrita = null;
            if (Session["ID"] != null && int.TryParse(Session["ID"].ToString(), out var _id)) idUsuario = _id;
            if (Session["TipoOrg"] != null && int.TryParse(Session["TipoOrg"].ToString(), out var _to)) tipoOrg = _to;
            if (Session["OrgAdscrita"] != null && int.TryParse(Session["OrgAdscrita"].ToString(), out var _oa)) orgAdscrita = _oa;

            DateTime? fi = null, ff = null;
            // si quieres, aquí capturas fechas de inputs; si no, quedarán en null y NO aplicará el default

            byte[] pdf = ReportHelper.GenerarRelacionOficiosPendientesPdf(
                idUsuario, tipoOrg, orgAdscrita, fi, ff); // ← dentro ya usa usarDefaultFechas=false

            string fileName = $"Oficios_Pendientes_{DateTime.Now:yyyyMMdd_HHmm}.pdf";
            Response.Clear();
            Response.ContentType = "application/pdf";
            Response.AddHeader("content-disposition", $"attachment; filename={fileName}");
            Response.BinaryWrite(pdf);
            Response.Flush();
            Response.End();
        }

        protected void btnRelacionDescargar_Click(object sender, EventArgs e)
        {
            // Lee sesión
            int.TryParse(Convert.ToString(Session["ID"]), out var idUsuario);
            int.TryParse(Convert.ToString(Session["TipoOrg"]), out var tipoOrg);
            int.TryParse(Convert.ToString(Session["OrgAdscrita"]), out var orgAdscrita);

            // Lee fechas del modal
            DateTime? fi = null, ff = null;
            if (DateTime.TryParse(txtRelacionInicio.Text, out var _fi)) fi = _fi;
            if (DateTime.TryParse(txtRelacionFin.Text, out var _ff)) ff = _ff;

            // Genera bytes del PDF
            var bytes = ReportHelper.GenerarRelacionOficiosPdf(
                idUsuario, tipoOrg, orgAdscrita, fi, ff
            );

            // Descarga
            var nombre = $"Relacion_Oficios_{DateTime.Now:yyyyMMdd_HHmm}.pdf";
            Response.Clear();
            Response.ContentType = "application/pdf";
            Response.AddHeader("Content-Disposition", $"attachment; filename=\"{nombre}\"");
            Response.OutputStream.Write(bytes, 0, bytes.Length);
            Response.Flush();
            Response.End();
        }

        protected void btnResumenDescargar_Click(object sender, EventArgs e)
        {
            // TODO: Obtén estos datos como ya lo haces en tu página
            int.TryParse(Convert.ToString(Session["ID"]), out var idUsuario);
            int.TryParse(Convert.ToString(Session["TipoOrg"]), out var tipoOrg);
            int.TryParse(Convert.ToString(Session["OrgAdscrita"]), out var orgAdscrita);

            DateTime? fi = string.IsNullOrWhiteSpace(txtResumenInicio.Text) ? (DateTime?)null : DateTime.Parse(txtResumenInicio.Text);
            DateTime? ff = string.IsNullOrWhiteSpace(txtResumenFin.Text) ? (DateTime?)null : DateTime.Parse(txtResumenFin.Text);

            bool conDetalle = chkConDetalle.Checked;

            byte[] pdf = ReportHelper.GenerarResumenDocumentosPdf(idUsuario, tipoOrg, orgAdscrita, fi, ff, conDetalle);

            Response.Clear();
            Response.ContentType = "application/pdf";
            Response.AddHeader("content-disposition", $"attachment;filename=ResumenDocumentos_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
            Response.BinaryWrite(pdf);
            Response.End();
        }

        // ========= INTERNOS: GENERAL (.xlsx) =========
        protected void btnDescargarInternos_Click(object sender, EventArgs e)
        {
            DateTime? fechaInicio = null;
            DateTime? fechaFin = null;
            int? estatus = null;

            int idUsuario = 0, tipoOrg = 1, orgAdscrita = 0;
            if (Session["ID"] != null)
                int.TryParse(Session["ID"].ToString(), out idUsuario);
            if (Session["TipoOrg"] != null)
                int.TryParse(Session["TipoOrg"].ToString(), out tipoOrg);
            if (Session["OrgAdscrita"] != null)
                int.TryParse(Session["OrgAdscrita"].ToString(), out orgAdscrita);

            // OJO: este método debe leer de la tabla OficiosInternos
            Reportes reportes = new Reportes();
            byte[] archivo = reportes.GenerarReporteExcelOficiosInternos(
                idUsuario,
                tipoOrg,
                orgAdscrita,
                fechaInicio,
                fechaFin,
                estatus
            );

            Response.Clear();
            Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            Response.AddHeader("content-disposition", "attachment; filename=Reporte_OficiosInternos.xlsx");
            Response.BinaryWrite(archivo);
            Response.End();
        }

        // ========= INTERNOS: PENDIENTES (PDF) =========
        protected void btnDescargarPendientesInternos_Click(object sender, EventArgs e)
        {
            int? idUsuario = null, tipoOrg = null, orgAdscrita = null;
            if (Session["ID"] != null && int.TryParse(Session["ID"].ToString(), out var _id)) idUsuario = _id;
            if (Session["TipoOrg"] != null && int.TryParse(Session["TipoOrg"].ToString(), out var _to)) tipoOrg = _to;
            if (Session["OrgAdscrita"] != null && int.TryParse(Session["OrgAdscrita"].ToString(), out var _oa)) orgAdscrita = _oa;

            DateTime? fi = null, ff = null;
            // Si en el modal de internos manejas fechas, las puedes leer aquí y pasar; si no, se van en null.

            // OJO: este helper debe usar OficiosInternos
            byte[] pdf = ReportHelper.GenerarRelacionOficiosInternosPendientesPdf(
                idUsuario, tipoOrg, orgAdscrita, fi, ff
            );

            string fileName = $"OficiosInternos_Pendientes_{DateTime.Now:yyyyMMdd_HHmm}.pdf";
            Response.Clear();
            Response.ContentType = "application/pdf";
            Response.AddHeader("content-disposition", $"attachment; filename={fileName}");
            Response.BinaryWrite(pdf);
            Response.Flush();
            Response.End();
        }

        // ========= INTERNOS: POR FECHA (.xlsx) =========
        protected void btnDescargarInternosPorFecha_Click(object sender, EventArgs e)
        {
            DateTime? fechaInicio = null;
            DateTime? fechaFin = null;

            if (DateTime.TryParse(txtFechaInicioInternos.Text, out var fi))
                fechaInicio = fi;
            if (DateTime.TryParse(txtFechaFinInternos.Text, out var ff))
                fechaFin = ff;

            int idUsuario = 0, tipoOrg = 1, orgAdscrita = 0;
            if (Session["ID"] != null)
                int.TryParse(Session["ID"].ToString(), out idUsuario);
            if (Session["TipoOrg"] != null)
                int.TryParse(Session["TipoOrg"].ToString(), out tipoOrg);
            if (Session["OrgAdscrita"] != null)
                int.TryParse(Session["OrgAdscrita"].ToString(), out orgAdscrita);

            int? estatus = null;

            // OJO: debe consultar OficiosInternos
            Reportes reportes = new Reportes();
            byte[] archivo = reportes.GenerarReporteExcelOficiosInternos(
                idUsuario,
                tipoOrg,
                orgAdscrita,
                fechaInicio,
                fechaFin,
                estatus
            );

            Response.Clear();
            Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            Response.AddHeader("content-disposition", "attachment; filename=Reporte_OficiosInternos_PorFecha.xlsx");
            Response.BinaryWrite(archivo);
            Response.End();
        }

        // ========= INTERNOS: POR ESTATUS (.xlsx) =========
        protected void btnDescargarInternosPorEstatus_Click(object sender, EventArgs e)
        {
            DateTime? fechaInicio = null;
            DateTime? fechaFin = null;
            int? estatus = null;

            if (!string.IsNullOrWhiteSpace(ddlEstatusInternos.SelectedValue) &&
                int.TryParse(ddlEstatusInternos.SelectedValue, out var est))
            {
                estatus = est;
            }

            int idUsuario = 0, tipoOrg = 1, orgAdscrita = 0;
            if (Session["ID"] != null)
                int.TryParse(Session["ID"].ToString(), out idUsuario);
            if (Session["TipoOrg"] != null)
                int.TryParse(Session["TipoOrg"].ToString(), out tipoOrg);
            if (Session["OrgAdscrita"] != null)
                int.TryParse(Session["OrgAdscrita"].ToString(), out orgAdscrita);

            // OJO: debe consultar OficiosInternos
            Reportes reportes = new Reportes();
            byte[] archivo = reportes.GenerarReporteExcelOficiosInternos(
                idUsuario,
                tipoOrg,
                orgAdscrita,
                fechaInicio,
                fechaFin,
                estatus
            );

            Response.Clear();
            Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            Response.AddHeader("content-disposition", "attachment; filename=Reporte_OficiosInternos_PorEstatus.xlsx");
            Response.BinaryWrite(archivo);
            Response.End();
        }

        // ========= INTERNOS: RESUMEN DOCUMENTOS (PDF) =========
        protected void btnResumenInternosDescargar_Click(object sender, EventArgs e)
        {
            int.TryParse(Convert.ToString(Session["ID"]), out var idUsuario);
            int.TryParse(Convert.ToString(Session["TipoOrg"]), out var tipoOrg);
            int.TryParse(Convert.ToString(Session["OrgAdscrita"]), out var orgAdscrita);

            DateTime? fi = string.IsNullOrWhiteSpace(txtResumenInicioInternos.Text)
                ? (DateTime?)null
                : DateTime.Parse(txtResumenInicioInternos.Text);

            DateTime? ff = string.IsNullOrWhiteSpace(txtResumenFinInternos.Text)
                ? (DateTime?)null
                : DateTime.Parse(txtResumenFinInternos.Text);

            bool conDetalle = chkConDetalleInternos.Checked;

            // OJO: este helper debe leer OficiosInternos
            byte[] pdf = ReportHelper.GenerarResumenDocumentosInternosPdf(
                idUsuario,
                tipoOrg,
                orgAdscrita,
                fi,
                ff,
                conDetalle
            );

            Response.Clear();
            Response.ContentType = "application/pdf";
            Response.AddHeader("content-disposition",
                $"attachment;filename=ResumenDocumentosInternos_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
            Response.BinaryWrite(pdf);
            Response.End();
        }
    }



}


