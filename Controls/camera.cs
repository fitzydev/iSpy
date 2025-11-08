using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using iSpyApplication.Sources;
using iSpyApplication.Sources.Video;
using iSpyApplication.Utilities;
using iSpyApplication.Vision; // Still needed for FishEyeCorrect class logic
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util; // For VectorOfVectorOfPoint
using Image = System.Drawing.Image;
using Point = System.Drawing.Point;

namespace iSpyApplication.Controls
{
    /// <summary>
    /// Camera class
    /// </summary>
    public class Camera : IDisposable
    {
        public CameraWindow CW;
        public bool MotionDetected;

        private bool _motionRecentlyDetected;

        public bool MotionRecentlyDetected
        {
            get
            {
                bool b = _motionRecentlyDetected;
                _motionRecentlyDetected = false;
                return MotionDetected || b;
            }
        }

        public float MotionLevel;

        public Rectangle[] MotionZoneRectangles;
        public IVideoSource VideoSource;
        public double Framerate;
        public double RealFramerate;
        private Queue<double> _framerates;

        private readonly object _sync = new object();

        // --- Start of Emgu.CV Replacements ---
        private MotionDetector _motionDetector;
        private Mat _fgMask = new Mat(); // A re-usable mask for motion detection
        private readonly FishEyeCorrect _feCorrect = new FishEyeCorrect(); // Assuming FishEyeCorrect is a helper class you have
        private readonly Mat _fisheyeMap1 = new Mat();
        private readonly Mat _fisheyeMap2 = new Mat();
        private bool _fisheyeMapsInitialized = false;
        // --- End of Emgu.CV Replacements ---

        private DateTime _motionlastdetected = DateTime.MinValue;

        // alarm level
        private double _alarmLevel = 0.0005;

        private double _alarmLevelMax = 1;
        private int _height = -1;
        public DateTime LastFrameEvent = DateTime.MinValue;

        private int _width = -1;
        private bool _pluginTrigger;
        private Brush _foreBrush, _backBrush;
        private Font _drawfont;

        //digital controls
        public float ZFactor = 1;

        private Point _zPoint = Point.Empty;

        public Point ZPoint
        {
            get { return _zPoint; }
            set
            {
                if (value.X < 0)
                    value.X = 0;
                if (value.Y < 0)
                    value.Y = 0;
                if (value.X > Width)
                    value.X = Width;
                if (value.Y > Height)
                    value.Y = Height;
                _zPoint = value;
            }
        }

        public event Delegates.ErrorHandler ErrorHandler;

        // AForge HSL Filtering is removed. This would need to be re-implemented with CvInvoke.InRange
        /*public HSLFiltering Filter ... */

        internal Rectangle ViewRectangle
        {
            get
            {
                int newWidth = Convert.ToInt32(Width / ZFactor);
                int newHeight = Convert.ToInt32(Height / ZFactor);

                int left = ZPoint.X - newWidth / 2;
                int top = ZPoint.Y - newHeight / 2;
                int right = ZPoint.X + newWidth / 2;
                int bot = ZPoint.Y + newHeight / 2;

                if (left < 0)
                {
                    right += (0 - left);
                    left = 0;
                }
                if (right > Width)
                {
                    left -= (right - Width);
                    right = Width;
                }
                if (top < 0)
                {
                    bot += (0 - top);
                    top = 0;
                }
                if (bot > Height)
                {
                    top -= (bot - Height);
                    bot = Height;
                }

                return new Rectangle(left, top, right - left, bot - top);
            }
        }

        private object _plugin;

