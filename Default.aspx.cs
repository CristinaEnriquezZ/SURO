using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI;
using System.Web.UI.WebControls;
using Newtonsoft.Json;

namespace SURO2
{
    public partial class Default : System.Web.UI.Page
    {
        // Totales (si los quieres usar en server)
        protected int TotalCapturados;
        protected int TotalTurnados;
        protected int TotalEnProcesos;
        protected int TotalOficiosGeneral;
        protected int TotalConcluido;

        // Listas para serializar a JS
        public List<Dictionary<string, object>> estadisticaList = new List<Dictionary<string, object>>();
        public List<Dictionary<string, object>> semaforoDetalleList = new List<Dictionary<string, object>>();

        protected void Page_Load(object sender, EventArgs e)
        {
            // Exponer TipoOrg a JS SIEMPRE (postbacks incluidos)
            int tipoOrg = 1;
            if (Session["TipoOrg"] != null) int.TryParse(Session["TipoOrg"].ToString(), out tipoOrg);
            hfTipoOrg.Value = tipoOrg.ToString();

            if (!IsPostBack)
            {
                BindDashboard(); // Primera carga
                                 // Opcional: asegura que el JS se ejecute tras la primera carga
                ScriptManager.RegisterStartupScript(this, GetType(), "initDashFirst", "initDashboard();", true);
            }

        }

        /* ==========================================================
           Tarjeta "Semáforo (Total)" - usa el 2º result set del SP
           ========================================================== */
        private void BindDashboard()
        {
            // Este hidden sí debe refrescar siempre (ya lo haces en Page_Load)

            // 1) Tarjetas de totales
            CargarTotalesPorEstatus();

            // 2) Grupos Dir/Dpto/Área
            estadisticaList = GetOficiosEstadisticaList();

            // 3) Tarjeta de semáforo (chips y total)
            CargarTarjetaSemaforo();

            // 4) Listas por color (primer result set)
            semaforoDetalleList = GetSemaforoDetalleList();
        }

        protected void tmrRefresh_Tick(object sender, EventArgs e)
        {
            // Recalcula todo en cada tick
            BindDashboard();

            // Re-ejecuta el JS que pinta (tu Sys.Application.add_load ya ayuda, esto es doble red)
            ScriptManager.RegisterStartupScript(this, GetType(), "initDashTick", "initDashboard();", true);

            // Fuerza la actualización del UpdatePanel en este postback parcial
            UpdatePanel1.Update();
        }



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
                using (SqlCommand cmd = new SqlCommand("dbo.sp_Oficios_Semaforo", con))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.CommandTimeout = 120;
                    cmd.Parameters.AddWithValue("@TipoOrg", tipoOrg);
                    cmd.Parameters.AddWithValue("@OrgAdscrita", orgAdscrita);
                    cmd.Parameters.AddWithValue("@IncluirParaConocimiento", 0);

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        DataSet ds = new DataSet();
                        da.Fill(ds); // [0]=detalle, [1]=conteo

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
                            throw new Exception("El SP no devolvió el 2º result set (conteo por semáforo).");
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

