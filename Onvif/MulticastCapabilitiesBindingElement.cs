using CoreWCF.Channels;

namespace iSpyApplication.Onvif
{
    public class MulticastCapabilitiesBindingElement : BindingElement
    {
        public override BindingElement Clone()
        {
            return new MulticastCapabilitiesBindingElement();
        }

        public override T GetProperty<T>(BindingContext context) where T : class
        {
            return context.GetInnerProperty<T>();
        }

        public override bool CanBuildChannelFactory<TChannel>(BindingContext context)
        {
            return context.CanBuildInnerChannelFactory<TChannel>();
        }

        public override IChannelFactory<TChannel> BuildChannelFactory<TChannel>(BindingContext context)
        {
            return context.BuildInnerChannelFactory<TChannel>();
        }
    }
}