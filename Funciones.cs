using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.UI;


namespace SURO2
{
    public class Funciones
    {
        // Conexión a la base de datos
        private SqlConnection conn;

        public SqlConnection ConBD()
        {
            if (conn == null)
            {
                conn = new SqlConnection(ConfigurationManager.ConnectionStrings["SUROConnectionString"].ConnectionString);
            }
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }
            return conn;
        }

        public DataTable ObtenerTodasLasDirecciones()
        {
            DataTable dt = new DataTable();

            try
            {

                Funciones funciones = new Funciones();

                using (SqlConnection conn = funciones.ConBD())
                {
                    using (SqlCommand cmd = new SqlCommand("SP_ObtengoDirecciones", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        object result = cmd.ExecuteScalar();

                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                // Manejo de errores
                System.Diagnostics.Debug.WriteLine("Error en ObtenerTodasLasDirecciones(): " + ex.Message);



            }
            return dt;
        }
        public int ObtenerTipoOrgDesdeTablaSimple(int idEntidad, string tipoTabla)
        {
            int tipoOrg = 0;
            string tableName = "";

            // Validar el nombre de la tabla para evitar inyección SQL y errores
            switch (tipoTabla.ToLower())
            {
                case "direccion":
                    tableName = "Direccion"; // Asegúrate de que este sea el nombre exacto de tu tabla de Direcciones
                    break;
                case "departamento":
                    tableName = "Departamento"; // Asegúrate de que este sea el nombre exacto de tu tabla de Departamentos
                    break;
                case "area":
                    tableName = "Area"; // Asegúrate de que este sea el nombre exacto de tu tabla de Áreas
                    break;
                default:
                    // Si el tipoTabla no es reconocido, retorna 0 o lanza una excepción
                    System.Diagnostics.Debug.WriteLine($"Error: Tipo de tabla '{tipoTabla}' no reconocido en ObtenerTipoOrgDesdeTablaSimple.");
                    return 0;
            }

            using (SqlConnection conn = ConBD()) 
            {
                // Consulta para obtener el IdTipoOrg de la tabla especificada
           
                string query = $"SELECT IdTipoOrg FROM {tableName} WHERE ID =@ID" ;
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ID", idEntidad);
                    try
                    {
                        
                        object result = cmd.ExecuteScalar(); // Ejecuta la consulta y obtiene el primer valor
                        if (result != null && result != DBNull.Value)
                        {
                            tipoOrg = Convert.ToInt32(result);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Manejo de errores: loguear la excepción para depuración
                        System.Diagnostics.Debug.WriteLine($"Error al obtener IdTipoOrg desde {tableName} para ID {idEntidad}: {ex.Message}");
                    }
                }
            }
            return tipoOrg;
        }





        public void ConsultaUsuario(int id, string user, string pass, int proc)
        {
            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["SUROConnectionString"].ConnectionString))
            using (var cmd = new SqlCommand("SP_Usuario", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                //cmd.Parameters.Add("@IdUsuario", SqlDbType.Int).Value = id;
                cmd.Parameters.Add("@User", SqlDbType.VarChar, 50).Value = user;
                cmd.Parameters.Add("@Pass", SqlDbType.VarChar, 50).Value = pass;
                cmd.Parameters.Add("@Proc", SqlDbType.Int).Value = proc;

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        //Solo accede si hay datos
                        HttpContext.Current.Session["Usuario"] =user;
                        HttpContext.Current.Session["ID"] = reader["ID"];
                        HttpContext.Current.Session["TipoUser"] = reader["TipoUser"];
                        HttpContext.Current.Session["TipoOrg"] = reader["TipoOrg"];
                        HttpContext.Current.Session["OrgAdscrita"] = reader["OrgAdscrita"];
                        HttpContext.Current.Response.Redirect("Default.aspx");
                        
                    }
                    else
                    {

                        ////si no hay filas, significa que el usuario o la contraseña son incorrectos
                        ////Mando el mensaje de eror a la página de Login
                        HttpContext.Current.Session["LoginError"] = "Usuario o contraseña incorrectos.";
                        // Aquí podrías manejar el error de usuario o contraseña incorrectos, por ejemplo, mostrando un mensaje en la interfaz de usuario.
                        //Creo una variable de sesión para decirle que no haga la animación del Login
                      
                       HttpContext.Current.Response.Redirect("Login.aspx"); // Redirige sin animación

                    }
                        conn.Close();
                }
            }
        }

