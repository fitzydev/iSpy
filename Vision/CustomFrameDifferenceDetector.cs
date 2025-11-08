using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;


namespace iSpyApplication.Vision
{
    /// <summary>
    /// Motion detector based on difference with predefined background frame.
    /// </summary>
    public class CustomFrameDifferenceDetector : IMotionDetector
    {
        // frame's dimension
        private int _width;
        private int _height;

        // background frame
        private Mat _backgroundFrame;
        // current motion frame
        private Mat _motionFrame;
        // number of pixels changed in the new frame of video stream
        private int _pixelsChanged;

        private bool _manuallySetBackgroundFrame;

        // suppress noise
        private bool _suppressNoise = true;

        // threshold values
        private int _differenceThreshold = 15;

        // structuring element for noise suppression
        private readonly Mat _structuringElement = CvInvoke.GetStructuringElement(MorphShapes.Rectangle, new System.Drawing.Size(3, 3), new System.Drawing.Point(-1, -1));

        // dummy object to lock for synchronization
        private readonly object _sync = new object();

        /// <summary>
        /// Difference threshold value, [1, 255].
        /// </summary>
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

        /// <summary>
        /// Motion level value, [0, 1].
        /// </summary>
        public float MotionLevel
        {
            get
            {
                lock (_sync)
                {
                    if (_width == 0 || _height == 0)
                        return 0;
                    return (float)_pixelsChanged / (_width * _height);
                }
            }
        }

        /// <summary>
        /// Motion frame containing detected areas of motion.
        /// </summary>
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

        /// <summary>
        /// Suppress noise in video frames or not.
        /// </summary>
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

        public CustomFrameDifferenceDetector() { }

        public CustomFrameDifferenceDetector(bool suppressNoise)
        {
            _suppressNoise = suppressNoise;
        }

        /// <summary>
        /// Process new video frame.
        /// </summary>
        public void ProcessFrame(Mat videoFrame)
        {
            lock (_sync)
            {
                using (var grayFrame = new Mat())
                {
                    CvInvoke.CvtColor(videoFrame, grayFrame, ColorConversion.Bgr2Gray);

                    // check background frame
                    if (_backgroundFrame == null)
                    {
                        // save image dimension
                        _width = videoFrame.Width;
                        _height = videoFrame.Height;

                        _backgroundFrame = grayFrame.Clone();
                        _motionFrame = new Mat(_height, _width, DepthType.Cv8U, 1);
                        _motionFrame.SetTo(new MCvScalar(0));
                        _pixelsChanged = 0;
                        return;
                    }

                    // check image dimension
                    if ((videoFrame.Width != _width) || (videoFrame.Height != _height))
                    {
                        Reset();
                        _width = videoFrame.Width;
                        _height = videoFrame.Height;
                        _backgroundFrame = grayFrame.Clone();
                        _motionFrame = new Mat(_height, _width, DepthType.Cv8U, 1);
                        _motionFrame.SetTo(new MCvScalar(0));
                        _pixelsChanged = 0;
                        return;
                    }

                    // 1 - get difference between frames
                    CvInvoke.AbsDiff(grayFrame, _backgroundFrame, _motionFrame);
                    // 2 - threshold the difference
                    CvInvoke.Threshold(_motionFrame, _motionFrame, _differenceThreshold, 255, ThresholdType.Binary);

                    if (_suppressNoise)
                    {
                        // suppress noise
                        CvInvoke.Erode(_motionFrame, _motionFrame, _structuringElement, new Point(-1, -1), 1, BorderType.Default, new MCvScalar(0));
                    }

                    // calculate amount of motion pixels
                    _pixelsChanged = CvInvoke.CountNonZero(_motionFrame);
                }
            }
        }

        /// <summary>
        /// Reset motion detector to initial state.
        /// </summary>
        public void Reset()
        {
            Reset(false);
        }

        // Reset motion detector to initial state
        private void Reset(bool force)
        {
            lock (_sync)
            {
                if ((_backgroundFrame != null) && ((force) || (_manuallySetBackgroundFrame == false)))
                {
                    _backgroundFrame.Dispose();
                    _backgroundFrame = null;
                }

                _motionFrame?.Dispose();
                _motionFrame = null;

                _pixelsChanged = 0;
            }
        }

        /// <summary>
        /// Set background frame.
        /// </summary>
        public void SetBackgroundFrame(Bitmap backgroundFrame)
        {
            // .ToMat() extension method from Emgu.CV.Bitmap
            using (Mat mat = backgroundFrame.ToMat())
            {
                SetBackgroundFrame(mat);
            }
        }

        /// <summary>
        /// Set background frame.
        /// </summary>
        public void SetBackgroundFrame(Mat backgroundFrame)
        {
            // reset motion detection algorithm
            Reset(true);

            lock (_sync)
            {
                // save image dimension
                _width = backgroundFrame.Width;
                _height = backgroundFrame.Height;

                _backgroundFrame = new Mat();
                CvInvoke.CvtColor(backgroundFrame, _backgroundFrame, ColorConversion.Bgr2Gray);

                _manuallySetBackgroundFrame = true;
            }
        }
    }
}