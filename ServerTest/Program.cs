using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.EmbeddedHttp;
using System.IO;

namespace ServerTest
{
    class Program
    {
        static void Main(string[] args)
        {

            TemplateHandler templatetest = new TemplateHandler();
            templatetest.RegisterHooks("templatetest");
            templatetest.ResponseMime = "text/plain";
            templatetest.TemplateText = "Template test. Query parameters: {{query}}";
            templatetest.GetHandler += templatetest_GetHandler;

            

            EmbeddedHttpServer server = new EmbeddedHttpServer(8080);
            server.Path = Environment.CurrentDirectory;
            server.Handlers.Add(templatetest);
            server.LogDir = Path.Combine(Environment.CurrentDirectory, "Log");
            server.AppPackages.Add(Path.Combine(Environment.CurrentDirectory, "webdev.zip"));
            server.Start();
            Console.WriteLine("Server running. Press ESC to quit");
            ConsoleKeyInfo key;
            do
            {
                key = Console.ReadKey();
            }
            while (key.Key != ConsoleKey.Escape);
            server.Stop();
        }

        static void templatetest_GetHandler(object sender, HTTPEventArgs e)
        {
            TemplateHandler template = (TemplateHandler)sender;

            if (e.Parameters != null)
            {
                template.TemplateTags.Add("query", e.Parameters.ToString());
            }
            else
            {
                template.TemplateTags.Add("query", "No query parameters specified");
            }
        }
    }
}
