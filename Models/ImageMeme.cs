using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json.Converters;
using Homiebot.Images;

namespace Homiebot.Models
{
    public class ImageCollection
    {
        public string Name {get;set;}
        public string Tag {get;set;}
        public string Description {get;set;}
        public bool CanAdd {get;set;}
        public string PostText {get;set;}
    }
    // this is too good for this world
    public enum TextEffects
    {
        None = 0,
        UPPERCASE =1,
        lowercase =2,
        mOCkIngCaSE =3,
        UwuCase=4
    }
    public class ImageMeme 
    {
        public MemeTemplate Template {get; set;}
        public async Task<Byte[]> GetImageAsync(IImageProcessor processor, params string[] replacements) 
        {
            
            return await processor.ProcessImage(this, replacements);
        }
    }

    public class MemeTemplate
    {
        public string Name {get;set;}
        public string ImageBaseIdentifier{get;set;}
        public string Description {get;set;}
        public bool SingleReplacement {get;set;}
        public IEnumerable<MemeText> memeText {get;set;}
    }

    public class MemeText
    {
        public string TemplateText{get;set;}
        public string TextEffects{get;set;} 
        // Bottom Left to Top Right
        public int XStartPosition{get;set;}
        public int YStartPosition{get;set;}
        public int Width{get;set;}
        public int Height{get;set;}

        public string OutlineColor 
        {
            get => string.IsNullOrWhiteSpace(outLineColor) ? "black" : outLineColor;
            set => outLineColor = value;
        }
        private string outLineColor;
        public string FillColor 
        {
            get => string.IsNullOrWhiteSpace(fillColor) ? "white" : fillColor;
            set => fillColor = value;
        }
        private string fillColor;

        public string GetMemeText( Random random=null, params string[] replacements)
        {
            string retstr = TemplateText;
            int index = 0;
            foreach(string r in replacements)
            {
                index++;
                retstr = retstr.Replace($"@REPLACEMENT{index}@",r);
            }
            switch(Enum.Parse<TextEffects>(TextEffects))
            {
                case Homiebot.Models.TextEffects.lowercase:
                    return retstr.ToLowerInvariant();
                case Homiebot.Models.TextEffects.UPPERCASE:
                    return retstr.ToUpperInvariant();
                case Homiebot.Models.TextEffects.mOCkIngCaSE:
                    return retstr.ToMockingCase(random);
                case Homiebot.Models.TextEffects.UwuCase:
                    return retstr.ToUwuCase();
                case Homiebot.Models.TextEffects.None:
                default:
                    return retstr; 
            }
        }
    }
}