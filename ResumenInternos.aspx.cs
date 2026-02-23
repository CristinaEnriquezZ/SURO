using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI;
using Newtonsoft.Json;

namespace SURO2
{
    public partial class ResumenInternos : System.Web.UI.Page
    {
        // (Opcionales) para uso en server si los necesitas
        protected int TotalCapturados;
        protected int TotalTurnados;
        protected int TotalEnProcesos;
        protected int TotalOficiosGeneral;
        protected int TotalConcluido;

        // Se serializan a JS
        public List<Dictionary<string, object>> estadisticaList = new List<Dictionary<string, object>>();
        public List<Dictionary<string, object>> semaforoDetalleList = new List<Dictionary<string, object>>();

        protected void Page_Load(object sender, EventArgs e)
        {
            // Exponer TipoOrg a JS en cada ciclo
            int tipoOrg = 1;
            if (Session["TipoOrg"] != null) int.TryParse(Session["TipoOrg"].ToString(), out tipoOrg);
            hfTipoOrg.Value = tipoOrg.ToString();

            if (!IsPostBack)
            {
                BindDashboard();     // llenar listas y labels
                EmitClientData();    // inyectar window.estadisticaData / window.semaforoDetalleData
                // Inicialización de UI
                ScriptManager.RegisterStartupScript(this, GetType(), "initDashFirst", "initDashboard();", true);
            }
        }

        /// <summary>
        /// Empaqueta y orquesta toda la carga de datos del dashboard.
        /// </summary>
        private void BindDashboard()
        {
            // 1) Totales por estatus
            CargarTotalesPorEstatus();

            // 2) Estadística agrupada Dir/Dep/Área (para barras)
            estadisticaList = GetOficiosEstadisticaList();

            // 3) Semáforo (chips totales)
            CargarTarjetaSemaforo();

            // 4) Detalle por color (tablas)
            semaforoDetalleList = GetSemaforoDetalleList();
        }

        /// <summary>
        /// Inyecta datos en variables globales del navegador para evitar carreras en UpdatePanel.
        /// </summary>
        private void EmitClientData()
        {
            var jsonStats = JsonConvert.SerializeObject(estadisticaList ?? new List<Dictionary<string, object>>());
            var jsonSem = JsonConvert.SerializeObject(semaforoDetalleList ?? new List<Dictionary<string, object>>());

            ScriptManager.RegisterStartupScript(
                this, GetType(), "injectDataVars",
                $"window.estadisticaData = {jsonStats}; window.semaforoDetalleData = {jsonSem};",
                addScriptTags: true
            );
        }

        protected void tmrRefresh_Tick(object sender, EventArgs e)
        {
            BindDashboard();   // recalcula
            EmitClientData();  // primero datos
            ScriptManager.RegisterStartupScript(this, GetType(), "initDashTick", "initDashboard();", true); // luego UI
            UpdatePanel1.Update();
        }

        #region Semáforo (tarjeta + tablas)

        private void CargarTarjetaSemaforo()
        {
            int tipoOrg = 1, orgAdscrita = 0;
            if (Session["TipoOrg"] != null) int.TryParse(Session["TipoOrg"].ToString(), out tipoOrg);
            if (Session["OrgAdscrita"] != null) int.TryParse(Session["OrgAdscrita"].ToString(), out orgAdscrita);

            int total = 0, rojo = 0, naranja = 0, amarillo = 0;
            lblSemaforoError.Visible = false;

            try
            {
                var funciones = new Funciones();
                using (SqlConnection con = funciones.ConBD())
                using (SqlCommand cmd = new SqlCommand("dbo.sp_Oficios_Internos_Semaforo", con))   // INTERNOS
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 120;
                    cmd.Parameters.AddWithValue("@TipoOrg", tipoOrg);
                    cmd.Parameters.AddWithValue("@OrgAdscrita", orgAdscrita);
                    // Para la tarjeta quieres contar TODOS los internos, incl. ParaConocimiento:
                    cmd.Parameters.AddWithValue("@IncluirParaConocimiento", 0); // <<

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        var ds = new DataSet();
                        da.Fill(ds); // [0]=detalle, [1]=conteo por color

                        if (ds.Tables.Count >= 2)
                        {
                            foreach (DataRow r in ds.Tables[1].Rows)
                            {
                                string sem = (r["Semaforo"]?.ToString() ?? "").Trim().ToUpperInvariant();
                                int cnt = r["Conteo"] != DBNull.Value ? Convert.ToInt32(r["Conteo"]) : 0;
                                switch (sem)
                                {
                                    case "ROJO": rojo = cnt; break;
                                    case "NARANJA": naranja = cnt; break;
                                    case "AMARILLO": amarillo = cnt; break;
                                    case "TOTAL": total = cnt; break;
                                }
                            }
                        }
                        else
                        {
                            throw new Exception("El SP de internos no devolvió el 2º result set (conteos del semáforo).");
                        }
                    }
                }

