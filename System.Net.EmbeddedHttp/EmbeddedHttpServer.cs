using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;

namespace System.Net.EmbeddedHttp
{
    /// <summary>
    /// A LightWeight Embedable HTTP server with Request handlers
    /// </summary>
    public sealed class EmbeddedHttpServer: IDisposable
    {
        private readonly HttpListener _listener;
        private ServerLog _log;
        private List<ReqestHandler> _handlers;
        private Dictionary<string, byte[]> _cache;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="port">Listen port</param>
        public EmbeddedHttpServer(uint port)
        {
            if (!HttpListener.IsSupported) throw new NotSupportedException("Needs Windows XP SP2, Server 2003 or later.");
            _listener = new HttpListener();
            _handlers = new List<ReqestHandler>();
            _listener.Prefixes.Add(string.Format("http://*:{0}/", port));
            _log = new ServerLog();
            _cache = new Dictionary<string, byte[]>();
            AppPackages = new List<string>();
            CacheLimit = 10 * 1024 * 1024; //initial cache limit is 10Mb
            CustomMimeData = new Dictionary<string, string>();
            CustomMimeData.Add(".svg", "image/svg+xml");
            CustomMimeData.Add(".ttf", "application/x-font-truetype");
            CustomMimeData.Add(".eot", "application / vnd.ms - fontobject");
            CustomMimeData.Add(".woff", "application/font-woff");
            CustomMimeData.Add(".otf", "application/x-font-opentype");
        }

        private static string FormatPath(string path)
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32Windows:
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.Xbox:
                case PlatformID.WinCE:
                    return path.Replace(@"/", @"\");
                default:
                    return path.Replace(@"\", @"/");
            }
        }

        /// <summary>
        /// Rebuilds server cache. Caches files that are smaller than 300 Kib
        /// </summary>
        private long RebuildCache()
        {
            _cache.Clear();
            long size = 0;
            string[] files = Directory.GetFiles(Path, "*.*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                string key = FormatPath(file).Replace(Path+"\\", "").Replace("\\", "/");
                FileInfo fi = new FileInfo(file);
                if (fi.Length < (300 * 1024))
                {
                    if (size + fi.Length < CacheLimit)
                    {
                        var f = File.OpenRead(file);
                        if (fi.Length < 1) continue; //0b file caching fix
                        using (var br = new BinaryReader(f))
                        {
                            _cache.Add(key, br.ReadBytes((int)fi.Length));
                        }
                        size += fi.Length;
                    }
                }
            }
            return size;
        }

