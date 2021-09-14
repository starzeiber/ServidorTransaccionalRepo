using System;
using System.Text;
using System.Threading.Tasks;

namespace CapaNegocio.Clases
{
    /// <summary>
    /// Clase que contiene todas las propiedades de una respuesta para la compra de datos en protocolo tpv
    /// </summary>
    public class RespuestaCompraTpvDatos : RespuestaCompraTpvBase
    {
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
        /// Ingresa la información de la compra en las propiedades de la respuesta
        /// </summary>
        /// <param name="compraTpvDatos"></param>
        /// <returns></returns>
        public bool Ingresar(CompraTpvDatos compraTpvDatos)
        {
            try
            {
                pCode = compraTpvDatos.pCode;
                monto = compraTpvDatos.monto;
                systemTrace = compraTpvDatos.systemTrace;
                issuer = compraTpvDatos.issuer;
                referencia = compraTpvDatos.referencia;
                TerminalId = compraTpvDatos.TerminalId;
                merchantData = compraTpvDatos.merchantData;
                telefono = compraTpvDatos.telefono;
                fechaHora = compraTpvDatos.fechaHora;
                horaTerminal = compraTpvDatos.horaTerminal;
                fechaTerminal = compraTpvDatos.fechaTerminal;
                fechaCapturaTerminal = compraTpvDatos.fechaCapturaTerminal;
                fechaContableTerminal = compraTpvDatos.fechaContableTerminal;
                return true;
            }
            catch (Exception ex)
            {
                Task.Run(() => Utileria.Log(Utileria.ObtenerNombreFuncion(ex.Message), Utileria.TiposLog.error));
                return false;
            }
        }

        /// <summary>
        /// Obtiene una trama a partir de las propiedades de la clase
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
