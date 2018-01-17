namespace IA.SDK
{
    public interface IProxy<T>
    {
        T ToNativeObject();
    }
}