using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Cliente
{
    class Program
    {
        static int numClientesYaRespondidos = 0;
        //static private ManualResetEvent manual = new ManualResetEvent(false);

        static SemaphoreSlim semaforo = new SemaphoreSlim(100);
        static void Main(string[] args)
        {
            List<Thread> listaHilos = new List<Thread>();
            int numClientes = 0;
            int numTrxPorCliente = 0;
            List< AutoResetEvent> autos;
            do
            {
                numClientesYaRespondidos = 0;
                //manual.Set();
                Console.WriteLine("Cuantos clientes");
                numClientes = int.Parse(Console.ReadLine());
                autos = new List<AutoResetEvent>(numClientes);
                Console.WriteLine("cuantas trx por cliente");
                numTrxPorCliente = int.Parse(Console.ReadLine());


                for (int i = 0; i < numClientes; i++)
                {
                    AutoResetEvent Auto = new AutoResetEvent(false);
                    autos.Add(Auto);
                    Thread thread = new Thread(() => EnvioTrx(Auto, (object)numTrxPorCliente));
                    thread.Start();
                    listaHilos.Add(thread);
                }                

                
                //WaitHandle.WaitAll(autos.ToArray());                
                autos.Clear();
                //manual.Reset();

                listaHilos.Clear();

                //Console.WriteLine("escribe stop para detener, de lo contrario cualquier tecla para reiniciar");
                //Console.WriteLine("numClientesYaRespondidos: " + numClientesYaRespondidos.ToString());
            } while (Console.ReadLine() != "stop");

        }

        private static void EnvioTrx(AutoResetEvent auto,object numTrx)
        {
            //manual.WaitOne();

            IPAddress iPAddress = IPAddress.Parse("10.0.0.70");
            //IPAddress iPAddress = IPAddress.Parse("192.168.69.12");
            IPEndPoint endPointProcesa = new IPEndPoint(iPAddress, 8002);
            
            semaforo.Wait();

            // se genera un socket que será usado en el envío y recepción            
            Socket socketDeTrabajo = new Socket(endPointProcesa.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socketDeTrabajo.ReceiveTimeout = 30000;
            socketDeTrabajo.SendTimeout = 30000;
            try
            {
                socketDeTrabajo.Connect(endPointProcesa);                
                Interlocked.Increment(ref numClientesYaRespondidos);
                Console.WriteLine(numClientesYaRespondidos);
            }
            catch (Exception ex )
            {
                Interlocked.Increment(ref numClientesYaRespondidos);
                Console.WriteLine(numClientesYaRespondidos.ToString() + ". " + ex.Message);
                socketDeTrabajo.Close();
                socketDeTrabajo.Dispose();
                semaforo.Release();
                return;
            }

            Thread.Sleep(1000);

            try
            {
                for (int i = 0; i < (int)numTrx; i++)
                {
                    byte[] msg = Encoding.UTF8.GetBytes("130011000100010001130211150030098469766750482       452170489941665.");
                    byte[] bytes = new byte[1024];


                    int byteCount = socketDeTrabajo.Send(msg, 0, msg.Length, SocketFlags.None);

                    byteCount = socketDeTrabajo.Receive(bytes);
                    string respuesta = Encoding.UTF8.GetString(bytes).Substring(2);
                                        
                }
                semaforo.Release();
            }
            catch (Exception ex)
            {
                Console.WriteLine(numClientesYaRespondidos.ToString() + ". " + ex.Message);
                socketDeTrabajo.Close();
                socketDeTrabajo.Dispose();
                semaforo.Release();
                return;
            }
            finally
            {
                auto.Set();                  
            }   
        }
    }
}
