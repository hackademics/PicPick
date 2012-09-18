using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Net;
using System.IO;

using GiveProps.MVC.Models;
using HtmlAgilityPack;

namespace GiveProps.MVC.Controllers
{
    public class HomeController : Controller
    {

        public ActionResult Index()
        {
            var page = new ScrapedPage();
            string url = !string.IsNullOrEmpty(Request.QueryString["url"]) ? Request.QueryString["url"].ToString() : string.Empty;

            if (!string.IsNullOrEmpty(url))
            {
                page = GetPage(url);
                return View(page);
            }
            else
            {
                page = null;
                return View(page);
            }

            
        }


        private ScrapedPage GetPage(string url)
        {
            HtmlWeb htmlWeb = new HtmlWeb();
            HtmlDocument doc = htmlWeb.Load(url);

            var page = new ScrapedPage();
            page.URL = url;
            page.Host = htmlWeb.ResponseUri.Host;
            page.Title = GetTitle(doc);
            page.Images = GetImages(doc);
            page.Links = GetLinks(doc);
            page.Keywords = GetKeywords(doc);
            page.Description = GetDescription(doc);

            if (page.Images.Count > 0)
            {
               page.Images = ScoreImages(page);
            }

            return page;
        }


        private List<Img> ScoreImages(ScrapedPage page)
        {
            var list = new List<Img>();

            foreach (var img in page.Images)
            {
                var score = 0;
                bool isJpg = false;
                bool isPng = false;
                bool isGif = false;
                bool fetchSize = false;

                FixPath(img, page.Host);

                //is it a jpg or png, if so up it's score
                if (img.Src.ToLower().Contains(".jpg") || img.Src.ToLower().Contains(".jpeg"))
                {
                    img.IsJpg = true;
                    score = (score + 2);
                }
                else if (img.Src.ToLower().Contains(".png"))
                {
                    isPng = true;
                    score++;
                }
                else if (img.Src.ToLower().Contains(".gif"))
                {
                    isGif = true;
                }
                else
                {
                    fetchSize = true;
                }

                //only resize the JPGS
                if (!img.HasDimensions || fetchSize)
                {
                    SizeImage(img);
                }

                if (!isGif)
                {
                    if (img.Width > 100 || img.Height > 100)
                    {
                        score++;
                    }

                    //is it big
                    if (img.CombinedSize > 200)
                    {
                        score++;

                        //does it have a good perspective
                        if (isJpg && img.Perspective < 2)
                        {
                            score++;
                        }
                    }
                }



                //does it have an alt tag
                if (!isGif && img.Alt.Length > 20)
                {
                    score++;
                }

                //knock it down if it's a logo
                if (img.Src.ToLower().Contains("logo") || img.Src.ToLower().Contains("sprite") || img.Src.ToLower().Contains("loading"))
                {
                    score = (score - 2);
                }

                img.Score = img.Score + score;

                if (score > 1)
                {
                    list.Add(img);
                }

                

                
            }

            return list.OrderByDescending(x => x.Score).ToList();
        }

        private void FixPath(Img img, string host)
        {
            if (img.Src.StartsWith("/"))
            {
                img.Src = "http://" + host + img.Src;
            }
        }

        private void SizeImage(Img image)
        {
            try
            {
                WebClient client = new WebClient();
                client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");

                byte[] b = client.DownloadData(image.Src);
                MemoryStream stream = new MemoryStream(b);
                var img = Image.FromStream(stream);

                if (img != null)
                {
                    image.Width = img.Width;
                    image.Height = img.Height;

                    if (img.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Jpeg))
                    {
                        image.IsJpg = true;
                        image.Score = (image.Score + 2);
                    }
                }

                img.Dispose();
                stream.Close();
                stream.Dispose();
                client.Dispose();
            }
            catch{}

        }


