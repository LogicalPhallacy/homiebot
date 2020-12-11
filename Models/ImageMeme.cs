using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ImageMagick;
using homiebot.images;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json.Converters;

namespace homiebot 
{
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
        public async Task<Byte[]> GetImageAsync(IImageStore imageStore, Random random =null, params string[] replacements) 
        {
            var factory = new MagickImageFactory();
            using (var image = factory.Create(await(await imageStore.GetImageAsync(this.Template.ImageBaseIdentifier)).GetBytes()))
            {
                List<MagickImage> MemeTexts = new List<MagickImage>(); 
                foreach(var m in Template.memeText)
                {
                    MagickImage mti;
                    
                    try
                    {
                        mti = new MagickImage($"caption:{m.GetMemeText(random, replacements)}",m.Composition);
                    }
                    catch(Exception e)
                    {
                        throw e;
                    }
                    
                    MemeTexts.Add(mti);
                    image.Composite(mti,m.XStartPosition,m.YStartPosition,CompositeOperator.Over);
                }
                var bytes = image.ToByteArray();
                // dispose our dynamic images
                //image.Write(writeStream);
                foreach(var meme in MemeTexts){
                    meme.Dispose();
                }
                return bytes;
            }
        }
    }

    public class MemeTemplate
    {
        public string Name {get;set;}
        public string ImageBaseIdentifier{get;set;}
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

        public MagickReadSettings Composition 
        {
            get => new MagickReadSettings(){
                Font = "Impact",
                TextGravity = Gravity.Center,
                BackgroundColor = MagickColors.Transparent,
                StrokeColor = new MagickColor(OutlineColor),
                FillColor = new MagickColor(FillColor),
                Height = this.Height,
                Width = this.Width
            };
        }

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
                case homiebot.TextEffects.lowercase:
                    return retstr.ToLowerInvariant();
                case homiebot.TextEffects.UPPERCASE:
                    return retstr.ToUpperInvariant();
                case homiebot.TextEffects.mOCkIngCaSE:
                    return retstr.ToMockingCase(random);
                case homiebot.TextEffects.UwuCase:
                // TODO: implement uwu
                case homiebot.TextEffects.None:
                default:
                    return retstr; 
            }
        }
    }
}