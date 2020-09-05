using Newtonsoft.Json.Serialization;
namespace homiebot 
{
    public interface IStoredFile
    {
        public string Name {get;set;}
        public string TypeName{get;set;}
        public byte[] LoadBytes();
    }
}