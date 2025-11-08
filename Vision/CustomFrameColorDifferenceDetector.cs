using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace iSpyApplication.Vision
{
    /// <summary>
    /// Motion detector based on difference with predefined background frame (Color).
    /// </summary>
    public class CustomFrameColorDifferenceDetector : IMotionDetector
    {
        private int _width;
        private int _height;

        private Mat _backgroundFrame; // BGR
        private Mat _motionFrame; // Grayscale
        private int _pixelsChanged;

        private bool _manuallySetBackgroundFrame;
        private bool _suppressNoise = true;
        private int _differenceThreshold = 15;

        private readonly Mat _structuringElement = CvInvoke.GetStructuringElement(MorphShapes.Rectangle, new System.Drawing.Size(3, 3), new System.Drawing.Point(-1, -1));
        private readonly object _sync = new object();

        public int DifferenceThreshold
        {
            get { return _differenceThreshold; }
            set
            {
                lock (_sync)
                {
                    _differenceThreshold = Math.Max(1, Math.Min(255, value));
                }
            }
        }

        public float MotionLevel
        {
            get
            {
                lock (_sync)
                {
                    if (_width == 0 || _height == 0) return 0;
                    return (float)_pixelsChanged / (_width * _height);
                }
            }
        }

        public Mat MotionFrame
        {
            get
            {
                lock (_sync)
                {
                    return _motionFrame;
                }
            }
        }

        public bool SuppressNoise
        {
            get { return _suppressNoise; }
            set
            {
                lock (_sync)
                {
                    _suppressNoise = value;
                }
            }
        }

        // public bool KeepObjectsEdges (AForge property, removed)

        public CustomFrameColorDifferenceDetector() { }

        public CustomFrameColorDifferenceDetector(bool suppressNoise)
        {
            _suppressNoise = suppressNoise;
        }

        public CustomFrameColorDifferenceDetector(bool suppressNoise, bool keepObjectEdges)
        {
            _suppressNoise = suppressNoise;
            // _keepObjectEdges = keepObjectEdges; // AForge property
        }

        public void ProcessFrame(Mat videoFrame)
        {
            lock (_sync)
            {
                if (_backgroundFrame == null)
                {
                    _width = videoFrame.Width;
                    _height = videoFrame.Height;
                    _backgroundFrame = videoFrame.Clone(); // Store color background
                    _motionFrame = new Mat(_height, _width, DepthType.Cv8U, 1);
                    _motionFrame.SetTo(new MCvScalar(0));
                    _pixelsChanged = 0;
                    return;
                }

                if ((videoFrame.Width != _width) || (videoFrame.Height != _height))
                {
                    Reset();
                    _width = videoFrame.Width;
                    _height = videoFrame.Height;
                    _backgroundFrame = videoFrame.Clone();
                    _motionFrame = new Mat(_height, _width, DepthType.Cv8U, 1);
                    _motionFrame.SetTo(new MCvScalar(0));
                    _pixelsChanged = 0;
                    return;
                }

                // 1 - get difference between frames
                using (Mat diffFrame = new Mat())
                {
                    CvInvoke.AbsDiff(videoFrame, _backgroundFrame, diffFrame);

                    // 2 - Convert to grayscale
                    CvInvoke.CvtColor(diffFrame, _motionFrame, ColorConversion.Bgr2Gray);
                }

                // 3 - threshold the difference
                CvInvoke.Threshold(_motionFrame, _motionFrame, _differenceThreshold, 255, ThresholdType.Binary);

                if (_suppressNoise)
                {
                    CvInvoke.Erode(_motionFrame, _motionFrame, _structuringElement, new Point(-1, -1), 1, BorderType.Default, new MCvScalar(0));
                }

                _pixelsChanged = CvInvoke.CountNonZero(_motionFrame);
            }
        }

        public void Reset()
        {
            Reset(false);
        }

        private void Reset(bool force)
        {
            lock (_sync)
            {
                if ((_backgroundFrame != null) && ((force) || (!_manuallySetBackgroundFrame)))
                {
                    _backgroundFrame.Dispose();
                    _backgroundFrame = null;
                }
                _motionFrame?.Dispose();
                _motionFrame = null;
                _pixelsChanged = 0;
            }
        }

        public void SetBackgroundFrame(Bitmap backgroundFrame)
        {
            using (Mat mat = backgroundFrame.ToMat())
            {
                SetBackgroundFrame(mat);
            }
        }

        public void SetBackgroundFrame(Mat backgroundFrame)
        {
            Reset(true);
            lock (_sync)
            {
                _width = backgroundFrame.Width;
                _height = backgroundFrame.Height;
                _backgroundFrame = backgroundFrame.Clone(); // Store color frame
                _manuallySetBackgroundFrame = true;
            }
        }
    }
}