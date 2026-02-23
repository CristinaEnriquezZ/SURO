using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;



    public class Seguimiento
    {
        //Propiedades para el seguimiento del oficio
        public int ID { get; set; }
        public string NotasSeguimiento { get; set; }
        public string RutaArchivoSeguimiento { get; set; }
        public DateTime? FechaSeguimiento { get; set; }
        public string IdUsuario { get; set; }

    }
