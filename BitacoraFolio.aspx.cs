using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SURO2
{
    public partial class BitacoraFolio : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
                CargarGrid();
        }

         protected void btnBuscar_Click(object sender, EventArgs e)
        {
            gvBitacora.PageIndex = 0;
            CargarGrid();
        }

        protected void btnLimpiar_Click(object sender, EventArgs e)
        {
            txtFolio.Text = "";           
            txtFechaDesde.Text = "";
            txtFechaHasta.Text = "";
            chkSoloModificados.Checked = false;

            gvBitacora.PageIndex = 0;
            CargarGrid();
        }

        protected void gvBitacora_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvBitacora.PageIndex = e.NewPageIndex;
            CargarGrid();
        }

        protected void gvBitacora_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                var lbl = e.Row.FindControl("lblModificado") as Label;
                if (lbl != null)
                {
                    bool mod = false;

                    // Puede venir como bit/boolean/0/1
                    if (lbl.Text == "1") mod = true;
                    else if (lbl.Text == "0") mod = false;
                    else bool.TryParse(lbl.Text, out mod);

                    lbl.Text = mod ? "Sí" : "No";
                    lbl.CssClass = mod ? "badge bg-warning text-dark" : "badge bg-success";
                }
            }
        }

        private void CargarGrid()
       {
            lblMsg.Text = "";

            if (Session["ID"] == null || string.IsNullOrWhiteSpace(Session["ID"].ToString()))
            {
                lblMsg.Text = "Sesión no válida.";
                gvBitacora.DataSource = null;
                gvBitacora.DataBind();
                return;
            }

            int idUser = Convert.ToInt32(Session["ID"].ToString());

            try
            {
                Funciones f = new Funciones();
                using (SqlConnection con = f.ConBD())
                using (SqlCommand cmd = new SqlCommand("dbo.SP_BitacoraFolio_Listar", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@IdUsuario", idUser);

                    // Folio
                    string folio = (txtFolio.Text ?? "").Trim();
                    cmd.Parameters.AddWithValue("@Folio", string.IsNullOrWhiteSpace(folio) ? (object)DBNull.Value : folio);

                  
                    // Solo modificados
                    cmd.Parameters.AddWithValue("@SoloModificados", chkSoloModificados.Checked ? 1 : 0);

                    // Fechas (DATE)
                    if (DateTime.TryParse(txtFechaDesde.Text, out DateTime fd))
                        cmd.Parameters.Add("@FechaDesde", SqlDbType.Date).Value = fd.Date;
                    else
                        cmd.Parameters.Add("@FechaDesde", SqlDbType.Date).Value = DBNull.Value;

                    if (DateTime.TryParse(txtFechaHasta.Text, out DateTime fh))
                        cmd.Parameters.Add("@FechaHasta", SqlDbType.Date).Value = fh.Date;
                    else
                        cmd.Parameters.Add("@FechaHasta", SqlDbType.Date).Value = DBNull.Value;

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        gvBitacora.DataSource = dt;
                        gvBitacora.DataBind();
                    }
                }
            }
            catch (Exception ex)
            {
                lblMsg.Text = "Error al cargar bitácora: " + ex.Message;
            }
        }

    }
}