

using System;
using System.Collections.Generic;
using System.Drawing;

public class Oficio
    {
        public String Id { get; set; }
        public string FolioCaptura { get; set; }
        public string FolioOficio { get; set; }
        public string NumeroOficio { get; set; }
        public string Anio { get; set; }
        public string Remitente { get; set; }
        public string LugarRemitente { get; set; }
        public string MunicipioRemitente { get; set; }
        public string TipoDocumento { get; set; }
        public string RutaPDF { get; set; }
        public string NivelAtencion { get; set; }
        public string Estatus { get; set; }
        public bool Conocimiento { get; set; }
        public DateTime? FechaMaxAtencion { get; set; }
        public string Asunto { get; set; }
        public string FechaOficio { get; set; }
        public string Telefono { get; set; }
        public string Correo { get; set; }



    public List<Seguimiento> Seguimientos { get; set; } = new List<Seguimiento>();
    public List<Conclusion> Conclusiones { get; set; } = new List<Conclusion>();

}


