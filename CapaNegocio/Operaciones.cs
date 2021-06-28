using CapaNegocio.Clases;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace CapaNegocio
{
    /// <summary>
    /// Clase que contiene todas las operaciones del sistema
    /// </summary>
    public static class Operaciones
    {
        ///// <summary>
        ///// Cadena de conexión al BO
        ///// </summary>
        //public static String cadenaConexionBO;
        ///// <summary>
        ///// Cadena de conexión a una transaccional
        ///// </summary>
        //public static String cadenaConexionTrx;

        //public static String nombreLog;



        /// <summary>
        /// Enumerado con los tipos de log
        /// </summary>
        public enum TiposLog
        {
            /// <summary>
            /// Informativo
            /// </summary>
            info = 0,
            /// <summary>
            /// Alerta
            /// </summary>
            warnning = 1,
            /// <summary>
            /// Error
            /// </summary>
            error
        }

        /// <summary>
        /// Listado de cabeceras de trama para identificar el tipo de solicitud
        /// </summary>
        public enum CabecerasTrama
        {
            compraTaePx = 13,
            consultaTaePx = 17,
            compraDatosPx = 21,
            consultaDatosPx = 25,
            compraTpv = 200,
            consultaTpv = 220
        }

        public enum tipoMensajeria
        {
            PX = 0,
            TenServer = 1
        }

        public enum categoriaProducto
        {
            TAE = 0,
            Datos = 1,
            Otro=2
        }


        /// <summary>
        /// Función que graba en un log interno desde afuera de la capa de negocio
        /// </summary>
        /// <param name="mensaje"></param>
        /// <param name="tipoLog"></param>
        public static void EscribirLogInterno(string mensaje, TiposLog tipoLog)
        {
            switch (tipoLog)
            {
                case TiposLog.info:
                    log.EscribirLogEvento(mensaje);
                    break;
                case TiposLog.warnning:
                    log.EscribirLogAdvertencia(mensaje);
                    break;
                case TiposLog.error:
                    log.EscribirLogError(mensaje);
                    break;
                default:
                    log.EscribirLogEvento(mensaje);
                    break;
            }
        }

        public static string PrepararMensajeriaProveedor(string trama)
        {
            string tramaProveedor = string.Empty;
            try
            {
                if (int.TryParse(trama.Substring(0, 2), out int encabezado))
                {
                    trama = trama.Substring(2);
                    switch (encabezado)
                    {
                        case (int)CabecerasTrama.compraTaePx:
                            tramaProveedor = Compra(trama, tipoMensajeria.PX, categoriaProducto.TAE);
                            break;
                        case (int)CabecerasTrama.consultaTaePx:
                            tramaProveedor = Consulta(trama, tipoMensajeria.PX, categoriaProducto.TAE);
                            break;
                        case (int)CabecerasTrama.compraDatosPx:
                            tramaProveedor = Compra(trama, tipoMensajeria.PX, categoriaProducto.Datos);
                            break;
                        case (int)CabecerasTrama.consultaDatosPx:
                            tramaProveedor = Consulta(trama, tipoMensajeria.PX, categoriaProducto.Datos);
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    trama = trama.Substring(3);
                    if (int.TryParse(trama.Substring(0, 3), out encabezado))
                    {
                        switch (encabezado)
                        {
                            case (int)CabecerasTrama.compraTpv:
                                tramaProveedor = Compra(trama, tipoMensajeria.TenServer,categoriaProducto.Otro);
                                break;
                            case (int)CabecerasTrama.consultaTpv:
                                tramaProveedor = Consulta(trama, tipoMensajeria.TenServer, categoriaProducto.Otro);
                                break;
                            default:
                                break;
                        }
                    }
                }
                return tramaProveedor;
            }
            catch (Exception ex)
            {
                UtileriaVariablesGlobales.log.EscribirLogError("Error al identificar el tipo de mensajeria: " + ex.Message);
                return tramaProveedor;
            }
        }


        private static string Compra(string trama, tipoMensajeria tipoMensajeria, categoriaProducto categoriaProducto)
        {
            string tramaRespuesta=string.Empty;
            switch (tipoMensajeria)
            {
                case tipoMensajeria.PX:
                    switch (categoriaProducto)
                    {
                        case categoriaProducto.TAE:
                            break;
                        case categoriaProducto.Datos:
                            break;
                        default:
                            break;
                    }
                    break;
                case tipoMensajeria.TenServer:
                    break;
                default:
                    break;
            }
            
            return tramaRespuesta;
        }

        private static string Consulta(string trama, tipoMensajeria tipoMensajeria, categoriaProducto categoriaProducto)
        {
            string tramaRespuesta = string.Empty;
            switch (tipoMensajeria)
            {
                case tipoMensajeria.PX:
                    switch (categoriaProducto)
                    {
                        case categoriaProducto.TAE:
                            break;
                        case categoriaProducto.Datos:
                            break;
                        default:
                            break;
                    }
                    break;
                case tipoMensajeria.TenServer:
                    break;
                default:
                    break;
            }

            return tramaRespuesta;
        }


        //private static string CompraTaePx(string trama)
        //{
        //    //se aumenta el contador de rendimiento
        //    Task.Run(() => UtileriaVariablesGlobales.peformancePeticionesEntrantes.Increment());

        //    SolicitudPxTae solicitudPxTae = new SolicitudPxTae(trama);
        //    RespuestaSolicitudPxTae respuestaSolicitudPxTae = new RespuestaSolicitudPxTae(solicitudPxTae);

        //    //TODO variable para indicar que tiene activo el control de crédito

        //    //se comprueba el saldo disponible si es que lo tiene activo
        //    Task<float> saldoClienteTask = Task.Run(() => ObtenerSaldoCliente(solicitudPxTae.idGrupo, solicitudPxTae.idCadena, solicitudPxTae.idTienda));
        //    if (saldoClienteTask.Result == -1)
        //    {
        //        respuestaSolicitudPxTae.codigoRespuesta = (int)UtileriaVariablesGlobales.codigosRespuesta.ErrorObteniendoCredito;
        //        return respuestaSolicitudPxTae.ObtenerTrama();
        //    }
        //    else if (saldoClienteTask.Result == 0)
        //    {
        //        respuestaSolicitudPxTae.codigoRespuesta = (int)UtileriaVariablesGlobales.codigosRespuesta.SinCreditoDisponible;
        //        return respuestaSolicitudPxTae.ObtenerTrama();
        //    }

        //    //obtener los datos para conectarme a Procesa dependiendo del id de grupo
        //    Task<DatosConexion> DatosConexionTask = Task.Run(() => ObtenerIpPuertoTimeOutSiglasProcesa(solicitudPxTae.idGrupo));
        //    DatosConexionTask.Wait();
        //    if (DatosConexionTask.Result.puerto == 0)
        //    {
        //        respuestaSolicitudPxTae.codigoRespuesta = (int)UtileriaVariablesGlobales.codigosRespuesta.ErrorAccesoDB;
        //        return respuestaSolicitudPxTae.ObtenerTrama();
        //    }

        //    //Se prepara a trama para enviarla a procesa
        //    SolicitudTpv solicitudTpv = new SolicitudTpv(solicitudPxTae)
        //    {
        //        TerminalId = DatosConexionTask.Result.siglas + "TN" + Validaciones.formatoValor(solicitudPxTae.idGrupo.ToString().Substring(1), TipoFormato.N, 3) +
        //        Validaciones.formatoValor(solicitudPxTae.idCadena.ToString(), TipoFormato.N, 5) + Validaciones.formatoValor(solicitudPxTae.idTienda.ToString(), TipoFormato.N, 4) +
        //        Validaciones.formatoValor(solicitudPxTae.idPos.ToString(), TipoFormato.N, 5),
        //        merchantData = "TARJETASN      " + Validaciones.formatoValor(solicitudPxTae.idGrupo.ToString().Substring(1), TipoFormato.N, 3) +
        //        Validaciones.formatoValor(solicitudPxTae.idCadena.ToString(), TipoFormato.N, 5) + Validaciones.formatoValor(solicitudPxTae.idTienda.ToString(), TipoFormato.N, 4) +
        //        Validaciones.formatoValor(solicitudPxTae.idPos.ToString(), TipoFormato.N, 5) + "DF MX"
        //    };





        //    //TODO si la respuesta es exitosa se guarda en DBA y se descuenta de tu saldo



        //}


        //public static float ObtenerSaldoCliente(int idGrupo, int idCadena, int idTienda)
        //{
        //    float saldo = 0;
        //    try
        //    {
        //        //lista de paramatros sobre la consulta
        //        List<SqlParameter> listaParametros = new List<SqlParameter>()
        //        {
        //            new SqlParameter("@idGrupo", idGrupo),
        //            new SqlParameter("@idCadena", idCadena),
        //            new SqlParameter("@idTienda", idTienda)
        //        };

        //        AccesoBaseDatos.ResultadoBaseDatos resultadoBaseDatos = AccesoBaseDatos.OperacionesBaseDatos.EjecutaSP("sprObtenerSaldoCliente", listaParametros, UtileriaVariablesGlobales.cadenaConexionBO);


        //        // se pregunta si existió un error en base de datos
        //        if (!resultadoBaseDatos.Error)
        //        {
        //            //Se revisa si existen resultados
        //            if (resultadoBaseDatos.Datos.Tables.Count > 0 && resultadoBaseDatos.Datos.Tables[0].Rows.Count > 0)
        //            {
        //                foreach (DataRow dataRow in resultadoBaseDatos.Datos.Tables[0].Rows)
        //                {
        //                    saldo = float.Parse(dataRow["saldo"].ToString());
        //                }
        //            }
        //            else
        //            {
        //                Task.Run(() => UtileriaVariablesGlobales.log.EscribirLogAdvertencia(UtileriaVariablesGlobales.ObtenerNombreFuncion("No hay resultados con la operación")));
        //            }
        //        }
        //        else
        //        {
        //            Task.Run(() => UtileriaVariablesGlobales.log.EscribirLogAdvertencia(UtileriaVariablesGlobales.ObtenerNombreFuncion(resultadoBaseDatos.Excepcion.Message)));
        //            saldo = -1;
        //        }
        //        return saldo;
        //    }
        //    catch (Exception ex)
        //    {
        //        Task.Run(() => UtileriaVariablesGlobales.log.EscribirLogAdvertencia(UtileriaVariablesGlobales.ObtenerNombreFuncion(ex.Message)));
        //        return -1;
        //    }
        //    finally
        //    {
        //        GC.Collect();
        //    }
        //}

        public static DatosConexion ObtenerIpPuertoTimeOutSiglasProcesa(int idGrupo)
        {
            DatosConexion datosConexion = new DatosConexion();
            try
            {
                //lista de paramatros sobre la consulta
                List<SqlParameter> listaParametros = new List<SqlParameter>()
                {
                    new SqlParameter("@idGrupo", idGrupo)
                };

                AccesoBaseDatos.ResultadoBaseDatos resultadoBaseDatos = AccesoBaseDatos.OperacionesBaseDatos.EjecutaSP("sprObtenerIpPuertoToProcesa", listaParametros, UtileriaVariablesGlobales.cadenaConexionTrx);


                // se pregunta si existió un error en base de datos
                if (!resultadoBaseDatos.Error)
                {
                    //Se revisa si existen resultados
                    if (resultadoBaseDatos.Datos.Tables.Count > 0 && resultadoBaseDatos.Datos.Tables[0].Rows.Count > 0)
                    {
                        foreach (DataRow dataRow in resultadoBaseDatos.Datos.Tables[0].Rows)
                        {
                            datosConexion.ip = dataRow["ip"].ToString();
                            datosConexion.puerto = int.Parse(dataRow["puerto"].ToString());
                            datosConexion.timeOut = int.Parse(dataRow["timeOut"].ToString());
                            datosConexion.siglas = dataRow["siglas"].ToString();
                        }
                    }
                    else
                    {
                        Task.Run(() => UtileriaVariablesGlobales.log.EscribirLogAdvertencia(UtileriaVariablesGlobales.ObtenerNombreFuncion("No hay resultados con la operación")));
                    }
                }
                else
                {
                    Task.Run(() => UtileriaVariablesGlobales.log.EscribirLogAdvertencia(UtileriaVariablesGlobales.ObtenerNombreFuncion(resultadoBaseDatos.Excepcion.Message)));
                }
                return datosConexion;
            }
            catch (Exception ex)
            {
                Task.Run(() => UtileriaVariablesGlobales.log.EscribirLogAdvertencia(UtileriaVariablesGlobales.ObtenerNombreFuncion(ex.Message)));
                return datosConexion;
            }
            finally
            {
                GC.Collect();
            }
        }

    }
}
