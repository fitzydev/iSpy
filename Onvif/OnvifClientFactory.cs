using CoreWCF;
using CoreWCF.Channels;
using System;
using System.ServiceModel;

namespace iSpy.Onvif
{
    public class OnvifClientFactory : IOnvifClientFactory
    {
        public T CreateClient<T>(string uri, string username = "", string password = "")
        {
            var binding = new BasicHttpBinding
            {
                MaxBufferSize = int.MaxValue,
                ReaderQuotas = { MaxDepth = 32 },
                MaxReceivedMessageSize = int.MaxValue,
                AllowCookies = true,
                Security =
                {
                    Mode = BasicHttpSecurityMode.TransportCredentialOnly,
                    Transport = { ClientCredentialType = HttpClientCredentialType.Digest }
                }
            };

            var endpoint = new EndpointAddress(uri);
            var factory = new ChannelFactory<T>(binding, endpoint);

            if (!string.IsNullOrEmpty(username))
            {
                factory.Credentials.UserName.UserName = username;
                factory.Credentials.UserName.Password = password;
            }

            return factory.CreateChannel();
        }
    }
}
