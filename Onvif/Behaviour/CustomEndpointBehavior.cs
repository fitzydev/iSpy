using CoreWCF.Channels;
using CoreWCF.Description;
using CoreWCF.Dispatcher;
using System;

namespace iSpy.Onvif.Behaviour
{
    public class CustomEndpointBehavior : IEndpointBehavior
    {
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
