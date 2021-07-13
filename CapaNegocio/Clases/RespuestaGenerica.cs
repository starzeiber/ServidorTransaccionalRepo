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

        //public CompraPxTae compraPxTae { get; set; }
        //public RespuestaCompraPxTae respuestaCompraPxTae { get; set; }

        //public CompraPxDatos compraPxDatos { get; set; }
        //public RespuestaCompraPxDatos respuestaCompraPxDatos { get; set; }

        //public ConsultaPxTae consultaPxTae { get; set; }
        //public RespuestaConsultaPxTae respuestaConsultaPxTae { get; set; }

        //public ConsultaPxDatos consultaPxDatos { get; set; }
        //public RespuestaConsultaPxDatos respuestaConsultaPxDatos { get; set; }

        public object objPeticionCliente { get; set; }

        public object objRespuestaCliente { get; set; }

        public RespuestaGenerica()
        {
            codigoRespuesta = 0;
            trama = string.Empty;
        }
    }
}
