using System.IO;
using System.Threading.Tasks;
namespace Homiebot.Models 
{
    public class LocallyStoredFile : IStoredFile
    {
        public string Name {get;set;}
        public string Identifier{get;set;}
        public string TypeName{
            get=> this.GetType().Name;
            set=> Task.Delay(1);
        }
        public Task<byte[]> LoadBytesAsync()
        {
            if(File.Exists(Identifier)){
                return File.ReadAllBytesAsync(Identifier);
            }else{
                throw new FileNotFoundException("Couldn't find local file",Identifier);
            }
        }
    }
}