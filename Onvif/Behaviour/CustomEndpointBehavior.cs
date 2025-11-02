using CoreWCF.Channels;
using CoreWCF.Description;
using CoreWCF.Dispatcher;
using System;

namespace iSpyApplication.Onvif.Behaviour
{
    public class CustomEndpointBehavior(CustomMessageInspector clientInspector) : IEndpointBehavior
    {
        private CustomMessageInspector clientInspector = clientInspector;

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            clientRuntime.ClientMessageInspectors.Add(new CustomMessageInspector(new OnvifClientFactory()));
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
        }

        public void Validate(ServiceEndpoint endpoint)
        {
        }
    }
}
