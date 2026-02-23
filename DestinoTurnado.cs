using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SURO2
{
    public class DestinoTurnado
    {
        [JsonProperty("id")] // ¡Esta es la conexión! Mapea "id" del JSON a la propiedad Id de C#
        public string Id { get; set; } // Por eso Id es de tipo string aquí.
        [JsonProperty("nombre")]
        public string Nombre { get; set; }
        [JsonProperty("tipo")]
        public string Tipo { get; set; }

        [JsonProperty("ParaConocimiento")]
        public bool Conocimiento { get; set; }
        public string FechaTurnado { get; set; }

        public DateTime? FechaMaxAtencion { get; set; }


    }
}