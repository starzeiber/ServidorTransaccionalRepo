using System;
using static CapaNegocio.Operaciones;

namespace CapaNegocio
{

    public class RespuestaProcesosProveedor
    {
        public CabecerasTrama cabeceraTrama { get; set; }
        public int codigoRespuesta { get; set; }
        public object objPeticionProveedor { get; set; }
        public object objRespuestaProveedor { get; set; }
        public categoriaProducto categoriaProducto { get; set; }

        /// <summary>
        /// Objeto que se utilizará como respuesta genérica de cualquier función
        /// </summary>
        public object objetoAux { get; set; }

        public static implicit operator RespuestaProcesosProveedor(RespuestaProcesosCliente v)
        {
            throw new NotImplementedException();
        }
    }
}
