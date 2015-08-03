using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Net.EmbeddedHttp
{
    public delegate void GetParametersHandler(object sender, HTTPEventArgs e);

    public class HTTPEventArgs: EventArgs
    {
        public HTTPEventArgs() : base() 
        {
            File = "";
            Parameters = new Dictionary<string, string>();
        }

        public HTTPEventArgs(string file, NameValueCollection parameters) : base()
        {
            File = file;
            Parameters = new Dictionary<string, string>();
            foreach (var k in parameters.AllKeys)
            {
                Parameters.Add(k, parameters[k]);
            }  
        }

        public string File { get; private set; }
        public Dictionary<string, string> Parameters { get; private set; }
    }
}
