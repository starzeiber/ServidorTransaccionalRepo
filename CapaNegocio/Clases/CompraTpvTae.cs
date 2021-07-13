using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaNegocio.Clases
{
    public class CompraTpvTae:CompraTpvBase
    {        
        public bool Ingresar(CompraPxTae compraPxTae)
        {
            try
            {
                pCode = 650000;
                //TODO obtener el monto a partir del SKU de la solicitud PX
                monto = 100;
                systemTrace = compraPxTae.numeroTransaccion;
                // TODO obtener el issuer de la base
                issuer = "106800000001";
                referencia =Task.Run(() => UtileriaVariablesGlobales.ObtenerNumeroResultadoAleatorio(6)).Result;
                TerminalId = "STTN" +
                    Validaciones.formatoValor(compraPxTae.idGrupo.ToString(), TipoFormato.N, 3) +
                    Validaciones.formatoValor(compraPxTae.idCadena.ToString(), TipoFormato.N, 5) +
                    Validaciones.formatoValor(compraPxTae.idTienda.ToString(), TipoFormato.N, 4);
                merchantData = "TARJETASN      " +
                    Validaciones.formatoValor(compraPxTae.idGrupo.ToString(), TipoFormato.N, 5) +
                    Validaciones.formatoValor(compraPxTae.idCadena.ToString(), TipoFormato.N, 5) +
                    Validaciones.formatoValor(compraPxTae.idTienda.ToString(), TipoFormato.N, 5) +
                    Validaciones.formatoValor(compraPxTae.idPos.ToString(), TipoFormato.N, 5) +
                    "DF MX";
                telefono = compraPxTae.telefono;
                return true;
            }
            catch (Exception ex)
            {
                Task.Run(() => UtileriaVariablesGlobales.Log(UtileriaVariablesGlobales.ObtenerNombreFuncion(ex.Message), UtileriaVariablesGlobales.TiposLog.error));
                return false;
            }
            
        }
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
                respuesta.Append(Validaciones.formatoValor(TerminalId, TipoFormato.ANS, 16));
                respuesta.Append(Validaciones.formatoValor(merchantData, TipoFormato.ANS, 40));
                respuesta.Append(Validaciones.formatoValor(codigoMoneda.ToString(), TipoFormato.N, 3));
                respuesta.Append(Validaciones.formatoValor(datosAdicionales, TipoFormato.N, 15));
                respuesta.Append(Validaciones.formatoValor(telefono, TipoFormato.N, 18));

                return respuesta.ToString();
            }
            catch (Exception ex)
            {
                Task.Run(() => UtileriaVariablesGlobales.Log(UtileriaVariablesGlobales.ObtenerNombreFuncion(ex.Message), UtileriaVariablesGlobales.TiposLog.error));
                return String.Empty;
            }
        }
    }
}
