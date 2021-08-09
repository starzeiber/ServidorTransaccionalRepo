namespace CapaNegocio.Clases
{
    /// <summary>
    /// Contiene todas las propiedades de un producto
    /// </summary>
    public class ProveedorInfo
    {
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
        public string marca { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string issuer { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int idMaster { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public ProveedorInfo()
        {
            nombreProveedor = "";
            marca = "";
            issuer = "6800000001";
        }
    }
}
