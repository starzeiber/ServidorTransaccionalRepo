using System;

namespace CapaNegocio
{
    /// <summary>
    /// Clase que contiene todas las propiedades constantes en una solicitud de TAE al PX
    /// </summary>
    public class CompraPxBase
    {
        /// <summary>
        /// Encabezado de la mensajeria
        /// </summary>
        public int encabezado { get; set; }
        /// <summary>
        /// Identificador del grupo
        /// </summary>
        public int idGrupo { get; set; }
        /// <summary>
        /// Identificador de la cadena
        /// </summary>
        public int idCadena { get; set; }
        /// <summary>
        /// Identificador de la tienda
        /// </summary>
        public int idTienda { get; set; }
        /// <summary>
        /// identificador del cadejo
        /// </summary>
        public int idPos { get; set; }
        /// <summary>
        /// Fecha de la transacción en formato YYMMdd
        /// </summary>
        public String fecha { get; set; }
        /// <summary>
        /// Hora de la transacción en formato HHmmss
        /// </summary>
        public String hora { get; set; }
        /// <summary>
        /// Region asignada al teléfono del abono
        /// </summary>
        public int region { get; set; }
        /// <summary>
        /// Identificador del producto
        /// </summary>
        public String sku { get; set; }
        /// <summary>
        /// Teléfono para el abono de saldo
        /// </summary>
        public String telefono { get; set; }
        /// <summary>
        /// Numero consecutivo que identifica la transacción
        /// </summary>
        public int numeroTransaccion { get; set; }
    }
}
