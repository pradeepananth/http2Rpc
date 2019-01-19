namespace nRpc
{
    public class Procedure<TRequest, TResponse>
    {
        public Procedure(string name)
        {
            ThrowIf.IsNullOrEmpty(nameof(name), name);
            Name = name;
        }

        public string Name { get; private set; }
    }
}
