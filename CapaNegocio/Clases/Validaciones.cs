using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaNegocio
{
    static internal class Validaciones
    {
        internal enum opcionesFormato
        {
            espaciosDerecha=1,
            espaciosIzquierda=2,
            cerosDerecha=3,
            cerosIzquierda=4            
        }

        static internal String DarFormato(String campo, opcionesFormato opciones, Int16 longitudCampo)
        {
            try
            {
                switch (opciones)
                {
                    case opcionesFormato.cerosDerecha:
                        while (campo.Length<longitudCampo)
                        {
                            campo = campo + "0";
                        }                        
                        break;
                    case opcionesFormato.cerosIzquierda:
                        while (campo.Length < longitudCampo)
                        {
                            campo = "0" + campo;
                        }                        
                        break;
                    case opcionesFormato.espaciosDerecha:
                        while (campo.Length < longitudCampo)
                        {
                            campo = campo + " ";
                        }                        
                        break;
                    case opcionesFormato.espaciosIzquierda:
                        while (campo.Length < longitudCampo)
                        {
                            campo = " " + campo;
                        }                        
                        break;
                }
                return campo;
            }
            catch (Exception)
            {
                //TODO: log
                return campo;
            }
        }
    }
}