        /* ==========================================================
           Listas por color (Rojo/Naranja/Amarillo) - 1er result set
           ========================================================== */
        private List<Dictionary<string, object>> GetSemaforoDetalleList()
        {
            DataTable dt = ObtenerSemaforoDetalle();
            var list = new List<Dictionary<string, object>>();
            foreach (DataRow row in dt.Rows)
            {
                var dict = new Dictionary<string, object>();
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
                using (SqlCommand cmd = new SqlCommand("dbo.sp_Oficios_Semaforo", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@TipoOrg", tipoOrg);
                    cmd.Parameters.AddWithValue("@OrgAdscrita", orgAdscrita);
                    cmd.Parameters.AddWithValue("@IncluirParaConocimiento", 0);

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        DataSet ds = new DataSet();
                        da.Fill(ds); // [0]=detalle, [1]=conteo
                        if (ds.Tables.Count >= 1)
                            dt = ds.Tables[0];
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error en ObtenerSemaforoDetalle: " + ex);
            }

            return dt;
        }

        /* ==========================================================
           Tarjetas de totales por estatus (lo que ya tenías)
           ========================================================== */
        private void CargarTotalesPorEstatus()
        {
            Funciones funciones = new Funciones();
            SqlConnection con = funciones.ConBD();

            int TotalCapturados = 0;
            int TotalTurnados = 0;
            int TotalEnProcesos = 0;
            int TotalConcluido = 0;
            int TotalOficiosUnicos = 0;

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

                using (SqlCommand cmd = new SqlCommand("sp_ConteoJerarquicoPorEstatusConFiltros", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@idUsuario", idUsuario);
                    cmd.Parameters.AddWithValue("@tipoOrg", tipoOrg);
                    cmd.Parameters.AddWithValue("@OrgAdscrita", orgAdscrita);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        // 1er result set (por unidad) — lo ignoramos
                        while (reader.Read()) { }

                        // 2º result set: totales por estatus
                        if (reader.NextResult())
                        {
                            while (reader.Read())
                            {
                                string nombreEstatus = reader["Estatus"].ToString();
                                int cantidad = Convert.ToInt32(reader["Total"]);

                                TotalOficiosUnicos += cantidad;

                                if (nombreEstatus.Equals("Capturado", StringComparison.OrdinalIgnoreCase))
                                    TotalCapturados = cantidad;
                                else if (nombreEstatus.Equals("Turnado", StringComparison.OrdinalIgnoreCase))
                                    TotalTurnados = cantidad;
                                else if (nombreEstatus.Equals("En proceso", StringComparison.OrdinalIgnoreCase))
                                    TotalEnProcesos = cantidad;
                                else if (nombreEstatus.Equals("Concluido", StringComparison.OrdinalIgnoreCase))
                                    TotalConcluido = cantidad;
                            }
                        }

                        lblTotalCapturados.Text = TotalCapturados.ToString();
                        lblTotalTurnados.Text = TotalTurnados.ToString();
                        lblTotalEnProceso.Text = TotalEnProcesos.ToString();
                        lbltotalConcluidos.Text = TotalConcluido.ToString();
                        lblTotalOficiosUnicos.Text = TotalOficiosUnicos.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en CargarTotalesPorEstatus: {ex}");
            }
            finally
            {
                if (con != null && con.State == ConnectionState.Open)
                    con.Close();
            }
        }

        /* ==========================================================
           Estadística por Dirección/Departamento/Área (lo tuyo)
           ========================================================== */
        private List<Dictionary<string, object>> GetOficiosEstadisticaList()
        {
            DataTable dt = ObtenerOficiosEstadistica();
            var list = new List<Dictionary<string, object>>();
            foreach (DataRow row in dt.Rows)
            {
                var dict = new Dictionary<string, object>();
                foreach (DataColumn col in dt.Columns)
                    dict[col.ColumnName] = row[col];
                list.Add(dict);
            }
            return list;
        }

        private DataTable ObtenerOficiosEstadistica()
        {
            DataTable dt = new DataTable();
            try
            {
                int idUsuario = 0, tipoOrg = 1, orgAdscrita = 0;
                if (Session["ID"] != null)
                    int.TryParse(Session["ID"].ToString(), out idUsuario);
                if (Session["TipoOrg"] != null)
                    int.TryParse(Session["TipoOrg"].ToString(), out tipoOrg);
                if (Session["OrgAdscrita"] != null)
                    int.TryParse(Session["OrgAdscrita"].ToString(), out orgAdscrita);

                Funciones funciones = new Funciones();
                using (SqlConnection con = funciones.ConBD())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_OficiosEstadistica", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@idUsuario", idUsuario);
                        cmd.Parameters.AddWithValue("@tipoOrg", tipoOrg);
                        cmd.Parameters.AddWithValue("@OrgAdscrita", orgAdscrita);

                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en ObtenerOficiosEstadistica: {ex}");
            }
            return dt;
        }
    }
}
