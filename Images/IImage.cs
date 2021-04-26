using System;
using System.Threading.Tasks;
using System.Collections.Generic;
namespace Homiebot.Images 
{
    public interface IImage 
    {
        string ImageIdentifier{get;set;}
        IEnumerable<string> ImageTags {get;}
        Task<byte[]> GetBytes();
        void addTag(string tag);
    }
}