using CoreWCF;
using CoreWCF.Channels;
using CoreWCF.Dispatcher;
using System;
using Message = CoreWCF.Channels.Message;

namespace iSpyApplication.Onvif.Behaviour
{
    public class CustomMessageInspector(IOnvifClientFactory factory) : IClientMessageInspector
    {
        private readonly IOnvifClientFactory _factory = factory;

        public void AfterReceiveReply(ref Message reply, object correlationState)
        {
        }

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            return null;
        }
    }
}
