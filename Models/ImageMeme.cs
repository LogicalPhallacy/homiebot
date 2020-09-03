using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ImageMagick;


namespace homiebot 
{
    public class ImageMeme 
    {
        public MemeTemplate Template {get; set;}
        public Byte[] GetImage(params string[] replacements) 
        {
            var factory = new MagickImageFactory();
            using (var image = factory.Create(Template.ImageBase))
            {
                List<MagickImage> MemeTexts = new List<MagickImage>(); 
                foreach(var m in Template.memeText)
                {
                    MagickImage mti;
                    
                    try
                    {
                        mti = new MagickImage($"caption:{m.GetMemeText(replacements)}",m.Composition);
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
        public FileInfo ImageBase{get;set;}
        public IEnumerable<MemeText> memeText {get;set;}
    }

    public class MemeText
    {
        public string TemplateText{get;set;}
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

        public string GetMemeText(params string[] replacements)
        {
            string retstr = TemplateText;
            int index = 0;
            foreach(string r in replacements)
            {
                index++;
                retstr = retstr.Replace($"@REPLACEMENT{index}@",r);
            }
            return retstr;
        }
    }
}