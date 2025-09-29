namespace ServerCore
{
    public interface ISecurity
    {
        string licence { get; set; }
        string manufacturer { get; set; }
        string processorId { get; set; }
        string product { get; set; }

        bool GetInfoPc();
        string RunQuery(string TableName, string MethodName);
    }
}