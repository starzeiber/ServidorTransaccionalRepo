using static ServerCore.Constants.ServerCoreConstants;

namespace ServerCore.Classes
{
    internal interface ILogTrace
    {
        string NombreLog { get; }

        void EscribirLog(string mensaje, LogType tipoLog);
    }
}