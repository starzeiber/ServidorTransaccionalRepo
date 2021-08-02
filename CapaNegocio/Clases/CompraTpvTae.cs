using System;
using System.Text;
using System.Threading.Tasks;

namespace CapaNegocio.Clases
{
    public class CompraTpvTae : CompraTpvBase
    {
        public bool Ingresar(CompraPxTae compraPxTae)
        {
            try
            {
                pCode = 650000;
                decimal montoX = 100.5M;
                //TODO obtener el monto a partir del SKU de la solicitud PX                
                monto = Convert.ToDouble(montoX);
                systemTrace = compraPxTae.numeroTransaccion;
                // TODO obtener el issuer de la base
                issuer = "106800000001";
                referencia = Task.Run(() => Utileria.ObtenerNumeroResultadoAleatorio(6)).Result;
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
                Task.Run(() => Utileria.Log(Utileria.ObtenerNombreFuncion(ex.Message), Utileria.TiposLog.error));
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
                int dosDecimales = (int)(((decimal)monto % 1) * 100);
                respuesta.Append(Validaciones.formatoValor(monto.ToString().Split('.')[0] + dosDecimales.ToString("00"), TipoFormato.N, 12));
                respuesta.Append(Validaciones.formatoValor(fechaHora.ToString(), TipoFormato.N, 10));
                respuesta.Append(Validaciones.formatoValor(systemTrace.ToString(), TipoFormato.N, 6));
                respuesta.Append(Validaciones.formatoValor(horaTerminal, TipoFormato.N, 6));
                respuesta.Append(Validaciones.formatoValor(fechaTerminal, TipoFormato.N, 4));
                respuesta.Append(Validaciones.formatoValor(fechaContableTerminal.ToString(), TipoFormato.N, 4));
                respuesta.Append(Validaciones.formatoValor(fechaCapturaTerminal.ToString(), TipoFormato.N, 4));
                respuesta.Append(Validaciones.formatoValor(adquiriente, TipoFormato.N, 12));
                respuesta.Append(Validaciones.formatoValor(issuer, TipoFormato.N, 11));
                respuesta.Append(Validaciones.formatoValor(referencia.ToString(), TipoFormato.N, 12));
                respuesta.Append(Validaciones.formatoValor(TerminalId, TipoFormato.ANS, 16));
                respuesta.Append(Validaciones.formatoValor(merchantData, TipoFormato.ANS, 40));
                respuesta.Append(Validaciones.formatoValor(codigoMoneda.ToString(), TipoFormato.N, 3));
                respuesta.Append(Validaciones.formatoValor(datosAdicionales, TipoFormato.N, 15));
                respuesta.Append("015" + Validaciones.formatoValor(telefono, TipoFormato.N, 15));

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