        public int VerificaConlusion(int idOrganizacionAds, int idUser, int idOficio)
        {
            int idOficioDB = 0;
            DataTable dt = new DataTable();

            Funciones fun = new Funciones();

            using (SqlConnection conn = fun.ConBD())
            {
                using (SqlCommand cmd = new SqlCommand("SP_ConsultoConclusion", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@idUser", idUser);
                    cmd.Parameters.AddWithValue("@idOrgAds", idOrganizacionAds);
                    cmd.Parameters.AddWithValue("@idOficio", idOficio);
                    object result = cmd.ExecuteScalar(); 
                    if(result != null & result != DBNull.Value)
                    {
                        idOficioDB = Convert.ToInt32(result);
                    }

                }
            }
            return idOficioDB;
        }
        public int ObtenerDireccionUsuario(int idOrganizacionAds, int idUser)
        {

            int idDireccion = 0;
            DataTable dt = new DataTable();
            //Obtengo la dirección de la organización adscrita

            Funciones funciones = new Funciones();

            using (SqlConnection conn = funciones.ConBD())
            {
                using (SqlCommand cmd = new SqlCommand("SP_ObtengoDireccionxUsuario", conn))
                {
                   
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@IdUser", SqlDbType.Int).Value = idUser;
                    cmd.Parameters.AddWithValue("@IdOrgAds", SqlDbType.Int).Value = idOrganizacionAds;
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        idDireccion = Convert.ToInt32(result);
                    }
                }
                
            }
            return idDireccion;
        }

        public int ObtenerDepartamentoUsuario(int idOrganizacionAds)
        {
            int idDepartamento = 0;
            DataTable dt = new DataTable();

            Funciones funciones = new Funciones();
            using (SqlConnection conn = funciones.ConBD())
            {
                using (SqlCommand cmd = new SqlCommand("SP_ObtengoDepartamentoxUsuario", conn)) 
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@IdOrgAds", SqlDbType.Int).Value = idOrganizacionAds;
                    object result = cmd.ExecuteScalar();
                    if(result != null && result != DBNull.Value)
                    {
                        idDepartamento = Convert.ToInt32(result);
                    }

                }
                
            }
            return idDepartamento;
        }
        public DataTable ObtenerAreasParaTurno(int idDepartamento)
        {
            DataTable dt = new DataTable();
            using (SqlConnection conn = ConBD())
            {
                using (SqlCommand cmd = new SqlCommand("SP_ObtengoAreasparaDD", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@IdDepartamento", SqlDbType.Int).Value = idDepartamento;
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
            }
            return dt;
        }
        public DataTable ObtenerDepartamentosParaTurno(int idDireccion)
        {
            DataTable dt = new DataTable();
            using (SqlConnection conn = ConBD())
            {
                using (SqlCommand cmd = new SqlCommand("SP_ObtengoDepartamentosParaDD", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@IdDireccion", SqlDbType.Int).Value = idDireccion;
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
            }
            return dt;
        }
        public DataTable ObtenerDireccionesParaTurno(int idDireccion)
        {
            DataTable dt = new DataTable();
            using (SqlConnection conn = ConBD())
            {
                using (SqlCommand cmd = new SqlCommand("SP_ObtengoDireccionesParaDD", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@IdDireccion", SqlDbType.Int).Value = idDireccion;
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
            }
            return dt;
        }
        public void Desconectar()
        {
            if (conn != null && conn.State == ConnectionState.Open)
            {
                conn.Close();
            }
        }

        
    }
}