using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GiveProps.MVC.Models
{
    public class ScrapedPage
    {
        public string URL { get; set; }
        public string Host { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public List<string> Keywords { get; set; }
        public List<Link> Links { get; set; }
        public List<Img> Images { get; set; }
    }

    public class Link
    {
        public string Text { get; set; }
        public string Href { get; set; }
        public string Alt { get; set; }

        public override string ToString()
        {
            return string.Format("<a href='{0}'>{1}</a>", Href, Text);
        }
    }

    public class Img
    {
        public string Alt { get; set; }
        public string Src { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Aspect { get; set; }
        public int Score { get; set; }
        public bool IsJpg { get; set; }


        public string Thumbnail
        {
            get
            {
                return string.Format("<img src='{0}' alt='{1}' width='100%' height='100%' />", Src, Alt);
            }
        }

        public bool HasDimensions
        {
            get
            {
                if (Width > 0 && Height > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public int Perspective
        {
            get
            {
                return Width / Height;
            }
        }

        public int CombinedSize
        {
            get
            {
                return Width + Height;
            }
        }
    }

    
}