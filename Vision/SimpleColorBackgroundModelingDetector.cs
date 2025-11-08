using System;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace iSpyApplication.Vision
{
    /// <summary>
    /// Motion detector based on simple background modeling (Color).
    /// </summary>
    public class SimpleColorBackgroundModelingDetector : IMotionDetector
    {
        private int _width;
        private int _height;

        private Mat _backgroundFrame; // BGR
        private Mat _motionFrame; // Grayscale
        private int _pixelsChanged;

        private bool _suppressNoise = true;
        private int _differenceThreshold = 15;

        private int _framesPerBackgroundUpdate = 2;
        private int _framesCounter;

        private int _millisecondsPerBackgroundUpdate;
        private int _millisecondsLeftUnprocessed;
        private DateTime _lastTimeMeasurment;

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

        public int FramesPerBackgroundUpdate
        {
            get { return _framesPerBackgroundUpdate; }
            set { _framesPerBackgroundUpdate = Math.Max(1, Math.Min(50, value)); }
        }

        public int MillisecondsPerBackgroundUpdate
        {
            get { return _millisecondsPerBackgroundUpdate; }
            set { _millisecondsPerBackgroundUpdate = Math.Max(0, Math.Min(5000, value)); }
        }

        public SimpleColorBackgroundModelingDetector() { }

        public SimpleColorBackgroundModelingDetector(bool suppressNoise)
        {
            _suppressNoise = suppressNoise;
        }

        public SimpleColorBackgroundModelingDetector(bool suppressNoise, bool keepObjectEdges)
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
                    _lastTimeMeasurment = DateTime.Now;
                    _width = videoFrame.Width;
                    _height = videoFrame.Height;
                    _backgroundFrame = videoFrame.Clone();
                    _motionFrame = new Mat(_height, _width, DepthType.Cv8U, 1);
                    _motionFrame.SetTo(new MCvScalar(0));
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
                    return;
                }

                // update background frame
                if (_millisecondsPerBackgroundUpdate == 0)
                {
                    if (++_framesCounter == _framesPerBackgroundUpdate)
                    {
                        _framesCounter = 0;
                        // Use Emgu.CV's weighted average to update the background
                        CvInvoke.AddWeighted(_backgroundFrame, 0.95, videoFrame, 0.05, 0, _backgroundFrame);
                    }
                }
                else
                {
                    DateTime currentTime = DateTime.Now;
                    TimeSpan timeDff = currentTime - _lastTimeMeasurment;
                    _lastTimeMeasurment = currentTime;

                    int millisonds = (int)timeDff.TotalMilliseconds + _millisecondsLeftUnprocessed;
                    _millisecondsLeftUnprocessed = millisonds % _millisecondsPerBackgroundUpdate;
                    int updateAmount = (millisonds / _millisecondsPerBackgroundUpdate);

                    if (updateAmount > 0)
                    {
                        double alpha = Math.Min(0.1 * updateAmount, 1.0); // Simple heuristic
                        CvInvoke.AddWeighted(_backgroundFrame, 1.0 - alpha, videoFrame, alpha, 0, _backgroundFrame);
                    }
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
            lock (_sync)
            {
                _backgroundFrame?.Dispose();
                _backgroundFrame = null;
                _motionFrame?.Dispose();
                _motionFrame = null;
                _framesCounter = 0;
                _pixelsChanged = 0;
            }
        }
    }
}