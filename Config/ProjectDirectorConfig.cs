using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GlobalLink.Connect.Config
{
    public class ProjectDirectorConfig
    {
        /**
         * Project director password.
         */
        public String password { get; set; }

        /**
         * [Optional] Proxy configuration
         */
        public ProxyConfig proxy { get; set; }

        /**
         * [Optional] - Response Timeout in miliseconds (Default is 3000)
         */
        public int timeOut { get; set; }

        /**
         * URL of the project director server to connect to.
         */
        public String url { get; set; }
        /**
         * Project director username.
         */
        public String username { get; set; }
        /**
         * User-agent.
         */
        public String userAgent { get; set; }

    }
}
