namespace NinMemApi.Data.Models
{
    public class AreaCode
    {
        public AreaCode(string code, string name)
        {
            Code = code;
            Name = name;
        }

        public string Code { get; }
        public string Name { get; }
    }
}