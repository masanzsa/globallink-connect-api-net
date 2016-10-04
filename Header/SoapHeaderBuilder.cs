using System.Xml.Linq;

namespace GlobalLink.Connect
{
    public static class SoapHeaderBuilder
    {
        public static XElement CreateHeader(UsernameToken usernameToken)
        {
            XNamespace wsuNs = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";
            XNamespace wsseNs = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";

            var xml =
                new XElement(wsseNs + "UsernameToken",
                    new XAttribute(wsuNs + "id", usernameToken.Id),
                    new XElement(wsseNs + "Username", usernameToken.Username),
                    new XElement(wsseNs + "Password", usernameToken.Password));

            return xml;
        }
    }
}