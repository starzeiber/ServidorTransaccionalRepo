using System;
using System.Text;
using System.Threading.Tasks;

namespace CapaNegocio.Clases
{
    /// <summary>
    /// Clase que contiene todas las propiedades de una compra TPV
    /// </summary>
    public class CompraTpvTae : CompraTpvBase
    {
        /// <summary>
        /// 
        /// </summary>
        public string sku { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int idProveedor { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int idMaster { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public decimal saldoActual { get; set; }

        public DateTime fechaHoraCompleta { get; set; }


        /// <summary>
        /// Función que obtiene las propiedades a partir de la instancia CompraPxTae
        /// </summary>
        /// <param name="compraPxTae">Instancia CompraPxTae</param>
        /// <returns></returns>
        public bool Ingresar(CompraPxTae compraPxTae)
        {
            try
            {
                pCode = 650000;
                monto = compraPxTae.productoInfo.monto;
                fechaHora = compraPxTae.fecha.Substring(2) + compraPxTae.hora;
                fechaHoraCompleta = DateTime.Now;
                systemTrace = compraPxTae.numeroTransaccion;
                //TODO es para pruebas la siguiente línea
                //systemTrace = Utileria.ObtenerNumeroResultadoAleatorio(6);
                horaTerminal = compraPxTae.hora;
                fechaTerminal = compraPxTae.fecha.Substring(2);
                fechaContableTerminal = fechaTerminal;
                fechaCapturaTerminal = fechaTerminal;
                issuer = compraPxTae.proveedorInfo.issuer.Length + compraPxTae.proveedorInfo.issuer;
                referencia = Utileria.ObtenerNumeroResultadoAleatorio(6);
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

                sku = compraPxTae.productoInfo.sku;
                idProveedor = compraPxTae.proveedorInfo.idProveedor;
                idMaster = compraPxTae.proveedorInfo.idMaster;
                return true;
            }
            catch (Exception ex)
            {
                Task.Run(() => Utileria.Log(Utileria.ObtenerNombreFuncion(ex.Message), Utileria.TiposLog.error));
                return false;
            }

        }

        public bool Ingresar(ConsultaPxTae consultaPxTae)
        {
            try
            {
                pCode = 650000;
                monto = consultaPxTae.productoInfo.monto;
                fechaHora = consultaPxTae.fecha.Substring(2) + consultaPxTae.hora;
                systemTrace = consultaPxTae.numeroTransaccion;
                horaTerminal = consultaPxTae.hora;
                fechaTerminal = consultaPxTae.fecha.Substring(2);
                fechaContableTerminal = fechaTerminal;
                fechaCapturaTerminal = fechaTerminal;
                issuer = consultaPxTae.proveedorInfo.issuer.Length + consultaPxTae.proveedorInfo.issuer;
                referencia = Utileria.ObtenerNumeroResultadoAleatorio(6);
                TerminalId = "STTN" +
                    Validaciones.formatoValor(consultaPxTae.idGrupo.ToString(), TipoFormato.N, 3) +
                    Validaciones.formatoValor(consultaPxTae.idCadena.ToString(), TipoFormato.N, 5) +
                    Validaciones.formatoValor(consultaPxTae.idTienda.ToString(), TipoFormato.N, 4);
                merchantData = "TARJETASN      " +
                    Validaciones.formatoValor(consultaPxTae.idGrupo.ToString(), TipoFormato.N, 5) +
                    Validaciones.formatoValor(consultaPxTae.idCadena.ToString(), TipoFormato.N, 5) +
                    Validaciones.formatoValor(consultaPxTae.idTienda.ToString(), TipoFormato.N, 5) +
                    Validaciones.formatoValor(consultaPxTae.idPos.ToString(), TipoFormato.N, 5) +
                    "DF MX";
                telefono = consultaPxTae.telefono;

                sku = consultaPxTae.productoInfo.sku;
                idProveedor = consultaPxTae.proveedorInfo.idProveedor;
                idMaster = consultaPxTae.proveedorInfo.idMaster;
                return true;
            }
            catch (Exception ex)
            {
                Task.Run(() => Utileria.Log(Utileria.ObtenerNombreFuncion(ex.Message), Utileria.TiposLog.error));
                return false;
            }

        }

        /// <summary>
        /// Obtiene la trama en procotolo TPV
        /// </summary>
        /// <returns></returns>
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
