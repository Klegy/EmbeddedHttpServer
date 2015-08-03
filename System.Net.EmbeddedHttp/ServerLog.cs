using System.IO;
using System.Text;
using System.Timers;

namespace System.Net.EmbeddedHttp
{
    /// <summary>
    /// Log event type enumeration
    /// </summary>
    public enum EventType
    {
        /// <summary>
        /// Informational events
        /// </summary>
        Info,
        
        /// <summary>
        /// Error Events
        /// </summary>
        Error,
        
        /// <summary>
        /// Warning events
        /// </summary>
        Warning,

        /// <summary>
        /// Other events that don't fit into any of the categories.
        /// </summary>
        Other
    }

    /// <summary>
    /// Logs server messages to a directory in CSV format.
    /// </summary>
    public class ServerLog: IDisposable
    {
        private StringBuilder _buffer;
        private Timer _writetimer;
        private string _todayfile;

        /// <summary>
        /// Creates a new Instance of the ServerLog
        /// </summary>
        public ServerLog()
        {
            _buffer = new StringBuilder(4096);
            _writetimer = new Timer();
            _writetimer.Interval = 5000;
            _writetimer.Elapsed += _writetimer_Elapsed;
        }

        /// <summary>
        /// Writes the cache to the current log file
        /// </summary>
        private void WriteCache()
        {
            try
            {
                StreamWriter sw;
                if (File.Exists(_todayfile)) sw = File.AppendText(_todayfile);
                else sw = File.CreateText(_todayfile);
                sw.Write(_buffer);
                _buffer.Clear();
                sw.Close();
            }
            catch (IOException) { }
        }

        private void _writetimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_buffer.Length > 0) WriteCache();
        }

        /// <summary>
        /// Starts the log service
        /// </summary>
        public void Start(string TargetPath)
        {
            if (!Directory.Exists(TargetPath))
            {
                try
                {
                    Directory.CreateDirectory(TargetPath);
                }
                catch (Exception)
                {
                    throw new ArgumentException("TargetPath doesn't exist and can't be created");
                }
            }
            var todayfile = string.Format("{0}-{1}-{2}.csv", DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
            _todayfile = Path.Combine(TargetPath, todayfile);
            _writetimer.Start();
        }

        /// <summary>
        /// Stops the log service
        /// </summary>
        public void Stop()
        {
            _writetimer.Stop();
            if (_buffer.Length > 0) WriteCache();
        }

        /// <summary>
        /// Logs an event
        /// </summary>
        /// <param name="level">Event level</param>
        /// <param name="message">Event Message</param>
        /// <param name="pars">Event parameters (optional)</param>
        public void Log(EventType level, string message, params object[] pars)
        {
            _buffer.AppendFormat("{0};{1};{2};{3};{4}\r\n", DateTime.Now.ToString("yyyy-MM-dd"), DateTime.Now.ToString("HH:mm:ss"), level, message, string.Join(";", pars));
            if (_buffer.Length > 4096) WriteCache();
        }

        /// <summary>
        /// Logs an Error message
        /// </summary>
        /// <param name="message">Error message text</param>
        /// <param name="pars">Error event parameters (optional)</param>
        public void Error(string message, params object[] pars)
        {
            Log(EventType.Error, message, pars);
        }

        /// <summary>
        /// Logs an Info message
        /// </summary>
        /// <param name="message">Info message</param>
        /// <param name="pars">Info event parameters (optional)</param>
        public void Info(string message, params object[] pars)
        {
            Log(EventType.Info, message, pars);
        }

        /// <summary>
        /// Logs a warning message
        /// </summary>
        /// <param name="message">Warning message</param>
        /// <param name="pars">Warning event parameters (optional)</param>
        public void Warning(string message, params object[] pars)
        {
            Log(EventType.Warning, message, pars);
        }

        /// <summary>
        /// Logs an other type message
        /// </summary>
        /// <param name="message">Message to log</param>
        /// <param name="pars">Message parameters (optional)</param>
        public void Other(string message, params object[] pars)
        {
            Log(EventType.Other, message, pars);
        }

        /// <summary>
        /// Overrideable version of dispose code. Override this function is child classes
        /// </summary>
        /// <param name="native">free native resources or not.</param>
        protected virtual void Dispose(bool native)
        {
            if (_writetimer != null)
            {
                _writetimer.Dispose();
                _writetimer = null;
            }
        }

        /// <summary>
        /// IDisposeable implementation
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
