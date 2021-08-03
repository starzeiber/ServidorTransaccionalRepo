using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaNegocio.Clases
{
    public class CompraTpvDatos:CompraTpvBase
    {
        public string codigoProducto { get; set; }
        public string referencia2 { get; set; }

        public CompraTpvDatos()
        {
            codigoProducto = "";
            referencia2 = "";
        }


        public bool DividirTrama(CompraPxDatos solicitudPxDatos)
        {
            try
            {
                pCode = 650000;                            
                systemTrace = solicitudPxDatos.numeroTransaccion;                
                issuer = "106800000001";
                referencia = Task.Run(() => Utileria.ObtenerNumeroResultadoAleatorio(6)).Result;
                TerminalId = "STTN" +
                    Validaciones.formatoValor(solicitudPxDatos.idGrupo.ToString(), TipoFormato.N, 3) +
                    Validaciones.formatoValor(solicitudPxDatos.idCadena.ToString(), TipoFormato.N, 5) +
                    Validaciones.formatoValor(solicitudPxDatos.idTienda.ToString(), TipoFormato.N, 4);
                merchantData = "TARJETASN      " +
                    Validaciones.formatoValor(solicitudPxDatos.idGrupo.ToString(), TipoFormato.N, 5) +
                    Validaciones.formatoValor(solicitudPxDatos.idCadena.ToString(), TipoFormato.N, 5) +
                    Validaciones.formatoValor(solicitudPxDatos.idTienda.ToString(), TipoFormato.N, 5) +
                    Validaciones.formatoValor(solicitudPxDatos.idPos.ToString(), TipoFormato.N, 5) +
                    "DF MX";
                codigoProducto = solicitudPxDatos.datosAdicionales.Substring(0, 10);                
                telefono = solicitudPxDatos.telefono;
                referencia2 = telefono;
                return true;
            }
            catch (Exception ex)
            {
                Task.Run(() => Utileria.Log(Utileria.ObtenerNombreFuncion(ex.Message), Utileria.TiposLog.error));
                return false;
            }
            
        }
        public string ObtenerTrama()
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
                respuesta.Append(Validaciones.formatoValor(codigoProducto, TipoFormato.N, 18));
                respuesta.Append(Validaciones.formatoValor(referencia2, TipoFormato.ANS, 100));

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