        public object Plugin
        {
            get
            {
                if (_plugin == null)
                {
                    foreach (string p in MainForm.Plugins)
                    {
                        if (p.EndsWith("\\" + CW.Camobject.alerts.mode + ".dll", StringComparison.CurrentCultureIgnoreCase))
                        {
                            Assembly ass = Assembly.LoadFrom(p);
                            Plugin = ass.CreateInstance("Plugins.Main", true);
                            if (_plugin != null)
                            {
                                Type o = null;
                                try
                                {
                                    o = _plugin.GetType();
                                    if (o.GetProperty("WorkingDirectory") != null)
                                        o.GetProperty("WorkingDirectory").SetValue(_plugin, Program.AppDataPath, null);
                                    if (o.GetProperty("VideoSource") != null)
                                        o.GetProperty("VideoSource")
                                            .SetValue(_plugin, CW.Camobject.settings.videosourcestring, null);
                                    if (o.GetProperty("Configuration") != null)
                                        o.GetProperty("Configuration")
                                            .SetValue(_plugin, CW.Camobject.alerts.pluginconfig, null);
                                    if (o.GetProperty("Groups") != null)
                                    {
                                        string groups =
                                            MainForm.Conf.Permissions.Aggregate("",
                                                (current, g) => current + (g.name + ",")).Trim(',');
                                        o.GetProperty("Groups").SetValue(_plugin, groups, null);
                                    }
                                    if (o.GetProperty("Group") != null)
                                        o.GetProperty("Group").SetValue(_plugin, MainForm.Group, null);

                                    if (o.GetMethod("LoadConfiguration") != null)
                                        o.GetMethod("LoadConfiguration").Invoke(_plugin, null);

                                    if (o.GetProperty("DeviceList") != null)
                                    {
                                        //used for network kinect setting syncing
                                        string dl = "";

                                        //build a pipe and star delimited string of all cameras that are using the kinect plugin
                                        // ReSharper disable once LoopCanBeConvertedToQuery
                                        foreach (var oc in MainForm.Cameras)
                                        {
                                            string s = oc.settings.namevaluesettings;
                                            if (!string.IsNullOrEmpty(s))
                                            {
                                                //we're only looking for ispykinect devices
                                                if (
                                                    s.ToLower()
.Contains("custom=network kinect"))
                                                {
                                                    dl += oc.name.Replace("*", "").Replace("|", "") + "|" + oc.id + "|" +
                                                          oc.settings.videosourcestring + "*";
                                                }
                                            }
                                        }
                                        //the ispykinect plugin takes this delimited list and uses it for copying settings
                                        if (!string.IsNullOrEmpty(dl))
                                            o.GetProperty("DeviceList").SetValue(_plugin, dl, null);
                                    }

                                    if (o.GetProperty("CameraName") != null)
                                        o.GetProperty("CameraName").SetValue(_plugin, CW.Camobject.name, null);

                                    var l = o.GetMethod("LogExternal");
                                    l?.Invoke(_plugin, ["Plugin Initialised"]);
                                }
                                catch (Exception ex)
                                {
                                    //config corrupted
                                    ErrorHandler?.Invoke("Error configuring plugin - trying with a blank configuration (" +
                                                         ex.Message + ")");

                                    try
                                    {
                                        CW.Camobject.alerts.pluginconfig = "";
                                        if (o?.GetProperty("Configuration") != null)
                                        {
                                            o.GetProperty("Configuration").SetValue(_plugin, "", null);
                                        }
                                    }
                                    catch
                                    {
                                        //ignore error
                                    }
                                }
                            }
                            break;
                        }
                    }
                }
                return _plugin;
            }
            set
            {
                if (_plugin != null)
                {
                    try { _plugin.GetType().GetMethod("Dispose")?.Invoke(_plugin, null); }
                    catch
                    {
                        // ignored
                    }
                }
                _plugin = value;
            }
        }

        public void FilterChanged()
        {
            lock (_sync)
            {
                //_filter = null; // AForge filter removed
            }
        }

        public Camera() : this(null, null)
        {
        }

        public Camera(IVideoSource source)
        {
            VideoSource = source;
            _motionDetector = null;
            VideoSource.NewFrame += VideoNewFrame;
        }

        public Camera(IVideoSource source, MotionDetector detector) // Changed from BackgroundSubtractorMOG2
        {
            VideoSource = source;
            _motionDetector = detector;
            VideoSource.NewFrame += VideoNewFrame;
        }

