using System;
using System.ServiceModel;
using System.Text;
using System.Xml;
using GlobalLink.Connect.Header;

namespace GlobalLink.Connect.Proxy
{
    public static class Constants
    {
        static Constants()
        {
            XmlDictionaryReaderQuotas readerQuotas = new XmlDictionaryReaderQuotas();
            readerQuotas.MaxDepth = 32;
            readerQuotas.MaxStringContentLength = int.MaxValue;
            readerQuotas.MaxArrayLength = 16384;
            readerQuotas.MaxBytesPerRead = 4096;
            readerQuotas.MaxNameTableCharCount = 16384;

            BINDING = new BasicHttpBinding();
            BINDING.MessageEncoding = WSMessageEncoding.Mtom;
            BINDING.Name = "PDServiceSoap11Binding";
            BINDING.CloseTimeout = new TimeSpan(0, 1, 0);
            BINDING.OpenTimeout = new TimeSpan(0, 1, 0);
            BINDING.ReceiveTimeout = new TimeSpan(0, 10, 0);
            BINDING.SendTimeout = new TimeSpan(0, 10, 0);
            BINDING.AllowCookies = false;
            BINDING.BypassProxyOnLocal = false;
            BINDING.HostNameComparisonMode = HostNameComparisonMode.StrongWildcard;
            BINDING.MaxBufferSize = 99999999;
            BINDING.MaxBufferPoolSize = 99999999;
            BINDING.MaxReceivedMessageSize = 99999999;
            BINDING.MessageEncoding = WSMessageEncoding.Mtom;
            BINDING.TextEncoding = Encoding.UTF8;
            BINDING.TransferMode = TransferMode.Buffered;
            BINDING.UseDefaultWebProxy = true;
            BINDING.ReaderQuotas = readerQuotas;
        }
        public static BasicHttpBinding BINDING;
    }
    public static class SecureConstants
    {
        static SecureConstants()
        {
            XmlDictionaryReaderQuotas readerQuotas = new XmlDictionaryReaderQuotas();
            readerQuotas.MaxDepth = 32;
            readerQuotas.MaxStringContentLength = int.MaxValue;
            readerQuotas.MaxArrayLength = 16384;
            readerQuotas.MaxBytesPerRead = 4096;
            readerQuotas.MaxNameTableCharCount = 16384;

            BINDING = new BasicHttpBinding();
            BINDING.MessageEncoding = WSMessageEncoding.Mtom;
            BINDING.Name = "PDServiceSoap11Binding";
            BINDING.CloseTimeout = new TimeSpan(0, 1, 0);
            BINDING.OpenTimeout = new TimeSpan(0, 1, 0);
            BINDING.ReceiveTimeout = new TimeSpan(0, 10, 0);
            BINDING.SendTimeout = new TimeSpan(0, 10, 0);
            BINDING.AllowCookies = false;
            BINDING.BypassProxyOnLocal = false;
            BINDING.HostNameComparisonMode = HostNameComparisonMode.StrongWildcard;
            BINDING.MaxBufferSize = 99999999;
            BINDING.MaxBufferPoolSize = 99999999;
            BINDING.MaxReceivedMessageSize = 99999999;
            BINDING.MessageEncoding = WSMessageEncoding.Mtom;
            BINDING.TextEncoding = Encoding.UTF8;
            BINDING.TransferMode = TransferMode.Buffered;
            BINDING.UseDefaultWebProxy = true;
            BINDING.ReaderQuotas = readerQuotas;
            BINDING.Security.Mode = BasicHttpSecurityMode.Transport;
            BINDING.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;
        }
        public static BasicHttpBinding BINDING;
    }
}
