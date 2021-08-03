namespace CapaNegocio.Clases
{
    public class ProductoInfo
    {
        public string sku { get; set; }
        public int idProveedor { get; set; }
        public string nombreProveedor { get; set; }
        public decimal monto { get; set; }
        public string mensajeTicket1 { get; set; }
        public string mensajeTicket2 { get; set; }
        public string idPaquete { get; set; }

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
