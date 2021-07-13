using ServidorCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CapaNegocio;
using CapaNegocio.Clases;

namespace CapaPresentacion
{
    public class EstadoDelProveedor: EstadoDelProveedorBase
    {
        public override void PreparaTramaAlProveedor(int cabeceraMensaje, object objPeticionCliente)
        {
            try
            {
                switch (cabeceraMensaje)
                {
                    case (int)Operaciones.CabecerasTrama.compraTaePx:
                        CompraPxTae compraPxTae= objPeticionCliente as CompraPxTae;
                        Task<RespuestaGenerica> procesarMensajeriaTask = Task.Run(() => Operaciones.CompraTpv(compraPxTae));
                        procesarMensajeriaTask.Wait();
                        codigoRespuesta = procesarMensajeriaTask.Result.codigoRespuesta;
                        break;
                    case (int)Operaciones.CabecerasTrama.compraDatosPx:
                        break;
                    case (int)Operaciones.CabecerasTrama.consultaTaePx:
                        break;
                    case (int)Operaciones.CabecerasTrama.consultaDatosPx:
                        break;
                    default:
                        break;
                }
            }
            catch (Exception)
            {

                throw;
            }
            

        }
        public override void ProcesarTramaDelProveeedor(string mensaje)
        {
            
        }
    }
}
