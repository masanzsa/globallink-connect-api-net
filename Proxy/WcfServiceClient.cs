using System.ServiceModel;
using GlobalLink.Connect.Header;
using GlobalLink.Connect.Proxy;
using System.ServiceModel.Channels;
using GlobalLink.Connect;

namespace GlobalLink.Connect
{
    public delegate void Execute();

    public class WcfServiceClient<T> : ClientBase<T> where T : class
    {
        private readonly UsernameToken _usernameToken;
        
        public T Service
        { 
            get
            {  
                return Channel;
            }
        }

        public WcfServiceClient(UsernameToken usernameToken)
        {
            _usernameToken = usernameToken;
        }

        public WcfServiceClient(string endpointConfigurationName, UsernameToken usernameToken) : base(endpointConfigurationName)
        {
            _usernameToken = usernameToken;
        }

        public WcfServiceClient(Binding binding, EndpointAddress endpointAddress, UsernameToken usernameToken) :
            base(binding, endpointAddress)
        {
            _usernameToken = usernameToken;
        }

        public void CallServiceMethod(Execute execute)
        {
            base.ClientCredentials.UserName.UserName = _usernameToken.Username;
            base.ClientCredentials.UserName.Password = _usernameToken.Password;
            
            this.Endpoint.Behaviors.Add(new MessageViewerInspector(_usernameToken));

            execute();
        }
    }
}