        // Running property
        public bool IsRunning
        {
            get
            {
                var v = VideoSource;
                return (v != null) && v.IsRunning;
            }
        }

        public void Restart()
        {
            VideoSource?.Restart();
        }

        // Width property
        public int Width => _width;

        // Height property
        public int Height => _height;

        // AlarmLevel property
        public double AlarmLevel
        {
            get { return _alarmLevel; }
            set { _alarmLevel = value; }
        }

        // AlarmLevel property
        public double AlarmLevelMax
        {
            get { return _alarmLevelMax; }
            set { _alarmLevelMax = value; }
        }

        // motionDetector property - updated for Emgu.CV
        public MotionDetector MotionDetector
        {
            get { return _motionDetector; }
            set
            {
                _motionDetector = value;
                if (value != null) _motionDetector.MotionZones = MotionZoneRectangles;
            }
        }

        public Bitmap Mask { get; set; }

        public bool SetMotionZones(objectsCameraDetectorZone[] zones)
        {
            if (zones == null || zones.Length == 0)
            {
                ClearMotionZones();
                return true;
            }
            //rectangles come in as percentages to allow resizing and resolution changes

            if (_width > -1)
            {
                double wmulti = Convert.ToDouble(_width) / Convert.ToDouble(100);
                double hmulti = Convert.ToDouble(_height) / Convert.ToDouble(100);
                MotionZoneRectangles = zones.Select(r => new Rectangle(Convert.ToInt32(r.left * wmulti), Convert.ToInt32(r.top * hmulti), Convert.ToInt32(r.width * wmulti), Convert.ToInt32(r.height * hmulti))).ToArray();

                // We don't assign to _motionDetector.MotionZones anymore
                // This MotionZoneRectangles array will be used in ApplyMotionDetector

                return true;
            }
            return false;
        }

        public void ClearMotionZones()
        {
            MotionZoneRectangles = null;
        }

        public event NewFrameEventHandler NewFrame;

        public event EventHandler Detect;

        public event EventHandler Alert;

        public event PlayingFinishedEventHandler PlayingFinished;

        // Start video source
        public void Start()
        {
            if (VideoSource != null)
            {
                _framerates = new Queue<double>();
                LastFrameEvent = DateTime.MinValue;
                _motionRecentlyDetected = false;
                if (!CW.IsClone)
                {
                    VideoSource.PlayingFinished -= VideoSourcePlayingFinished;
                    VideoSource.PlayingFinished += VideoSourcePlayingFinished;
                    VideoSource.Start();
                }
            }
        }

        // Signal video source to stop
        public void Stop()
        {
            if (CW.IsClone)
                return;
            VideoSource?.Stop();
            _motionRecentlyDetected = false;
        }

        internal RotateFlipType RotateFlipType = RotateFlipType.RotateNoneFlipNone;

        public void DisconnectNewFrameEvent()
        {
            if (VideoSource != null)
                VideoSource.NewFrame -= VideoNewFrame;
        }

        private bool _updateResources = true;

        public void UpdateResources()
        {
            _updateResources = true;
        }

        private void SetMaskImage()
        {
            var p = CW.Camobject.settings.maskimage;
            if (!string.IsNullOrEmpty(p))
            {
                bool abs = false;
                try
                {
                    if (File.Exists(p))
                    {
                        Mask = (Bitmap)Image.FromFile(p);
                        abs = true;
                    }
                }
                catch
                {
                    // ignored
                }
                if (abs) return;
                p = Program.AppPath + "Masks\\" + p;
                try
                {
                    if (File.Exists(p))
                    {
                        Mask = (Bitmap)Image.FromFile(p);
                    }
                }
                catch
                {
                    // ignored
                }
            }
            else
            {
                if (Mask == null) return;
                Mask.Dispose();
                Mask = null;
            }
        }

