using System;
using System.Web;
using System.Text;
using System.Xml.Linq;
using System.Windows.Forms;
using Fiddler;
using kbinxmlcs;
using SyntaxView;
using eAmuseCore.Compression;

namespace EacnetInspector
{
    public class RequestInspector: Inspector2, IRequestInspector2
    {
        public override void AddToTab(TabPage page)
        {
            _text.AddToTab(page);
            page.Text = "Eacnet";
        }

        public override int GetOrder() => 0;
        
        public void Clear() => _text.Clear();

        public byte[] body
        {
            get => new byte[] {};
            set
            {
                Clear();
                
                try
                {
                    // Get the Base64 encoded request string from the POST body.
                    var body = Encoding.UTF8.GetString(value);
                    var qs = HttpUtility.ParseQueryString(body);

                    var request = qs.Get("request");

                    if (request == null)
                        return;
                    
                    // INFINITAS uses + characters in the request but ParseQueryString just replaced them with spaces.
                    request = request.Replace(" ", "+");
                    
                    // Other games use placeholders for + and /, so make sure we're using the real characters now.
                    request = request.Replace("-", "+");
                    request = request.Replace("_", "/");
                    
                    // Awkward moment where the request string might have had the trailing = character removed, so we
                    // just keep adding them to the end of the string until it works.
                    var data = new byte[] {};

                    for (var i = 0; i < 3; i++)
                    {
                        try
                        {
                            data = Convert.FromBase64String(request);
                            break;
                        } catch { request += "="; }
                    }
                    
                    var kxml = LZ77.Decompress(data);
                    var reader = new KbinReader(kxml);
                    
                    // Now parse it, solely for formatting purposes.
                    var xml = reader.Read().OuterXml;
                    var doc = XDocument.Parse(xml);

                    // Display in text box.
                    _text.body = Encoding.UTF8.GetBytes(doc.ToString());
                }
                catch (Exception e)
                {
                    _text.body = Encoding.UTF8.GetBytes("Error: " + e);
                }
            }
        }

        public bool bDirty { get; }
        public bool bReadOnly { get => true; set {} }
        public HTTPRequestHeaders headers { get; set; }
        private ResponseSyntaxView _text = new ResponseSyntaxView();
    }
}