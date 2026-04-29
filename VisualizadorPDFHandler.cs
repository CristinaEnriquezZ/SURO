using System;
using System.IO;
using System.Web;

public class VisualizadorPDFHandler : IHttpHandler
{
    public void ProcessRequest(HttpContext context)
    {
        string nombreArchivo = context.Request.QueryString["archivo"];

        if (string.IsNullOrWhiteSpace(nombreArchivo))
        {
            context.Response.StatusCode = 400;
            context.Response.ContentType = "text/plain";
            context.Response.Write("Error: Parámetro 'archivo' no especificado.");
            return;
        }

        // Configura estos valores en web.config (appSettings) o en variables de entorno
        string sharePath = @"\\10.18.24.185\Datos_Aplicaciones\SURO";
        string user = "WebUser"; // o "DOMINIO\\Usuario", o "SERVIDOR\\UsuarioLocal"
        string pass = "Usuarioweb1";

        string rutaCompletaArchivo = Path.Combine(sharePath, nombreArchivo);

        try
        {
            using (new NetworkConnection(sharePath, user, pass))
            {
                if (!File.Exists(rutaCompletaArchivo))
                {
                    context.Response.StatusCode = 404;
                    context.Response.ContentType = "text/plain";
                    context.Response.Write("Error: Archivo no encontrado.");
                    return;
                }

                context.Response.ContentType = "application/pdf";
                context.Response.AddHeader("Content-Disposition", "inline; filename=" + Path.GetFileName(nombreArchivo));
                context.Response.BufferOutput = false;
                context.Response.Cache.SetCacheability(HttpCacheability.Public);
                context.Response.Cache.SetMaxAge(TimeSpan.FromMinutes(10));
                context.Response.Cache.SetSlidingExpiration(false);
                context.Response.TransmitFile(rutaCompletaArchivo);
                context.Response.Flush();
                context.ApplicationInstance.CompleteRequest();
            }
        }
        catch (Exception ex)
        {
            // Loguea ex.Message / ex.ToString() a tus logs
            context.Response.StatusCode = 500;
            context.Response.ContentType = "text/plain";
            context.Response.Write("Error interno del servidor al procesar el archivo.");
        }
    }

    public bool IsReusable => false;
}
