﻿using AccesoBaseDatos;
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

        /// <summary>
        /// Funcion que recibe la trama del cliente, identifica de que tipo es y la particiona en sus propiedades
        /// </summary>
        /// <param name="trama">trama del cliente</param>
        /// <returns></returns>
        public static RespuestaGenerica ProcesarMensajeria(string trama)
        {
            RespuestaGenerica respuestaGenerica = new RespuestaGenerica();

            try
            {

                //Si la conversión de las 2 primeras posiciones son un número entonces no es una TPV, porque la TPV tiene un encabezado HEX
                if (int.TryParse(trama.Substring(0, 2), out int encabezado))
                {
                    // dependiendo del valor de cabecera es su tratamiento
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
                            Compra(trama, tipoMensajeria.PX, ref respuestaGenerica, categoriaProducto.Datos);
                            break;
                        case (int)CabecerasTrama.consultaDatosPx:
                            respuestaGenerica.cabecerasTrama = CabecerasTrama.consultaDatosPx;
                            Consulta(trama, tipoMensajeria.PX, ref respuestaGenerica, categoriaProducto.Datos);
                            break;
                        default:
                            respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.Denegada;
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
                                Compra(trama, tipoMensajeria.TenServer, ref respuestaGenerica);
                                break;
                            case (int)CabecerasTrama.consultaTpv:
                                Consulta(trama, tipoMensajeria.TenServer, ref respuestaGenerica);
                                break;
                            default:
                                respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.Denegada;
                                break;
                        }
                    }
                    else
                    {
                        respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.ErrorFormato;
                    }
                }
                return respuestaGenerica;
            }
            catch (Exception ex)
            {
                Utileria.Log(Utileria.ObtenerNombreFuncion("Error al identificar el tipo de mensajeria: " + ex.Message), Utileria.TiposLog.error);
                respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.ErrorProceso;
                return respuestaGenerica;
            }
        }

        #region Cliente

        /// <summary>
        /// Función que validará la mensajería que sea acorde a una compra dependiendo de su formato y petición
        /// </summary>
        /// <param name="trama">trama del cliente</param>
        /// <param name="tipoMensajeria">tipo de mensajería</param>
        /// <param name="respuestaGenerica">respuesta sobre el proceso</param>
        /// <param name="categoriaProducto">categoría del producto a comprar</param>
        private static void Compra(string trama, tipoMensajeria tipoMensajeria, ref RespuestaGenerica respuestaGenerica, categoriaProducto categoriaProducto = categoriaProducto.TAE)
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

                            if (ValidarGrupoCadenaTienda(compraPxTae.idGrupo, compraPxTae.idCadena, compraPxTae.idTienda) != true)
                            {
                                respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.TerminalInvalida;
                                return;
                            }

                            compraPxTae.productoInfo = ObtenerInfoProducto(compraPxTae.sku);
                            if (compraPxTae.productoInfo is null)
                            {
                                respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.Denegada;
                                return;
                            }

                            compraPxTae.proveedorInfo = ObtenerInfoProveedor(compraPxTae.productoInfo.idProveedor);
                            if (compraPxTae.proveedorInfo is null)
                            {
                                respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.Denegada;
                                return;
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

                            if (ValidarGrupoCadenaTienda(compraPxDatos.idGrupo, compraPxDatos.idCadena, compraPxDatos.idTienda) != true)
                            {
                                respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.TerminalInvalida;
                                return;
                            }

                            compraPxDatos.productoInfo = ObtenerInfoProducto(compraPxDatos.sku);
                            if (compraPxDatos.productoInfo.idProveedor == 0)
                            {
                                respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.Denegada;
                                return;
                            }

                            compraPxDatos.proveedorInfo = ObtenerInfoProveedor(compraPxDatos.productoInfo.idProveedor);
                            if (compraPxDatos.proveedorInfo is null)
                            {
                                respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.Denegada;
                                return;
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

        private static void Consulta(string trama, tipoMensajeria tipoMensajeria, ref RespuestaGenerica respuestaGenerica, categoriaProducto categoriaProducto = categoriaProducto.TAE)
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

                            if (ValidarGrupoCadenaTienda(consultaPxTae.idGrupo, consultaPxTae.idCadena, consultaPxTae.idTienda) != true)
                            {
                                respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.TerminalInvalida;
                                return;
                            }

                            consultaPxTae.productoInfo = ObtenerInfoProducto(consultaPxTae.sku);
                            if (consultaPxTae.productoInfo is null)
                            {
                                respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.Denegada;
                                return;
                            }

                            consultaPxTae.proveedorInfo = ObtenerInfoProveedor(consultaPxTae.productoInfo.idProveedor);
                            if (consultaPxTae.proveedorInfo is null)
                            {
                                respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.Denegada;
                                return;
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


                            if (ValidarGrupoCadenaTienda(consultaPxDatos.idGrupo, consultaPxDatos.idCadena, consultaPxDatos.idTienda) != true)
                            {
                                respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.TerminalInvalida;
                                return;
                            }

                            consultaPxDatos.productoInfo = ObtenerInfoProducto(consultaPxDatos.sku);
                            if (consultaPxDatos.productoInfo.idProveedor == 0)
                            {
                                respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.Denegada;
                            }

                            consultaPxDatos.proveedorInfo = ObtenerInfoProveedor(consultaPxDatos.productoInfo.idProveedor);
                            if (consultaPxDatos.proveedorInfo is null)
                            {
                                respuestaGenerica.codigoRespuesta = (int)Utileria.CodigosRespuesta.Denegada;
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


        #region Proveedor

        public static RespuestaGenerica CompraTpv(CompraPxTae compraPxTae)
        {
            RespuestaGenerica respuestaGenerica = new RespuestaGenerica();

            CompraTpvTae compraTpvTae = new CompraTpvTae();
            compraTpvTae.Ingresar(compraPxTae);

            RespuestaCompraTpvTAE respuestaCompraTpvTAE = new RespuestaCompraTpvTAE();

            //TODO validaciones

            respuestaGenerica.objPeticionProveedor = compraTpvTae;
            respuestaGenerica.objRespuestaProveedor = respuestaCompraTpvTAE;

            return respuestaGenerica;
        }

        #endregion

        /// <summary>
        /// Función que realiza la validación de los datos del punto de venta que realiza una compra
        /// </summary>
        /// <param name="idGrupo">identificador del grupo</param>
        /// <param name="idCadena">identificador de la cadena</param>
        /// <param name="idTienda">identificador de la tienda</param>
        /// <returns></returns>
        private static bool ValidarGrupoCadenaTienda(int idGrupo, int idCadena, int idTienda)
        {
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
                        return true;
                    }
                    else
                    {
                        Task.Run(() => Utileria.Log(Utileria.ObtenerNombreFuncion("No hay resultados con la operación"), Utileria.TiposLog.warnning));
                        return false;
                    }
                }
                else
                {
                    Task.Run(() => Utileria.Log(Utileria.ObtenerNombreFuncion(resultadoBaseDatos.Excepcion.Message), Utileria.TiposLog.error));
                    return false;
                }
            }
            catch (Exception ex)
            {
                Task.Run(() => Utileria.Log(Utileria.ObtenerNombreFuncion(ex.Message), Utileria.TiposLog.error));
                return false;
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
        private static ProductoInfo ObtenerInfoProducto(string sku)
        {
            ProductoInfo productoInfo = new ProductoInfo();
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
                            productoInfo.sku = sku;
                            productoInfo.idProveedor = int.Parse(cadaResultado["idProveedor"].ToString());
                            productoInfo.nombreProveedor= cadaResultado["nombreProveedor"].ToString();
                            productoInfo.monto= decimal.Parse(cadaResultado["monto"].ToString());
                            productoInfo.mensajeTicket1 = cadaResultado["mensajeTicket1"].ToString();
                            productoInfo.mensajeTicket2 = cadaResultado["mensajeTicket2"].ToString();
                        }
                    }
                    else
                    {
                        Task.Run(() => Utileria.Log(Utileria.ObtenerNombreFuncion("No hay resultados con la operación"), Utileria.TiposLog.warnning));

                    }
                }
                else
                {
                    Task.Run(() => Utileria.Log(Utileria.ObtenerNombreFuncion(resultadoBaseDatos.Excepcion.Message), Utileria.TiposLog.error));

                }
                return productoInfo;
            }
            catch (Exception ex)
            {
                Task.Run(() => Utileria.Log(Utileria.ObtenerNombreFuncion(ex.Message), Utileria.TiposLog.error));
                return productoInfo;
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
        private static ProveedorInfo ObtenerInfoProveedor(int idProveedor)
        {
            ProveedorInfo proveedorInfo = new ProveedorInfo();
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
                            proveedorInfo.idProveedor = idProveedor;
                            proveedorInfo.nombreProveedor = cadaResultado["nombreProveedor"].ToString();
                            proveedorInfo.marca= cadaResultado["marca"].ToString();
                            proveedorInfo.issuer= cadaResultado["issuer"].ToString();
                            proveedorInfo.idMaster = int.Parse(cadaResultado["idMaster"].ToString());
                        }
                    }
                    else
                    {
                        Task.Run(() => Utileria.Log(Utileria.ObtenerNombreFuncion("No hay resultados con la operación"), Utileria.TiposLog.warnning));

                    }
                }
                else
                {
                    Task.Run(() => Utileria.Log(Utileria.ObtenerNombreFuncion(resultadoBaseDatos.Excepcion.Message), Utileria.TiposLog.error));

                }
                return proveedorInfo;
            }
            catch (Exception ex)
            {
                Task.Run(() => Utileria.Log(Utileria.ObtenerNombreFuncion(ex.Message), Utileria.TiposLog.error));
                return proveedorInfo;
            }
            finally
            {
                GC.Collect();
            }
        }
    }
}
