using System.Reflection;
using System.Runtime.InteropServices;

// La información general de un ensamblado se controla mediante el siguiente 
// conjunto de atributos. Cambie estos valores de atributo para modificar la información
// asociada con un ensamblado.
[assembly: AssemblyTitle("UServerCore")]
[assembly: AssemblyDescription("Proyecto servidor transaccional")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("UMB")]
[assembly: AssemblyProduct("UServerCore")]
[assembly: AssemblyCopyright("Copyright ©  2017")]
[assembly: AssemblyTrademark("UMB")]
[assembly: AssemblyCulture("")]

// Si establece ComVisible en false, los tipos de este ensamblado no estarán visibles 
// para los componentes COM.  Si es necesario obtener acceso a un tipo en este ensamblado desde 
// COM, establezca el atributo ComVisible en true en este tipo.
[assembly: ComVisible(false)]

// El siguiente GUID sirve como id. de typelib si este proyecto se expone a COM.
[assembly: Guid("1684ca50-c181-4396-b92c-9a6098798dcd")]

// La información de versión de un ensamblado consta de los cuatro valores siguientes:
//
//      Versión principal
//      Versión secundaria
//      Número de compilación
//      Revisión
//
// Puede especificar todos los valores o usar los números de compilación y de revisión predeterminados
// mediante el carácter "*", como se muestra a continuación:
// [assembly: AssemblyVersion("1.0.*")]

//Autor     Fecha       Versión     Descripción
//@UMB      02/06/21    1.0.0.0     Creación del proyecto documentado en su totalidad, el core está funcionando aunque falta el log
//@UMB      06/10/21    2.0.0.0     Proyecto funcional, ya contiene logs, performances y uso de licencia
//@UMB      06/10/21    2.0.1.0     Se verifican los códigos de respuesta y se enlazan a un enum
//@UMB      15/10/21    2.1.0.0     Se implementa el modo test
//@UMB      19/10/21    2.2.0.0     Se implementa el modo router y se divide la utileria para tener una clase configuracion
//@UMB      04/11/21    2.2.0.1     pequeña corrección en el log
//@UMB      25/11/21    2.3.0.0     Se agrega el multipuerto a la ip de proveedor para balanceo     
//@UMB      06/12/21    2.3.1.0     Corrección  sobre el time out del cliente y proveedor
//@UMB      06/12/21    3.0.0.0     Se agrega que consulte al proveedor en caso de que en base de datos tenga código 71 la operación
//@UMB      08/12/21    3.0.1.0     Corrección en la variable esConsulta
//@UMB      10/12/21    4.0.0.0     Re estructura de los TimeOuts, y el corte de la operación cuando se generan
//@UMB      21/01/22    4.1.0.0     Se agrega un mayor detalle a los logs
//@UMB      03/02/22    4.1.1.0     Se  evaluan los objetos a null para  evitar  errores
//@UMB      08/02/22    4.1.2.0     Control de un error al parsear una trama
//@UMB      08/02/22    4.1.3.0     Se le agrega la fecha en el log
//@UMB      11/05/22    5.0.0.0     Cambio importante en cuanto a la escritura de logs y cerrado de sockets
//@UMB      12/05/22    6.0.0.0     Se implementa un ajuste total a los timers de TO, cambio importante
//@UMB      16/05/22    7.0.0.0     Corrección del time out en el timer (un rudo desafío)     
//@UMB      25/05/22    8.0.0.0     Se continuan las modificaciones al timer de TimeOut, ahora solo tendrá 1
//@UMB      25/05/22    9.0.0.0     Se re construye todo el guardado y el timeout por temas de errores cuando existe intermitencia

[assembly: AssemblyVersion("9.0.0.0")]
[assembly: AssemblyFileVersion("9.0.0.0")]
