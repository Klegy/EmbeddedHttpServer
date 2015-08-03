using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;

namespace System.Net.EmbeddedHttp
{
    /// <summary>
    /// A templated file request handler
    /// </summary>
    public class TemplateHandler: ReqestHandler
    {

        private StringBuilder _content;

        /// <summary>
        /// Event to run on Get method
        /// </summary>
        public event GetParametersHandler GetHandler;

        /// <summary>
        /// Creates a new instance of TemplateHandler
        /// </summary>
        public TemplateHandler(): base()
        {
            _content = new StringBuilder();
            TemplateTags = new Dictionary<string, object>();
            ResponseMime = "text/html";
        }

        /// <summary>
        /// Base template
        /// </summary>
        public string TemplateText
        {
            get;
            set;
        }

        /// <summary>
        /// Template tags to replace
        /// </summary>
        public Dictionary<string, object> TemplateTags
        {
            get;
            private set;
        }

        /// <summary>
        /// Response mime type
        /// </summary>
        public string ResponseMime
        {
            get;
            set;
        }

        /// <summary>
        /// Request handler function. Overriden from RequestHandler class
        /// </summary>
        /// <param name="context">Request context</param>
        public override void HandleReqest(HttpListenerContext context)
        {
            _content.Append(TemplateText);

            if (context.Request.HttpMethod == "GET")
            {
                if (GetHandler != null)
                {
                    GetHandler(this, new HTTPEventArgs(EmbeddedHttpServer.GetFileName(context.Request.RawUrl), context.Request.QueryString));
                }
            }

            if (TemplateTags != null)
            {
                foreach (var keyvaluepair in TemplateTags)
                {
                    var toreplace = "{{" + keyvaluepair.Key + "}}";
                    _content.Replace(toreplace, keyvaluepair.Value.ToString());
                }
            }

            var output = Encoding.UTF8.GetBytes(_content.ToString());
            context.Response.ContentType = ResponseMime;
            context.Response.ContentLength64 = output.Length;
            context.Response.OutputStream.Write(output, 0, output.Length);
        }
    }
}
