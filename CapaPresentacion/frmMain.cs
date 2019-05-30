using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ServidorCore;

namespace CapaPresentacion
{
    public partial class frmMain : Form
    {
        ServidorDeSockets<InfoSocketDelUsuarioDerivado, EstadoSocketDelUsuarioDerivado> servidor;
        public delegate void delegadoPintarLista(String mensaje);
        public delegadoPintarLista pintarLista;        

        public frmMain()
        {
            InitializeComponent();
        }
        private void frmMain_Load(object sender, EventArgs e)
        {
            pintarLista = new delegadoPintarLista(PintaMensajeLista);
            servidor = new ServidorDeSockets<InfoSocketDelUsuarioDerivado, EstadoSocketDelUsuarioDerivado>(10000, 1024, 10, this);
        }

        private void button_Iniciar_Click(object sender, EventArgs e)
        {
            PintaMensajeLista("Inicio");
            
            servidor.InicializarServidor();            
            servidor.IniciarServidor(5555);
            servidor.estadoDelSocketDelUsuario.formulario = this;
            button_Iniciar.Enabled = false;
            button_Detener.Enabled = true;
        }

        private void button_Detener_Click(object sender, EventArgs e)
        {
            servidor.DetenerServidor();
            button_Iniciar.Enabled = true;
            button_Detener.Enabled = false;
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            button_Detener_Click(this, e);
        }

        private void PintaMensajeLista(String mensaje)
        {
            listBox1.Items.Add(mensaje);
        }
    }
}
