using System;
using System.Threading.Tasks;
using System.Collections.Generic;
namespace homiebot.images 
{
    public interface IImage 
    {
        string ImageIdentifier{get;set;}
        IEnumerable<string> ImageTags {get;}
        Task<byte[]> GetBytes();
        void addTag(string tag);
    }
}