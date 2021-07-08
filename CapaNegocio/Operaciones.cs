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
            TPV=2
        }

        #region Entrada

        public static RespuestaGenerica ProcesarMensajeria(string trama)
        {
            RespuestaGenerica respuestaGenerica = new RespuestaGenerica();

            try
            {
                //Si la conversión de las 2 primeras posiciones son un número entonces no es una TPV
                if (int.TryParse(trama.Substring(0, 2), out int encabezado))
                {
                    switch (encabezado)
                    {
                        case (int)CabecerasTrama.compraTaePx:
                            respuestaGenerica.cabecerasTrama = CabecerasTrama.compraTaePx;
                            respuestaGenerica = Compra(trama, tipoMensajeria.PX, categoriaProducto.TAE);
                            break;
                        case (int)CabecerasTrama.consultaTaePx:
                            respuestaGenerica.cabecerasTrama = CabecerasTrama.consultaTaePx;
                            respuestaGenerica = Consulta(trama, tipoMensajeria.PX, categoriaProducto.TAE);
                            break;
                        case (int)CabecerasTrama.compraDatosPx:
                            respuestaGenerica.cabecerasTrama = CabecerasTrama.compraDatosPx;
                            respuestaGenerica = Compra(trama, tipoMensajeria.PX, categoriaProducto.Datos);
                            break;
                        case (int)CabecerasTrama.consultaDatosPx:
                            respuestaGenerica.cabecerasTrama = CabecerasTrama.consultaDatosPx;
                            respuestaGenerica = Consulta(trama, tipoMensajeria.PX, categoriaProducto.Datos);
                            break;
                        default:
                            respuestaGenerica.codigoRespuesta = (int)UtileriaVariablesGlobales.codigosRespuesta.Denegada;
                            break;
                    }
                }
                else
                {
                    //quito el encabezado HEX porque es una trama TPV
                    trama = trama.Substring(2);
                    //si las 3 posiciones es un número, entonces es correcta la trama
                    if (int.TryParse(trama.Substring(0, 3), out encabezado))
                    {

                        switch (encabezado)
                        {
                            case (int)CabecerasTrama.compraTpv:
                                respuestaGenerica = Compra(trama, tipoMensajeria.TenServer, categoriaProducto.TPV);
                                break;
                            case (int)CabecerasTrama.consultaTpv:
                                respuestaGenerica = Consulta(trama, tipoMensajeria.TenServer, categoriaProducto.TPV);
                                break;
                            default:
                                respuestaGenerica.codigoRespuesta = (int)UtileriaVariablesGlobales.codigosRespuesta.Denegada;
                                break;
                        }
                    }
                    else
                    {
                        //Trama no reconocida
                        respuestaGenerica.codigoRespuesta= (int)UtileriaVariablesGlobales.codigosRespuesta.ErrorFormato;
                    }
                }
                return respuestaGenerica;
            }
            catch (Exception ex)
            {
                UtileriaVariablesGlobales.Log(UtileriaVariablesGlobales.ObtenerNombreFuncion("Error al identificar el tipo de mensajeria: " + ex.Message),UtileriaVariablesGlobales.TiposLog.error);
                respuestaGenerica.codigoRespuesta = (int)UtileriaVariablesGlobales.codigosRespuesta.ErrorProceso;
                return respuestaGenerica;
            }
        }

        private static RespuestaGenerica Compra(string trama, tipoMensajeria tipoMensajeria, categoriaProducto categoriaProducto = categoriaProducto.TPV)
        {
            RespuestaGenerica respuestaGenerica = new RespuestaGenerica();

            switch (tipoMensajeria)
            {
                case tipoMensajeria.PX:
                    switch (categoriaProducto)
                    {
                        case categoriaProducto.TAE:
                            // se obtienen los campos de la trama
                            respuestaGenerica.solicitudPxTae = new SolicitudPxTae();
                            if (respuestaGenerica.solicitudPxTae.DividirTrama(trama) != true)
                            {
                                respuestaGenerica.codigoRespuesta = (int)UtileriaVariablesGlobales.codigosRespuesta.ErrorFormato;
                                return respuestaGenerica;
                            }

                            respuestaGenerica.respuestaSolicitudPxTae = new RespuestaSolicitudPxTae(respuestaGenerica.solicitudPxTae);

                            //TODO todas la validaciones de la trama

                            respuestaGenerica.trama = respuestaGenerica.respuestaSolicitudPxTae.ObtenerTrama();


                            break;
                        case categoriaProducto.Datos:
                            respuestaGenerica.solicitudPxDatos = new SolicitudPxDatos();

                            if (respuestaGenerica.solicitudPxDatos.DividirTrama(trama) != true)
                            {
                                respuestaGenerica.codigoRespuesta = (int)UtileriaVariablesGlobales.codigosRespuesta.ErrorFormato;
                                return respuestaGenerica;
                            }

                            respuestaGenerica.respuestaSolicitudPxDatos = new RespuestaSolicitudPxDatos(respuestaGenerica.solicitudPxDatos);

                            //TODO todas la validaciones de la trama

                            respuestaGenerica.trama = respuestaGenerica.respuestaSolicitudPxDatos.ObtenerTrama();

                            break;
                        default:
                            respuestaGenerica.codigoRespuesta = (int)UtileriaVariablesGlobales.codigosRespuesta.ErrorFormato;
                            break;
                    }
                    break;
                case tipoMensajeria.TenServer:

                    break;
                default:
                    respuestaGenerica.codigoRespuesta = (int)UtileriaVariablesGlobales.codigosRespuesta.ErrorFormato;
                    break;
            }

            return respuestaGenerica;
        }

        private static RespuestaGenerica Consulta(string trama, tipoMensajeria tipoMensajeria, categoriaProducto categoriaProducto)
        {
            RespuestaGenerica respuestaGenerica = new RespuestaGenerica();

            switch (tipoMensajeria)
            {
                case tipoMensajeria.PX:
                    switch (categoriaProducto)
                    {
                        case categoriaProducto.TAE:
                            // se obtienen los campos de la trama
                            respuestaGenerica.consultaPxTae = new ConsultaPxTae();
                            if (respuestaGenerica.consultaPxTae.DividirTrama(trama) != true)
                            {
                                respuestaGenerica.codigoRespuesta = (int)UtileriaVariablesGlobales.codigosRespuesta.ErrorFormato;
                                return respuestaGenerica;
                            }

                            respuestaGenerica.respuestaConsultaPxTae = new RespuestaConsultaPxTae(respuestaGenerica.consultaPxTae);

                            //TODO todas la validaciones de la trama

                            respuestaGenerica.trama = respuestaGenerica.respuestaConsultaPxTae.ObtenerTrama();


                            break;
                        case categoriaProducto.Datos:
                            respuestaGenerica.consultaPxDatos = new ConsultaPxDatos();

                            if (respuestaGenerica.consultaPxDatos.DividirTrama(trama) != true)
                            {
                                respuestaGenerica.codigoRespuesta = (int)UtileriaVariablesGlobales.codigosRespuesta.ErrorFormato;
                                return respuestaGenerica;
                            }

                            respuestaGenerica.respuestaConsultaPxDatos = new RespuestaConsultaPxDatos(respuestaGenerica.consultaPxDatos);

                            //TODO todas la validaciones de la trama

                            respuestaGenerica.trama = respuestaGenerica.respuestaConsultaPxDatos.ObtenerTrama();

                            break;
                        default:
                            respuestaGenerica.codigoRespuesta = (int)UtileriaVariablesGlobales.codigosRespuesta.ErrorFormato;
                            break;
                    }
                    break;
                case tipoMensajeria.TenServer:

                    break;
                default:
                    respuestaGenerica.codigoRespuesta = (int)UtileriaVariablesGlobales.codigosRespuesta.ErrorFormato;
                    break;
            }

            return respuestaGenerica;
        }

        #endregion



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
                        Task.Run(() => UtileriaVariablesGlobales.Log(UtileriaVariablesGlobales.ObtenerNombreFuncion("No hay resultados con la operación"),UtileriaVariablesGlobales.TiposLog.warnning));
                    }
                }
                else
                {
                    Task.Run(() => UtileriaVariablesGlobales.Log(UtileriaVariablesGlobales.ObtenerNombreFuncion(resultadoBaseDatos.Excepcion.Message), UtileriaVariablesGlobales.TiposLog.error));
                }
                return datosConexion;
            }
            catch (Exception ex)
            {
                Task.Run(() => UtileriaVariablesGlobales.Log(UtileriaVariablesGlobales.ObtenerNombreFuncion(ex.Message), UtileriaVariablesGlobales.TiposLog.error));
                return datosConexion;
            }
            finally
            {
                GC.Collect();
            }
        }

    }
}
