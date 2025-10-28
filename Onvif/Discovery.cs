using CoreWCF;
using CoreWCF.Discovery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace iSpy.Onvif
{
    public class Discovery
    {
        public static List<DiscoveryData> Discover()
        {
            var devices = new List<DiscoveryData>();
      
            try
            {
                var discoveryClient = new DiscoveryClient(new UdpDiscoveryEndpoint());
 
                var findCriteria = new FindCriteria
                {
                    Duration = TimeSpan.FromSeconds(3),
                    MaxResults = 50
                };

                findCriteria.ContractTypeNames.Add(new Uri("http://www.onvif.org/ver10/device/wsdl"));

                var findResponse = discoveryClient.Find(findCriteria);
           
                foreach (var endpoint in findResponse.Endpoints)
                {
                    var xaddr = endpoint.Address.Uri.ToString();
                    if (!string.IsNullOrEmpty(xaddr))
                    {
                        devices.Add(new DiscoveryData { XAddr = xaddr });
                    }
                }

                discoveryClient.Close();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return devices;
        }
    }

    public class DiscoveryData
    {
        public string XAddr { get; set; }
    }
}
