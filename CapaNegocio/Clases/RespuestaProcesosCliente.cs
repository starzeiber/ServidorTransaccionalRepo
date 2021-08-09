using static CapaNegocio.Operaciones;

namespace CapaNegocio
{

    public class RespuestaProcesosCliente
    {
        public CabecerasTrama cabeceraTrama { get; set; }
        public int codigoRespuesta { get; set; }
        public object objPeticionCliente { get; set; }
        public object objRespuestaCliente { get; set; }
        public object objPeticionProveedor { get; set; }
        public object objRespuestaProveedor { get; set; }
        public categoriaProducto categoriaProducto { get; set; }

        /// <summary>
        /// Objeto que se utilizará genericamente en cualquier función de forma auxiliar en las operaciones
        /// </summary>
        public object objetoAux { get; set; }


        public RespuestaProcesosCliente()
        {
            
        }
    }
}
