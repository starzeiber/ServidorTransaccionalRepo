using System;
using System.Text;
using System.Threading.Tasks;

namespace CapaNegocio.Clases
{
    /// <summary>
    /// Clase que contiene todas las propiedades de una compra de paquetes de datos TPV
    /// </summary>
    public class CompraTpvDatos : CompraTpvBase
    {
        /// <summary>
        /// 
        /// </summary>
        public string idPaquete { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public CompraTpvDatos()
        {
            idPaquete = "";
        }

        /// <summary>
        /// Función para dividir en las propiedades de la clase
        /// </summary>
        /// <param name="compraPxDatos">Instancia de CompraPxDatos</param>
        /// <returns></returns>
        public bool Ingresar(CompraPxDatos compraPxDatos)
        {
            try
            {
                pCode = 650101;
                systemTrace = compraPxDatos.numeroTransaccion;
                issuer = compraPxDatos.proveedorInfo.issuer.Length + compraPxDatos.proveedorInfo.issuer;
                referencia = Task.Run(() => Utileria.ObtenerNumeroResultadoAleatorio(6)).Result;
                TerminalId = "STTN" +
                    Validaciones.formatoValor(compraPxDatos.idGrupo.ToString(), TipoFormato.N, 3) +
                    Validaciones.formatoValor(compraPxDatos.idCadena.ToString(), TipoFormato.N, 5) +
                    Validaciones.formatoValor(compraPxDatos.idTienda.ToString(), TipoFormato.N, 4);
                merchantData = "TARJETASN      " +
                    Validaciones.formatoValor(compraPxDatos.idGrupo.ToString(), TipoFormato.N, 5) +
                    Validaciones.formatoValor(compraPxDatos.idCadena.ToString(), TipoFormato.N, 5) +
                    Validaciones.formatoValor(compraPxDatos.idTienda.ToString(), TipoFormato.N, 5) +
                    Validaciones.formatoValor(compraPxDatos.idPos.ToString(), TipoFormato.N, 5) +
                    "DF MX";
                telefono = "01500000" + compraPxDatos.telefono;
                idPaquete = compraPxDatos.datosAdicionales.Substring(0, 10);

                return true;
            }
            catch (Exception ex)
            {
                Task.Run(() => Utileria.Log(Utileria.ObtenerNombreFuncion(ex.Message), Utileria.TiposLog.error));
                return false;
            }

        }

        /// <summary>
        /// Función para obtener la trama TPV de compra
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
                respuesta.Append(Validaciones.formatoValor(TerminalId, TipoFormato.ANS, 16));
                respuesta.Append(Validaciones.formatoValor(merchantData, TipoFormato.ANS, 40));
                respuesta.Append(Validaciones.formatoValor(codigoMoneda.ToString(), TipoFormato.N, 3));
                respuesta.Append(Validaciones.formatoValor(datosAdicionales, TipoFormato.N, 15));
                respuesta.Append(Validaciones.formatoValor(telefono, TipoFormato.N, 18));
                respuesta.Append(Validaciones.formatoValor(idPaquete, TipoFormato.ANS, 10));


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
