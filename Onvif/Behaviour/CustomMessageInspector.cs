using CoreWCF.Channels;
using CoreWCF.Dispatcher;
using System;

namespace iSpy.Onvif.Behaviour
{
    public class CustomMessageInspector : IClientMessageInspector
    {
        private readonly IOnvifClientFactory _factory;

        public CustomMessageInspector(IOnvifClientFactory factory)
        {
            _factory = factory;
        }

        public void AfterReceiveReply(ref Message reply, object correlationState)
        {
        }

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            return null;
        }
    }
}
