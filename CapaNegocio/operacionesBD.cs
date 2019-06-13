using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CapaDatos;
using System.Data.SqlClient;
using System.Data;

namespace CapaNegocio
{
    public static class operacionesBD
    {
        public static List<String> Obtener(String algo)
        {
            ResultadoBaseDatos resultados = new ResultadoBaseDatos();
            List<String> lista = new List<String>();
            try
            {
                List<SqlParameter> parametros = new List<SqlParameter>()
                {
                    new SqlParameter("@","")
                };

                //111116 se ejecuta el SP
                resultados = OperacionesBaseDatos.EjecutaSP("SPR", parametros);

                //111116 se revisa que existe un error
                if (!resultados.Error)
                {
                    //111116 se revisa que existan resultados
                    if (resultados.Datos.Tables.Count > 0)
                    {
                        //111116 se recorren los resultados
                        if (resultados.Datos.Tables[0].Rows.Count > 0)
                        {
                            foreach (DataRow dr in resultados.Datos.Tables[0].Rows)
                            {
                                //Area area = new Area();
                                //area.idArea = dr.Field<int>("idArea");
                                //area.nombreArea = dr.Field<String>("nombreArea");
                                //listaAreas.Add(area);
                            }
                            //cLogErrores.Escribir_Log_Advertencia("Se realizó la inserción correctamente");
                        }
                        else
                        {                            
                            //cLogErrores.Escribir_Log_Advertencia("no hay resultados, GraficaServidoresNegocio,Operaciones,ObtenerAreasGrafica");
                        }
                    }
                    else
                    {                        
                        //cLogErrores.Escribir_Log_Advertencia("no hay resultados, GraficaServidoresNegocio,Operaciones,ObtenerAreasGrafica");
                    }
                }
                else
                {                    
                    //cLogErrores.Escribir_Log_Error(resultados.Excepcion.Message + " GraficaServidoresNegocio,Operaciones,ObtenerAreasGrafica");
                }
                return lista;
            }
            catch (Exception ex)
            {                
                //cLogErrores.Escribir_Log_Error("GraficaServidoresNegocio,Operaciones,ObtenerAreasGrafica: " + ex.Message);
                return lista;
            }
            finally
            {
                GC.Collect();
            }
        }
    }
}
