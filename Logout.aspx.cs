using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SURO2
{
    public partial class Logout : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cookies["loginAnimado"].Expires = DateTime.Now.AddDays(-1);

            //1.-Limpiar la sesión 
            Session.Clear(); // Limpia la sesión actual
            Session.Abandon(); // Abandona la sesión actual

            //2.-Eliminar cookies 
            if (Request.Cookies[".ASPXAUTH"] != null)
            {
                Response.Cookies[".ASPXAUTH"].Expires = DateTime.Now.AddDays(-1);
            }


            //3.-Redirigir al usuario a la página de inicio de sesión
            Response.Redirect("Login.aspx"); 
        }
    }
}