        // *** THIS IS THE MAIN REFACTORED METHOD ***
        private void VideoNewFrame(object sender, NewFrameEventArgs e)
        {
            var nf = NewFrame;
            var f = e.Frame; // This is a Bitmap

            if (nf == null || f == null)
                return;

            if (LastFrameEvent > DateTime.MinValue)
            {
                CalculateFramerates();
            }

            LastFrameEvent = Helper.Now;

            if (_updateResources)
            {
                _updateResources = false;
                DrawFont?.Dispose();
                DrawFont = null;
                ForeBrush?.Dispose();
                ForeBrush = null;
                BackBrush?.Dispose();
                BackBrush = null;
                SetMaskImage();
                RotateFlipType rft;
                if (Enum.TryParse(CW.Camobject.rotateMode, out rft))
                {
                    RotateFlipType = rft;
                }
                else
                {
                    RotateFlipType = RotateFlipType.RotateNoneFlipNone;
                }
                _fisheyeMapsInitialized = false; // Force re-init of fisheye maps
            }

            Bitmap bmResult = null;
            bool bMotion = false;

            // Mat is the Emgu.CV/OpenCV image container
            // Use ToMat() extension from Emgu.CV.Bitmap
            using (Mat mat = f.ToMat())
            {
                try
                {
                    // Handle rotation if needed
                    if (RotateFlipType != RotateFlipType.RotateNoneFlipNone)
                    {
                        // Emgu.CV uses different enums for rotation
                        if (RotateFlipType == RotateFlipType.Rotate90FlipNone)
                        {
                            CvInvoke.Rotate(mat, mat, RotateFlags.Rotate90Clockwise);
                        }
                        else if (RotateFlipType == RotateFlipType.Rotate180FlipNone)
                        {
                            CvInvoke.Rotate(mat, mat, RotateFlags.Rotate180);
                        }
                        else if (RotateFlipType == RotateFlipType.Rotate270FlipNone)
                        {
                            CvInvoke.Rotate(mat, mat, RotateFlags.Rotate90CounterClockwise);
                        }
                        else if (RotateFlipType == RotateFlipType.RotateNoneFlipX)
                        {
                            CvInvoke.Flip(mat, mat, FlipType.Horizontal);
                        }
                        else if (RotateFlipType == RotateFlipType.RotateNoneFlipY)
                        {
                            CvInvoke.Flip(mat, mat, FlipType.Vertical);
                        }
                        else if (RotateFlipType == RotateFlipType.RotateNoneFlipXY)
                        {
                            CvInvoke.Flip(mat, mat, FlipType.Both);
                        }
                    }

                    _width = mat.Width;
                    _height = mat.Height;

                    if (ZPoint == Point.Empty)
                    {
                        ZPoint = new Point(mat.Width / 2, mat.Height / 2);
                    }

                    if (CW.NeedMotionZones)
                        CW.NeedMotionZones = !SetMotionZones(CW.Camobject.detector.motionzones);

                    // Apply Mask using Emgu.CV
                    if (Mask != null)
                    {
                        using (var maskMat = Mask.ToMat())
                        using (var maskResized = new Mat())
                        {
                            // Ensure mask is the same size as the frame
                            CvInvoke.Resize(maskMat, maskResized, mat.Size);
                            // Convert mask to grayscale if it's not already
                            if (maskResized.NumberOfChannels > 1)
                                CvInvoke.CvtColor(maskResized, maskResized, ColorConversion.Bgr2Gray);

                            // Set masked pixels to black
                            mat.SetTo(new MCvScalar(0, 0, 0), maskResized);
                        }
                    }

                    // Plugin code seems to expect a Bitmap, so we'll convert back
                    if (CW.Camobject.alerts.active && Plugin != null && Detect != null)
                    {
                        Bitmap bmForPlugin;
                        using (var tempBmp = mat.ToBitmap()) // Use temp bitmap
                        {
                            bmForPlugin = (Bitmap)tempBmp.Clone(); // Clone for the plugin
                        }

                        bmResult = RunPlugin(bmForPlugin); // RunPlugin returns a new Bitmap
                        bmForPlugin.Dispose(); // Dispose the bitmap we gave to the plugin

                        // Convert the plugin's result back to a Mat for further processing
                        mat.Dispose(); // Dispose the old mat
                        bmResult.ToMat(mat); // Convert new bitmap into our mat
                        bmResult.Dispose(); // Dispose the bitmap
                    }


                    // --- Replaced AForge Motion Detector with Emgu.CV ---
                    if (_motionDetector != null)
                    {
                        bMotion = ApplyMotionDetector(mat);
                    }
                    else
                    {
                        MotionDetected = false;
                    }
                    // --- End Motion Detection ---


                    // --- Replaced AForge FishEyeCorrect with Emgu.CV ---
                    if (CW.Camobject.settings.FishEyeCorrect)
                    {
                        if (!_fisheyeMapsInitialized)
                        {
                            // Initialize fisheye correction maps (do this only once)
                            _feCorrect.Init(mat.Width, mat.Height, CW.Camobject.settings.FishEyeFocalLengthPX,
                                CW.Camobject.settings.FishEyeScale, ZPoint.X, ZPoint.Y);

                            CvInvoke.InitUndistortRectifyMap(_feCorrect.CameraMatrix, _feCorrect.DistortionCoeff, null,
                                _feCorrect.NewCameraMatrix, mat.Size, DepthType.Cv32F, 1, _fisheyeMap1, _fisheyeMap2);

                            _fisheyeMapsInitialized = true;
                        }

                        // Apply the fisheye correction
                        using (Mat tempMat = mat.Clone())
                        {
                            CvInvoke.Remap(tempMat, mat, _fisheyeMap1, _fisheyeMap2, Inter.Linear);
                        }
                    }
                    // --- End FishEye Correction ---


                    // --- Digital Zoom (ZFactor) ---
                    if (ZFactor > 1)
                    {
                        using (Mat tempMat = mat.Clone())
                        {
                            // Crop (using the existing ViewRectangle logic)
                            using (Mat cropped = new Mat(tempMat, ViewRectangle))
                            {
                                // Resize back to original size
                                CvInvoke.Resize(cropped, mat, mat.Size, 0, 0, Inter.Linear);
                            }
                        }
                    }

                    // Convert the final Mat back to a Bitmap for display
                    bmResult = mat.ToBitmap(); // Uses Emgu.CV.Bitmap extension

                    PiP(bmResult);
                    AddTimestamp(bmResult);
                }
                catch (Exception ex)
                {
                    CW.VideoSourceErrorState = true;
                    CW.VideoSourceErrorMessage = ex.Message;
                    bmResult?.Dispose();
                    return;
                }
            } // 'mat' is disposed here

            nf.Invoke(this, new NewFrameEventArgs(bmResult)); // Pass the final bitmap
            bmResult.Dispose(); // Dispose the bitmap after it's been handled

            if (bMotion)
            {
                TriggerDetect(this);
            }
        }


