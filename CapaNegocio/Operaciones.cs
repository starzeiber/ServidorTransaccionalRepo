using AccesoBaseDatos;
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

        //--------------------------------
        #region Cliente

        /// <summary>
        /// Funcion que recibe la trama del cliente, identifica de que tipo es y la particiona en sus propiedades
        /// </summary>
        /// <param name="trama">trama del cliente</param>
        /// <returns></returns>
        public static RespuestaProcesosCliente ProcesarMensajeriaCliente(string trama)
        {
            RespuestaProcesosCliente respuestaProcesosCliente = new RespuestaProcesosCliente();

            try
            {
                //Si la conversión de las 2 primeras posiciones son un número entonces no es una TPV, porque la TPV tiene un encabezado HEX
                if (int.TryParse(trama.Substring(0, 2), out int encabezado))
                {
                    // dependiendo del valor de cabecera es su tratamiento
                    switch (encabezado)
                    {
                        case (int)CabecerasTrama.compraTaePx:
                            respuestaProcesosCliente.cabeceraTrama = CabecerasTrama.compraTaePx;
                            ObtenerParametrosPorCompraCliente(trama, tipoMensajeria.PX, ref respuestaProcesosCliente, categoriaProducto.TAE);
                            break;
                        case (int)CabecerasTrama.consultaTaePx:
                            respuestaProcesosCliente.cabeceraTrama = CabecerasTrama.consultaTaePx;
                            ObtenerParametrosPorConsultaCliente(trama, tipoMensajeria.PX, ref respuestaProcesosCliente, categoriaProducto.TAE);
                            break;
                        case (int)CabecerasTrama.compraDatosPx:
                            respuestaProcesosCliente.cabeceraTrama = CabecerasTrama.compraDatosPx;
                            ObtenerParametrosPorCompraCliente(trama, tipoMensajeria.PX, ref respuestaProcesosCliente, categoriaProducto.Datos);
                            break;
                        case (int)CabecerasTrama.consultaDatosPx:
                            respuestaProcesosCliente.cabeceraTrama = CabecerasTrama.consultaDatosPx;
                            ObtenerParametrosPorConsultaCliente(trama, tipoMensajeria.PX, ref respuestaProcesosCliente, categoriaProducto.Datos);
                            break;
                        default:
                            respuestaProcesosCliente.codigoRespuesta = (int)Utileria.CodigosRespuesta.Denegada;
                            break;
                    }
                }
                else
                {
                    //quito el encabezado HEX porque es una trama TPV
                    trama = trama.Substring(2);
                    //si las 3 posiciones es un número, entonces es correcto el encabezado
                    if (int.TryParse(trama.Substring(0, 3), out encabezado))
                    {
                        switch (encabezado)
                        {
                            case (int)CabecerasTrama.compraTpv:
                                ObtenerParametrosPorCompraCliente(trama, tipoMensajeria.TenServer, ref respuestaProcesosCliente);
                                break;
                            case (int)CabecerasTrama.consultaTpv:
                                ObtenerParametrosPorConsultaCliente(trama, tipoMensajeria.TenServer, ref respuestaProcesosCliente);
                                break;
                            default:
                                respuestaProcesosCliente.codigoRespuesta = (int)Utileria.CodigosRespuesta.Denegada;
                                break;
                        }
                    }
                    else
                    {
                        respuestaProcesosCliente.codigoRespuesta = (int)Utileria.CodigosRespuesta.ErrorFormato;
                    }
                }
                return respuestaProcesosCliente;
            }
            catch (Exception ex)
            {
                Utileria.Log(Utileria.ObtenerNombreFuncion("Error al identificar el tipo de mensajeria: " + ex.Message), Utileria.TiposLog.error);
                respuestaProcesosCliente.codigoRespuesta = (int)Utileria.CodigosRespuesta.ErrorProceso;
                return respuestaProcesosCliente;
            }
        }

        /// <summary>
        /// Función que validará la mensajería que sea acorde a una compra dependiendo de su formato y petición
        /// </summary>
        /// <param name="trama">trama del cliente</param>
        /// <param name="tipoMensajeria">tipo de mensajería</param>
        /// <param name="respuestaGenerica">respuesta sobre el proceso</param>
        /// <param name="categoriaProducto">categoría del producto a comprar</param>
        private static void ObtenerParametrosPorCompraCliente(string trama, tipoMensajeria tipoMensajeria, ref RespuestaProcesosCliente respuestaGenerica, categoriaProducto categoriaProducto = categoriaProducto.TAE)
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
                                respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.ErrorFormato;
                                return;
                            }

                            RespuestaCompraPxTae respuestaCompraPxTae = new RespuestaCompraPxTae();

                            if (respuestaCompraPxTae.Ingresar(compraPxTae) != true)
                            {
                                respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.ErrorFormato;
                                return;
                            }
                            respuestaGenerica.objPeticionCliente = compraPxTae;
                            respuestaGenerica.objRespuestaCliente = respuestaCompraPxTae;

                            if ((bool)ValidarGrupoCadenaTienda(compraPxTae.idGrupo, compraPxTae.idCadena, compraPxTae.idTienda).objetoAux != true)
                            {
                                respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.TerminalInvalida;
                                return;
                            }

                            RespuestaProcesosCliente respuestaGenericatmp = ObtenerInfoProducto(compraPxTae.sku);
                            if (respuestaGenericatmp.codigoRespuesta != 0)
                            {
                                respuestaGenerica.codigoRespuesta = respuestaGenericatmp.codigoRespuesta;
                                return;
                            }
                            else
                            {
                                compraPxTae.productoInfo = respuestaGenericatmp.objetoAux as ProductoInfo;
                            }

                            respuestaGenericatmp = ObtenerInfoProveedor(compraPxTae.productoInfo.idProveedor);
                            if (respuestaGenericatmp.codigoRespuesta != 0)
                            {
                                respuestaGenerica.codigoRespuesta = respuestaGenericatmp.codigoRespuesta;
                                return;
                            }
                            else
                            {
                                compraPxTae.proveedorInfo = respuestaGenericatmp.objetoAux as ProveedorInfo;
                            }

                            respuestaCompraPxTae.Actualizar(compraPxTae);

                            break;
                        case categoriaProducto.Datos:
                            CompraPxDatos compraPxDatos = new CompraPxDatos();

                            if (compraPxDatos.Ingresar(trama) != true)
                            {
                                respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.ErrorFormato;
                                return;
                            }

                            RespuestaCompraPxDatos respuestaCompraPxDatos = new RespuestaCompraPxDatos();
                            if (respuestaCompraPxDatos.Ingresar(compraPxDatos) != true)
                            {
                                respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.ErrorFormato;
                                return;
                            }

                            respuestaGenerica.objPeticionCliente = compraPxDatos;
                            respuestaGenerica.objRespuestaCliente = respuestaCompraPxDatos;

                            if ((bool)ValidarGrupoCadenaTienda(compraPxDatos.idGrupo, compraPxDatos.idCadena, compraPxDatos.idTienda).objetoAux != true)
                            {
                                respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.TerminalInvalida;
                                return;
                            }

                            respuestaGenericatmp = ObtenerInfoProducto(compraPxDatos.sku);
                            if (respuestaGenerica.codigoRespuesta != 0)
                            {
                                respuestaGenerica.codigoRespuesta = respuestaGenericatmp.codigoRespuesta;
                                return;
                            }
                            else
                            {
                                compraPxDatos.productoInfo = respuestaGenericatmp.objetoAux as ProductoInfo;
                            }

                            respuestaGenericatmp = ObtenerInfoProveedor(compraPxDatos.productoInfo.idProveedor);
                            if (respuestaGenericatmp.codigoRespuesta != 0)
                            {
                                respuestaGenerica.codigoRespuesta = respuestaGenericatmp.codigoRespuesta;
                                return;
                            }
                            else
                            {
                                compraPxDatos.proveedorInfo = respuestaGenericatmp.objetoAux as ProveedorInfo;
                            }

                            respuestaCompraPxDatos.Actualizar(compraPxDatos);
                            break;
                        default:
                            respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.ErrorFormato;
                            break;
                    }
                    break;
                case tipoMensajeria.TenServer:

                    break;
                default:
                    respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.ErrorFormato;
                    break;
            }
        }

        private static void ObtenerParametrosPorConsultaCliente(string trama, tipoMensajeria tipoMensajeria, ref RespuestaProcesosCliente respuestaGenerica, categoriaProducto categoriaProducto = categoriaProducto.TAE)
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
                                respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.ErrorFormato;
                                return;
                            }

                            RespuestaConsultaPxTae respuestaConsultaPxTae = new RespuestaConsultaPxTae();
                            if (respuestaConsultaPxTae.Ingresar(consultaPxTae) != true)
                            {
                                respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.ErrorFormato;
                                return;
                            }

                            respuestaGenerica.objPeticionCliente = consultaPxTae;
                            respuestaGenerica.objRespuestaCliente = respuestaConsultaPxTae;

                            if ((bool)ValidarGrupoCadenaTienda(consultaPxTae.idGrupo, consultaPxTae.idCadena, consultaPxTae.idTienda).objetoAux != true)
                            {
                                respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.TerminalInvalida;
                                return;
                            }
                            RespuestaProcesosCliente respuestaGenericaTmp = ObtenerInfoProducto(consultaPxTae.sku);
                            if (respuestaGenericaTmp.codigoRespuesta != 0)
                            {
                                respuestaGenerica.codigoRespuesta = respuestaGenericaTmp.codigoRespuesta;
                                return;
                            }
                            else
                            {
                                consultaPxTae.productoInfo = respuestaGenerica.objetoAux as ProductoInfo;
                            }

                            respuestaGenericaTmp = ObtenerInfoProveedor(consultaPxTae.productoInfo.idProveedor);
                            if (respuestaGenericaTmp.codigoRespuesta != 0)
                            {
                                respuestaGenerica.codigoRespuesta = respuestaGenericaTmp.codigoRespuesta;
                                return;
                            }
                            else
                            {
                                consultaPxTae.proveedorInfo = respuestaGenericaTmp.objetoAux as ProveedorInfo;
                            }

                            respuestaConsultaPxTae.Actualizar(consultaPxTae);
                            break;
                        case categoriaProducto.Datos:
                            ConsultaPxDatos consultaPxDatos = new ConsultaPxDatos();

                            if (consultaPxDatos.Ingresar(trama) != true)
                            {
                                respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.ErrorFormato;
                                return;
                            }

                            RespuestaConsultaPxDatos respuestaConsultaPxDatos = new RespuestaConsultaPxDatos();
                            if (respuestaConsultaPxDatos.Ingresar(consultaPxDatos) != true)
                            {
                                respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.ErrorFormato;
                                return;
                            }

                            respuestaGenerica.objPeticionCliente = consultaPxDatos;
                            respuestaGenerica.objRespuestaCliente = respuestaConsultaPxDatos;

                            if ((bool)ValidarGrupoCadenaTienda(consultaPxDatos.idGrupo, consultaPxDatos.idCadena, consultaPxDatos.idTienda).objetoAux != true)
                            {
                                respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.TerminalInvalida;
                                return;
                            }
                            respuestaGenericaTmp = ObtenerInfoProducto(consultaPxDatos.sku);
                            if (respuestaGenericaTmp.codigoRespuesta != 0)
                            {
                                respuestaGenerica.codigoRespuesta = respuestaGenericaTmp.codigoRespuesta;
                                return;
                            }
                            else
                            {
                                consultaPxDatos.productoInfo = respuestaGenericaTmp.objetoAux as ProductoInfo;
                            }

                            respuestaGenericaTmp = ObtenerInfoProveedor(consultaPxDatos.productoInfo.idProveedor);
                            if (respuestaGenericaTmp.codigoRespuesta != 0)
                            {
                                respuestaGenerica.codigoRespuesta = respuestaGenericaTmp.codigoRespuesta;
                                return;
                            }
                            else
                            {
                                consultaPxDatos.proveedorInfo = respuestaGenericaTmp.objetoAux as ProveedorInfo;
                            }

                            respuestaConsultaPxDatos.Actualizar(consultaPxDatos);
                            break;
                        default:
                            respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.ErrorFormato;
                            break;
                    }
                    break;
                case tipoMensajeria.TenServer:

                    break;
                default:
                    respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.ErrorFormato;
                    break;
            }
        }

        #endregion

        //--------------------------------
        #region Proveedor
        
        public static RespuestaProcesosProveedor ProcesarMensajeriaProveedor(object objPeticionCliente)
        {
            RespuestaProcesosProveedor respuestaProcesosProveedor = new RespuestaProcesosProveedor();

            try
            {
                Type tipo = objPeticionCliente.GetType();               
                if (tipo==typeof(CompraPxTae))
                {
                    ObtenerParametrosPorCompraProveedor((objPeticionCliente as CompraPxTae), ref respuestaProcesosProveedor);
                }else if (tipo == typeof(CompraPxDatos))
                {
                    ObtenerParametrosPorCompraProveedor((objPeticionCliente as CompraPxDatos), ref respuestaProcesosProveedor);
                }

                return respuestaProcesosProveedor;
            }
            catch (Exception ex)
            {
                Utileria.Log(Utileria.ObtenerNombreFuncion("Error al identificar el tipo de mensajeria: " + ex.Message), Utileria.TiposLog.error);
                respuestaProcesosProveedor.codigoRespuesta = (int)Utileria.CodigosRespuesta.ErrorProceso;
                return respuestaProcesosProveedor;
            }
        }

        public static void ProcesarTramaProveedor(string trama, ref RespuestaProcesosProveedor respuestaProcesosProveedor)
        {           
            RespuestaCompraTpvTAE respuestaCompraTpvTAE = new RespuestaCompraTpvTAE();
            if (respuestaCompraTpvTAE.Ingresar(trama))
            {
                respuestaProcesosProveedor.codigoRespuesta = respuestaCompraTpvTAE.codigoRespuesta;
                respuestaProcesosProveedor.objRespuestaProveedor = respuestaCompraTpvTAE;
            }
           
        }


        public static void ObtenerParametrosPorCompraProveedor(CompraPxTae compraPxTae, ref RespuestaProcesosProveedor respuestaProcesosProveedor)
        {     
            CompraTpvTae compraTpvTae = new CompraTpvTae();
            if (compraTpvTae.Ingresar(compraPxTae) != true)
            {
                respuestaProcesosProveedor.codigoRespuesta = (int)Utileria.CodigosRespuesta.ErrorProceso;
                return;
            }

            RespuestaCompraTpvTAE respuestaCompraTpvTAE = new RespuestaCompraTpvTAE();

            if (respuestaCompraTpvTAE.Ingresar(compraTpvTae) != true)
            {
                respuestaProcesosProveedor.codigoRespuesta = (int)Utileria.CodigosRespuesta.ErrorProceso;
                return;
            }
            respuestaProcesosProveedor.objPeticionProveedor = compraTpvTae;
            respuestaProcesosProveedor.objRespuestaProveedor = respuestaCompraTpvTAE;
        }

        public static void ObtenerParametrosPorCompraProveedor(CompraPxDatos compraPxDatos, ref RespuestaProcesosProveedor respuestaProcesosProveedor)
        {
            CompraTpvDatos compraTpvDatos = new CompraTpvDatos();
            if (compraTpvDatos.Ingresar(compraPxDatos) != true)
            {
                respuestaProcesosProveedor.codigoRespuesta = (int)Utileria.CodigosRespuesta.ErrorProceso;
                return;
            }

            RespuestaCompraTpvDatos respuestaCompraTpvDatos = new RespuestaCompraTpvDatos();

            if (respuestaCompraTpvDatos.Ingresar(compraTpvDatos) != true)
            {
                respuestaProcesosProveedor.codigoRespuesta = (int)Utileria.CodigosRespuesta.ErrorProceso;
                return;
            }
            respuestaProcesosProveedor.objPeticionProveedor = compraTpvDatos;
            respuestaProcesosProveedor.objRespuestaProveedor = respuestaCompraTpvDatos;
        }

        #endregion


        //-------------------------------
        #region Control del crédito

        public static RespuestaProcesosCliente ValidarControlCredito(int idGrupo, int idCadena, int idTienda)
        {
            RespuestaProcesosCliente respuestaGenerica = new RespuestaProcesosCliente();
            try
            {
                //lista de paramatros sobre la consulta
                List<SqlParameter> listaParametros = new List<SqlParameter>()
                {
                    new SqlParameter("@idGrupo", idGrupo),
                    new SqlParameter("@idCadena", idCadena),
                    new SqlParameter("@idTienda", idTienda)
                };

                ResultadoBaseDatos resultadoBaseDatos = OperacionesBaseDatos.EjecutaSP("sprValidarControlCredito", listaParametros, Utileria.cadenaConexionTrx);


                // se pregunta si existió un error en base de datos
                if (!resultadoBaseDatos.Error)
                {
                    //Se revisa si existen resultados
                    if (resultadoBaseDatos.Datos.Tables.Count > 0 && resultadoBaseDatos.Datos.Tables[0].Rows.Count > 0)
                    {
                        foreach (DataRow cadaResultado in resultadoBaseDatos.Datos.Tables[0].Rows)
                        {
                            if ((int)cadaResultado["controlCredito"] == 1)
                            {
                                respuestaGenerica.objetoAux = true;
                            }
                        }
                    }
                    else
                    {
                        Task.Run(() => Utileria.Log(Utileria.ObtenerNombreFuncion("No hay resultados con la operación"), Utileria.TiposLog.warnning));
                        respuestaGenerica.objetoAux = false;
                        respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.ErrorAccesoDB;
                    }
                }
                else
                {
                    Task.Run(() => Utileria.Log(Utileria.ObtenerNombreFuncion(resultadoBaseDatos.Excepcion.Message), Utileria.TiposLog.error));
                    respuestaGenerica.objetoAux = false;
                    respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.ErrorAccesoDB;
                }
                return respuestaGenerica;
            }
            catch (Exception ex)
            {
                Task.Run(() => Utileria.Log(Utileria.ObtenerNombreFuncion(ex.Message), Utileria.TiposLog.error));
                respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.ErrorAccesoDB;
                return respuestaGenerica;
            }
            finally
            {
                GC.Collect();
            }
        }

        public static RespuestaProcesosCliente ObtenerSaldoDisponible(int idGrupo, int idCadena, int idTienda)
        {
            RespuestaProcesosCliente respuestaGenerica = new RespuestaProcesosCliente();
            try
            {
                //lista de paramatros sobre la consulta
                List<SqlParameter> listaParametros = new List<SqlParameter>()
                {
                    new SqlParameter("@Grupo", idGrupo),
                    new SqlParameter("@Cadena", idCadena),
                    new SqlParameter("@Tienda", idTienda)
                };

                ResultadoBaseDatos resultadoBaseDatos = OperacionesBaseDatos.EjecutaSP("spCreditoDisponible", listaParametros, Utileria.cadenaConexionBO);


                // se pregunta si existió un error en base de datos
                if (!resultadoBaseDatos.Error)
                {
                    //Se revisa si existen resultados
                    if (resultadoBaseDatos.Datos.Tables.Count > 0 && resultadoBaseDatos.Datos.Tables[0].Rows.Count > 0)
                    {
                        foreach (DataRow cadaResultado in resultadoBaseDatos.Datos.Tables[0].Rows)
                        {
                            respuestaGenerica.objetoAux = decimal.Parse(cadaResultado["CREDIT_TA_AV"].ToString());
                        }
                    }
                    else
                    {
                        Task.Run(() => Utileria.Log(Utileria.ObtenerNombreFuncion("No hay resultados con la operación"), Utileria.TiposLog.warnning));
                        respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.ErrorObteniendoCredito;
                    }
                }
                else
                {
                    Task.Run(() => Utileria.Log(Utileria.ObtenerNombreFuncion(resultadoBaseDatos.Excepcion.Message), Utileria.TiposLog.error));
                    respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.ErrorObteniendoCredito;
                }
                return respuestaGenerica;
            }
            catch (Exception ex)
            {
                Task.Run(() => Utileria.Log(Utileria.ObtenerNombreFuncion(ex.Message), Utileria.TiposLog.error));
                respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.ErrorObteniendoCredito;
                return respuestaGenerica;
            }
            finally
            {
                GC.Collect();
            }
        }

        #endregion

        //--------------------------------
        #region Funciones auxiliares

        /// <summary>
        /// Función que realiza la validación de los datos del punto de venta que realiza una compra
        /// </summary>
        /// <param name="idGrupo">identificador del grupo</param>
        /// <param name="idCadena">identificador de la cadena</param>
        /// <param name="idTienda">identificador de la tienda</param>
        /// <returns></returns>
        private static RespuestaProcesosCliente ValidarGrupoCadenaTienda(int idGrupo, int idCadena, int idTienda)
        {
            RespuestaProcesosCliente respuestaGenerica = new RespuestaProcesosCliente();
            try
            {
                //lista de paramatros sobre la consulta
                List<SqlParameter> listaParametros = new List<SqlParameter>()
                {
                    new SqlParameter("@idGrupo", idGrupo),
                    new SqlParameter("@idCadena", idCadena),
                    new SqlParameter("@idTienda", idTienda)
                };

                ResultadoBaseDatos resultadoBaseDatos = OperacionesBaseDatos.EjecutaSP("sprValidarGrupoCadenaTienda", listaParametros, Utileria.cadenaConexionTrx);


                // se pregunta si existió un error en base de datos
                if (!resultadoBaseDatos.Error)
                {
                    //Se revisa si existen resultados
                    if (resultadoBaseDatos.Datos.Tables.Count > 0 && resultadoBaseDatos.Datos.Tables[0].Rows.Count > 0)
                    {
                        respuestaGenerica.objetoAux = true;
                    }
                    else
                    {
                        Task.Run(() => Utileria.Log(Utileria.ObtenerNombreFuncion("No hay resultados con la operación"), Utileria.TiposLog.warnning));
                        respuestaGenerica.objetoAux = false;
                        respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.ErrorAccesoDB;
                    }
                }
                else
                {
                    Task.Run(() => Utileria.Log(Utileria.ObtenerNombreFuncion(resultadoBaseDatos.Excepcion.Message), Utileria.TiposLog.error));
                    respuestaGenerica.objetoAux = false;
                    respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.ErrorAccesoDB;
                }
                return respuestaGenerica;
            }
            catch (Exception ex)
            {
                Task.Run(() => Utileria.Log(Utileria.ObtenerNombreFuncion(ex.Message), Utileria.TiposLog.error));
                respuestaGenerica.objetoAux = false;
                respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.ErrorAccesoDB;
                return respuestaGenerica;
            }
            finally
            {
                GC.Collect();
            }
        }

        /// <summary>
        /// Función que obtiene toda la información del producto que se solicita comprar
        /// </summary>
        /// <param name="sku">SKU único del producto</param>
        /// <returns></returns>
        private static RespuestaProcesosCliente ObtenerInfoProducto(string sku)
        {
            RespuestaProcesosCliente respuestaGenerica = new RespuestaProcesosCliente();

            try
            {
                //lista de paramatros sobre la consulta
                List<SqlParameter> listaParametros = new List<SqlParameter>()
                {
                    new SqlParameter("@sku", sku.Trim())
                };

                ResultadoBaseDatos resultadoBaseDatos = OperacionesBaseDatos.EjecutaSP("sprObtenerInfoProducto", listaParametros, Utileria.cadenaConexionTrx);


                // se pregunta si existió un error en base de datos
                if (!resultadoBaseDatos.Error)
                {
                    //Se revisa si existen resultados
                    if (resultadoBaseDatos.Datos.Tables.Count > 0 && resultadoBaseDatos.Datos.Tables[0].Rows.Count > 0)
                    {
                        foreach (DataRow cadaResultado in resultadoBaseDatos.Datos.Tables[0].Rows)
                        {
                            ProductoInfo productoInfo = new ProductoInfo()
                            {
                                sku = sku,
                                idProveedor = int.Parse(cadaResultado["idProveedor"].ToString()),
                                nombreProveedor = cadaResultado["nombreProveedor"].ToString(),
                                monto = decimal.Parse(cadaResultado["monto"].ToString()),
                                mensajeTicket1 = cadaResultado["mensajeTicket1"].ToString(),
                                mensajeTicket2 = cadaResultado["mensajeTicket2"].ToString()
                            };
                            respuestaGenerica.objetoAux = productoInfo;
                        }
                    }
                    else
                    {
                        Task.Run(() => Utileria.Log(Utileria.ObtenerNombreFuncion("No hay resultados con la operación"), Utileria.TiposLog.warnning));
                        respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.Denegada;
                    }
                }
                else
                {
                    Task.Run(() => Utileria.Log(Utileria.ObtenerNombreFuncion(resultadoBaseDatos.Excepcion.Message), Utileria.TiposLog.error));
                    respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.ErrorAccesoDB;
                }
                return respuestaGenerica;
            }
            catch (Exception ex)
            {
                Task.Run(() => Utileria.Log(Utileria.ObtenerNombreFuncion(ex.Message), Utileria.TiposLog.error));
                respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.ErrorAccesoDB;
                return respuestaGenerica;
            }
            finally
            {
                GC.Collect();
            }
        }

        /// <summary>
        /// Función que obtiene la información de un proveedor
        /// </summary>
        /// <param name="idProveedor">identificador de un proveedor</param>
        /// <returns></returns>
        private static RespuestaProcesosCliente ObtenerInfoProveedor(int idProveedor)
        {
            RespuestaProcesosCliente respuestaGenerica = new RespuestaProcesosCliente();

            try
            {
                //lista de paramatros sobre la consulta
                List<SqlParameter> listaParametros = new List<SqlParameter>()
                {
                    new SqlParameter("@idProveedor", idProveedor)
                };

                ResultadoBaseDatos resultadoBaseDatos = OperacionesBaseDatos.EjecutaSP("sprObtenerInfoProveedor", listaParametros, Utileria.cadenaConexionTrx);


                // se pregunta si existió un error en base de datos
                if (!resultadoBaseDatos.Error)
                {
                    //Se revisa si existen resultados
                    if (resultadoBaseDatos.Datos.Tables.Count > 0 && resultadoBaseDatos.Datos.Tables[0].Rows.Count > 0)
                    {
                        foreach (DataRow cadaResultado in resultadoBaseDatos.Datos.Tables[0].Rows)
                        {
                            ProveedorInfo proveedorInfo = new ProveedorInfo()
                            {
                                idProveedor = idProveedor,
                                nombreProveedor = cadaResultado["nombreProveedor"].ToString(),
                                marca = cadaResultado["marca"].ToString(),
                                issuer = cadaResultado["issuer"].ToString(),
                                idMaster = int.Parse(cadaResultado["idMaster"].ToString())
                            };
                            respuestaGenerica.objetoAux = proveedorInfo;
                        }
                    }
                    else
                    {
                        Task.Run(() => Utileria.Log(Utileria.ObtenerNombreFuncion("No hay resultados con la operación"), Utileria.TiposLog.warnning));
                        respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.ErrorAccesoDB;
                    }
                }
                else
                {
                    Task.Run(() => Utileria.Log(Utileria.ObtenerNombreFuncion(resultadoBaseDatos.Excepcion.Message), Utileria.TiposLog.error));
                    respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.ErrorAccesoDB;
                }
                return respuestaGenerica;
            }
            catch (Exception ex)
            {
                Task.Run(() => Utileria.Log(Utileria.ObtenerNombreFuncion(ex.Message), Utileria.TiposLog.error));
                respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.ErrorAccesoDB;
                return respuestaGenerica;
            }
            finally
            {
                GC.Collect();
            }
        }

        public static RespuestaProcesosCliente GuardarTrx(RespuestaCompraTpvTAE respuestaCompraTpvTAE)
        {
            RespuestaProcesosCliente respuestaGenerica = new RespuestaProcesosCliente();
            
            try
            {
                //lista de paramatros sobre la consulta
                List<SqlParameter> listaParametros = new List<SqlParameter>()
                {
                    //new SqlParameter("@", DateTime.Now),
                    //new SqlParameter("@", respuestaCompraTpvTAE.fechaCapturaTerminal),
                    //new SqlParameter("@", respuestaCompraTpvTAE.horaTerminal),
                    //new SqlParameter("@encabezado", respuestaCompraTpvTAE.encabezado),
                    //new SqlParameter("@pCode", respuestaCompraTpvTAE.pCode),
                    //new SqlParameter("@issuer", respuestaCompraTpvTAE.issuer),
                    //new SqlParameter("@sku", sku),
                    //new SqlParameter("@folio", 0),
                    //new SqlParameter("@numeroReferencia", respuestaCompraTpvTAE.referencia),
                    //new SqlParameter("@telefono", respuestaCompraTpvTAE.telefono),
                    //new SqlParameter("@monto", respuestaCompraTpvTAE.monto),
                    //new SqlParameter("@idGrupo", respuestaCompraTpvTAE.TerminalId),
                    //new SqlParameter("@idCadena", ),
                    //new SqlParameter("@idTienda", ),
                    //new SqlParameter("@idProveedor", ),
                    //new SqlParameter("@idMaster", ),
                    //new SqlParameter("@numTransaccion", ),
                    //new SqlParameter("@autorizacion", ),
                    //new SqlParameter("@codigoRespuesta", ),
                    //new SqlParameter("@saldoActual", ),
                    //new SqlParameter("@mensaje200", ),
                    //new SqlParameter("@mensaje210", ),
                    //new SqlParameter("@mensaje220", ),
                    //new SqlParameter("@mensaje230", ),
                    //new SqlParameter("@tipoTrx", ),
                };

                ResultadoBaseDatos resultadoBaseDatos = OperacionesBaseDatos.EjecutaSP("sprInsertarVenta", listaParametros, Utileria.cadenaConexionTrx);


                // se pregunta si existió un error en base de datos
                if (!resultadoBaseDatos.Error)
                {
                    //Se revisa si existen resultados
                    if (resultadoBaseDatos.Datos.Tables.Count > 0 && resultadoBaseDatos.Datos.Tables[0].Rows.Count > 0)
                    {
                        
                    }
                    else
                    {
                        Task.Run(() => Utileria.Log(Utileria.ObtenerNombreFuncion("No hay resultados con la operación"), Utileria.TiposLog.warnning));
                        respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.NoExisteOriginal;
                    }
                }
                else
                {
                    Task.Run(() => Utileria.Log(Utileria.ObtenerNombreFuncion(resultadoBaseDatos.Excepcion.Message), Utileria.TiposLog.error));
                    respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.ErrorAccesoDB;
                }
                return respuestaGenerica;
            }
            catch (Exception ex)
            {
                Task.Run(() => Utileria.Log(Utileria.ObtenerNombreFuncion(ex.Message), Utileria.TiposLog.error));
                respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.ErrorAccesoDB;
                return respuestaGenerica;
            }
            finally
            {
                GC.Collect();
            }
        }

        #endregion


    }
}
