using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web; // Server.MapPath
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace SURO2
{
    public static class ReportHelper
    {
        // ================== Constantes ==================
        private const string LOGO_REL_PATH_PRIMARY = "~/img/SDR.png";

        // Header tuning
        private const float LOGO_MAX_W_PT = 110f;
        private const float LOGO_MAX_H_PT = 52f;

        private const string SIN_DIRECCION = "SIN DIRECCIÓN";
        private const string SIN_DEPARTAMENTO = "SIN DEPARTAMENTO";
        private const string SIN_AREA = "SIN ÁREA";
        private const string SIN_TURNAR = "SIN TURNAR";

        private const string KEY_SIN_DIR = "__SIN_DIR__";
        private const string KEY_SIN_TURNAR = "__SIN_TURNAR__";
        private const string SIN_KEY = "__SIN__";

        // Márgenes base
        private const float DOC_MARGIN_L = 20f;
        private const float DOC_MARGIN_R = 20f;
        private const float DOC_MARGIN_T = 20f;
        private const float DOC_MARGIN_B = 34f;

        private const float PAD_HEADER = 4.0f;
        private const float PAD_CELL = 4.0f;
        private const float SPACE_AFTER_TABLE = 4.0f;

        private static readonly BaseColor Zebra1 = new BaseColor(252, 252, 252);
        private static readonly BaseColor Zebra2 = new BaseColor(245, 245, 245);
        private static readonly BaseColor HeadBg = new BaseColor(236, 240, 243);
        private static readonly BaseColor HeadBorder = new BaseColor(200, 200, 200);
        private static readonly BaseColor RowBorder = new BaseColor(225, 225, 225);

        // Fuentes con fallback
        private static readonly Font FTitle = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12.5f) ?? new Font(Font.FontFamily.HELVETICA, 12.5f, Font.BOLD);
        private static readonly Font FSub = FontFactory.GetFont(FontFactory.HELVETICA, 8.0f) ?? new Font(Font.FontFamily.HELVETICA, 8.0f, Font.NORMAL);
        private static readonly Font FTh = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8.5f) ?? new Font(Font.FontFamily.HELVETICA, 8.5f, Font.BOLD);
        private static readonly Font FTd = FontFactory.GetFont(FontFactory.HELVETICA, 8.5f) ?? new Font(Font.FontFamily.HELVETICA, 8.5f, Font.NORMAL);
        private static readonly Font FIt = FontFactory.GetFont(FontFactory.HELVETICA_OBLIQUE, 8f) ?? new Font(Font.FontFamily.HELVETICA, 8.0f, Font.ITALIC);

        // Extra margen superior para no tapar contenido con el header fijo
        private const float EXTRA_TOP_FOR_HEADER = 60f;

        // ================== DTOs ==================
        private sealed class OficioRow
        {
            public int ID { get; set; }
            public int Año { get; set; }
            public int Folio { get; set; }
            public string NoOficio { get; set; }
            public string FolioOficio { get; set; }
            public DateTime? FechaOficio { get; set; }
            public string Remitente { get; set; }
            public string Asunto { get; set; }
            public string Direccion { get; set; }
            public string Departamento { get; set; }
            public string Area { get; set; }
            public int? Estatus { get; set; } // 3 = concluido
        }

        private sealed class GrupoOficio
        {
            public int ID { get; set; }
            public int Año { get; set; }
            public int Folio { get; set; }
            public string NoOficio { get; set; }
            public string FolioOficio { get; set; }
            public DateTime? FechaOficio { get; set; }
            public string Remitente { get; set; }
            public string Asunto { get; set; }
            public List<string> Destinatarios { get; set; } = new List<string>();
        }

        private sealed class SecundarioResumen
        {
            public string Nombre { get; set; }
            public int Total { get; set; }
            public int Concluidos { get; set; }
            public int Pendientes { get; set; }
        }

        private sealed class PrimarioResumen
        {
            public string Nombre { get; set; }
            public int Total { get; set; }
            public int Concluidos { get; set; }
            public int Pendientes { get; set; }
            public List<SecundarioResumen> Hijos { get; set; } = new List<SecundarioResumen>();
        }

        private sealed class IdEstatus
        {
            public int ID { get; set; }
            public int? Estatus { get; set; }
        }

        private sealed class Jerarquia
        {
            public Dictionary<string, List<string>> DirToDeps { get; } = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, List<string>> DepToAreas { get; } = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, string> DepToDirSingle { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, string> AreaToDepSingle { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        // ================== Helper: crea Document ==================
        private static Document CreateDocument(bool landscape, float ml, float mr, float mt, float mb)
        {
            Rectangle page = new Rectangle(PageSize.LETTER);
            if (landscape)
            {
                page = page.Rotate();
            }
            return new Document(page, ml, mr, mt, mb);
        }

        // ================== API PÚBLICA – EXTERNOS ==================

        public static byte[] GenerarRelacionOficiosPdf(int? idUsuario, int? tipoOrg, int? orgAdscrita, DateTime? fechaInicio, DateTime? fechaFin)
        {
            var tuple = ObtenerDatos(idUsuario, tipoOrg, orgAdscrita, fechaInicio, fechaFin);
            var grupos = AgruparPorOficio(tuple.rows, tipoOrg);

            return RenderIndicePorDestinatario(
                grupos ?? new List<GrupoOficio>(),
                fechaInicio, fechaFin, tipoOrg, tuple.userName,
                true,
                "Relación de Oficios",
                true);
        }

        public static byte[] GenerarRelacionOficiosPendientesPdf(int? idUsuario, int? tipoOrg, int? orgAdscrita, DateTime? fechaInicio, DateTime? fechaFin)
        {
            var tuple = ObtenerDatos(idUsuario, tipoOrg, orgAdscrita, fechaInicio, fechaFin, false);
            bool esUsuarioEspecial = idUsuario.HasValue && idUsuario.Value == 2;

            var rows = esUsuarioEspecial ? tuple.rows : tuple.rows.Where(r => r.Estatus != 3).ToList();
            var grupos = AgruparPorOficio(rows, tipoOrg) ?? new List<GrupoOficio>();

            if (esUsuarioEspecial)
            {
                return RenderRelacionCompacta(
                    grupos, fechaInicio, fechaFin, tipoOrg, tuple.userName,
                    "Relación de Documentos", true, true, "Firma de Entregado", "Capturado");
            }

            return RenderIndicePorDestinatario(
                grupos, fechaInicio, fechaFin, tipoOrg, tuple.userName,
                false, "Oficios Pendientes", true);
        }

        public static byte[] GenerarResumenDocumentosPdf(int? idUsuario, int? tipoOrg, int? orgAdscrita, DateTime? fechaInicio, DateTime? fechaFin, bool conDetalle)
        {
            return GenerarResumenDocumentosPdf(idUsuario, tipoOrg, orgAdscrita, fechaInicio, fechaFin, conDetalle, true);
        }

        public static byte[] GenerarResumenDocumentosPdf(int? idUsuario, int? tipoOrg, int? orgAdscrita,
            DateTime? fechaInicio, DateTime? fechaFin, bool conDetalle, bool incluirSinSecundarioEnDetalle)
        {
            var tuple = ObtenerDatos(idUsuario, tipoOrg, orgAdscrita, fechaInicio, fechaFin);
            var rows = tuple.rows;
            var userName = tuple.userName;
            var jer = tuple.jer;

            Func<OficioRow, string> selectorPrimarioEff;
            Func<OficioRow, string> selectorSecundario = null;
            string etiquetaPrimario, etiquetaSecundario = null;

            if (tipoOrg == 1 || tipoOrg == 2)
            {
                selectorPrimarioEff = r => DireccionEfectiva(r, jer);
                selectorSecundario = r => Normalize(r.Departamento);
                etiquetaPrimario = "Dirección";
                etiquetaSecundario = "Departamento";
            }
            else if (tipoOrg == 3)
            {
                selectorPrimarioEff = r => DepartamentoEfectivo(r, jer);
                selectorSecundario = r => Normalize(r.Area);
                etiquetaPrimario = "Departamento";
                etiquetaSecundario = "Área";
            }
            else
            {
                selectorPrimarioEff = r => Normalize(r.Area);
                etiquetaPrimario = "Área";
                conDetalle = false;
            }

            var idToPrimarios = new Dictionary<int, HashSet<string>>();
            var idToBucketEspecial = new Dictionary<int, string>();

            foreach (var g in rows.GroupBy(r => r.ID))
            {
                var prims = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                bool allDirEmpty = true, allDepEmpty = true, allAreaEmpty = true;

                foreach (var r in g)
                {
                    var p = selectorPrimarioEff(r);
                    if (!string.IsNullOrWhiteSpace(p)) prims.Add(p);

                    if (!string.IsNullOrWhiteSpace(r.Direccion)) allDirEmpty = false;
                    if (!string.IsNullOrWhiteSpace(r.Departamento)) allDepEmpty = false;
                    if (!string.IsNullOrWhiteSpace(r.Area)) allAreaEmpty = false;
                }

                if (prims.Count > 0)
                {
                    idToPrimarios[g.Key] = prims;
                }
                else
                {
                    if (allDirEmpty && allDepEmpty && allAreaEmpty)
                        idToBucketEspecial[g.Key] = KEY_SIN_TURNAR;
                    else
                        idToBucketEspecial[g.Key] = KEY_SIN_DIR;
                }
            }

            var nombresPrimarios = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var set in idToPrimarios.Values)
                foreach (var v in set) nombresPrimarios.Add(v);
            if (idToBucketEspecial.Values.Any(v => v == KEY_SIN_TURNAR)) nombresPrimarios.Add(KEY_SIN_TURNAR);
            if (idToBucketEspecial.Values.Any(v => v == KEY_SIN_DIR)) nombresPrimarios.Add(KEY_SIN_DIR);

            var ordenados = nombresPrimarios
                .OrderBy(x => x == KEY_SIN_TURNAR ? "" : (x == KEY_SIN_DIR ? " " : x), StringComparer.OrdinalIgnoreCase)
                .ToList();

            var primarios = new List<PrimarioResumen>();

            foreach (var prim in ordenados)
            {
                var idsBucket = new HashSet<int>();
                if (prim == KEY_SIN_TURNAR || prim == KEY_SIN_DIR)
                {
                    foreach (var kv in idToBucketEspecial) if (kv.Value == prim) idsBucket.Add(kv.Key);
                }
                else
                {
                    foreach (var kv in idToPrimarios) if (kv.Value.Contains(prim)) idsBucket.Add(kv.Key);
                }
                if (idsBucket.Count == 0) continue;

                var universo = rows.Where(r => idsBucket.Contains(r.ID)).ToList();
                var ids = universo.GroupBy(r => r.ID).Select(g => new IdEstatus { ID = g.Key, Estatus = g.First().Estatus }).ToList();

                int total = ids.Count;
                int concluidos = ids.Count(x => x.Estatus.HasValue && x.Estatus.Value == 3);
                int pendientes = total - concluidos;

                var hijos = new List<SecundarioResumen>();
                if (conDetalle && selectorSecundario != null)
                {
                    var obsPorSec = universo
                        .GroupBy(r => KeySec(selectorSecundario(r)), StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(
                            k => k.Key,
                            v => v.GroupBy(r => r.ID).Select(x => new IdEstatus { ID = x.Key, Estatus = x.First().Estatus }).ToList(),
                            StringComparer.OrdinalIgnoreCase);

                    bool incluirSinSecundario = (tipoOrg == 3) && incluirSinSecundarioEnDetalle;

                    foreach (var kv in obsPorSec)
                    {
                        bool esSin = IsSinKey(kv.Key);
                        if (esSin && !incluirSinSecundario) continue;

                        int t = kv.Value.Count; if (t <= 0) continue;
                        int c = kv.Value.Count(x => x.Estatus.HasValue && x.Estatus.Value == 3);
                        int p = t - c;

                        string nombreHijo = esSin ? EtiquetaSin(etiquetaSecundario) : kv.Key;

                        hijos.Add(new SecundarioResumen { Nombre = nombreHijo, Total = t, Concluidos = c, Pendientes = p });
                    }

                    hijos = hijos.OrderBy(h => h.Nombre ?? "", StringComparer.OrdinalIgnoreCase).ToList();
                }

                string nombreBucket = prim;
                if (prim == KEY_SIN_TURNAR) nombreBucket = SIN_TURNAR;
                else if (prim == KEY_SIN_DIR)
                    nombreBucket = (tipoOrg == 1 || tipoOrg == 2) ? SIN_DIRECCION : (tipoOrg == 3 ? SIN_DEPARTAMENTO : SIN_AREA);

                primarios.Add(new PrimarioResumen
                {
                    Nombre = nombreBucket,
                    Total = total,
                    Concluidos = concluidos,
                    Pendientes = pendientes,
                    Hijos = hijos
                });
            }

            return RenderResumenAdaptativo(
                primarios,
                fechaInicio,
                fechaFin,
                tipoOrg,
                userName,
                etiquetaPrimario,
                etiquetaSecundario,
                conDetalle,
                rows,
                "Resumen de Documentos");
        }

        // ================== API PÚBLICA – INTERNOS ==================

        public static byte[] GenerarRelacionOficiosInternosPdf(
            int? idUsuario,
            int? tipoOrg,
            int? orgAdscrita,
            DateTime? fechaInicio,
            DateTime? fechaFin)
        {
            var tuple = ObtenerDatosInternos(idUsuario, tipoOrg, orgAdscrita, fechaInicio, fechaFin);
            var grupos = AgruparPorOficio(tuple.rows, tipoOrg);

            return RenderIndicePorDestinatario(
                grupos ?? new List<GrupoOficio>(),
                fechaInicio,
                fechaFin,
                tipoOrg,
                tuple.userName,
                true,
                "Relación de Oficios Internos",
                true);
        }

        public static byte[] GenerarRelacionOficiosInternosPendientesPdf(
            int? idUsuario,
            int? tipoOrg,
            int? orgAdscrita,
            DateTime? fechaInicio,
            DateTime? fechaFin)
        {
            var tuple = ObtenerDatosInternos(idUsuario, tipoOrg, orgAdscrita, fechaInicio, fechaFin, false);
            bool esUsuarioEspecial = idUsuario.HasValue && idUsuario.Value == 2;

            var rows = esUsuarioEspecial
                ? tuple.rows
                : tuple.rows.Where(r => r.Estatus != 3).ToList();

            var grupos = AgruparPorOficio(rows, tipoOrg) ?? new List<GrupoOficio>();

            if (esUsuarioEspecial)
            {
                return RenderRelacionCompacta(
                    grupos,
                    fechaInicio,
                    fechaFin,
                    tipoOrg,
                    tuple.userName,
                    "Relación de Documentos Internos",
                    true,
                    true,
                    "Firma de Entregado",
                    "Capturado");
            }

            return RenderIndicePorDestinatario(
                grupos,
                fechaInicio,
                fechaFin,
                tipoOrg,
                tuple.userName,
                false,
                "Oficios Internos Pendientes",
                true);
        }

        public static byte[] GenerarResumenDocumentosInternosPdf(
            int? idUsuario,
            int? tipoOrg,
            int? orgAdscrita,
            DateTime? fechaInicio,
            DateTime? fechaFin,
            bool conDetalle)
        {
            return GenerarResumenDocumentosInternosPdf(idUsuario, tipoOrg, orgAdscrita, fechaInicio, fechaFin, conDetalle, true);
        }

        public static byte[] GenerarResumenDocumentosInternosPdf(
            int? idUsuario,
            int? tipoOrg,
            int? orgAdscrita,
            DateTime? fechaInicio,
            DateTime? fechaFin,
            bool conDetalle,
            bool incluirSinSecundarioEnDetalle)
        {
            var tuple = ObtenerDatosInternos(idUsuario, tipoOrg, orgAdscrita, fechaInicio, fechaFin);
            var rows = tuple.rows;
            var userName = tuple.userName;
            var jer = tuple.jer;

            Func<OficioRow, string> selectorPrimarioEff;
            Func<OficioRow, string> selectorSecundario = null;
            string etiquetaPrimario, etiquetaSecundario = null;

            if (tipoOrg == 1 || tipoOrg == 2)
            {
                selectorPrimarioEff = r => DireccionEfectiva(r, jer);
                selectorSecundario = r => Normalize(r.Departamento);
                etiquetaPrimario = "Dirección";
                etiquetaSecundario = "Departamento";
            }
            else if (tipoOrg == 3)
            {
                selectorPrimarioEff = r => DepartamentoEfectivo(r, jer);
                selectorSecundario = r => Normalize(r.Area);
                etiquetaPrimario = "Departamento";
                etiquetaSecundario = "Área";
            }
            else
            {
                selectorPrimarioEff = r => Normalize(r.Area);
                etiquetaPrimario = "Área";
                conDetalle = false;
            }

            var idToPrimarios = new Dictionary<int, HashSet<string>>();
            var idToBucketEspecial = new Dictionary<int, string>();

            foreach (var g in rows.GroupBy(r => r.ID))
            {
                var prims = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                bool allDirEmpty = true, allDepEmpty = true, allAreaEmpty = true;

                foreach (var r in g)
                {
                    var p = selectorPrimarioEff(r);
                    if (!string.IsNullOrWhiteSpace(p)) prims.Add(p);

                    if (!string.IsNullOrWhiteSpace(r.Direccion)) allDirEmpty = false;
                    if (!string.IsNullOrWhiteSpace(r.Departamento)) allDepEmpty = false;
                    if (!string.IsNullOrWhiteSpace(r.Area)) allAreaEmpty = false;
                }

                if (prims.Count > 0)
                {
                    idToPrimarios[g.Key] = prims;
                }
                else
                {
                    if (allDirEmpty && allDepEmpty && allAreaEmpty)
                        idToBucketEspecial[g.Key] = KEY_SIN_TURNAR;
                    else
                        idToBucketEspecial[g.Key] = KEY_SIN_DIR;
                }
            }

            var nombresPrimarios = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var set in idToPrimarios.Values)
                foreach (var v in set) nombresPrimarios.Add(v);
            if (idToBucketEspecial.Values.Any(v => v == KEY_SIN_TURNAR)) nombresPrimarios.Add(KEY_SIN_TURNAR);
            if (idToBucketEspecial.Values.Any(v => v == KEY_SIN_DIR)) nombresPrimarios.Add(KEY_SIN_DIR);

            var ordenados = nombresPrimarios
                .OrderBy(x => x == KEY_SIN_TURNAR ? "" : (x == KEY_SIN_DIR ? " " : x), StringComparer.OrdinalIgnoreCase)
                .ToList();

            var primarios = new List<PrimarioResumen>();

            foreach (var prim in ordenados)
            {
                var idsBucket = new HashSet<int>();
                if (prim == KEY_SIN_TURNAR || prim == KEY_SIN_DIR)
                {
                    foreach (var kv in idToBucketEspecial) if (kv.Value == prim) idsBucket.Add(kv.Key);
                }
                else
                {
                    foreach (var kv in idToPrimarios) if (kv.Value.Contains(prim)) idsBucket.Add(kv.Key);
                }
                if (idsBucket.Count == 0) continue;

                var universo = rows.Where(r => idsBucket.Contains(r.ID)).ToList();
                var ids = universo.GroupBy(r => r.ID).Select(g => new IdEstatus { ID = g.Key, Estatus = g.First().Estatus }).ToList();

                int total = ids.Count;
                int concluidos = ids.Count(x => x.Estatus.HasValue && x.Estatus.Value == 3);
                int pendientes = total - concluidos;

                var hijos = new List<SecundarioResumen>();
                if (conDetalle && selectorSecundario != null)
                {
                    var obsPorSec = universo
                        .GroupBy(r => KeySec(selectorSecundario(r)), StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(
                            k => k.Key,
                            v => v.GroupBy(r => r.ID).Select(x => new IdEstatus { ID = x.Key, Estatus = x.First().Estatus }).ToList(),
                            StringComparer.OrdinalIgnoreCase);

                    bool incluirSinSecundario = (tipoOrg == 3) && incluirSinSecundarioEnDetalle;

                    foreach (var kv in obsPorSec)
                    {
                        bool esSin = IsSinKey(kv.Key);
                        if (esSin && !incluirSinSecundario) continue;

                        int t = kv.Value.Count; if (t <= 0) continue;
                        int c = kv.Value.Count(x => x.Estatus.HasValue && x.Estatus.Value == 3);
                        int p = t - c;

                        string nombreHijo = esSin ? EtiquetaSin(etiquetaSecundario) : kv.Key;

                        hijos.Add(new SecundarioResumen { Nombre = nombreHijo, Total = t, Concluidos = c, Pendientes = p });
                    }

                    hijos = hijos.OrderBy(h => h.Nombre ?? "", StringComparer.OrdinalIgnoreCase).ToList();
                }

                string nombreBucket = prim;
                if (prim == KEY_SIN_TURNAR) nombreBucket = SIN_TURNAR;
                else if (prim == KEY_SIN_DIR)
                    nombreBucket = (tipoOrg == 1 || tipoOrg == 2) ? SIN_DIRECCION : (tipoOrg == 3 ? SIN_DEPARTAMENTO : SIN_AREA);

                primarios.Add(new PrimarioResumen
                {
                    Nombre = nombreBucket,
                    Total = total,
                    Concluidos = concluidos,
                    Pendientes = pendientes,
                    Hijos = hijos
                });
            }

            return RenderResumenAdaptativo(
                primarios,
                fechaInicio,
                fechaFin,
                tipoOrg,
                userName,
                etiquetaPrimario,
                etiquetaSecundario,
                conDetalle,
                rows,
                "Resumen de Documentos Internos");
        }

        // ================== Datos – EXTERNOS ==================
        private static (List<OficioRow> rows, string userName, Jerarquia jer) ObtenerDatos(
            int? idUsuario, int? tipoOrg, int? orgAdscrita,
            DateTime? fechaInicio, DateTime? fechaFin,
            bool usarDefaultFechas = true)
        {
            var result = new List<OficioRow>();
            string printedBy = null;
            var jer = new Jerarquia();

            if (usarDefaultFechas && !fechaInicio.HasValue && !fechaFin.HasValue)
            {
                fechaFin = DateTime.Today;
                fechaInicio = fechaFin.Value.AddDays(-1);
            }

            string fiStr = fechaInicio?.ToString("yyyy-MM-dd");
            string ffStr = fechaFin?.ToString("yyyy-MM-dd");

            var func = new Funciones();
            using (SqlConnection cn = func.ConBD())
            {
                if (cn.State != ConnectionState.Open) cn.Open();

                using (var pre = new SqlCommand(@"
                    SET ARITHABORT ON;
                    SET CONCAT_NULL_YIELDS_NULL ON;
                    SET ANSI_WARNINGS ON;
                    SET ANSI_PADDING ON;
                    SET ANSI_NULLS ON;
                    SET QUOTED_IDENTIFIER ON;
                    SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;", cn))
                { pre.ExecuteNonQuery(); }

                using (var cmd = new SqlCommand("dbo.sp_OficiosTurnadosListado", cn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 180;

                    cmd.Parameters.Add("@idUsuario", SqlDbType.Int).Value = (object)idUsuario ?? DBNull.Value;
                    cmd.Parameters.Add("@TipoOrg", SqlDbType.Int).Value = (object)tipoOrg ?? DBNull.Value;
                    cmd.Parameters.Add("@OrgAdscrita", SqlDbType.Int).Value = (object)orgAdscrita ?? DBNull.Value;
                    cmd.Parameters.Add("@FechaInicio", SqlDbType.VarChar, 15).Value = (object)fiStr ?? DBNull.Value;
                    cmd.Parameters.Add("@FechaFin", SqlDbType.VarChar, 15).Value = (object)ffStr ?? DBNull.Value;

                    using (var rd = cmd.ExecuteReader())
                    {
                        int TryOrd(string col) { try { return rd.GetOrdinal(col); } catch { return -1; } }

                        int iID = TryOrd("ID");
                        int iAño = TryOrd("Año");
                        int iFolio = TryOrd("Folio");
                        int iNoOficio = TryOrd("NoOficio");
                        int iFolioOficio = TryOrd("FolioOficio");
                        int iFechaOficio = TryOrd("FechaOficio");
                        int iRemitente = TryOrd("Remitente");
                        int iAsunto = TryOrd("Asunto");
                        int iDireccion = TryOrd("Direccion");
                        int iDepartamento = TryOrd("Departamento");
                        int iArea = TryOrd("Area");
                        int iEstatus = TryOrd("Estatus");

                        while (rd.Read())
                        {
                            int GetInt(int ord) { return (ord >= 0 && !rd.IsDBNull(ord)) ? Convert.ToInt32(rd.GetValue(ord)) : 0; }
                            int? GetIntN(int ord) { return (ord >= 0 && !rd.IsDBNull(ord)) ? Convert.ToInt32(rd.GetValue(ord)) : (int?)null; }
                            string GetStr(int ord) { return (ord >= 0 && !rd.IsDBNull(ord)) ? rd.GetString(ord).Trim() : ""; }
                            DateTime? GetDt(int ord) { return (ord >= 0 && !rd.IsDBNull(ord)) ? rd.GetDateTime(ord) : (DateTime?)null; }

                            result.Add(new OficioRow
                            {
                                ID = GetInt(iID),
                                Año = GetInt(iAño),
                                Folio = GetInt(iFolio),
                                NoOficio = (iNoOficio >= 0 && !rd.IsDBNull(iNoOficio)) ? rd.GetString(iNoOficio) : null,
                                FolioOficio = (iFolioOficio >= 0 && !rd.IsDBNull(iFolioOficio)) ? rd.GetString(iFolioOficio) : null,
                                FechaOficio = GetDt(iFechaOficio),
                                Remitente = GetStr(iRemitente),
                                Asunto = GetStr(iAsunto),
                                Direccion = GetStr(iDireccion),
                                Departamento = GetStr(iDepartamento),
                                Area = GetStr(iArea),
                                Estatus = GetIntN(iEstatus)
                            });
                        }

                        // Usuario
                        if (rd.NextResult() && rd.Read())
                        {
                            int iUser = TryOrd("UserName");
                            printedBy = (iUser >= 0 && !rd.IsDBNull(iUser)) ? rd.GetString(iUser).Trim() : null;
                        }

                        // Dir -> Deps
                        if (rd.NextResult())
                        {
                            int iDir = TryOrd("Direccion");
                            int iDep = TryOrd("Departamento");
                            while (rd.Read())
                            {
                                string dir = (iDir >= 0 && !rd.IsDBNull(iDir)) ? rd.GetString(iDir).Trim() : null;
                                string dep = (iDep >= 0 && !rd.IsDBNull(iDep)) ? rd.GetString(iDep).Trim() : null;
                                if (string.IsNullOrWhiteSpace(dir) || string.IsNullOrWhiteSpace(dep)) continue;

                                List<string> list;
                                if (!jer.DirToDeps.TryGetValue(dir, out list))
                                {
                                    list = new List<string>();
                                    jer.DirToDeps[dir] = list;
                                }
                                if (!list.Any(x => x.Equals(dep, StringComparison.OrdinalIgnoreCase)))
                                {
                                    list.Add(dep);
                                }

                                if (!jer.DepToDirSingle.ContainsKey(dep))
                                    jer.DepToDirSingle[dep] = dir;
                            }

                            var keys = jer.DirToDeps.Keys.ToList();
                            foreach (var k in keys)
                                jer.DirToDeps[k] = jer.DirToDeps[k].OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
                        }

                        // Dep -> Áreas
                        if (rd.NextResult())
                        {
                            int iDep = TryOrd("Departamento");
                            int iArea2 = TryOrd("Area");
                            while (rd.Read())
                            {
                                string dep = (iDep >= 0 && !rd.IsDBNull(iDep)) ? rd.GetString(iDep).Trim() : null;
                                string area = (iArea2 >= 0 && !rd.IsDBNull(iArea2)) ? rd.GetString(iArea2).Trim() : null;
                                if (string.IsNullOrWhiteSpace(dep) || string.IsNullOrWhiteSpace(area)) continue;

                                List<string> list;
                                if (!jer.DepToAreas.TryGetValue(dep, out list))
                                {
                                    list = new List<string>();
                                    jer.DepToAreas[dep] = list;
                                }
                                if (!list.Any(x => x.Equals(area, StringComparison.OrdinalIgnoreCase)))
                                {
                                    list.Add(area);
                                }

                                if (!jer.AreaToDepSingle.ContainsKey(area))
                                    jer.AreaToDepSingle[area] = dep;
                            }

                            var keys2 = jer.DepToAreas.Keys.ToList();
                            foreach (var k in keys2)
                                jer.DepToAreas[k] = jer.DepToAreas[k].OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
                        }
                    }
                }
            }

            return (result, printedBy, jer);
        }

        // ================== Datos – INTERNOS ==================
        private static (List<OficioRow> rows, string userName, Jerarquia jer) ObtenerDatosInternos(
            int? idUsuario, int? tipoOrg, int? orgAdscrita,
            DateTime? fechaInicio, DateTime? fechaFin,
            bool usarDefaultFechas = true)
        {
            var result = new List<OficioRow>();
            string printedBy = null;
            var jer = new Jerarquia();

            if (usarDefaultFechas && !fechaInicio.HasValue && !fechaFin.HasValue)
            {
                fechaFin = DateTime.Today;
                fechaInicio = fechaFin.Value.AddDays(-1);
            }

            string fiStr = fechaInicio?.ToString("yyyy-MM-dd");
            string ffStr = fechaFin?.ToString("yyyy-MM-dd");

            var func = new Funciones();
            using (SqlConnection cn = func.ConBD())
            {
                if (cn.State != ConnectionState.Open) cn.Open();

                using (var pre = new SqlCommand(@"
                    SET ARITHABORT ON;
                    SET CONCAT_NULL_YIELDS_NULL ON;
                    SET ANSI_WARNINGS ON;
                    SET ANSI_PADDING ON;
                    SET ANSI_NULLS ON;
                    SET QUOTED_IDENTIFIER ON;
                    SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;", cn))
                { pre.ExecuteNonQuery(); }

                // ⚠️ AJUSTA EL NOMBRE DEL SP SI TUYO ES OTRO
                using (var cmd = new SqlCommand("dbo.sp_OficiosTurnadosListadoInternos", cn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 180;

                    cmd.Parameters.Add("@idUsuario", SqlDbType.Int).Value = (object)idUsuario ?? DBNull.Value;
                    

                    using (var rd = cmd.ExecuteReader())
                    {
                        int TryOrd(string col) { try { return rd.GetOrdinal(col); } catch { return -1; } }

                        int iID = TryOrd("ID");
                        int iAño = TryOrd("Año");
                        int iFolio = TryOrd("Folio");
                        int iNoOficio = TryOrd("NoOficio");
                        int iFolioOficio = TryOrd("FolioOficio");
                        int iFechaOficio = TryOrd("FechaOficio");
                        int iRemitente = TryOrd("Remitente");
                        int iAsunto = TryOrd("Asunto");
                        int iDireccion = TryOrd("Direccion");
                        int iDepartamento = TryOrd("Departamento");
                        int iArea = TryOrd("Area");
                        int iEstatus = TryOrd("Estatus");

                        while (rd.Read())
                        {
                            int GetInt(int ord) { return (ord >= 0 && !rd.IsDBNull(ord)) ? Convert.ToInt32(rd.GetValue(ord)) : 0; }
                            int? GetIntN(int ord) { return (ord >= 0 && !rd.IsDBNull(ord)) ? Convert.ToInt32(rd.GetValue(ord)) : (int?)null; }
                            string GetStr(int ord)
                            {
                                if (ord < 0 || rd.IsDBNull(ord)) return "";
                                object val = rd.GetValue(ord);
                                return val == null ? "" : val.ToString().Trim();
                            }
                            DateTime? GetDt(int ord) { return (ord >= 0 && !rd.IsDBNull(ord)) ? rd.GetDateTime(ord) : (DateTime?)null; }

                            result.Add(new OficioRow
                            {
                                ID = GetInt(iID),
                                Año = GetInt(iAño),
                                Folio = GetInt(iFolio),
                                NoOficio = (iNoOficio >= 0 && !rd.IsDBNull(iNoOficio)) ? rd.GetString(iNoOficio) : null,
                                FolioOficio = (iFolioOficio >= 0 && !rd.IsDBNull(iFolioOficio)) ? rd.GetString(iFolioOficio) : null,
                                FechaOficio = GetDt(iFechaOficio),
                                Remitente = GetStr(iRemitente),
                                Asunto = GetStr(iAsunto),
                                Direccion = GetStr(iDireccion),
                                Departamento = GetStr(iDepartamento),
                                Area = GetStr(iArea),
                                Estatus = GetIntN(iEstatus)
                            });
                        }

                        // Usuario
                        if (rd.NextResult() && rd.Read())
                        {
                            int iUser = TryOrd("UserName");
                            printedBy = (iUser >= 0 && !rd.IsDBNull(iUser)) ? rd.GetString(iUser).Trim() : null;
                        }

                        // Dir -> Deps
                        if (rd.NextResult())
                        {
                            int iDir = TryOrd("Direccion");
                            int iDep = TryOrd("Departamento");
                            while (rd.Read())
                            {
                                string dir = (iDir >= 0 && !rd.IsDBNull(iDir)) ? rd.GetString(iDir).Trim() : null;
                                string dep = (iDep >= 0 && !rd.IsDBNull(iDep)) ? rd.GetString(iDep).Trim() : null;
                                if (string.IsNullOrWhiteSpace(dir) || string.IsNullOrWhiteSpace(dep)) continue;

                                List<string> list;
                                if (!jer.DirToDeps.TryGetValue(dir, out list))
                                {
                                    list = new List<string>();
                                    jer.DirToDeps[dir] = list;
                                }
                                if (!list.Any(x => x.Equals(dep, StringComparison.OrdinalIgnoreCase)))
                                {
                                    list.Add(dep);
                                }

                                if (!jer.DepToDirSingle.ContainsKey(dep))
                                    jer.DepToDirSingle[dep] = dir;
                            }

                            var keys = jer.DirToDeps.Keys.ToList();
                            foreach (var k in keys)
                                jer.DirToDeps[k] = jer.DirToDeps[k].OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
                        }

                        // Dep -> Áreas
                        if (rd.NextResult())
                        {
                            int iDep = TryOrd("Departamento");
                            int iArea2 = TryOrd("Area");
                            while (rd.Read())
                            {
                                string dep = (iDep >= 0 && !rd.IsDBNull(iDep)) ? rd.GetString(iDep).Trim() : null;
                                string area = (iArea2 >= 0 && !rd.IsDBNull(iArea2)) ? rd.GetString(iArea2).Trim() : null;
                                if (string.IsNullOrWhiteSpace(dep) || string.IsNullOrWhiteSpace(area)) continue;

                                List<string> list;
                                if (!jer.DepToAreas.TryGetValue(dep, out list))
                                {
                                    list = new List<string>();
                                    jer.DepToAreas[dep] = list;
                                }
                                if (!list.Any(x => x.Equals(area, StringComparison.OrdinalIgnoreCase)))
                                {
                                    list.Add(area);
                                }

                                if (!jer.AreaToDepSingle.ContainsKey(area))
                                    jer.AreaToDepSingle[area] = dep;
                            }

                            var keys2 = jer.DepToAreas.Keys.ToList();
                            foreach (var k in keys2)
                                jer.DepToAreas[k] = jer.DepToAreas[k].OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
                        }
                    }
                }
            }

            return (result, printedBy, jer);
        }

        private static List<GrupoOficio> AgruparPorOficio(List<OficioRow> rows, int? tipoOrg)
        {
            Func<OficioRow, string> selectorDestino;
            if (tipoOrg == 3) selectorDestino = r => r.Departamento;
            else if (tipoOrg == 4 || tipoOrg == 5) selectorDestino = r => r.Area;
            else selectorDestino = r => r.Direccion;

            return rows
                .GroupBy(r => new { r.ID, r.Año, r.Folio, r.NoOficio, r.FolioOficio, r.FechaOficio, r.Remitente, r.Asunto })
                .Select(g => new GrupoOficio
                {
                    ID = g.Key.ID,
                    Año = g.Key.Año,
                    Folio = g.Key.Folio,
                    NoOficio = g.Key.NoOficio,
                    FolioOficio = g.Key.FolioOficio,
                    FechaOficio = g.Key.FechaOficio,
                    Remitente = g.Key.Remitente,
                    Asunto = g.Key.Asunto,
                    Destinatarios = g.Select(selectorDestino)
                                     .Where(s => !string.IsNullOrWhiteSpace(s))
                                     .Distinct(StringComparer.OrdinalIgnoreCase)
                                     .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                                     .ToList()
                })
                .OrderBy(g => g.ID)
                .ToList();
        }

        // ================== Renders ==================
        private static byte[] RenderResumenAdaptativo(
            List<PrimarioResumen> data,
            DateTime? fi,
            DateTime? ff,
            int? tipoOrg,
            string userName,
            string etiquetaPrimario,
            string etiquetaSecundario,
            bool conDetalle,
            List<OficioRow> rawRowsForTotals,
            string titulo)
        {
            using (var ms = new MemoryStream())
            {
                var pdfDoc = CreateDocument(
                    landscape: false,
                    ml: DOC_MARGIN_L, mr: DOC_MARGIN_R,
                    mt: DOC_MARGIN_T + EXTRA_TOP_FOR_HEADER, mb: DOC_MARGIN_B);

                var writer = PdfWriter.GetInstance(pdfDoc, ms);
                writer.CloseStream = false;
                writer.PageEvent = new PageHeaderFooterEvent(titulo, fi, ff, tipoOrg, userName);
                pdfDoc.Open();

                if (data == null || data.Count == 0)
                {
                    pdfDoc.Add(new Paragraph("No hay datos para el criterio seleccionado.", FIt));
                    pdfDoc.Close();
                    return ms.ToArray();
                }

                var tbl = new PdfPTable(new float[] { 56f, 14f, 14f, 16f })
                {
                    WidthPercentage = 100,
                    SpacingAfter = SPACE_AFTER_TABLE,
                    HeaderRows = 1
                };
                AddHeaderCell(tbl, etiquetaPrimario, FTh);
                AddHeaderCell(tbl, "Total", FTh, Element.ALIGN_RIGHT);
                AddHeaderCell(tbl, "Concluidos", FTh, Element.ALIGN_RIGHT);
                AddHeaderCell(tbl, "Pendientes", FTh, Element.ALIGN_RIGHT);

                var globalIDs = rawRowsForTotals.GroupBy(r => r.ID).Select(g => new IdEstatus { ID = g.Key, Estatus = g.First().Estatus }).ToList();

                bool zebra = false;
                foreach (var p in data)
                {
                    BaseColor bg = zebra ? Zebra2 : Zebra1;
                    zebra = !zebra;

                    AddRowCell(tbl, p.Nombre ?? EtiquetaSin(etiquetaPrimario), FTd, Element.ALIGN_LEFT, bg);
                    AddRowCell(tbl, p.Total.ToString("N0"), FTd, Element.ALIGN_RIGHT, bg);
                    AddRowCell(tbl, p.Concluidos.ToString("N0"), FTd, Element.ALIGN_RIGHT, bg);
                    AddRowCell(tbl, p.Pendientes.ToString("N0"), FTd, Element.ALIGN_RIGHT, bg);

                    if (conDetalle && p.Hijos != null && p.Hijos.Count > 0 && !string.IsNullOrWhiteSpace(etiquetaSecundario))
                    {
                        var nested = new PdfPTable(new float[] { 60f, 13f, 13f, 14f })
                        {
                            WidthPercentage = 98f,
                            SpacingBefore = 2f,
                            SpacingAfter = 2f
                        };
                        AddHeaderCell(nested, "   " + etiquetaSecundario, FTh);
                        AddHeaderCell(nested, "Total", FTh, Element.ALIGN_RIGHT);
                        AddHeaderCell(nested, "Concluidos", FTh, Element.ALIGN_RIGHT);
                        AddHeaderCell(nested, "Pendientes", FTh, Element.ALIGN_RIGHT);

                        bool zebra2 = false;
                        foreach (var h in p.Hijos)
                        {
                            BaseColor bg2 = zebra2 ? Zebra2 : Zebra1;
                            zebra2 = !zebra2;
                            AddRowCell(nested, "   " + (h.Nombre ?? EtiquetaSin(etiquetaSecundario)), FTd, Element.ALIGN_LEFT, bg2);
                            AddRowCell(nested, h.Total.ToString("N0"), FTd, Element.ALIGN_RIGHT, bg2);
                            AddRowCell(nested, h.Concluidos.ToString("N0"), FTd, Element.ALIGN_RIGHT, bg2);
                            AddRowCell(nested, h.Pendientes.ToString("N0"), FTd, Element.ALIGN_RIGHT, bg2);
                        }

                        var nestedContainer = new PdfPCell(nested)
                        {
                            PaddingTop = 2f,
                            PaddingBottom = 4f,
                            PaddingLeft = 6f,
                            PaddingRight = 6f,
                            BorderColor = new BaseColor(230, 230, 230),
                            Colspan = 4
                        };
                        tbl.AddCell(nestedContainer);
                    }
                }

                pdfDoc.Add(tbl);

                int globalConcluidos = globalIDs.Count(x => x.Estatus.HasValue && x.Estatus.Value == 3);
                int globalTotal = globalIDs.Count;
                int globalPendientes = globalTotal - globalConcluidos;

                var pie = new Paragraph(
                    string.Format("{0}: {1:N0}   |   Total (únicos): {2:N0}   |   Concluidos: {3:N0}   |   Pendientes: {4:N0}",
                    (tipoOrg == 1 || tipoOrg == 2) ? "Direcciones" : (tipoOrg == 3) ? "Departamentos" : "Áreas",
                    data.Count(p => !EsPlaceholder(p.Nombre, etiquetaPrimario)),
                    globalTotal, globalConcluidos, globalPendientes), FIt)
                { Alignment = Element.ALIGN_LEFT, SpacingBefore = 4f };
                pdfDoc.Add(pie);

                pdfDoc.Close();
                return ms.ToArray();
            }
        }

        private static byte[] RenderIndicePorDestinatario(
            List<GrupoOficio> data,
            DateTime? fi,
            DateTime? ff,
            int? tipoOrg,
            string userName,
            bool incluirFirma,
            string titulo,
            bool incluirSinDestinatario = false)
        {
            using (var ms = new MemoryStream())
            {
                var pdfDoc = CreateDocument(
                    landscape: true,
                    ml: DOC_MARGIN_L, mr: DOC_MARGIN_R,
                    mt: DOC_MARGIN_T + EXTRA_TOP_FOR_HEADER, mb: DOC_MARGIN_B);

                var writer = PdfWriter.GetInstance(pdfDoc, ms);
                writer.CloseStream = false;
                writer.PageEvent = new PageHeaderFooterEvent(titulo, fi, ff, tipoOrg, userName);
                pdfDoc.Open();

                const string SIN_DEST = "SIN DESTINATARIO";

                IEnumerable<(string Dest, GrupoOficio G)> Expand(GrupoOficio g)
                {
                    var dests = (g.Destinatarios ?? new List<string>())
                                .Select(d => (d ?? "").Trim())
                                .Where(d => !string.IsNullOrWhiteSpace(d))
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .ToList();
                    if (dests.Count == 0)
                    {
                        if (incluirSinDestinatario) yield return (SIN_DEST, g);
                    }
                    else
                    {
                        foreach (var d in dests) yield return (d, g);
                    }
                }

                var porDest = data
                    .SelectMany(g => Expand(g))
                    .GroupBy(x => x.Dest, StringComparer.OrdinalIgnoreCase)
                    .OrderBy(g => incluirSinDestinatario && g.Key.Equals(SIN_DEST, StringComparison.OrdinalIgnoreCase) ? "" : g.Key);

                if (!porDest.Any())
                {
                    pdfDoc.Add(new Paragraph("No hay destinatarios en este rango/alcance.", FIt) { Alignment = Element.ALIGN_LEFT });
                }
                else
                {
                    foreach (var grp in porDest)
                    {
                        var chip = new PdfPTable(1) { WidthPercentage = 100, SpacingAfter = 3f };
                        var chipCell = new PdfPCell(new Phrase(
                            (incluirSinDestinatario && grp.Key.Equals(SIN_DEST, StringComparison.OrdinalIgnoreCase)) ? SIN_DEST : "Destinatario: " + grp.Key, FTh))
                        {
                            BackgroundColor = new BaseColor(245, 245, 245),
                            Border = Rectangle.BOX,
                            BorderColor = new BaseColor(215, 215, 215),
                            Padding = PAD_HEADER
                        };
                        chip.AddCell(chipCell);
                        pdfDoc.Add(chip);

                        var small = new PdfPTable(new float[] { 9f, 11f, 11f, 49f, 20f })
                        {
                            WidthPercentage = 100,
                            SpacingAfter = SPACE_AFTER_TABLE
                        };
                        small.DefaultCell.Border = Rectangle.NO_BORDER;

                        void H2(string t) => small.AddCell(new PdfPCell(new Phrase(t, FTh)) { Border = Rectangle.NO_BORDER, PaddingBottom = 1.5f });

                        H2("Folio"); H2("Fecha"); H2("Oficio"); H2("Asunto"); H2("Remitente");

                        foreach (var x in grp.Select(y => y.G).GroupBy(g => g.ID).Select(g => g.First()))
                        {
                            string fecha = x.FechaOficio.HasValue ? x.FechaOficio.Value.ToString("dd/MM/yyyy") : "";
                            string oficio = string.IsNullOrWhiteSpace(x.NoOficio) ? "S/N" : x.NoOficio;

                            small.AddCell(CellTxt(x.Folio.ToString(), FTd));
                            small.AddCell(CellTxt(fecha, FTd));
                            small.AddCell(CellTxt(oficio, FTd));
                            small.AddCell(CellTxtPad((x.Asunto ?? "").Trim(), FTd, Element.ALIGN_LEFT, 2f, 6f));
                            small.AddCell(CellTxt((x.Remitente ?? "").Trim(), FTd));
                        }

                        pdfDoc.Add(small);

                        if (incluirFirma)
                        {
                            var sigTbl = new PdfPTable(1)
                            {
                                WidthPercentage = 36,
                                HorizontalAlignment = Element.ALIGN_CENTER,
                                SpacingBefore = 4f,
                                SpacingAfter = 3f,
                                KeepTogether = true,
                                SplitLate = false
                            };
                            var firmaCell = new PdfPCell(new Phrase(" ", FTd))
                            {
                                Border = Rectangle.BOTTOM_BORDER,
                                BorderWidthBottom = 1.0f,
                                BorderColorBottom = BaseColor.BLACK,
                                PaddingTop = 10f,
                                PaddingBottom = 5f,
                                HorizontalAlignment = Element.ALIGN_CENTER
                            };
                            sigTbl.AddCell(firmaCell);
                            sigTbl.AddCell(new PdfPCell(new Phrase("Firma del Responsable", FIt))
                            {
                                Border = Rectangle.NO_BORDER,
                                PaddingTop = 2f,
                                HorizontalAlignment = Element.ALIGN_CENTER
                            });
                            sigTbl.CompleteRow();
                            pdfDoc.Add(sigTbl);
                        }

                        var sep = new PdfPTable(1) { WidthPercentage = 100, SpacingBefore = 4f, SpacingAfter = 6f };
                        var sepCell = new PdfPCell(new Phrase(" ")) { Border = Rectangle.NO_BORDER, MinimumHeight = 6f };
                        sepCell.CellEvent = new DottedSeparatorEvent();
                        sep.AddCell(sepCell);
                        pdfDoc.Add(sep);
                    }
                }

                pdfDoc.Add(new Paragraph("Total de oficios en el reporte: " + data.Count.ToString("N0"), FIt)
                { Alignment = Element.ALIGN_LEFT, SpacingBefore = 4f });

                pdfDoc.Close();
                return ms.ToArray();
            }
        }

        private static readonly Font FCmpTh = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8.0f) ?? new Font(Font.FontFamily.HELVETICA, 8.0f, Font.BOLD);
        private static readonly Font FCmpTd = FontFactory.GetFont(FontFactory.HELVETICA, 7.5f) ?? new Font(Font.FontFamily.HELVETICA, 7.5f, Font.NORMAL);

        private const float CMP_MARGIN_L = 12f, CMP_MARGIN_R = 12f, CMP_MARGIN_T = 14f, CMP_MARGIN_B = 26f;
        private const float CMP_PAD = 1.6f;

        private static byte[] RenderRelacionCompacta(
            List<GrupoOficio> data,
            DateTime? fi,
            DateTime? ff,
            int? tipoOrg,
            string userName,
            string titulo,
            bool incluirDestinatario,
            bool incluirFirmaFinal,
            string etiquetaFirma,
            string destinatarioVacio = null)
        {
            using (var ms = new MemoryStream())
            {
                var pdfDoc = CreateDocument(
                    landscape: false,
                    ml: CMP_MARGIN_L, mr: CMP_MARGIN_R,
                    mt: CMP_MARGIN_T + EXTRA_TOP_FOR_HEADER, mb: CMP_MARGIN_B);

                var writer = PdfWriter.GetInstance(pdfDoc, ms);
                writer.CloseStream = false;
                writer.PageEvent = new PageHeaderFooterEvent(titulo, fi, ff, tipoOrg, userName);

                pdfDoc.Open();

                if (data == null || data.Count == 0)
                {
                    pdfDoc.Add(new Paragraph("No hay datos para el criterio seleccionado.", FIt));
                    pdfDoc.Close();
                    return ms.ToArray();
                }

                float[] widths = incluirDestinatario
                    ? new float[] { 8f, 17f, 18f, 39f, 18f }
                    : new float[] { 9f, 20f, 21f, 50f };

                var tbl = new PdfPTable(widths)
                {
                    WidthPercentage = 100,
                    SpacingBefore = 0f,
                    SpacingAfter = 2f,
                    HeaderRows = 1
                };

                PdfPCell H(string t)
                {
                    return new PdfPCell(new Phrase(t, FCmpTh))
                    {
                        BackgroundColor = HeadBg,
                        BorderColor = new BaseColor(210, 210, 210),
                        Padding = CMP_PAD,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE
                    };
                }

                tbl.AddCell(H("FOLIO"));
                tbl.AddCell(H("NO. DE OFICIO"));
                tbl.AddCell(H("REMITENTE"));
                tbl.AddCell(H("ASUNTO"));
                if (incluirDestinatario) tbl.AddCell(H("DESTINATARIO (DIRECCIÓN TURNADO)"));

                var filas = data.GroupBy(g => g.ID).Select(g => g.First()).OrderBy(x => x.ID);

                PdfPCell C(string txt, int align = Element.ALIGN_LEFT, float padL = 1.2f, float padR = 1.2f)
                {
                    return new PdfPCell(new Phrase(txt ?? "", FCmpTd))
                    {
                        PaddingTop = CMP_PAD,
                        PaddingBottom = CMP_PAD,
                        PaddingLeft = padL,
                        PaddingRight = padR,
                        BorderColor = new BaseColor(220, 220, 220),
                        HorizontalAlignment = align,
                        VerticalAlignment = Element.ALIGN_TOP
                    };
                }

                foreach (var x in filas)
                {
                    string oficio = string.IsNullOrWhiteSpace(x.NoOficio) ? "S/N" : x.NoOficio;
                    tbl.AddCell(C(x.Folio.ToString(), Element.ALIGN_CENTER));
                    tbl.AddCell(C(oficio));
                    tbl.AddCell(C((x.Remitente ?? "").Trim()));
                    tbl.AddCell(C((x.Asunto ?? "").Trim(), Element.ALIGN_JUSTIFIED, 1.6f, 2.0f));

                    if (incluirDestinatario)
                    {
                        var destsEnum = (x.Destinatarios ?? new List<string>())
                                        .Where(s => !string.IsNullOrWhiteSpace(s))
                                        .Distinct(StringComparer.OrdinalIgnoreCase)
                                        .OrderBy(s => s, StringComparer.OrdinalIgnoreCase);

                        string destTexto = destsEnum.Any() ? string.Join("; ", destsEnum) : (destinatarioVacio ?? "");
                        tbl.AddCell(C(destTexto));
                    }
                }

                pdfDoc.Add(tbl);

                pdfDoc.Add(new Paragraph("Total de oficios: " + data.Count.ToString("N0"), FIt) { Alignment = Element.ALIGN_LEFT, SpacingBefore = 2f });

                if (incluirFirmaFinal)
                {
                    var sigTbl = new PdfPTable(1)
                    {
                        WidthPercentage = 40,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        SpacingBefore = 12f,
                        KeepTogether = true
                    };
                    var firmaCell = new PdfPCell(new Phrase(" ", FCmpTd))
                    {
                        Border = Rectangle.BOTTOM_BORDER,
                        BorderWidthBottom = 1.0f,
                        BorderColorBottom = BaseColor.BLACK,
                        PaddingTop = 12f,
                        PaddingBottom = 5f,
                        HorizontalAlignment = Element.ALIGN_CENTER
                    };
                    sigTbl.AddCell(firmaCell);
                    sigTbl.AddCell(new PdfPCell(new Phrase(etiquetaFirma, FIt)) { Border = Rectangle.NO_BORDER, PaddingTop = 2f, HorizontalAlignment = Element.ALIGN_CENTER });
                    pdfDoc.Add(sigTbl);
                }

                pdfDoc.Close();
                return ms.ToArray();
            }
        }

        // ================== Helpers de tabla ==================
        private static void AddHeaderCell(PdfPTable t, string text, Font f, int align = Element.ALIGN_LEFT)
        {
            t.AddCell(new PdfPCell(new Phrase(text ?? "", f))
            {
                BackgroundColor = HeadBg,
                Padding = PAD_CELL,
                HorizontalAlignment = align,
                BorderColor = HeadBorder
            });
        }

        private static void AddRowCell(PdfPTable t, string text, Font f, int align, BaseColor bg)
        {
            t.AddCell(new PdfPCell(new Phrase(text ?? "", f))
            {
                BackgroundColor = bg,
                Padding = PAD_CELL,
                HorizontalAlignment = align,
                BorderColor = RowBorder
            });
        }

        private static PdfPCell CellTxt(string txt, Font f, int align = Element.ALIGN_LEFT)
        {
            return new PdfPCell(new Phrase(txt ?? "", f))
            {
                Border = Rectangle.NO_BORDER,
                HorizontalAlignment = align,
                VerticalAlignment = Element.ALIGN_TOP,
                PaddingTop = 2f,
                PaddingBottom = 2f
            };
        }

        private static PdfPCell CellTxtPad(string txt, Font f, int align = Element.ALIGN_LEFT, float padLeft = 2f, float padRight = 6f)
        {
            return new PdfPCell(new Phrase(txt ?? "", f))
            {
                Border = Rectangle.NO_BORDER,
                HorizontalAlignment = align,
                VerticalAlignment = Element.ALIGN_TOP,
                PaddingTop = 2f,
                PaddingBottom = 2f,
                PaddingLeft = padLeft,
                PaddingRight = padRight
            };
        }

        private sealed class DottedSeparatorEvent : IPdfPCellEvent
        {
            public void CellLayout(PdfPCell cell, Rectangle rect, PdfContentByte[] canvases)
            {
                var cb = canvases[PdfPTable.LINECANVAS];
                cb.SaveState();
                cb.SetGrayStroke(0.7f);
                cb.SetLineWidth(0.5f);
                cb.SetLineDash(1.2f, 3.2f, 0f);
                float y = rect.Bottom + rect.Height / 2f;
                cb.MoveTo(rect.Left + 2f, y);
                cb.LineTo(rect.Right - 2f, y);
                cb.Stroke();
                cb.RestoreState();
            }
        }

        // ===== PageEvent robusto =====
        private sealed class PageHeaderFooterEvent : PdfPageEventHelper
        {
            private readonly string _titulo;
            private readonly DateTime? _fi, _ff;
            private readonly int? _tipoOrg;
            private readonly string _userName;

            private PdfTemplate _totalTemplate;
            private BaseFont _bf;
            private Image _logo;

            private const float HeaderHeight = 58f;
            private const float HeaderGapY = 8f;
            private const float FooterFontSize = 8f;

            public PageHeaderFooterEvent(string titulo, DateTime? fi, DateTime? ff, int? tipoOrg, string userName)
            {
                _titulo = titulo;
                _fi = fi; _ff = ff;
                _tipoOrg = tipoOrg;
                _userName = userName;
            }

            public override void OnOpenDocument(PdfWriter writer, Document document)
            {
                try
                {
                    if (_totalTemplate == null && writer != null && writer.DirectContent != null)
                        _totalTemplate = writer.DirectContent.CreateTemplate(40, 16);
                }
                catch { }

                try
                {
                    if (_bf == null)
                        _bf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                }
                catch { }

                try
                {
                    _logo = CargarLogoExacto(LOGO_REL_PATH_PRIMARY, LOGO_MAX_W_PT, LOGO_MAX_H_PT);
                    if (_logo != null)
                    {
                        _logo.ScaleToFit(LOGO_MAX_W_PT, LOGO_MAX_H_PT);
                        _logo.Alignment = Image.ALIGN_LEFT;
                        try { _logo.SetDpi(300, 300); } catch { }
                    }
                }
                catch { _logo = null; }
            }

            public override void OnEndPage(PdfWriter writer, Document document)
            {
                if (_totalTemplate == null)
                {
                    try { if (writer != null && writer.DirectContent != null) _totalTemplate = writer.DirectContent.CreateTemplate(40, 16); } catch { }
                }
                if (_bf == null)
                {
                    try { _bf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED); } catch { }
                }
                if (_logo == null)
                {
                    try
                    {
                        _logo = CargarLogoExacto(LOGO_REL_PATH_PRIMARY, LOGO_MAX_W_PT, LOGO_MAX_H_PT);
                        if (_logo != null)
                        {
                            _logo.ScaleToFit(LOGO_MAX_W_PT, LOGO_MAX_H_PT);
                            _logo.Alignment = Image.ALIGN_LEFT;
                            try { _logo.SetDpi(300, 300); } catch { }
                        }
                    }
                    catch { _logo = null; }
                }

                var cb = writer.DirectContent;
                float left = document.LeftMargin;
                float right = document.PageSize.Width - document.RightMargin;
                float usableWidth = right - left;

                // Header
                try
                {
                    var header = new PdfPTable(new float[] { 20f, 80f })
                    {
                        TotalWidth = usableWidth,
                        LockedWidth = true
                    };
                    header.DefaultCell.Border = Rectangle.NO_BORDER;

                    var logoCell = new PdfPCell { Border = Rectangle.NO_BORDER, Padding = 0f, VerticalAlignment = Element.ALIGN_MIDDLE };
                    if (_logo != null) logoCell.AddElement(_logo);
                    header.AddCell(logoCell);

                    var tituloP = new Paragraph(_titulo ?? "Reporte", FTitle) { Alignment = Element.ALIGN_LEFT, SpacingAfter = 1.5f };

                    string rango = (_fi.HasValue || _ff.HasValue)
                        ? string.Format("{0:dd/MM/yyyy} - {1:dd/MM/yyyy}", _fi, _ff)
                        : "Sin filtro de fecha";

                    string printed = string.IsNullOrWhiteSpace(_userName) ? "N/D" : _userName.Trim();
                    string nivelDesc = (_tipoOrg == 1 || _tipoOrg == 2) ? "Direcciones" : (_tipoOrg == 3) ? "Departamentos" : "Áreas";

                    bool esInterno = (_titulo ?? "").IndexOf("interno", StringComparison.OrdinalIgnoreCase) >= 0;
                    string tipoDoc = esInterno ? "Internos" : "Externos";

                    var subP = new Paragraph(
                        string.Format("Tipo: {0}   |   Nivel: {1}   |   Rango de Fecha: {2}   |   Impreso por: {3}   |   Fecha de impresión: {4:dd/MM/yyyy HH:mm}",
                        tipoDoc, nivelDesc, rango, printed, DateTime.Now), FSub)
                    { Alignment = Element.ALIGN_LEFT };

                    var titleCell = new PdfPCell { Border = Rectangle.NO_BORDER, PaddingLeft = 6f, PaddingTop = 1f, PaddingBottom = 1f };
                    titleCell.AddElement(tituloP);
                    titleCell.AddElement(subP);
                    header.AddCell(titleCell);

                    float headerTopY = document.PageSize.Height - document.TopMargin + (HeaderHeight - 4f);
                    header.WriteSelectedRows(0, -1, left, headerTopY, cb);

                    cb.SaveState();
                    cb.SetGrayStroke(0.7f);
                    cb.SetLineWidth(0.5f);
                    cb.SetLineDash(1.2f, 3.2f, 0f);
                    float sepY = headerTopY - HeaderHeight + HeaderGapY;
                    cb.MoveTo(left, sepY);
                    cb.LineTo(right, sepY);
                    cb.Stroke();
                    cb.RestoreState();
                }
                catch
                {
                    try
                    {
                        cb.BeginText();
                        var bf = _bf ?? BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                        cb.SetFontAndSize(bf, 11f);
                        cb.ShowTextAligned(Element.ALIGN_LEFT, _titulo ?? "Reporte", left, document.PageSize.Height - document.TopMargin + 6f, 0);
                        cb.EndText();
                    }
                    catch { }
                }

                // Footer "Página X de Y"
                try
                {
                    string text = "Página " + writer.PageNumber + " de ";
                    float x = document.PageSize.Width - document.RightMargin;
                    float y = document.BottomMargin - 12f;

                    cb.BeginText();
                    var bf = _bf ?? BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                    cb.SetFontAndSize(bf, FooterFontSize);
                    cb.ShowTextAligned(Element.ALIGN_RIGHT, text, x, y, 0);
                    cb.EndText();

                    if (_totalTemplate != null) cb.AddTemplate(_totalTemplate, x, y);
                }
                catch { }
            }

            public override void OnCloseDocument(PdfWriter writer, Document document)
            {
                try
                {
                    var bf = _bf ?? BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                    if (_totalTemplate != null)
                    {
                        _totalTemplate.BeginText();
                        _totalTemplate.SetFontAndSize(bf, 8f);
                        _totalTemplate.ShowTextAligned(Element.ALIGN_LEFT, writer.PageNumber.ToString(), 0, 0, 0);
                        _totalTemplate.EndText();
                    }
                }
                catch { }
            }
        }

        // ================== Utilidades ==================
        private static string Normalize(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
        private static bool StringEqualsCI(string a, string b) => string.Equals(a ?? "", b ?? "", StringComparison.OrdinalIgnoreCase);
        private static string KeySec(string s) => string.IsNullOrWhiteSpace(s) ? SIN_KEY : s.Trim();
        private static bool IsSinKey(string key) => StringEqualsCI(key, SIN_KEY);

        private static string EtiquetaSin(string etiqueta)
        {
            if (string.Equals(etiqueta, "Dirección", StringComparison.OrdinalIgnoreCase)) return SIN_DIRECCION;
            if (string.Equals(etiqueta, "Departamento", StringComparison.OrdinalIgnoreCase)) return SIN_DEPARTAMENTO;
            return SIN_AREA;
        }

        private static bool EsPlaceholder(string nombre, string etiquetaPrimario)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return true;
            string sin = EtiquetaSin(etiquetaPrimario);
            return nombre.Trim().Equals(sin, StringComparison.OrdinalIgnoreCase) ||
                   nombre.Trim().Equals(SIN_TURNAR, StringComparison.OrdinalIgnoreCase);
        }

        private static string DireccionEfectiva(OficioRow r, Jerarquia jer)
        {
            var dir = Normalize(r.Direccion);
            if (!string.IsNullOrWhiteSpace(dir)) return dir;
            var dep = Normalize(r.Departamento);
            if (string.IsNullOrWhiteSpace(dep)) return null;
            string dueño;
            if (jer.DepToDirSingle.TryGetValue(dep, out dueño)) return dueño;
            return null;
        }

        private static string DepartamentoEfectivo(OficioRow r, Jerarquia jer)
        {
            var dep = Normalize(r.Departamento);
            if (!string.IsNullOrWhiteSpace(dep)) return dep;
            var area = Normalize(r.Area);
            if (string.IsNullOrWhiteSpace(area)) return null;
            string dueño;
            if (jer.AreaToDepSingle.TryGetValue(area, out dueño)) return dueño;
            return null;
        }

        private static Image CargarLogoExacto(string primaryRelPath, float maxWpt, float maxHpt)
        {
            try
            {
                string Resolve(string rel)
                {
                    if (string.IsNullOrWhiteSpace(rel)) return null;
                    if (rel.StartsWith("~/", StringComparison.Ordinal))
                    {
                        if (HttpContext.Current != null) return HttpContext.Current.Server.MapPath(rel);
                        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory ?? "",
                            rel.TrimStart('~', '/').Replace('/', Path.DirectorySeparatorChar));
                    }
                    return rel;
                }

                string[] intentos = new[] { primaryRelPath };
                foreach (var rel in intentos)
                {
                    var full = Resolve(rel);
                    if (!string.IsNullOrWhiteSpace(full) && File.Exists(full))
                    {
                        var img = Image.GetInstance(full);
                        try { img.SetDpi(300, 300); } catch { }
                        if (img.ScaledWidth > maxWpt || img.ScaledHeight > maxHpt) img.ScaleToFit(maxWpt, maxHpt);
                        try { img.CompressionLevel = 0; } catch { }
                        img.Alignment = Image.ALIGN_LEFT;
                        try { img.Border = Rectangle.NO_BORDER; } catch { }
                        return img;
                    }
                }
            }
            catch { }
            return null;
        }
    }
}
