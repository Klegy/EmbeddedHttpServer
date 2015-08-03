using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Net.EmbeddedHttp
{
    /// <summary>
    /// Request handler base class
    /// </summary>
    public abstract class ReqestHandler
    {
        private List<string> _hooks;

        public ReqestHandler()
        {
            _hooks = new List<string>();
        }

        /// <summary>
        /// Handler hooks. Eg: /something
        /// </summary>
        public string[] Hooks
        {
            get
            {
                return _hooks.ToArray();
            }
        }

        /// <summary>
        /// Registers handler hooks
        /// </summary>
        /// <param name="hooks">hooks to register</param>
        public void RegisterHooks(params string[] hooks)
        {
            _hooks.AddRange(hooks);
        }

        /// <summary>
        /// Request handler function. Override this in child classes to get extra functionality
        /// </summary>
        /// <param name="context">Request context</param>
        public abstract void HandleReqest(HttpListenerContext context);
    }
}
