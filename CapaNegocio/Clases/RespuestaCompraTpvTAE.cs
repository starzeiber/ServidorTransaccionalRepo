using System;
using System.Text;
using System.Threading.Tasks;

namespace CapaNegocio.Clases
{
    /// <summary>
    /// Contiene todas las propiedades de una respuesta en protocolo TPV
    /// </summary>
    public class RespuestaCompraTpvTAE : RespuestaCompraTpvBase
    {

        /// <summary>
        /// Ingresa la información en las propiedades de la clase a partir de la compra
        /// </summary>
        /// <param name="compraTpvTae">Instancia de CompraTpvTae</param>
        /// <returns></returns>
        public bool Ingresar(CompraTpvTae compraTpvTae)
        {
            try
            {
                pCode = compraTpvTae.pCode;
                monto = compraTpvTae.monto;
                systemTrace = compraTpvTae.systemTrace;
                issuer = compraTpvTae.issuer;
                referencia = compraTpvTae.referencia;
                TerminalId = compraTpvTae.TerminalId;
                merchantData = compraTpvTae.merchantData;
                telefono = compraTpvTae.telefono;
                fechaHora = compraTpvTae.fechaHora;
                horaTerminal = compraTpvTae.horaTerminal;
                fechaTerminal = compraTpvTae.fechaTerminal;
                fechaCapturaTerminal = compraTpvTae.fechaCapturaTerminal;
                fechaContableTerminal = compraTpvTae.fechaContableTerminal;
                return true;
            }
            catch (Exception ex)
            {
                Task.Run(() => Utileria.Log(Utileria.ObtenerNombreFuncion(ex.Message), Utileria.TiposLog.error));
                return false;
            }
        }

        /// <summary>
        /// Ingresa la información en las propiedades de la clase a partir de la trama de respuesta
        /// </summary>
        /// <param name="trama"></param>
        /// <returns></returns>
        public bool Ingresar(string trama)
        {
            try
            {
                autorizacion = int.Parse(trama.Substring(91, 6));
                codigoRespuesta = int.Parse(trama.Substring(97, 2));
                return true;
            }
            catch (Exception ex)
            {
                Task.Run(() => Utileria.Log(Utileria.ObtenerNombreFuncion(ex.Message), Utileria.TiposLog.error));
                return false;
            }
        }

        /// <summary>
        /// Obtiene la trama de respuesta a partir de las propiedades de la clase
        /// </summary>
        /// <returns></returns>
        public string Obtener()
        {
            StringBuilder respuesta = new StringBuilder();
            try
            {
                respuesta.Append(encabezado.ToString());
                respuesta.Append(Validaciones.formatoValor(pCode.ToString(), TipoFormato.N, 6));
                respuesta.Append(Validaciones.formatoValor(monto.ToString(), TipoFormato.N, 12));
                respuesta.Append(Validaciones.formatoValor(fechaHora.ToString(), TipoFormato.N, 10));
                respuesta.Append(Validaciones.formatoValor(systemTrace.ToString(), TipoFormato.N, 6));
                respuesta.Append(Validaciones.formatoValor(horaTerminal, TipoFormato.N, 6));
                respuesta.Append(Validaciones.formatoValor(fechaTerminal, TipoFormato.N, 4));
                respuesta.Append(Validaciones.formatoValor(fechaContableTerminal.ToString(), TipoFormato.N, 4));
                respuesta.Append(Validaciones.formatoValor(fechaCapturaTerminal.ToString(), TipoFormato.N, 4));
                respuesta.Append(Validaciones.formatoValor(adquiriente, TipoFormato.N, 12));
                respuesta.Append(Validaciones.formatoValor(issuer, TipoFormato.N, 11));
                respuesta.Append(Validaciones.formatoValor(autorizacion.ToString(), TipoFormato.N, 6));
                respuesta.Append(Validaciones.formatoValor(codigoRespuesta.ToString(), TipoFormato.N, 2));
                respuesta.Append(Validaciones.formatoValor(referencia.ToString(), TipoFormato.N, 12));
                respuesta.Append(Validaciones.formatoValor(TerminalId, TipoFormato.ANS, 16));
                respuesta.Append(Validaciones.formatoValor(merchantData, TipoFormato.ANS, 40));
                respuesta.Append(Validaciones.formatoValor(codigoMoneda.ToString(), TipoFormato.N, 3));
                respuesta.Append(Validaciones.formatoValor(datosAdicionales, TipoFormato.N, 15));
                respuesta.Append(Validaciones.formatoValor(telefono, TipoFormato.N, 18));

                return respuesta.ToString();
            }
            catch (Exception ex)
            {
                Task.Run(() => Utileria.Log(Utileria.ObtenerNombreFuncion(ex.Message), Utileria.TiposLog.error));
                return String.Empty;
            }
        }
    }
}
