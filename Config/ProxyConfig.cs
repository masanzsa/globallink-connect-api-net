using System;

namespace GlobalLink.Connect.Config
{
    public class ProxyConfig
    {
        /**
         * Proxy server IP address or hostname
         */
        public String proxyHost { get; set; }
        /**
         * [Optional] - Proxy user password
         */
        public String proxyPassword { get; set; }
        /**
         * Proxy server port
         */
        public int proxyPort { get; set; }
        /**
         * [Optional] - Proxy user
         */
        public String proxyUser { get; set; }

    }
}
