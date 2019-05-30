using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CapaNegocio
{
    public class Operaciones
    {  
        private String trama { get; set; }

        public Operaciones(String trama)
        {
            this.trama = trama;
        }

        public Boolean ParsearTrama()
        {
            if (trama.Substring(0, 3) == "200")
            {
                //TODO implementar
            }
            else if (trama.Substring(0, 2) == "13")
            {
                MensajePxSolicitud mensajePxSolicitud = new MensajePxSolicitud();
                MensajePXRespuesta mensajePXRespuesta;
                if (mensajePxSolicitud.Parsear(trama))
                {
                    mensajePXRespuesta = new MensajePXRespuesta(mensajePxSolicitud);
                }
                else
                {
                    return false;
                }

                MensajeTpvSolicitud mensajeTpvSolicitud = new MensajeTpvSolicitud();

                if (MapearMensajeria(mensajePxSolicitud, mensajeTpvSolicitud))
                {
                    MensajeTpvRespuesta mensajeTpvRespuesta = new MensajeTpvRespuesta(mensajeTpvSolicitud);
                }

            }

            

            return true;
        }

        private Boolean MapearMensajeria(MensajePxSolicitud mensajePxSolicitud, MensajeTpvSolicitud mensajeTpvSolicitud)
        {
            try
            {
                mensajeTpvSolicitud.pCode = "650000";
                mensajeTpvSolicitud.monto = "20";
                mensajeTpvSolicitud.fechaHoraMMDDhhmmss = mensajePxSolicitud.fechaYYMMDD.Substring(2) + mensajePxSolicitud.horaHHMMSS;
                mensajeTpvSolicitud.systemTrace = mensajePxSolicitud.transNumber;
                mensajeTpvSolicitud.horaTerminalhhmmss = mensajePxSolicitud.horaHHMMSS;
                mensajeTpvSolicitud.fechaTerminalMMDD = mensajePxSolicitud.fechaYYMMDD.Substring(2);
                mensajeTpvSolicitud.fechaContable = mensajePxSolicitud.fechaYYMMDD.Substring(2);
                mensajeTpvSolicitud.fechaCaptura = mensajePxSolicitud.fechaYYMMDD.Substring(2);
                mensajeTpvSolicitud.adquiriente = "106900000001";
                mensajeTpvSolicitud.issuer = "106800000001";
                mensajeTpvSolicitud.referencia = "1";
                mensajeTpvSolicitud.terminaId = "R5ST68400010001";
                mensajeTpvSolicitud.merchantData = "Tienda x";
                mensajeTpvSolicitud.codigoMoneda = "484";
                mensajeTpvSolicitud.datosAdicionales = "012B999PRO1+000";
                mensajeTpvSolicitud.telefono = "01500000" + mensajePxSolicitud.telefono;
                return true;
            }
            catch (Exception)
            {
                //TODO: log
                return false;
            }
            
        }

        private String EnviarTramaProveedor(String trama)
        {
            String mensajeRespueta = String.Empty;
            EnviarRecibirSocketProveedor enviarRecibirSocketProveedor = new EnviarRecibirSocketProveedor();
            //tranformo el texto en formato de ip
            IPAddress oIP = IPAddress.Parse("192.168.69.91");

            //junto con el puerto instancio el punto de destino de conexión
            IPEndPoint oIP_Puerto = new IPEndPoint(oIP, int.Parse("20002"));
            try
            {
                if (enviarRecibirSocketProveedor.conectarSocket(oIP_Puerto) != true)
                {
                     return "Error al intentar conectarse";
                }
                
                enviarRecibirSocketProveedor.Send(enviarRecibirSocketProveedor.socketPrincipal, trama);

                enviarRecibirSocketProveedor.Receive(enviarRecibirSocketProveedor.socketPrincipal);

                mensajeRespueta = enviarRecibirSocketProveedor.mensajeRespuesta;

                return mensajeRespueta;
            }
            catch (Exception)
            {
                //TODO: Log
                return mensajeRespueta;
            }
        }
    }
}
