using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaNegocio
{
    /// <summary>
    /// Clase que contiene todas las propiedades y funciones sobre la trama de abono
    /// </summary>
    internal class mensaje200
    {
        #region "Propiedades"
        /// <summary>
        /// Identificador del tipo de mensaje
        /// </summary>
        internal String tipoMensaje = "200";
        /// <summary>
        /// codigo de producto
        /// </summary>
        internal int codigoProducto = 0;
        /// <summary>
        /// Monto donde las ultimas dos posiciones son decimales
        /// </summary>
        internal long monto = 0;
        /// <summary>
        /// Fecha y hora de la transacción en formato MMddHHmmss
        /// </summary>
        internal String fechaHora = String.Empty;
        /// <summary>
        /// Número de la transacción interno
        /// </summary>
        internal long systemTrace = 0;
        /// <summary>
        /// Hora de la terminal en formato hhmmss
        /// </summary>
        internal String horaTerminal = String.Empty;
        /// <summary>
        /// Fecha de la terminal
        /// </summary>
        internal String fechaTerminal = String.Empty;
        /// <summary>
        /// Fecha contable en formato MMdd
        /// </summary>
        internal String fechaContable = String.Empty;
        /// <summary>
        /// Fecha de entrada en formato MMdd
        /// </summary>
        internal String fechaEntrada = String.Empty;
        /// <summary>
        /// Identificador del adquiriente
        /// </summary>
        internal long lladquiriente = 0;
        /// <summary>
        /// Issuer asignado a la operación
        /// </summary>
        internal long llissuer = 0;
        /// <summary>
        /// Número de referencia
        /// </summary>
        internal long numeroReferencia = 0;
        /// <summary>
        /// Identificador de la terminal
        /// </summary>
        internal String identificadorTerminal = String.Empty;
        /// <summary>
        /// Datos de la tienda
        /// </summary>
        internal String datosTienda = String.Empty;
        /// <summary>
        /// Codigo de moneda
        /// </summary>
        internal Int16 codigoMoneda = 0;
        /// <summary>
        /// Datos adicionales
        /// </summary>
        internal String llldatosAdicionales = String.Empty;
        /// <summary>
        /// Número telefónico
        /// </summary>
        internal long lllnumeroTelefonico = 0;
        #endregion

        #region "Tamaño de campos"
        /// <summary>
        /// Tamano de Identificador del tipo de mensaje
        /// </summary>
        private Int16 tamanoTipoMensaje = 3;
        /// <summary>
        /// Tamano de codigo de producto
        /// </summary>
        private Int16 tamanoCodigoProducto = 6;
        /// <summary>
        /// Tamano de Monto donde las ultimas dos posiciones son decimales
        /// </summary>
        private Int16 tamanoMonto = 12;
        /// <summary>
        /// Tamano de Fecha y hora de la transacción en formato MMddHHmmss
        /// </summary>
        private Int16 tamanoFechaHora = 10;
        /// <summary>
        /// Tamano de Número de la transacción interno
        /// </summary>
        private Int16 tamanoSystemTrace = 6;
        /// <summary>
        /// Tamano de Hora de la terminal en formato hhmmss
        /// </summary>
        private Int16 tamanoHoraTerminal = 6;
        /// <summary>
        /// Tamano de Fecha de la terminal
        /// </summary>
        private Int16 tamanoFechaTerminal = 4;
        /// <summary>
        /// Tamano de Fecha contable en formato MMdd
        /// </summary>
        private Int16 tamanoFechaContable = 4;
        /// <summary>
        /// Tamano de Fecha de entrada en formato MMdd
        /// </summary>
        private Int16 tamanoFechaEntrada = 4;
        /// <summary>
        /// Tamano de Identificador del adquiriente
        /// </summary>
        private Int16 tamanoAdquiriente = 12;
        /// <summary>
        /// Tamano de Issuer asignado a la operación
        /// </summary>
        private Int16 tamanoIssuer = 12;
        /// <summary>
        /// Tamano de Número de referencia
        /// </summary>
        private Int16 tamanoNumeroReferencia = 12;
        /// <summary>
        /// Tamano de Identificador de la terminal
        /// </summary>
        private Int16 tamanoIdentificadorTerminal = 16;
        /// <summary>
        /// Tamano de Datos de la tienda
        /// </summary>
        private Int16 tamanoDatosTienda = 40;
        /// <summary>
        /// Tamano de Codigo de moneda
        /// </summary>
        private Int16 tamanoCodigoMoneda = 3;
        /// <summary>
        /// Tamano de Datos adicionales
        /// </summary>
        private Int16 tamanoDatosAdicionales = 15;
        /// <summary>
        /// Tamano de Número telefónico
        /// </summary>
        private Int16 tamanoNumeroTelefonico = 18;
        #endregion

        #region "Funciones"

        internal Boolean desgloceTrama(String trama)
        {
            try
            {
                Int16 posicionSiguiente = 0;
                tipoMensaje = trama.Substring(0, tamanoTipoMensaje);
                posicionSiguiente += tamanoTipoMensaje;
                codigoProducto = int.Parse(trama.Substring(posicionSiguiente, tamanoCodigoProducto));
                posicionSiguiente += tamanoCodigoProducto;
                monto =long.Parse(trama.Substring(posicionSiguiente, tamanoMonto));
                posicionSiguiente += tamanoMonto;
                fechaHora = trama.Substring(posicionSiguiente, tamanoFechaHora);
                posicionSiguiente += tamanoFechaHora;
                systemTrace = long.Parse(trama.Substring(posicionSiguiente, tamanoSystemTrace));
                posicionSiguiente += tamanoSystemTrace;
                horaTerminal = trama.Substring(posicionSiguiente, tamanoHoraTerminal);
                posicionSiguiente += tamanoHoraTerminal;
                fechaTerminal = trama.Substring(posicionSiguiente, tamanoFechaTerminal);
                posicionSiguiente += tamanoFechaTerminal;
                fechaContable = trama.Substring(posicionSiguiente, tamanoFechaContable);
                posicionSiguiente += tamanoFechaContable;
                fechaEntrada = trama.Substring(posicionSiguiente, tamanoFechaEntrada);
                posicionSiguiente += tamanoFechaEntrada;                
                lladquiriente = long.Parse(trama.Substring(posicionSiguiente, tamanoAdquiriente));
                posicionSiguiente += tamanoAdquiriente;
                llissuer= long.Parse(trama.Substring(posicionSiguiente, tamanoIssuer));
                posicionSiguiente += tamanoIssuer;
                numeroReferencia= long.Parse(trama.Substring(posicionSiguiente, tamanoNumeroReferencia));
                posicionSiguiente += tamanoNumeroReferencia;
                identificadorTerminal= trama.Substring(posicionSiguiente, tamanoIdentificadorTerminal);
                posicionSiguiente += tamanoIdentificadorTerminal;
                datosTienda= trama.Substring(posicionSiguiente, tamanoDatosTienda);
                posicionSiguiente += tamanoDatosTienda;
                codigoMoneda=Int16.Parse(trama.Substring(posicionSiguiente, tamanoCodigoMoneda));
                posicionSiguiente += tamanoCodigoMoneda;
                llldatosAdicionales= trama.Substring(posicionSiguiente, tamanoDatosAdicionales);
                posicionSiguiente += tamanoDatosAdicionales;
                lllnumeroTelefonico=long.Parse(trama.Substring(posicionSiguiente, tamanoNumeroTelefonico));

                return true;
            }
            catch (Exception ex)
            {
                //TODO: colocar log
                return false;
                
            }
        }
#endregion
    }
}