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
            /// <summary>
            /// Compra PX tae
            /// </summary>
            compraTaePx = 13,
            /// <summary>
            /// Consulta PX tae
            /// </summary>
            consultaTaePx = 17,
            /// <summary>
            /// Compra px datos
            /// </summary>
            compraDatosPx = 25,
            /// <summary>
            /// consulta px datos
            /// </summary>
            consultaDatosPx = 27,
            /// <summary>
            /// compra protocoloco tpv
            /// </summary>
            compraTpv = 200,
            /// <summary>
            /// Consulta protocolo tpv
            /// </summary>
            consultaTpv = 220
        }

        /// <summary>
        /// Tipo de mensajería que podrá procesar el servidor
        /// </summary>
        public enum tipoMensajeria
        {
            /// <summary>
            /// protocolo PX
            /// </summary>
            PX = 0,
            /// <summary>
            /// Protocolo tenserver
            /// </summary>
            TenServer = 1
        }

        /// <summary>
        /// Categorías de productos que manejará el servidor
        /// </summary>
        public enum CategoriaProducto
        {
            /// <summary>
            /// Tiempo aire electrónico
            /// </summary>
            TAE = 0,
            /// <summary>
            /// Paquetes de tiempo aire electrónico
            /// </summary>
            Datos = 1
        }

        internal enum tipoTransaccion
        {
            /// <summary>
            /// Tiempo aire electrónico
            /// </summary>
            TAE = 1,
            /// <summary>
            /// Paquetes de tiempo aire electrónico
            /// </summary>
            Datos = 8
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
                            //respuestaProcesosCliente.cabeceraTrama = CabecerasTrama.compraTaePx;
                            ObtenerParametrosPorCompraCliente(trama, tipoMensajeria.PX, ref respuestaProcesosCliente, CategoriaProducto.TAE);
                            break;
                        case (int)CabecerasTrama.consultaTaePx:
                            //respuestaProcesosCliente.cabeceraTrama = CabecerasTrama.consultaTaePx;
                            ObtenerParametrosPorConsultaCliente(trama, tipoMensajeria.PX, ref respuestaProcesosCliente, CategoriaProducto.TAE);
                            break;
                        case (int)CabecerasTrama.compraDatosPx:
                            //respuestaProcesosCliente.cabeceraTrama = CabecerasTrama.compraDatosPx;
                            ObtenerParametrosPorCompraCliente(trama, tipoMensajeria.PX, ref respuestaProcesosCliente, CategoriaProducto.Datos);
                            break;
                        case (int)CabecerasTrama.consultaDatosPx:
                            //respuestaProcesosCliente.cabeceraTrama = CabecerasTrama.consultaDatosPx;
                            ObtenerParametrosPorConsultaCliente(trama, tipoMensajeria.PX, ref respuestaProcesosCliente, CategoriaProducto.Datos);
                            break;
                        default:
                            respuestaProcesosCliente.codigoRespuesta = (int)Utileria.CodigosRespuesta.ErrorFormato;
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
        private static void ObtenerParametrosPorCompraCliente(string trama, tipoMensajeria tipoMensajeria, ref RespuestaProcesosCliente respuestaGenerica, CategoriaProducto categoriaProducto = CategoriaProducto.TAE)
        {
            switch (tipoMensajeria)
            {
                case tipoMensajeria.PX:
                    switch (categoriaProducto)
                    {
                        case CategoriaProducto.TAE:
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

                            RespuestaProcesosCliente respuestaInfoProductoProveedor = ObtenerInfoProducto(compraPxTae.sku);
                            if (respuestaInfoProductoProveedor.codigoRespuesta != 0)
                            {
                                respuestaGenerica.codigoRespuesta = respuestaInfoProductoProveedor.codigoRespuesta;
                                return;
                            }
                            else
                            {
                                compraPxTae.productoInfo = respuestaInfoProductoProveedor.objetoAux as ProductoInfo;
                            }

                            respuestaInfoProductoProveedor = ObtenerInfoProveedor(compraPxTae.productoInfo.idProveedor);
                            if (respuestaInfoProductoProveedor.codigoRespuesta != 0)
                            {
                                respuestaGenerica.codigoRespuesta = respuestaInfoProductoProveedor.codigoRespuesta;
                                return;
                            }
                            else
                            {
                                compraPxTae.proveedorInfo = respuestaInfoProductoProveedor.objetoAux as ProveedorInfo;
                            }

                            respuestaCompraPxTae.Actualizar(compraPxTae);

                            break;
                        case CategoriaProducto.Datos:
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

                            respuestaInfoProductoProveedor = ObtenerInfoProducto(compraPxDatos.sku);
                            if (respuestaInfoProductoProveedor.codigoRespuesta != 0)
                            {
                                respuestaGenerica.codigoRespuesta = respuestaInfoProductoProveedor.codigoRespuesta;
                                return;
                            }
                            else
                            {
                                compraPxDatos.productoInfo = respuestaInfoProductoProveedor.objetoAux as ProductoInfo;                                
                            }

                            respuestaInfoProductoProveedor = ObtenerInfoProveedor(compraPxDatos.productoInfo.idProveedor);
                            if (respuestaInfoProductoProveedor.codigoRespuesta != 0)
                            {
                                respuestaGenerica.codigoRespuesta = respuestaInfoProductoProveedor.codigoRespuesta;
                                return;
                            }
                            else
                            {
                                compraPxDatos.proveedorInfo = respuestaInfoProductoProveedor.objetoAux as ProveedorInfo;
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

        /// <summary>
        /// Función que divide en sus propiedades la trama de consulta de transacción recibida del cliente
        /// </summary>
        /// <param name="trama">Trama del cliente</param>
        /// <param name="tipoMensajeria">tipo de mensajería de la consulta</param>
        /// <param name="respuestaGenerica">instancia de RespuestaGenerica</param>
        /// <param name="categoriaProducto">categoría del producto</param>
        private static void ObtenerParametrosPorConsultaCliente(string trama, tipoMensajeria tipoMensajeria, ref RespuestaProcesosCliente respuestaGenerica, CategoriaProducto categoriaProducto = CategoriaProducto.TAE)
        {

            switch (tipoMensajeria)
            {
                case tipoMensajeria.PX:
                    switch (categoriaProducto)
                    {
                        case CategoriaProducto.TAE:
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
                                consultaPxTae.productoInfo = respuestaGenericaTmp.objetoAux as ProductoInfo;
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
                        case CategoriaProducto.Datos:
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

        /// <summary>
        /// Función que procesará la mensajería del proveedor contenida en el objeto genérico
        /// </summary>
        /// <param name="objPeticionCliente">Objeto genérico que recibe cualquier clase del sistema</param>
        /// <returns></returns>
        public static RespuestaProcesosProveedor ProcesarMensajeriaProveedor(object objPeticionCliente)
        {
            RespuestaProcesosProveedor respuestaProcesosProveedor = new RespuestaProcesosProveedor();

            try
            {
                Type tipo = objPeticionCliente.GetType();
                if (tipo == typeof(CompraPxTae))
                {
                    ObtenerParametrosPorCompraProveedor((objPeticionCliente as CompraPxTae), ref respuestaProcesosProveedor);
                    respuestaProcesosProveedor.categoriaProducto = CategoriaProducto.TAE;
                }
                else if (tipo == typeof(ConsultaPxTae))
                {
                    ObtenerParametrosPorCompraProveedor((objPeticionCliente as ConsultaPxTae), ref respuestaProcesosProveedor);
                    respuestaProcesosProveedor.categoriaProducto = CategoriaProducto.TAE;
                }
                else if (tipo == typeof(CompraPxDatos))
                {
                    ObtenerParametrosPorCompraProveedor((objPeticionCliente as CompraPxDatos), ref respuestaProcesosProveedor);
                    respuestaProcesosProveedor.categoriaProducto = CategoriaProducto.Datos;
                }
                else if (tipo == typeof(ConsultaPxDatos))
                {
                    //TODO consulta de datos
                    //ObtenerParametrosPorCompraProveedor((objPeticionCliente as ConsultaPxDatos), ref respuestaProcesosProveedor);
                    respuestaProcesosProveedor.categoriaProducto = CategoriaProducto.Datos;
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

        /// <summary>
        /// Función que divide en sus propiedades la trama de respuesta de un proveedor
        /// </summary>
        /// <param name="trama">Trama de un proveedor</param>
        /// <param name="respuestaProcesosProveedor">Instancia de RespuestaProcesosProveedor</param>
        public static void ProcesarTramaProveedor(string trama, ref RespuestaProcesosProveedor respuestaProcesosProveedor)
        {
            RespuestaCompraTpvTAE respuestaCompraTpvTAE = new RespuestaCompraTpvTAE();
            RespuestaCompraTpvDatos respuestaCompraTpvDatos = new RespuestaCompraTpvDatos();
            switch (respuestaProcesosProveedor.categoriaProducto)
            {
                case CategoriaProducto.TAE:
                    
                    if (respuestaCompraTpvTAE.Ingresar(trama.Substring(2)))
                    {
                        respuestaProcesosProveedor.codigoRespuesta = respuestaCompraTpvTAE.codigoRespuesta;
                        respuestaProcesosProveedor.objRespuestaProveedor = respuestaCompraTpvTAE;
                    }
                    break;
                case CategoriaProducto.Datos:                    
                    if (respuestaCompraTpvDatos.Ingresar(trama.Substring(2)))
                    {
                        respuestaProcesosProveedor.codigoRespuesta = respuestaCompraTpvDatos.codigoRespuesta;
                        respuestaProcesosProveedor.objRespuestaProveedor = respuestaCompraTpvDatos;
                    }
                    break;
                default:
                    if (respuestaCompraTpvTAE.Ingresar(trama.Substring(2)))
                    {
                        respuestaProcesosProveedor.codigoRespuesta = respuestaCompraTpvTAE.codigoRespuesta;
                        respuestaProcesosProveedor.objRespuestaProveedor = respuestaCompraTpvTAE;
                    }
                    break;
            }
        }

        /// <summary>
        /// Función que obtiene las propiedades de una solicitud de compra a un proveedor a partir de la clase de compra del cliente
        /// </summary>
        /// <param name="compraPxTae"></param>
        /// <param name="respuestaProcesosProveedor"></param>
        private static void ObtenerParametrosPorCompraProveedor(CompraPxTae compraPxTae, ref RespuestaProcesosProveedor respuestaProcesosProveedor)
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

        /// <summary>
        /// Función que obtiene las propiedades de una solicitud de compra a un proveedor a partir de la clase de compra del cliente
        /// </summary>
        /// <param name="consultaPxTae"></param>
        /// <param name="respuestaProcesosProveedor"></param>
        private static void ObtenerParametrosPorCompraProveedor(ConsultaPxTae consultaPxTae, ref RespuestaProcesosProveedor respuestaProcesosProveedor)
        {
            CompraTpvTae compraTpvTae = new CompraTpvTae();
            if (compraTpvTae.Ingresar(consultaPxTae) != true)
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

        /// <summary>
        /// Función que obtiene las propiedades de una solicitud de consulta a un proveedor a partir de la clase de compra del cliente
        /// </summary>
        /// <param name="compraPxDatos"></param>
        /// <param name="respuestaProcesosProveedor"></param>
        private static void ObtenerParametrosPorCompraProveedor(CompraPxDatos compraPxDatos, ref RespuestaProcesosProveedor respuestaProcesosProveedor)
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
        /// <summary>
        /// Función que valida el tipo de control de crédito de una petición de compra
        /// </summary>
        /// <param name="idGrupo"></param>
        /// <param name="idCadena"></param>
        /// <param name="idTienda"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Funcion que obtendrá el saldo disponible en una petición de compra
        /// </summary>
        /// <param name="idGrupo"></param>
        /// <param name="idCadena"></param>
        /// <param name="idTienda"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Guarda en la base cualquier transacción de compra
        /// </summary>
        /// <param name="objPeticionCliente">objeto de petición del cliente</param>
        /// <param name="objRespuestaTpv">Objeto de respuesta de la mensajería TPV</param>
        /// <returns></returns>
        public static RespuestaProcesosProveedor GuardarTrx(object objPeticionCliente, object objRespuestaTpv)
        {
            if (objPeticionCliente is null)
            {
                Task.Run(() => Utileria.Log(Utileria.ObtenerNombreFuncion("El objeto de peticion TPV no está instanciado"), Utileria.TiposLog.error));
                return null;
            }
            if (objRespuestaTpv is null)
            {
                Task.Run(() => Utileria.Log(Utileria.ObtenerNombreFuncion("El objeto de respuesta TPV no está instanciado"), Utileria.TiposLog.error));
                return null;
            }

            RespuestaProcesosProveedor respuestaProcesosProveedor = new RespuestaProcesosProveedor();

            try
            {
                Type t = objPeticionCliente.GetType();
                List<SqlParameter> listaParametros;
                if (t == typeof(CompraTpvTae))
                {
                    CompraTpvTae compraTpvTae = objPeticionCliente as CompraTpvTae;
                    RespuestaCompraTpvTAE respuestaCompraTpvTAE = objRespuestaTpv as RespuestaCompraTpvTAE;
                    listaParametros = new List<SqlParameter>()
                {
                    new SqlParameter("@fechaHora", compraTpvTae.fechaHoraCompleta),
                    new SqlParameter("@fecha", respuestaCompraTpvTAE.fechaCapturaTerminal),
                    new SqlParameter("@hora", respuestaCompraTpvTAE.horaTerminal),
                    new SqlParameter("@encabezado", respuestaCompraTpvTAE.encabezado),
                    new SqlParameter("@pCode", respuestaCompraTpvTAE.pCode),
                    new SqlParameter("@issuer", respuestaCompraTpvTAE.issuer),
                    new SqlParameter("@sku", compraTpvTae.sku.Trim()),
                    new SqlParameter("@folio", "NA"),
                    new SqlParameter("@numeroReferencia", respuestaCompraTpvTAE.referencia),
                    new SqlParameter("@telefono", respuestaCompraTpvTAE.telefono),
                    new SqlParameter("@monto", respuestaCompraTpvTAE.monto),
                    new SqlParameter("@idGrupo", respuestaCompraTpvTAE.TerminalId.Substring(4,3)),
                    new SqlParameter("@idCadena", respuestaCompraTpvTAE.TerminalId.Substring(7,5)),
                    new SqlParameter("@idTienda", respuestaCompraTpvTAE.TerminalId.Substring(12,4)),
                    new SqlParameter("@idProveedor", compraTpvTae.idProveedor),
                    new SqlParameter("@idMaster", compraTpvTae.idMaster),
                    new SqlParameter("@numTransaccion", respuestaCompraTpvTAE.systemTrace),
                    new SqlParameter("@autorizacion", respuestaCompraTpvTAE.autorizacion),
                    new SqlParameter("@codigoRespuesta", respuestaCompraTpvTAE.codigoRespuesta),
                    new SqlParameter("@saldoActual", compraTpvTae.saldoActual),
                    new SqlParameter("@mensaje200", compraTpvTae.Obtener().Trim()),
                    new SqlParameter("@mensaje210", respuestaCompraTpvTAE.Obtener().Trim()),
                    new SqlParameter("@mensaje220", "NA"),
                    new SqlParameter("@mensaje230", "NA"),
                    new SqlParameter("@tipoTrx", (int) tipoTransaccion.TAE)
                };
                }
                else
                {
                    CompraTpvDatos compraTpvDatos = objPeticionCliente as CompraTpvDatos;
                    RespuestaCompraTpvDatos respuestaCompraTpvDatos = objRespuestaTpv as RespuestaCompraTpvDatos;
                    //lista de paramatros sobre la consulta
                    listaParametros = new List<SqlParameter>()
                {
                    new SqlParameter("@fechaHora", compraTpvDatos.fechaHoraCompleta),
                    new SqlParameter("@fecha", respuestaCompraTpvDatos.fechaCapturaTerminal),
                    new SqlParameter("@hora", respuestaCompraTpvDatos.horaTerminal),
                    new SqlParameter("@encabezado", respuestaCompraTpvDatos.encabezado),
                    new SqlParameter("@pCode", respuestaCompraTpvDatos.pCode),
                    new SqlParameter("@issuer", respuestaCompraTpvDatos.issuer),
                    new SqlParameter("@sku", compraTpvDatos.sku.Trim()),
                    new SqlParameter("@numeroReferencia", respuestaCompraTpvDatos.referencia),
                    new SqlParameter("@telefono", respuestaCompraTpvDatos.telefono),
                    new SqlParameter("@monto", respuestaCompraTpvDatos.monto),
                    new SqlParameter("@idGrupo", respuestaCompraTpvDatos.TerminalId.Substring(4,3)),
                    new SqlParameter("@idCadena", respuestaCompraTpvDatos.TerminalId.Substring(7,5)),
                    new SqlParameter("@idTienda", respuestaCompraTpvDatos.TerminalId.Substring(12,4)),
                    new SqlParameter("@idProveedor", compraTpvDatos.idProveedor),
                    new SqlParameter("@idMaster", compraTpvDatos.idMaster),
                    new SqlParameter("@numTransaccion", respuestaCompraTpvDatos.systemTrace),
                    new SqlParameter("@autorizacion", respuestaCompraTpvDatos.autorizacion),
                    new SqlParameter("@codigoRespuesta", respuestaCompraTpvDatos.codigoRespuesta),
                    new SqlParameter("@saldoActual", compraTpvDatos.saldoActual),
                    new SqlParameter("@mensaje200", compraTpvDatos.Obtener().Trim()),
                    new SqlParameter("@mensaje210", respuestaCompraTpvDatos.Obtener().Trim()),
                    new SqlParameter("@mensaje220", "NA"),
                    new SqlParameter("@mensaje230", "NA"),
                    new SqlParameter("@folio", compraTpvDatos.idPaquete),
                    new SqlParameter("@tipoTrx", (int) tipoTransaccion.Datos)

                };
                }


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
                        respuestaProcesosProveedor.codigoRespuesta = (int)Utileria.CodigosRespuesta.ErrorAccesoDB;
                    }
                }
                else
                {
                    Task.Run(() => Utileria.Log(Utileria.ObtenerNombreFuncion(resultadoBaseDatos.Excepcion.Message), Utileria.TiposLog.error));
                    respuestaProcesosProveedor.codigoRespuesta = (int)Utileria.CodigosRespuesta.ErrorAccesoDB;
                }
                return respuestaProcesosProveedor;
            }
            catch (Exception ex)
            {
                Task.Run(() => Utileria.Log(Utileria.ObtenerNombreFuncion(ex.Message), Utileria.TiposLog.error));
                respuestaProcesosProveedor.codigoRespuesta = (int)Utileria.CodigosRespuesta.ErrorAccesoDB;
                return respuestaProcesosProveedor;
            }
            finally
            {
                GC.Collect();
            }
        }

        /// <summary>
        /// Consulta el estado de la transacción en la base de datos
        /// </summary>
        /// <param name="objPeticionCliente">objeto de petición del cliente<</param>
        /// <returns></returns>
        public static RespuestaProcesosCliente ConsultaTrxBaseTransaccional(object objPeticionCliente)
        {
            RespuestaProcesosCliente respuestaGenerica = new RespuestaProcesosCliente();

            try
            {
                //lista de paramatros sobre la consulta
                List<SqlParameter> listaParametros = new List<SqlParameter>();

                Type t = objPeticionCliente.GetType();

                if (t == typeof(ConsultaPxTae))
                {
                    ConsultaPxTae consultaPxTae = objPeticionCliente as ConsultaPxTae;
                    listaParametros.Add(new SqlParameter("@idGrupo", consultaPxTae.idGrupo));
                    listaParametros.Add(new SqlParameter("@idCadena", consultaPxTae.idCadena));
                    listaParametros.Add(new SqlParameter("@idTienda", consultaPxTae.idTienda));
                    listaParametros.Add(new SqlParameter("@telefono", consultaPxTae.telefono));
                    listaParametros.Add(new SqlParameter("@fecha", consultaPxTae.fecha.Substring(2)));
                    listaParametros.Add(new SqlParameter("@hora", consultaPxTae.hora));
                    listaParametros.Add(new SqlParameter("@numeroTransaccion", consultaPxTae.numeroTransaccion));
                    listaParametros.Add(new SqlParameter("@sku", consultaPxTae.productoInfo.sku));
                }
                else if (t == typeof(ConsultaPxDatos))
                {
                    ConsultaPxDatos consultaPxDatos = objPeticionCliente as ConsultaPxDatos;
                    listaParametros.Add(new SqlParameter("@idGrupo", consultaPxDatos.idGrupo));
                    listaParametros.Add(new SqlParameter("@idCadena", consultaPxDatos.idCadena));
                    listaParametros.Add(new SqlParameter("@idTienda", consultaPxDatos.idTienda));
                    listaParametros.Add(new SqlParameter("@telefono", consultaPxDatos.telefono));
                    listaParametros.Add(new SqlParameter("@fecha", consultaPxDatos.fecha.Substring(2)));
                    listaParametros.Add(new SqlParameter("@hora", consultaPxDatos.hora));
                    listaParametros.Add(new SqlParameter("@numeroTransaccion", consultaPxDatos.numeroTransaccion));
                    listaParametros.Add(new SqlParameter("@sku", consultaPxDatos.productoInfo.sku));
                }



                ResultadoBaseDatos resultadoBaseDatos = OperacionesBaseDatos.EjecutaSP("sprObtenerInfoTrx", listaParametros, Utileria.cadenaConexionTrx);


                // se pregunta si existió un error en base de datos
                if (!resultadoBaseDatos.Error)
                {
                    //Se revisa si existen resultados
                    if (resultadoBaseDatos.Datos.Tables.Count > 0 && resultadoBaseDatos.Datos.Tables[0].Rows.Count > 0)
                    {
                        foreach (DataRow cadaResultado in resultadoBaseDatos.Datos.Tables[0].Rows)
                        {
                            if (t == typeof(ConsultaPxTae))
                            {
                                RespuestaConsultaPxTae respuestaConsultaPxTae = new RespuestaConsultaPxTae()
                                {
                                    codigoRespuesta = int.Parse(cadaResultado["codigoRespuesta"].ToString()),
                                    autorizacion = int.Parse(cadaResultado["autorizacion"].ToString())
                                };
                                respuestaGenerica.objetoAux = respuestaConsultaPxTae.autorizacion;
                            }
                            else if (t == typeof(ConsultaPxDatos))
                            {
                                RespuestaConsultaPxDatos respuestaConsultaPxDatos = new RespuestaConsultaPxDatos()
                                {
                                    codigoRespuesta = int.Parse(cadaResultado["codigoRespuesta"].ToString()),
                                    autorizacion = int.Parse(cadaResultado["autorizacion"].ToString())
                                };
                                respuestaGenerica.objetoAux = respuestaConsultaPxDatos.autorizacion;
                            }
                            return respuestaGenerica;
                        }
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