        /// <summary>
        /// Processes app package .zip files and adds them to the cache
        /// </summary>
        private void ProcessAppPackages()
        {
            foreach (var f in AppPackages)
            {
                using (var file = File.OpenRead(f))
                {
                    using (var zip = new ZipArchive(file))
                    {
                        foreach (ZipArchiveEntry entry in zip.Entries)
                        {
                            string key = FormatPath(entry.FullName).Replace(Path + "\\", "").Replace("\\", "/");
                            if (entry.Length < 1) continue; //0b file caching fix
                            using (var br = new BinaryReader(entry.Open()))
                            {
                                _cache.Add(key, br.ReadBytes((int)entry.Length));
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Serve an error page
        /// </summary>
        /// <param name="context">HTTP context</param>
        /// <param name="ErrorCode">Error code to display</param>
        /// <param name="ex">Exception data</param>
        private void HandleError(HttpListenerContext context, int ErrorCode, Exception ex)
        {
            var content = Properties.Resources.ErrorTemplate.Replace("{{number}}", ErrorCode.ToString());
            content = content.Replace("{{message}}", ex.Message);
            content = content.Replace("{{trace}}", ex.StackTrace);
            byte[] buff = Encoding.UTF8.GetBytes(content);
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.ContentType = "text/html";
            context.Response.ContentLength64 = buff.Length;
            context.Response.OutputStream.Write(buff, 0, buff.Length);
        }

        /// <summary>
        /// Request handlers list
        /// </summary>
        public List<ReqestHandler> Handlers
        {
            get { return _handlers; }
        }

        /// <summary>
        /// Server startup directory
        /// </summary>
        public string Path
        {
            get;
            set;
        }

        /// <summary>
        /// Server Log directory
        /// </summary>
        public string LogDir
        {
            get;
            set;
        }

        /// <summary>
        /// Limit memory cache size. Initialy it's 10MB
        /// </summary>
        public long CacheLimit
        {
            get;
            set;
        }


        /// <summary>
        /// Property to get AppPackages list.
        /// </summary>
        public List<string> AppPackages
        {
            get;
            private set;
        }

        /// <summary>
        /// Custom mime type database. Key is extension. Value is mime type
        /// </summary>
        public Dictionary<string, string> CustomMimeData
        {
            get;
            private set;
        }


        /// <summary>
        /// Gets the filename from an url request
        /// </summary>
        /// <param name="rawURL">raw url</param>
        /// <returns>a filename without query parameters</returns>
        public static string GetFileName(string rawURL)
        {
            var noquery = Regex.Replace(rawURL, @"\?.+", "");
            if (noquery.StartsWith("/"))
                return noquery.Substring(1, noquery.Length - 1);
            else
                return noquery;
            
        }

        private string GetMime(string file)
        {
            var extension = System.IO.Path.GetExtension(file);
            if (CustomMimeData.Keys.Contains(extension))
                return CustomMimeData[extension];
            else
                return MimeMapping.GetMimeMapping(file);
        }

        /// <summary>
        /// Hook matcher function
        /// </summary>
        /// <param name="hooks">Hooks to text. Can contain regular expressions</param>
        /// <param name="text">text to match</param>
        /// <returns>ture if text matches to any of the hooks, false if text doesn't mach any hooks</returns>
        private bool MathesHook(string[] hooks, string text)
        {
            foreach (var i in hooks)
            {
                if (Regex.IsMatch(text, i)) return true;
            }
            return false;
        }

        /// <summary>
        /// Starts the server
        /// </summary>
        public void Start()
        {
            _log.Start(LogDir);
            _log.Info("Cacheing directory", Path);
            RebuildCache();
            _log.Info("Processing App packages");
            ProcessAppPackages();
            _listener.Start();
            _log.Info("Server started.", "Prefix", _listener.Prefixes.First());
            //threaded serve function
            ThreadPool.QueueUserWorkItem((o) =>
            {
                try
                {
                    while (_listener.IsListening)
                    {
                        ThreadPool.QueueUserWorkItem((c) =>
                        {
                            var ctx = c as HttpListenerContext;
                            try
                            {
                                var file = GetFileName(ctx.Request.RawUrl);
                                if (string.IsNullOrEmpty(file)) file = "index.html";


                                var handler = (from i in _handlers where MathesHook(i.Hooks, file) select i).FirstOrDefault();

                                if (handler != null)
                                {
                                    _log.Info("Handling reqest by custom handler", handler, ctx.Request.RawUrl);
                                    handler.HandleReqest(ctx);
                                }
                                else
                                {
                                    if (_cache.ContainsKey(file))
                                    {
                                        byte[] data = _cache[file];
                                        ctx.Response.StatusCode = 200;
                                        ctx.Response.ContentLength64 = data.Length;
                                        ctx.Response.ContentType = GetMime(file);
                                        ctx.Response.OutputStream.Write(data, 0, data.Length);
                                        _log.Info("Handling reqest from cache", ctx.Request.RawUrl);
                                    }
                                    else
                                    {
                                        var f = Path + "\\" + FormatPath(file);
                                        if (Directory.Exists(f)) f += "\\index.html";

                                        if (File.Exists(f))
                                        {
                                            using (var fs = File.OpenRead(f))
                                            {
                                                ctx.Response.StatusCode = 200;
                                                ctx.Response.ContentType = MimeMapping.GetMimeMapping(f);
                                                ctx.Response.ContentLength64 = fs.Length;
                                                fs.CopyTo(ctx.Response.OutputStream);
                                            }
                                            _log.Info("Handling reqest from disk", ctx.Request.RawUrl);
                                        }
                                        else
                                        {
                                            _log.Error("File doesn't exist error", ctx.Request.RawUrl);
                                            HandleError(ctx, 404, new Exception(ctx.Request.RawUrl));
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _log.Error("Error handling request", ctx.Request.RawUrl, ex.Message);
                                HandleError(ctx, 500, ex);
                            }
                            finally
                            {
                                ctx.Response.OutputStream.Close();
                            }
                        }, _listener.GetContext());
                    }
                }
                catch (Exception ex) 
                {
                    _log.Error("Server Error", ex.Message);
                }
            });
        }

        /// <summary>
        /// Stops the server
        /// </summary>
        public void Stop()
        {
            _listener.Stop();
            _listener.Close();
            _log.Info("Server stopped");
            _log.Stop();
        }

        /// <summary>
        /// Gets a cached file contents as a bye array
        /// </summary>
        /// <param name="file">File to get</param>
        /// <returns>a byte array or null, if the file is not found in the cache</returns>
        public byte[] GetCachedFile(string file)
        {
            if (_cache.ContainsKey(file))
            {
                return _cache[file];
            }
            else return null;
        }

        public void Dispose()
        {
            if (_listener != null)
            {
                _listener.Close();
            }
            if (_log != null)
            {
                _log.Dispose();
                _log = null;
            }
            GC.SuppressFinalize(this);
        }
    }
}
