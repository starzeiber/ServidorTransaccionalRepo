﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaNegocio.Clases
{
    public class RespuestaCompraTpvBase
    {
        public int encabezado { get; set; }
        public int pCode { get; set; }
        public decimal monto { get; set; }
        /// <summary>
        /// MMDDHHmmss
        /// </summary>
        public string fechaHora { get; set; }
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
        public string adquiriente { get; set; }
        public string issuer { get; set; }
        public double referencia { get; set; }
        public int autorizacion { get; set; }
        public int codigoRespuesta { get; set; }
        public string TerminalId { get; set; }
        public string merchantData { get; set; }
        public int codigoMoneda { get; set; }
        public string datosAdicionales { get; set; }
        public string telefono { get; set; }

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