        private List<Img> GetImages(HtmlDocument doc)
        {

            List<Img> list = new List<Img>();

            if (doc.DocumentNode.SelectNodes("//img[@src]") == null)
            {
                return list;
            }

            foreach (HtmlNode link in doc.DocumentNode.SelectNodes("//img[@src]"))
            {
                Img obj = new Img();

                HtmlAttribute src = link.Attributes["src"];
                HtmlAttribute alt = link.Attributes["alt"];
                HtmlAttribute width = link.Attributes["width"];
                HtmlAttribute height = link.Attributes["height"];
                
                obj.Src = (src != null && !string.IsNullOrEmpty(src.Value)) ? src.Value : "";
                obj.Alt = (alt != null && !string.IsNullOrEmpty(alt.Value)) ? alt.Value : "";


                if (width != null)
                {
                    int x = 0;
                    int.TryParse(width.Value, out x);
                    obj.Width = x;
                }

                if (height != null)
                {
                    int x = 0;
                    int.TryParse(height.Value, out x);
                    obj.Height = x;
                }

                //check to see if we already have it

                var i = (from p in list where p.Src == obj.Src select p).FirstOrDefault();

                if (i == null)
                {
                    list.Add(obj);
                }
            }

            return list;
        }

        private List<Link> GetLinks(HtmlDocument doc)
        {

            List<Link> list = new List<Link>();

            if (doc.DocumentNode.SelectNodes("//a") == null)
            {
                return list;
            }

            foreach (HtmlNode link in doc.DocumentNode.SelectNodes("//a"))
            {
                Link obj = new Link();

                HtmlAttribute href = link.Attributes["href"];
                HtmlAttribute alt = link.Attributes["alt"];
                
                obj.Text = link.InnerText;
                obj.Href = (href != null && !string.IsNullOrEmpty(href.Value)) ? href.Value : "";
                obj.Alt = (alt != null && !string.IsNullOrEmpty(alt.Value)) ? alt.Value : "";
              
                //make sure it's not an anchor tag

                list.Add(obj);
            }

            return list;

        }

        private string GetTitle(HtmlDocument doc)
        {
            string result = "";
            var list = doc.DocumentNode.Descendants("title").Select(x => x.InnerText).ToList();

            if (list.Count > 0)
            {
                result = list[0];
            }

            return result;
        }


        private List<string> GetKeywords(HtmlDocument doc)
        {
            List<string> sb = new List<string>();

            if (doc.DocumentNode.SelectNodes("//meta") == null)
            {
                return null;
            }

            var list = doc.DocumentNode.Descendants("meta").Select(x => x.InnerText).ToList();

            foreach (HtmlNode obj in doc.DocumentNode.SelectNodes("//meta"))
            {
                var n = obj.Attributes["name"];
                var c = obj.Attributes["content"];

                if (n != null && !string.IsNullOrEmpty(n.Value))
                {
                    string name = n.Value.ToString().ToLower().Trim();

                    if (name == "keywords")
                    {
                        if (c != null && !string.IsNullOrEmpty(c.Value))
                        {
                            string keywords = c.Value.ToString();
                            string[] foo = keywords.Split(new char[] { ',' });

                            if (foo.Length > 0)
                            {
                                foreach (var s in foo)
                                {
                                    sb.Add(s);
                                }
                            }
                        }

                        break;
                    }
                }
            }

            return sb;
        }

        private string GetDescription(HtmlDocument doc)
        {
            string result = "";
            
            if (doc.DocumentNode.SelectNodes("//meta") == null)
            {
                return result;
            }

            foreach (HtmlNode obj in doc.DocumentNode.SelectNodes("//meta"))
            {
                var n = obj.Attributes["name"];
                var c = obj.Attributes["content"];

                if (n != null && !string.IsNullOrEmpty(n.Value))
                {
                    string name = n.Value.ToString().ToLower().Trim();
                    if (name == "description")
                    {
                        if (c != null && !string.IsNullOrEmpty(c.Value))
                        {
                            result = c.Value;
                            break;
                        }
                    }
                }
            }

            return result;
        }

    }

}
