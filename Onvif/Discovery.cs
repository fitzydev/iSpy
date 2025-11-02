using iSpyApplication.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace iSpyApplication.Onvif
{
    public class Discovery
    {
        public static async Task<List<DiscoveryData>> Discover()
        {
            var devices = new List<DiscoveryData>();
      
            try
            {
                var discoveredDevices = await Discovery.Discover(5);

                foreach (var device in discoveredDevices)
                {
                    if (device.XAdresses.Any())
                    {
                        devices.Add(new DiscoveryData { 
                            XAddr = device.XAdresses.First().ToString(),
                            Address = device.Address
                        });
                    }
                }
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
        public System.Net.IPAddress Address { get; set; }
    }
}
