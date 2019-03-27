using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace CapaDatos
{
    public class OperacionesBaseDatos
    {
        public static String dataSource { get; set; }
        public static Boolean seguridadLogin { get; set; }
        public static String catalogoInicial { get; set; }
        public static String usuario { get; set; }
        public static String pass { get; set; }


        /// <summary>
        /// método statico para la ejecución de SP's seguros en base de datos
        /// </summary>
        /// <param name="Procedimiento">Nombre del procedimiento a ejecutar</param>
        /// <param name="Parametros">lista de parámetros del procedimiento</param>
        /// <returns>clase ResultadoBaseDatos</returns>
        public static ResultadoBaseDatos EjecutaSP(string Procedimiento, List<SqlParameter> Parametros)
        {            
            //@091116 objeto sobre la clase que tiene los resultados de la consulta a base de datos o sus errores sobre la misma
            ResultadoBaseDatos salida = new ResultadoBaseDatos();

            SqlConnectionStringBuilder constructorCadena = new SqlConnectionStringBuilder();
            constructorCadena["Data Source"] = dataSource;
            constructorCadena["integrated Security"] = seguridadLogin;
            constructorCadena["Initial Catalog"] = catalogoInicial;
            constructorCadena["User"] = usuario;
            constructorCadena["Password"] = pass;
            
            //@091116 se inicia la conexión correspondiente hacia la base de datos
            //@081116 se cambia la conexión hacia una base localDB por medio de las propiedades de configuración            
            using (SqlConnection conexion = new SqlConnection(constructorCadena.ToString()))            
            //using (SqlConnection conexion = new SqlConnection(constructorCadena.ToString()))            
            {
                try
                {
                    //@091116 siempre se llama a stores procedures y se preparan con el objeto command
                    SqlCommand command = new SqlCommand(Procedimiento, conexion);
                    //@091116 se especifica que siempre son SP's
                    command.CommandType = CommandType.StoredProcedure;
                    //@091116 se abre la conexión
                    conexion.Open();
                    //@091116 con esta acción analiza que parámetros necesita el SP
                    SqlCommandBuilder.DeriveParameters(command);

                    //@091116 se recorre el arreglo de parámetros para enviar al SP
                    foreach (SqlParameter pr in Parametros)
                    {
                        //@091116 cada parámetro se agrega a la colección de parámetros del objeto command
                        command.Parameters[pr.ParameterName].Value = pr.Value;
                    }

                    //@091116 con el adaptador se realiza la sentencia transaccional
                    SqlDataAdapter dataAdapter = new SqlDataAdapter(command);
                    //@091116se prepara el objeto que recibirá los datos recibidos
                    DataSet dataSet = new DataSet();
                    //@091116 cualquier información recibida, el adaptador llenará el objeto data set
                    dataAdapter.Fill(dataSet);

                    //@091116 se llena el objeto de respuesta con la información de la consulta
                    salida.Datos = dataSet;
                    salida.Error = false;
                    salida.Excepcion = null;
                }
                catch (Exception ex)
                {
                    //@091116 cualquier error se indica en el objeto de respuesta
                    salida.Error = true;
                    salida.Excepcion = ex;
                    salida.Datos = null;
                }
                finally
                {
                    //@091116 siempre se realiza la desconexión a la base
                    if (conexion.State == ConnectionState.Open)
                    {
                        conexion.Close();
                    }
                    GC.Collect();
                }
            }

            return salida;
        }
    }
}
