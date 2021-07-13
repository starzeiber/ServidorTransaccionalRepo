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
            Datos = 1            
        }


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
                            Compra(trama, tipoMensajeria.PX, ref respuestaGenerica, categoriaProducto.TAE);
                            break;
                        case (int)CabecerasTrama.consultaTaePx:
                            respuestaGenerica.cabecerasTrama = CabecerasTrama.consultaTaePx;
                            Consulta(trama, tipoMensajeria.PX, ref respuestaGenerica, categoriaProducto.TAE);
                            break;
                        case (int)CabecerasTrama.compraDatosPx:
                            respuestaGenerica.cabecerasTrama = CabecerasTrama.compraDatosPx;
                            Compra(trama, tipoMensajeria.PX, ref respuestaGenerica,categoriaProducto.Datos);
                            break;
                        case (int)CabecerasTrama.consultaDatosPx:
                            respuestaGenerica.cabecerasTrama = CabecerasTrama.consultaDatosPx;
                            Consulta(trama, tipoMensajeria.PX, ref respuestaGenerica ,categoriaProducto.Datos);
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
                                Compra(trama, tipoMensajeria.TenServer, ref respuestaGenerica);
                                break;
                            case (int)CabecerasTrama.consultaTpv:
                                Consulta(trama, tipoMensajeria.TenServer, ref respuestaGenerica);
                                break;
                            default:
                                respuestaGenerica.codigoRespuesta = (int)UtileriaVariablesGlobales.codigosRespuesta.Denegada;
                                break;
                        }
                    }
                    else
                    {
                        //Trama no reconocida
                        respuestaGenerica.codigoRespuesta = (int)UtileriaVariablesGlobales.codigosRespuesta.ErrorFormato;
                    }
                }
                return respuestaGenerica;
            }
            catch (Exception ex)
            {
                UtileriaVariablesGlobales.Log(UtileriaVariablesGlobales.ObtenerNombreFuncion("Error al identificar el tipo de mensajeria: " + ex.Message), UtileriaVariablesGlobales.TiposLog.error);
                respuestaGenerica.codigoRespuesta = (int)UtileriaVariablesGlobales.codigosRespuesta.ErrorProceso;
                return respuestaGenerica;
            }
        }

        #region Cliente

        private static void Compra(string trama, tipoMensajeria tipoMensajeria, ref RespuestaGenerica respuestaGenerica, categoriaProducto categoriaProducto=categoriaProducto.TAE)
        {           

            switch (tipoMensajeria)
            {
                case tipoMensajeria.PX:
                    switch (categoriaProducto)
                    {
                        case categoriaProducto.TAE:
                            // se obtienen los campos de la trama
                            CompraPxTae compraPxTae = new CompraPxTae();
                            if (compraPxTae.Ingresar(trama) != true)
                            {
                                respuestaGenerica.codigoRespuesta = (int)UtileriaVariablesGlobales.codigosRespuesta.ErrorFormato;
                                return;
                            }

                            RespuestaCompraPxTae respuestaCompraPxTae = new RespuestaCompraPxTae();

                            respuestaCompraPxTae.Ingresar(compraPxTae);

                            //TODO todas la validaciones de la trama

                            respuestaGenerica.trama = respuestaCompraPxTae.ObtenerTrama();
                            respuestaGenerica.objPeticionCliente = compraPxTae;
                            respuestaGenerica.objRespuestaCliente = respuestaCompraPxTae;

                            break;
                        case categoriaProducto.Datos:
                            CompraPxDatos compraPxDatos = new CompraPxDatos();

                            if (compraPxDatos.Ingresar(trama) != true)
                            {
                                respuestaGenerica.codigoRespuesta = (int)UtileriaVariablesGlobales.codigosRespuesta.ErrorFormato;
                                return;
                            }

                            RespuestaCompraPxDatos respuestaCompraPxDatos = new RespuestaCompraPxDatos();

                            //TODO todas la validaciones de la trama

                            respuestaGenerica.trama = respuestaCompraPxDatos.ObtenerTrama();
                            respuestaGenerica.objPeticionCliente = compraPxDatos;
                            respuestaGenerica.objRespuestaCliente = respuestaCompraPxDatos;

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
        }

        private static void Consulta(string trama, tipoMensajeria tipoMensajeria, ref RespuestaGenerica respuestaGenerica, categoriaProducto categoriaProducto=categoriaProducto.TAE)
        {           

            switch (tipoMensajeria)
            {
                case tipoMensajeria.PX:
                    switch (categoriaProducto)
                    {
                        case categoriaProducto.TAE:
                            // se obtienen los campos de la trama
                            ConsultaPxTae consultaPxTae = new ConsultaPxTae();
                            if (consultaPxTae.Ingresar(trama) != true)
                            {
                                respuestaGenerica.codigoRespuesta = (int)UtileriaVariablesGlobales.codigosRespuesta.ErrorFormato;
                                return;
                            }

                            RespuestaConsultaPxTae respuestaConsultaPxTae = new RespuestaConsultaPxTae();
                            respuestaConsultaPxTae.Ingresar(consultaPxTae);

                            //TODO todas la validaciones de la trama

                            respuestaGenerica.trama = respuestaConsultaPxTae.ObtenerTrama();
                            respuestaGenerica.objPeticionCliente = consultaPxTae;
                            respuestaGenerica.objRespuestaCliente = respuestaConsultaPxTae;

                            break;
                        case categoriaProducto.Datos:
                            ConsultaPxDatos consultaPxDatos = new ConsultaPxDatos();

                            if (consultaPxDatos.Ingresar(trama) != true)
                            {
                                respuestaGenerica.codigoRespuesta = (int)UtileriaVariablesGlobales.codigosRespuesta.ErrorFormato;
                                return;
                            }

                            RespuestaConsultaPxDatos respuestaConsultaPxDatos = new RespuestaConsultaPxDatos();
                            respuestaConsultaPxDatos.Ingresar(consultaPxDatos);
                            //TODO todas la validaciones de la trama

                            respuestaGenerica.trama = respuestaConsultaPxDatos.ObtenerTrama();
                            respuestaGenerica.objPeticionCliente = consultaPxDatos;
                            respuestaGenerica.objRespuestaCliente = respuestaConsultaPxDatos;

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
        }

        #endregion


        #region Proveedor

        public static RespuestaGenerica CompraTpv(CompraPxTae compraPxTae)
        {
            RespuestaGenerica respuestaGenerica = new RespuestaGenerica();

            CompraTpvTae compraTpvTae = new CompraTpvTae();
            compraTpvTae.Ingresar(compraPxTae);

            RespuestaCompraTpvTAE respuestaCompraTpvTAE = new RespuestaCompraTpvTAE();

            //TODO validaciones

            respuestaGenerica.objPeticionCliente = compraTpvTae;
            respuestaGenerica.objRespuestaCliente = respuestaCompraTpvTAE;

            return respuestaGenerica;
        }

        #endregion

        //public static DatosConexion ObtenerIpPuertoTimeOutSiglasProcesa(int idGrupo)
        //{
        //    DatosConexion datosConexion = new DatosConexion();
        //    try
        //    {
        //        //lista de paramatros sobre la consulta
        //        List<SqlParameter> listaParametros = new List<SqlParameter>()
        //        {
        //            new SqlParameter("@idGrupo", idGrupo)
        //        };

        //        AccesoBaseDatos.ResultadoBaseDatos resultadoBaseDatos = AccesoBaseDatos.OperacionesBaseDatos.EjecutaSP("sprObtenerIpPuertoToProcesa", listaParametros, UtileriaVariablesGlobales.cadenaConexionTrx);


        //        // se pregunta si existió un error en base de datos
        //        if (!resultadoBaseDatos.Error)
        //        {
        //            //Se revisa si existen resultados
        //            if (resultadoBaseDatos.Datos.Tables.Count > 0 && resultadoBaseDatos.Datos.Tables[0].Rows.Count > 0)
        //            {
        //                foreach (DataRow dataRow in resultadoBaseDatos.Datos.Tables[0].Rows)
        //                {
        //                    datosConexion.ip = dataRow["ip"].ToString();
        //                    datosConexion.puerto = int.Parse(dataRow["puerto"].ToString());
        //                    datosConexion.timeOut = int.Parse(dataRow["timeOut"].ToString());
        //                    datosConexion.siglas = dataRow["siglas"].ToString();
        //                }
        //            }
        //            else
        //            {
        //                Task.Run(() => UtileriaVariablesGlobales.Log(UtileriaVariablesGlobales.ObtenerNombreFuncion("No hay resultados con la operación"),UtileriaVariablesGlobales.TiposLog.warnning));
        //            }
        //        }
        //        else
        //        {
        //            Task.Run(() => UtileriaVariablesGlobales.Log(UtileriaVariablesGlobales.ObtenerNombreFuncion(resultadoBaseDatos.Excepcion.Message), UtileriaVariablesGlobales.TiposLog.error));
        //        }
        //        return datosConexion;
        //    }
        //    catch (Exception ex)
        //    {
        //        Task.Run(() => UtileriaVariablesGlobales.Log(UtileriaVariablesGlobales.ObtenerNombreFuncion(ex.Message), UtileriaVariablesGlobales.TiposLog.error));
        //        return datosConexion;
        //    }
        //    finally
        //    {
        //        GC.Collect();
        //    }
        //}

    }
}