        private void PiP(Bitmap bmp)
        {
            //pip
            try
            {
                if (CW.Camobject.settings.pip.enabled)
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.CompositingMode = CompositingMode.SourceCopy;
                        g.CompositingQuality = CompositingQuality.HighSpeed;
                        g.PixelOffsetMode = PixelOffsetMode.Half;
                        g.SmoothingMode = SmoothingMode.None;
                        g.InterpolationMode = InterpolationMode.Default;

                        double wmulti = Convert.ToDouble(_width) / Convert.ToDouble(100);
                        double hmulti = Convert.ToDouble(_height) / Convert.ToDouble(100);

                        foreach (var pip in _piPEntries)
                        {
                            if (pip.CW != null && !pip.CW.VideoSourceErrorState)
                            {
                                var bmppip = pip.CW.LastFrame;
                                if (bmppip != null)
                                {
                                    var r = new Rectangle(Convert.ToInt32(pip.R.X * wmulti),
                                        Convert.ToInt32(pip.R.Y * hmulti), Convert.ToInt32(pip.R.Width * wmulti),
                                        Convert.ToInt32(pip.R.Height * hmulti));

                                    g.DrawImage(bmppip, r);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        private string _piPConfig = "";

        public string PiPConfig
        {
            get { return _piPConfig; }
            set
            {
                lock (_sync)
                {
                    _piPEntries = new List<PiPEntry>();
                    var cfg = value.Split('|');
                    foreach (var s in cfg)
                    {
                        if (s != "")
                        {
                            var t = s.Split(',');
                            if (t.Length == 5)
                            {
                                int cid, x, y, w, h;
                                if (int.TryParse(t[0], out cid) && int.TryParse(t[1], out x) &&
                                    int.TryParse(t[2], out y) && int.TryParse(t[3], out w) &&
                                    int.TryParse(t[4], out h))
                                {
                                    var cw = CW.MainClass.GetCameraWindow(cid);
                                    if (cw != null)
                                    {
                                        _piPEntries.Add(new PiPEntry { CW = cw, R = new Rectangle(x, y, w, h) });
                                    }
                                }
                            }
                        }
                    }
                    _piPConfig = value;
                }
            }
        }

        private List<PiPEntry> _piPEntries = new List<PiPEntry>();

        private struct PiPEntry
        {
            public CameraWindow CW;
            public Rectangle R;
        }

        private Dictionary<string, string> _tags;

        internal Dictionary<string, string> Tags
        {
            get
            {
                if (_tags == null)
                {
                    _tags = Helper.GetDictionary(this.CW.Camobject.settings.tagsnv, ';');
                }
                return _tags;
            }
            set { _tags = value; }
        }

        private void AddTimestamp(Bitmap bmp)
        {
            if (CW.Camobject.settings.timestamplocation != 0 &&
                !string.IsNullOrEmpty(CW.Camobject.settings.timestampformatter))
            {
                using (Graphics gCam = Graphics.FromImage(bmp))
                {
                    var ts = CW.Camobject.settings.timestampformatter.Replace("{FPS}",
                        $"{Framerate:F2}");
                    ts = ts.Replace("{CAMERA}", CW.Camobject.name);
                    ts = ts.Replace("{REC}", CW.Recording ? "REC" : "");
                    var c = CW.Camera;
                    ts = ts.Replace("{LEVEL}", c?.MotionLevel.ToString("0.##") ?? "");

                    if (MainForm.Tags.Count > 0)
                    {
                        var l = MainForm.Tags.ToList();
                        foreach (var t in l)
                        {
                            string sval = "";
                            if (Tags.ContainsKey(t))
                                sval = Tags[t];
                            ts = ts.Replace(t, sval);
                        }
                    }

                    var timestamp = "Invalid Timestamp";
                    try
                    {
                        timestamp = String.Format(ts,
                            DateTime.Now.AddHours(
                                Convert.ToDouble(CW.Camobject.settings.timestampoffset))).Trim();
                    }
                    catch
                    {
                        // ignored
                    }

                    var rs = gCam.MeasureString(timestamp, DrawFont).ToSize();
                    rs.Width += 5;
                    var p = new Point(0, 0);
                    switch (CW.Camobject.settings.timestamplocation)
                    {
                        case 2:
                            p.X = _width / 2 - (rs.Width / 2);
                            break;

                        case 3:
                            p.X = _width - rs.Width;
                            break;

                        case 4:
                            p.Y = _height - rs.Height;
                            break;

                        case 5:
                            p.Y = _height - rs.Height;
                            p.X = _width / 2 - (rs.Width / 2);
                            break;

                        case 6:
                            p.Y = _height - rs.Height;
                            p.X = _width - rs.Width;
                            break;
                    }
                    if (CW.Camobject.settings.timestampshowback)
                    {
                        var rect = new Rectangle(p, rs);
                        gCam.FillRectangle(BackBrush, rect);
                    }
                    gCam.DrawString(timestamp, DrawFont, ForeBrush, p);
                }
            }
        }

        // AForge auto-tracking removed. This would need to be re-implemented
        // using Emgu.CV's blob detection (CvInvoke.FindContours) on the _fgMask
        /*private void ProcessAutoTracking()
        {
            ...
        }*/

        private DateTime _lastProcessed = DateTime.MinValue;

        // *** THIS IS THE 2ND REFACTORED METHOD ***
        [HandleProcessCorruptedStateExceptions]
        private bool ApplyMotionDetector(Mat frame)
        {
            if (Detect != null)
            {
                if ((DateTime.UtcNow - _lastProcessed).TotalMilliseconds > CW.Camobject.detector.processframeinterval || CW.Calibrating)
                {
                    _lastProcessed = DateTime.UtcNow;

                    try
                    {
                        // Apply the background subtractor to get a motion mask
                        // _fgMask is a class-level Mat, so we don't 'using' it
                        _motionDetector.Apply(frame, _fgMask);

                        // Apply motion zones if they exist
                        if (MotionZoneRectangles != null && MotionZoneRectangles.Length > 0)
                        {
                            using (Mat zoneMask = new Mat(_fgMask.Size, DepthType.Cv8U, 1))
                            {
                                zoneMask.SetTo(new MCvScalar(0)); // Start with a black mask
                                // Draw white rectangles for each active zone
                                foreach (var rect in MotionZoneRectangles)
                                {
                                    CvInvoke.Rectangle(zoneMask, rect, new MCvScalar(255), -1);
                                }
                                // 'AND' the motion mask with the zone mask
                                CvInvoke.BitwiseAnd(_fgMask, zoneMask, _fgMask);
                            }
                        }

                        // Calculate motion level
                        MotionLevel = (float)CvInvoke.CountNonZero(_fgMask) / (_fgMask.Width * _fgMask.Height);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Error processing motion: " + ex.Message);
                    }

                    MotionLevel = MotionLevel * CW.Camobject.detector.gain;

                    if (MotionLevel >= _alarmLevel)
                    {
                        if (Math.Min(MotionLevel, 0.99) <= _alarmLevelMax)
                        {
                            return true;
                        }
                    }
                    else
                        MotionDetected = false;
                }
            }
            else
                MotionDetected = false;
            return false;
        }


        internal void TriggerDetect(object sender)
        {
            MotionDetected = true;
            _motionlastdetected = Helper.Now;
            _motionRecentlyDetected = true;
            var al = Detect;
            al?.BeginInvoke(sender, new EventArgs(), null, null);
        }

        internal void TriggerPlugin()
        {
            _pluginTrigger = true;
        }

        private Bitmap ResizeBmOrig(Bitmap f)
        {
            var sz = Helper.CalcResizeSize(CW.Camobject.settings.resize, f.Size,
                new System.Drawing.Size(CW.Camobject.settings.resizeWidth, CW.Camobject.settings.resizeHeight));
            if (CW.Camobject.settings.resize && f.Size != sz)
            {
                var result = new Bitmap(sz.Width, sz.Height, PixelFormat.Format24bppRgb);
                try
                {
                    using (Graphics g2 = Graphics.FromImage(result))
                    {
                        g2.CompositingMode = CompositingMode.SourceCopy;
                        g2.CompositingQuality = CompositingQuality.HighSpeed;
                        g2.PixelOffsetMode = PixelOffsetMode.Half;
                        g2.SmoothingMode = SmoothingMode.None;
                        g2.InterpolationMode = InterpolationMode.Default;
                        g2.DrawImage(f, 0, 0, result.Width, result.Height);
                    }
                    return result;
                }
                catch
                {
                    result.Dispose();
                }
            }
            if (CW.HasClones)
                return new Bitmap(f);
            return (Bitmap)f.Clone();
        }

        private void CalculateFramerates()
        {
            TimeSpan tsFr = Helper.Now - LastFrameEvent;
            _framerates.Enqueue(1000d / tsFr.TotalMilliseconds);
            if (_framerates.Count >= 30)
                _framerates.Dequeue();
            Framerate = _framerates.Average();
        }

        private void ApplyMask(Bitmap bmOrig)
        {
            // This is now handled inside VideoNewFrame with Emgu.CV
            // This method can be kept for legacy calls, but it's redundant
            using (Graphics g = Graphics.FromImage(bmOrig))
            {
                g.CompositingMode = CompositingMode.SourceOver;
                g.CompositingQuality = CompositingQuality.HighSpeed;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.SmoothingMode = SmoothingMode.None;
                g.InterpolationMode = InterpolationMode.Default;
                g.DrawImage(Mask, 0, 0, _width, _height);
            }
        }

        public volatile bool PluginRunning;

        private Bitmap RunPlugin(Bitmap bmOrig)
        {
            if (!CW.IsEnabled)
                return bmOrig;
            bool runplugin = true;
            switch (CW.Camobject.alerts.processmode)
            {
                case "motion":
                    //only run plugin if motion detected within last 3 seconds
                    runplugin = _motionlastdetected > Helper.Now.AddSeconds(-3);
                    break;

                case "trigger":
                    //only run plugin if triggered and then reset trigger
                    runplugin = _pluginTrigger;
                    _pluginTrigger = false;
                    break;
            }

            if (runplugin)
            {
                PluginRunning = true;
                var o = _plugin.GetType();

                try
                {
                    //pass and retrieve the latest bitmap from the plugin
                    bmOrig = (Bitmap)o.GetMethod("ProcessFrame").Invoke(Plugin, new object[] { bmOrig });
                }
                catch (Exception ex)
                {
                    ErrorHandler?.Invoke(ex.Message);
                }

                //check the plugin alert flag and alarm if it is set
                var pluginAlert = (string)o.GetField("Alert").GetValue(Plugin);
                if (pluginAlert != "")
                    Alert?.Invoke(pluginAlert, EventArgs.Empty);

                //reset the plugin alert flag if it supports that
                if (o.GetMethod("ResetAlert") != null)
                    o.GetMethod("ResetAlert").Invoke(_plugin, null);

                PluginRunning = false;
            }
            return bmOrig;
        }

        public Font DrawFont
        {
            get
            {
                if (_drawfont != null)
                    return _drawfont;
                _drawfont = FontXmlConverter.ConvertToFont(CW.Camobject.settings.timestampfont);
                return _drawfont;
            }
            set { _drawfont = value; }
        }

        public Brush ForeBrush
        {
            get
            {
                if (_foreBrush != null)
                    return _foreBrush;
                Color c = CW.Camobject.settings.timestampforecolor.ToColor();
                _foreBrush = new SolidBrush(Color.FromArgb(255, c.R, c.G, c.B));
                return _foreBrush;
            }
            set { _foreBrush = value; }
        }

        public Brush BackBrush
        {
            get
            {
                if (_backBrush != null)
                    return _backBrush;
                Color c = CW.Camobject.settings.timestampbackcolor.ToColor();
                _backBrush = new SolidBrush(Color.FromArgb(128, c.R, c.G, c.B));
                return _backBrush;
            }
            set { _backBrush = value; }
        }

        private bool _disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this); // Suppress finalization
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // Dispose managed resources
                ClearMotionZones();
                Detect = null;
                NewFrame = null;
                PlayingFinished = null;
                Plugin = null;

                ForeBrush?.Dispose();
                BackBrush?.Dispose();
                DrawFont?.Dispose();
                _framerates?.Clear();

                Mask?.Dispose();
                Mask = null;

                VideoSource?.Dispose();
                VideoSource = null;

                _motionDetector?.Dispose();
                _motionDetector = null;

                _fgMask?.Dispose();
                _fisheyeMap1?.Dispose();
                _fisheyeMap2?.Dispose();
            }

            // Dispose unmanaged resources (if any)

            _disposed = true;
        }

        private void VideoSourcePlayingFinished(object sender, PlayingFinishedEventArgs e)
        {
            PlayingFinished?.Invoke(sender, e);
        }
    }
}