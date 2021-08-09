using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaNegocio.Clases
{
    /// <summary>
    /// Clase que contiene todas las propiedades de una respueta a una compra del protocolo TPV
    /// </summary>
    public class RespuestaCompraTpvBase
    {
        /// <summary>
        /// 
        /// </summary>
        public int encabezado { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int pCode { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public decimal monto { get; set; }
        /// <summary>
        /// MMDDHHmmss
        /// </summary>
        public string fechaHora { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int systemTrace { get; set; }
        /// <summary>
        /// hhmmss
        /// </summary>
        public string horaTerminal { get; set; }
        /// <summary>
        /// MMDD
        /// </summary>
        public string fechaTerminal { get; set; }
        /// <summary>
        /// MMDD
        /// </summary>
        public string fechaContableTerminal { get; set; }
        /// <summary>
        /// MMDD
        /// </summary>
        public string fechaCapturaTerminal { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string adquiriente { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string issuer { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double referencia { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int autorizacion { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int codigoRespuesta { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string TerminalId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string merchantData { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int codigoMoneda { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string datosAdicionales { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string telefono { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public RespuestaCompraTpvBase()
        {
            encabezado = 210;
            pCode = 0;
            monto = 0;
            fechaHora = DateTime.Now.ToString("MMddHHmmss");
            systemTrace = 0;
            fechaTerminal = DateTime.Now.ToString("MMdd");
            horaTerminal = DateTime.Now.ToString("hhmmss");
            fechaContableTerminal = DateTime.Now.ToString("MMdd");
            fechaCapturaTerminal = DateTime.Now.ToString("MMdd");
            adquiriente = "106900000001";
            issuer = "106800000001";
            referencia = 0;
            autorizacion = 0;
            codigoRespuesta = 0;
            TerminalId = "";
            merchantData = "";
            codigoMoneda = 484;
            datosAdicionales = "012B999PRO1+000";
            telefono = "";
        }

    }
}
