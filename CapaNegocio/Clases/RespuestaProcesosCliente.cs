namespace CapaNegocio
{
    /// <summary>
    /// Contiene todas las propiedades que se utilizan como respuesta entre los procesos internos
    /// </summary>
    public class RespuestaProcesosCliente
    {
        //public CabecerasTrama cabeceraTrama { get; set; }
        /// <summary>
        /// Codigo de respuesta del proceso
        /// </summary>
        public int codigoRespuesta { get; set; }
        /// <summary>
        /// Objeto que recibe la clase que contiene las propiedades de una petición
        /// </summary>
        public object objPeticionCliente { get; set; }
        /// <summary>
        /// Objeto que recibe la clase que contiene las propiedades de una respuesta
        /// </summary>
        public object objRespuestaCliente { get; set; }
        ///// <summary>
        ///// Instancia de CategoriaProducto
        ///// </summary>
        //public CategoriaProducto categoriaProducto { get; set; }

        /// <summary>
        /// Objeto que se utilizará genericamente en cualquier función de forma auxiliar en las operaciones
        /// </summary>
        public object objetoAux { get; set; }

    }
}
