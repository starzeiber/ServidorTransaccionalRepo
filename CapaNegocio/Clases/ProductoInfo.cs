namespace CapaNegocio.Clases
{
    /// <summary>
    /// Contiene todas las propiedades de un producto
    /// </summary>
    public class ProductoInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public string sku { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int idProveedor { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string nombreProveedor { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public decimal monto { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string mensajeTicket1 { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string mensajeTicket2 { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string idPaquete { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public ProductoInfo()
        {
            sku = "";
            nombreProveedor = "";
            mensajeTicket1 = "";
            mensajeTicket2 = "";
            idPaquete = "";
        }
    }
}
