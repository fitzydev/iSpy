using System;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace iSpyApplication.Vision
{
    /// <summary>
    /// Motion detector based on two continues frames difference (Color).
    /// </summary>
    public class TwoFramesColorDifferenceDetector : IMotionDetector
    {
        private int _width;
        private int _height;

        private Mat _previousFrame; // BGR
        private Mat _motionFrame; // Grayscale
        private int _pixelsChanged;

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

        // AForge KeepObjectsEdges removed
        // public bool KeepObjectsEdges ...

        public TwoFramesColorDifferenceDetector() { }

        public TwoFramesColorDifferenceDetector(bool suppressNoise)
        {
            _suppressNoise = suppressNoise;
        }

        public TwoFramesColorDifferenceDetector(bool suppressNoise, bool keepObjectEdges)
        {
            _suppressNoise = suppressNoise;
            // _keepObjectEdges = keepObjectEdges; // AForge property
        }

        public void ProcessFrame(Mat videoFrame)
        {
            lock (_sync)
            {
                if (_previousFrame == null)
                {
                    _width = videoFrame.Width;
                    _height = videoFrame.Height;
                    _previousFrame = videoFrame.Clone(); // Store color frame
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
                    _previousFrame = videoFrame.Clone();
                    _motionFrame = new Mat(_height, _width, DepthType.Cv8U, 1);
                    _motionFrame.SetTo(new MCvScalar(0));
                    _pixelsChanged = 0;
                    return;
                }

                // 1 - get difference between frames
                using (Mat diffFrame = new Mat())
                {
                    CvInvoke.AbsDiff(videoFrame, _previousFrame, diffFrame);
                    // 2 - Convert to grayscale
                    CvInvoke.CvtColor(diffFrame, _motionFrame, ColorConversion.Bgr2Gray);
                }

                // 3 - threshold the difference
                CvInvoke.Threshold(_motionFrame, _motionFrame, _differenceThreshold, 255, ThresholdType.Binary);

                // Update previous frame
                videoFrame.CopyTo(_previousFrame);

                if (_suppressNoise)
                {
                    CvInvoke.Erode(_motionFrame, _motionFrame, _structuringElement, new Point(-1, -1), 1, BorderType.Default, new MCvScalar(0));
                }

                _pixelsChanged = CvInvoke.CountNonZero(_motionFrame);
            }
        }

        public void Reset()
        {
            lock (_sync)
            {
                _previousFrame?.Dispose();
                _previousFrame = null;
                _motionFrame?.Dispose();
                _motionFrame = null;
                _pixelsChanged = 0;
            }
        }
    }
}