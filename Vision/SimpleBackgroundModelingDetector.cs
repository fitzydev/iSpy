using System;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace iSpyApplication.Vision
{
    /// <summary>
    /// Motion detector based on simple background modeling.
    /// </summary>
    public class SimpleBackgroundModelingDetector : IMotionDetector
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

        // suppress noise
        private bool _suppressNoise = true;

        // threshold values
        private int _differenceThreshold = 15;

        private int _framesPerBackgroundUpdate = 2;
        private int _framesCounter;

        private int _millisecondsPerBackgroundUpdate;
        private int _millisecondsLeftUnprocessed;
        private DateTime _lastTimeMeasurment;

        // structuring element for noise suppression
        private readonly Mat _structuringElement = CvInvoke.GetStructuringElement(MorphShapes.Rectangle, new Size(3, 3), new Point(-1, -1));

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

        // AForge KeepObjectsEdges removed
        // public bool KeepObjectsEdges ...

        /// <summary>
        /// Frames per background update, [1, 50].
        /// </summary>
        public int FramesPerBackgroundUpdate
        {
            get { return _framesPerBackgroundUpdate; }
            set { _framesPerBackgroundUpdate = Math.Max(1, Math.Min(50, value)); }
        }

        /// <summary>
        /// Milliseconds per background update, [0, 5000].
        /// </summary>
        public int MillisecondsPerBackgroundUpdate
        {
            get { return _millisecondsPerBackgroundUpdate; }
            set { _millisecondsPerBackgroundUpdate = Math.Max(0, Math.Min(5000, value)); }
        }

        public SimpleBackgroundModelingDetector() { }

        public SimpleBackgroundModelingDetector(bool suppressNoise)
        {
            _suppressNoise = suppressNoise;
        }

        public SimpleBackgroundModelingDetector(bool suppressNoise, bool keepObjectEdges)
        {
            _suppressNoise = suppressNoise;
            // _keepObjectEdges = keepObjectEdges; // AForge property, removed
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
                        _lastTimeMeasurment = DateTime.Now;

                        // save image dimension
                        _width = videoFrame.Width;
                        _height = videoFrame.Height;

                        _backgroundFrame = grayFrame.Clone();
                        _motionFrame = new Mat(_height, _width, DepthType.Cv8U, 1);
                        _motionFrame.SetTo(new MCvScalar(0));
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
                        return;
                    }

                    // update background frame
                    if (_millisecondsPerBackgroundUpdate == 0)
                    {
                        // update background frame using frame counter as a base
                        if (++_framesCounter == _framesPerBackgroundUpdate)
                        {
                            _framesCounter = 0;
                            // Emgu.CV equivalent of AForge's byte-by-byte background adaption
                            // This is slow. A better way is CvInvoke.AbsDiff and then update.
                            // For simplicity, let's use a weighted average
                            CvInvoke.AddWeighted(_backgroundFrame, 0.95, grayFrame, 0.05, 0, _backgroundFrame);
                        }
                    }
                    else
                    {
                        // update background frame using timer as a base
                        DateTime currentTime = DateTime.Now;
                        TimeSpan timeDff = currentTime - _lastTimeMeasurment;
                        _lastTimeMeasurment = currentTime;

                        int millisonds = (int)timeDff.TotalMilliseconds + _millisecondsLeftUnprocessed;
                        _millisecondsLeftUnprocessed = millisonds % _millisecondsPerBackgroundUpdate;
                        int updateAmount = (millisonds / _millisecondsPerBackgroundUpdate);

                        if (updateAmount > 0)
                        {
                            // This logic is hard to replicate 1:1 in Emgu.CV without unsafe pointers
                            // Using a weighted average is the standard OpenCV way.
                            // The weight (alpha) should be based on updateAmount.
                            double alpha = Math.Min(0.1 * updateAmount, 1.0); // Simple heuristic
                            CvInvoke.AddWeighted(_backgroundFrame, 1.0 - alpha, grayFrame, alpha, 0, _backgroundFrame);
                        }
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