using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Xml.Linq;

namespace GlobalLink.Connect.Header
{
    public class MessageViewerInspector : IEndpointBehavior, IClientMessageInspector
    {
        #region Private Fields
        private readonly UsernameToken _usernameToken;
        #endregion Private Fields

        #region Constructors
        public MessageViewerInspector(UsernameToken usernameToken)
        {
            _usernameToken = usernameToken;
        }
        #endregion Constructors
        
        #region Properties
        public string RequestMessage { get; set; }
        public string ResponseMessage { get; set; }
        #endregion Properties

        #region IEndpointBehavior Members
        public void AddBindingParameters(ServiceEndpoint endpoint, System.ServiceModel.Channels.BindingParameterCollection bindingParameters)
        {

        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            clientRuntime.MessageInspectors.Add(this);
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {

        }

        public void Validate(ServiceEndpoint endpoint)
        {

        }
        #endregion

        #region IClientMessageInspector Members
        void IClientMessageInspector.AfterReceiveReply(ref System.ServiceModel.Channels.Message reply, object correlationState)
        {
            this.ResponseMessage = reply.ToString();
        }

        object IClientMessageInspector.BeforeSendRequest(ref System.ServiceModel.Channels.Message request, IClientChannel channel)
        {
            XElement objectValue = SoapHeaderBuilder.CreateHeader(_usernameToken);
            MessageHeader header = MessageHeader.CreateHeader("Security", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd", objectValue, true);
            MessageHeader userAgentHeader = MessageHeader.CreateHeader("userAgent", "http://commons.ws.projectdirector.gs4tr.org", _usernameToken.UserAgent);

            request.Headers.Clear();
            request.Headers.Add(userAgentHeader);
            request.Headers.Add(header);
            
            this.RequestMessage = request.ToString();
            return null;
        }
        #endregion
    }
}