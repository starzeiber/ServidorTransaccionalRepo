using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CapaNegocio.Operaciones;

namespace CapaNegocio
{
    
    public class RespuestaGenerica
    {
        public CabecerasTrama cabecerasTrama { get; set; }
        public int codigoRespuesta { get; set; }
        public string trama { get; set; }

        public SolicitudPxTae solicitudPxTae { get; set; }
        public RespuestaSolicitudPxTae respuestaSolicitudPxTae { get; set; }

        public SolicitudPxDatos solicitudPxDatos { get; set; }
        public RespuestaSolicitudPxDatos respuestaSolicitudPxDatos { get; set; }

        public ConsultaPxTae consultaPxTae { get; set; }
        public RespuestaConsultaPxTae respuestaConsultaPxTae { get; set; }

        public ConsultaPxDatos consultaPxDatos { get; set; }
        public RespuestaConsultaPxDatos respuestaConsultaPxDatos { get; set; }

        public RespuestaGenerica()
        {
            codigoRespuesta = 0;
            trama = string.Empty;
        }
    }
}
