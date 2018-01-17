namespace IA.SDK.Interfaces
{
    public interface IQuery<T>
    {
        T Query(string query);
    }
}