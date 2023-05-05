namespace ServerCore.Clases
{
    internal interface ILogTrace
    {
        string NombreLog { get; }

        void EscribirLog(string mensaje, Utileria.tipoLog tipoLog);
    }
}