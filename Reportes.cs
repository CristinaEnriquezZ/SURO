using ClosedXML.Excel;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;

namespace SURO2
{
    public class Reportes
    {
        public byte[] GenerarReporteExcelOficios(int idUsuario, int tipoOrg, int orgAdscrita, DateTime? fechaInicio = null, DateTime? fechaFin = null, int? estatus = null)
        {
            string fechaInicioStrinf = fechaInicio?.ToString("yyyy-MM-dd");
            string fechaFinString = fechaFin?.ToString("yyyy-MM-dd");

            Funciones funciones = new Funciones();
            using (SqlConnection con = funciones.ConBD())
            using (SqlCommand cmd = new SqlCommand("sp_ReporteOficios", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@tipoOrg", tipoOrg);
                cmd.Parameters.AddWithValue("@OrgAdscrita", orgAdscrita);
                cmd.Parameters.AddWithValue("@FechaInicio", (object)fechaInicioStrinf ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@FechaFin", (object)fechaFinString ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Estatus", (object)estatus ?? DBNull.Value);

                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    using (var wb = new XLWorkbook())
                    {
                        var ws = wb.Worksheets.Add("Oficios");
                        ws.Cell(1, 1).InsertTable(dt);
                        ws.Columns().AdjustToContents();

                        using (var ms = new MemoryStream())
                        {
                            wb.SaveAs(ms);
                            return ms.ToArray();
                        }
                    }
                }
            }
        }

        // ================= INTERNOS (usa tabla OficiosInternos vía SP) =================
        public byte[] GenerarReporteExcelOficiosInternos(int idUsuario, int tipoOrg, int orgAdscrita,
                                                         DateTime? fechaInicio = null,
                                                         DateTime? fechaFin = null,
                                                         int? estatus = null)
        {
            string fechaInicioString = fechaInicio?.ToString("yyyy-MM-dd");
            string fechaFinString = fechaFin?.ToString("yyyy-MM-dd");

            Funciones funciones = new Funciones();
            using (SqlConnection con = funciones.ConBD())
            // Asegúrate de que este SP consulte **OficiosInternos**
            using (SqlCommand cmd = new SqlCommand("sp_ReporteOficiosInternos", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@tipoOrg", tipoOrg);
                cmd.Parameters.AddWithValue("@OrgAdscrita", orgAdscrita);
                cmd.Parameters.AddWithValue("@FechaInicio", (object)fechaInicioString ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@FechaFin", (object)fechaFinString ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Estatus", (object)estatus ?? DBNull.Value);

                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    using (var wb = new XLWorkbook())
                    {
                        var ws = wb.Worksheets.Add("OficiosInternos");
                        ws.Cell(1, 1).InsertTable(dt);
                        ws.Columns().AdjustToContents();

                        using (var ms = new MemoryStream())
                        {
                            wb.SaveAs(ms);
                            return ms.ToArray();
                        }
                    }
                }
            }
        }
    }
}