                lblSemaforoTotal.Text = total.ToString();
                lblSemaforoRojo.Text = rojo.ToString();
                lblSemaforoNaranja.Text = naranja.ToString();
                lblSemaforoAmarillo.Text = amarillo.ToString();
            }
            catch (SqlException ex)
            {
                lblSemaforoError.Visible = true;
                lblSemaforoError.Text = Server.HtmlEncode($"SQL ({ex.Number}): {ex.Message}");
                lblSemaforoTotal.Text = lblSemaforoRojo.Text = lblSemaforoNaranja.Text = lblSemaforoAmarillo.Text = "0";
            }
            catch (Exception ex)
            {
                lblSemaforoError.Visible = true;
                lblSemaforoError.Text = Server.HtmlEncode(ex.Message);
                lblSemaforoTotal.Text = lblSemaforoRojo.Text = lblSemaforoNaranja.Text = lblSemaforoAmarillo.Text = "0";
            }
        }

        private List<Dictionary<string, object>> GetSemaforoDetalleList()
        {
            DataTable dt = ObtenerSemaforoDetalle();
            var list = new List<Dictionary<string, object>>(dt.Rows.Count);

            foreach (DataRow row in dt.Rows)
            {
                var dict = new Dictionary<string, object>(dt.Columns.Count);
                foreach (DataColumn col in dt.Columns)
                    dict[col.ColumnName] = row[col];
                list.Add(dict);
            }

            return list;
        }

        private DataTable ObtenerSemaforoDetalle()
        {
            var dt = new DataTable();

            int tipoOrg = 1, orgAdscrita = 0;
            if (Session["TipoOrg"] != null) int.TryParse(Session["TipoOrg"].ToString(), out tipoOrg);
            if (Session["OrgAdscrita"] != null) int.TryParse(Session["OrgAdscrita"].ToString(), out orgAdscrita);

            try
            {
                var funciones = new Funciones();
                using (SqlConnection con = funciones.ConBD())
                using (SqlCommand cmd = new SqlCommand("dbo.sp_Oficios_Internos_Semaforo", con))   // INTERNOS
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@TipoOrg", tipoOrg);
                    cmd.Parameters.AddWithValue("@OrgAdscrita", orgAdscrita);

                    // Aquí decides si el detalle mostrará también los PC:
                    // Con tus datos actuales (todos PC) si pones 0 NO verás nada.
                    cmd.Parameters.AddWithValue("@IncluirParaConocimiento", 0); // << pon 1 si quieres verlos

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        var ds = new DataSet();
                        da.Fill(ds); // [0]=detalle, [1]=conteos
                        if (ds.Tables.Count >= 1)
                            dt = ds.Tables[0];          // detalle = #FinalRows de internos
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error en ObtenerSemaforoDetalle (internos): " + ex);
            }

            return dt;
        }

        #endregion

        #region Totales por estatus

        private void CargarTotalesPorEstatus()
        {
            var funciones = new Funciones();

            using (SqlConnection con = funciones.ConBD())
            {
                int totalCapturados = 0;
                int totalTurnados = 0;
                int totalEnProcesos = 0;
                int totalConcluido = 0;
                int totalOficiosUnicos = 0;

                try
                {
                    if (con.State == ConnectionState.Closed)
                        con.Open();

                    int idUsuario = 0, tipoOrg = 1, orgAdscrita = 0;

                    if (Session["ID"] != null)
                        int.TryParse(Session["ID"].ToString(), out idUsuario);

                    if (Session["TipoOrg"] != null)
                        int.TryParse(Session["TipoOrg"].ToString(), out tipoOrg);

                    if (Session["OrgAdscrita"] != null)
                        int.TryParse(Session["OrgAdscrita"].ToString(), out orgAdscrita);

                    // También exponemos TipoOrg al hidden para el JS
                    hfTipoOrg.Value = tipoOrg.ToString();

                    using (SqlCommand cmd = new SqlCommand("sp_ConteoJerarquicoPorEstatusConFiltros_Internos", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@idUsuario", idUsuario);
                        cmd.Parameters.AddWithValue("@Estatus", (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@tipoOrg", tipoOrg);
                        cmd.Parameters.AddWithValue("@OrgAdscrita", orgAdscrita);

                        // Para los totales de internos normalmente te conviene incluir PC:
                        cmd.Parameters.AddWithValue("@IncluirParaConocimiento", 0); // <<

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            // 1er RESULTSET: PIVOT por unidad (no lo usamos aquí)
                            while (reader.Read())
                            {
                                // Si en algún momento quieres usar el PIVOT, aquí lo lees
                            }

                            // 2º RESULTSET: totales por estatus (incluye ParaConocimiento)
                            if (reader.NextResult())
                            {
                                while (reader.Read())
                                {
                                    string nombreEstatus = reader["Estatus"].ToString();
                                    int cantidad = Convert.ToInt32(reader["Total"]);

                                    totalOficiosUnicos += cantidad;

                                    if (nombreEstatus.Equals("Capturado", StringComparison.OrdinalIgnoreCase))
                                        totalCapturados = cantidad;
                                    else if (nombreEstatus.Equals("Turnado", StringComparison.OrdinalIgnoreCase))
                                        totalTurnados = cantidad;
                                    else if (nombreEstatus.Equals("En proceso", StringComparison.OrdinalIgnoreCase))
                                        totalEnProcesos = cantidad;
                                    else if (nombreEstatus.Equals("Concluido", StringComparison.OrdinalIgnoreCase))
                                        totalConcluido = cantidad;
                                }
                            }

                            lblTotalCapturados.Text = totalCapturados.ToString();
                            lblTotalTurnados.Text = totalTurnados.ToString();
                            lblTotalEnProceso.Text = totalEnProcesos.ToString();
                            lbltotalConcluidos.Text = totalConcluido.ToString();
                            lblTotalOficiosUnicos.Text = totalOficiosUnicos.ToString();

                            TotalCapturados = totalCapturados;
                            TotalTurnados = totalTurnados;
                            TotalEnProcesos = totalEnProcesos;
                            TotalConcluido = totalConcluido;
                            TotalOficiosGeneral = totalOficiosUnicos;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error en CargarTotalesPorEstatus (Internos): {ex}");

                    lblTotalCapturados.Text = "0";
                    lblTotalTurnados.Text = "0";
                    lblTotalEnProceso.Text = "0";
                    lbltotalConcluidos.Text = "0";
                    lblTotalOficiosUnicos.Text = "0";
                }
            }
        }

        #endregion

        #region Estadística agrupada (barras)

        private List<Dictionary<string, object>> GetOficiosEstadisticaList()
        {
            DataTable dt = ObtenerOficiosEstadistica();
            var list = new List<Dictionary<string, object>>(dt.Rows.Count);
            foreach (DataRow row in dt.Rows)
            {
                var dict = new Dictionary<string, object>(dt.Columns.Count);
                foreach (DataColumn col in dt.Columns)
                    dict[col.ColumnName] = row[col];
                list.Add(dict);
            }
            return list;
        }

        private DataTable ObtenerOficiosEstadistica()
        {
            var dt = new DataTable();
            try
            {
                int idUsuario = 0, tipoOrg = 1, orgAdscrita = 0;
                if (Session["ID"] != null) int.TryParse(Session["ID"].ToString(), out idUsuario);
                if (Session["TipoOrg"] != null) int.TryParse(Session["TipoOrg"].ToString(), out tipoOrg);
                if (Session["OrgAdscrita"] != null) int.TryParse(Session["OrgAdscrita"].ToString(), out orgAdscrita);

                var funciones = new Funciones();
                using (SqlConnection con = funciones.ConBD())
                using (SqlCommand cmd = new SqlCommand("sp_OficiosEstadistica_Internos", con)) // INTERNOS
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@idUsuario", idUsuario);
                    cmd.Parameters.AddWithValue("@TipoOrg", tipoOrg);
                    cmd.Parameters.AddWithValue("@OrgAdscrita", orgAdscrita);

                    // Aquí igual decides: si quieres que barras muestren internos PC:
                    cmd.Parameters.AddWithValue("@IncluirParaConocimiento", 0); // <<

                    da.Fill(dt);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en ObtenerOficiosEstadistica (internos): {ex}");
            }
            return dt;
        }

        #endregion
    }
}
