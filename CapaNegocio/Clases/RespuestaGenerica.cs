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
        public object objPeticionCliente { get; set; }
        public object objRespuestaCliente { get; set; }
        public object objPeticionProveedor { get; set; }
        public object objRespuestaProveedor { get; set; }

        public categoriaProducto categoriaProducto { get; set; }

        public RespuestaGenerica()
        {
            codigoRespuesta = 0;
            trama = string.Empty;
        }
    }
}
