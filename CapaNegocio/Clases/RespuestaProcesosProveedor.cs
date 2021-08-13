using static CapaNegocio.Operaciones;

namespace CapaNegocio
{
    /// <summary>
    /// Contiene todas las propiedades que se utilizan como respuesta entre los procesos internos
    /// </summary>
    public class RespuestaProcesosProveedor
    {
        //public CabecerasTrama cabeceraTrama { get; set; }
        /// <summary>
        /// Codigo de respuesta del proceso
        /// </summary>
        public int codigoRespuesta { get; set; }
        /// <summary>
        /// Objeto que recibe la clase que contiene las propiedades de una petición
        /// </summary>
        public object objPeticionProveedor { get; set; }
        /// <summary>
        /// Objeto que recibe la clase que contiene las propiedades de una respuesta
        /// </summary>
        public object objRespuestaProveedor { get; set; }
        /// <summary>
        /// Instancia de CategoriaProducto
        /// </summary>
        public CategoriaProducto categoriaProducto { get; set; }

        ///// <summary>
        ///// Objeto que se utilizará como respuesta genérica de cualquier función
        ///// </summary>
        //public object objetoAux { get; set; }

    }
}
