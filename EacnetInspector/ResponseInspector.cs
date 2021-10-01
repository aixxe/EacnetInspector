using System;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Windows.Forms;
using Fiddler;
using kbinxmlcs;
using SyntaxView;
using eAmuseCore.Compression;

namespace EacnetInspector
{
    public class ResponseInspector: Inspector2, IResponseInspector2
    {
        public override void AddToTab(TabPage o)
        {
            _text.AddToTab(o);
            o.Text = "Eacnet";
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
                    // Skip the header and get straight to the data.
                    var start = FindPattern(value, new byte[] { 0xA0, 0x42 });
                    var kxml = start == -1 ? value.ToArray(): value.Skip(start - 1).ToArray();
                    
                    KbinReader reader = null;
                    
                    // Try decoding without compression first. Mostly useful for resource info files.
                    try { reader = new KbinReader(kxml); } catch {}

                    // Try again, but with decompression this time.
                    if (reader == null)
                    {
                        kxml = LZ77.Decompress(kxml);
                        reader = new KbinReader(kxml);
                    }

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
        
        private static int FindPattern(byte[] src, byte[] pattern)
        {
            var max = src.Length - pattern.Length + 1;
            
            for (var i = 0; i < max; i++)
            {
                if (src[i] != pattern[0])
                    continue;
        
                for (var j = pattern.Length - 1; j >= 1; j--) 
                {
                    if (src[i + j] != pattern[j])
                        break;
                    
                    if (j == 1)
                        return i;
                }
            }
            
            return -1;
        }

        public bool bDirty { get; }
        public bool bReadOnly { get => true; set {} }
        public HTTPResponseHeaders headers { get; set; }
        private ResponseSyntaxView _text = new ResponseSyntaxView();
    }
}
