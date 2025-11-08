using iSpyApplication.Server;
using iSpyApplication.Utilities;
// --- Correct SharpOnvif Usings (based on the README) ---
using SharpOnvifClient;
using SharpOnvifClient.Media;
using SharpOnvifClient.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ServiceModel; // For EndpointAddress
using System.ServiceModel.Description; // For IEndpointBehavior
using System.Threading;
using System.Threading.Tasks;

// --- Removed old/conflicting usings ---
// using SharpOnvifCommon;
// using SharpOnvifServer;
// using OnvifDiscovery;


namespace iSpyApplication.CameraDiscovery
{
    public class CameraScanner
    {
        public Thread Urlscanner;
        private static readonly ManualResetEvent Finished = new ManualResetEvent(false);
        public string Make, Model;
        public string Username, Password;
        public int Channel;
        public Uri Uri;
        private volatile bool _quit;
        private List<Uri> _lp = new List<Uri>();

        public event EventHandler ScanComplete;
        public event EventHandler URLScan;
        public event EventHandler<ConnectionOptionEventArgs> URLFound;

        private URLDiscovery _discoverer;

        public void ScanCamera(ManufacturersManufacturer mm)
        {
            Stop();
            var l = new List<ManufacturersManufacturer>();
            if (mm != null)
                l.Add(mm);
            else
            {
                //scan all
                l.AddRange(MainForm.Sources);
            }
            _lp = new List<Uri>();
            _quit = false;
            Finished.Reset();

            Urlscanner = new Thread(async () => await ListCameras(l, Model));
            Urlscanner.Start();
        }

        public void Stop()
        {
            if (Running)
            {
                _quit = true;
                Finished.WaitOne(4000);
                _lp.Clear();
            }
        }

        public bool Running => Helper.ThreadRunning(Urlscanner);

        private async Task ListCameras(IEnumerable<ManufacturersManufacturer> mm, string model)
        {
            model = (model ?? "").ToLowerInvariant();

            _discoverer = new URLDiscovery(Uri);

            // --- Part 1: ONVIF Discovery (Corrected with SharpOnvif) ---
            try
            {
                var discoveredDevices = await OnvifDiscoveryClient.DiscoverAsync();
                string onvifUrl = null;

                foreach (var d in discoveredDevices)
                {
                    var deviceUri = d.Addresses.Select(uri => new Uri(uri)).FirstOrDefault();
                    if (deviceUri != null && deviceUri.DnsSafeHost == Uri.DnsSafeHost)
                    {
                        onvifUrl = d.Addresses.First(); // Found the device's ONVIF XAddr
                        break;
                    }
                }

                if (onvifUrl != null && !_quit)
                {
                    // --- This block is now based on the README ---
                    var credentials = new NetworkCredential(Username, Password);
                    var binding = OnvifBindingFactory.CreateBinding();
                    var endpoint = new EndpointAddress(onvifUrl);

                    // Create the specific legacy behavior for WsUsernameToken
                    IEndpointBehavior legacyAuth = new WsUsernameTokenBehavior(credentials);

                    // We need a MediaClient to get profiles
                    using (var mediaClient = new MediaClient(binding, endpoint))
                    {
                        // Set authentication using the 3-argument method
                        // FIX: Use 'WsUsernameToken' (lowercase 's')
                        mediaClient.SetOnvifAuthentication(
                            OnvifAuthentication.WsUsernameToken | OnvifAuthentication.HttpDigest,
                            credentials,
                            legacyAuth);

                        // Call GetProfiles
                        var profilesResponse = await mediaClient.GetProfilesAsync();

                        if (profilesResponse?.Profiles != null)
                        {
                            for (int profileIndex = 0; profileIndex < profilesResponse.Profiles.Length; profileIndex++)
                            {
                                var p = profilesResponse.Profiles[profileIndex];
                                if (p?.VideoEncoderConfiguration?.Resolution != null && p.VideoEncoderConfiguration.Resolution.Width > 0)
                                {
                                    // The "9" is iSpy's internal ID for an ONVIF source
                                    var co = new ConnectionOption(onvifUrl, null, 9, -1, null)
                                    {
                                        MediaIndex = profileIndex // Pass the profile index
                                    };
                                    URLFound?.Invoke(this, new ConnectionOptionEventArgs(co));
                                }
                            }
                        }
                    }
                    // --- End README-based block ---
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "SharpOnvif Discovery");
            }

            if (_quit)
            {
                Finished.Set();
                return;
            }

            // --- Part 2: Legacy URL Scan (Unchanged) ---
            foreach (var m in mm)
            {
                var cand = m.url.Where(p => p.version.ToLowerInvariant() == model).ToList();
                Scan(cand);
                cand = m.url.Where(p => p.version.ToLowerInvariant() != model).ToList();
                Scan(cand);
                if (_quit)
                    break;
            }

            ScanComplete?.Invoke(this, EventArgs.Empty);
            Finished.Set();
        }

        private void Scan(List<ManufacturersManufacturerUrl> cand)
        {
            if (_quit || cand.Count == 0)
                return;

            var un = Uri.EscapeDataString(Username);
            var pwd = Uri.EscapeDataString(Password);

            foreach (var s in cand)
            {
                Uri audioUri = null;
                int audioSourceTypeID = -1;
                var addr = _discoverer.GetAddr(s, Channel, un, pwd);
                if (addr != null && !_lp.Contains(addr))
                {
                    _lp.Add(addr);
                    URLScan?.Invoke(addr, EventArgs.Empty);
                    bool found = _discoverer.TestAddress(addr, s, un, pwd);
                    if (found)
                    {
                        if (!string.IsNullOrEmpty(s.AudioSource))
                        {
                            audioUri = _discoverer.GetAddr(s, Channel, un, pwd, true);
                            audioSourceTypeID = Helper.GetSourceType(s.AudioSource, 1);
                        }
                        ManufacturersManufacturerUrl s1 = s;

                        URLFound?.Invoke(this,
                            new ConnectionOptionEventArgs(new ConnectionOption(addr, audioUri,
                                Helper.GetSourceType(s1.Source, 2), audioSourceTypeID, s1)));
                    }
                }

                if (_quit)
                    return;
            }
        }
    }

    public class ConnectionOptionEventArgs : EventArgs
    {
        public ConnectionOption Co;

        public ConnectionOptionEventArgs(ConnectionOption co)
        {
            Co = co;
        }
    }
}