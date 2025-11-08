using Emgu.CV;
using Emgu.CV.CvEnum; // <<< FIX IS HERE
using Emgu.CV.Structure;
using Emgu.CV.Shape;
using System;
using System.Drawing; // <<< ADDED FOR Point

namespace iSpyApplication.Vision
{
    /// <summary>
    /// Motion detector based on two continues frames difference.
    /// </summary>
    public class TwoFramesDifferenceDetector : IMotionDetector
    {
        // frame's dimension
        private int _width;
        private int _height;

        // previous frame of video stream
        private Mat _previousFrame;
        // current frame of video sream
        private Mat _motionFrame;
        // number of pixels changed in the new frame of video stream
        private int _pixelsChanged;

        // suppress noise
        private bool _suppressNoise = true;

        // threshold values
        private int _differenceThreshold = 15;

        // structuring element for noise suppression
        private readonly Mat _structuringElement = CvInvoke.GetStructuringElement(MorphShapes.Rectangle, new System.Drawing.Size(3, 3), new System.Drawing.Point(-1, -1));

        /// <summary>
        /// Difference threshold value, [1, 255].
        /// </summary>
        public int DifferenceThreshold
        {
            get { return _differenceThreshold; }
            set
            {
                _differenceThreshold = Math.Max(1, Math.Min(255, value));
            }
        }

        /// <summary>
        /// Motion level value, [0, 1].
        /// </summary>
        public float MotionLevel
        {
            get
            {
                if (_width == 0 || _height == 0) return 0;
                return (float)_pixelsChanged / (_width * _height);
            }
        }

        /// <summary>
        /// Motion frame containing detected areas of motion.
        /// </summary>
        public Mat MotionFrame
        {
            get
            {
                return _motionFrame;
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
                _suppressNoise = value;
            }
        }

        public TwoFramesDifferenceDetector()
        {
        }

        public TwoFramesDifferenceDetector(bool suppressNoise)
        {
            _suppressNoise = suppressNoise;
        }

        /// <summary>
        /// Process new video frame.
        /// </summary>
        public void ProcessFrame(Mat videoFrame)
        {
            using (var grayFrame = new Mat())
            {
                CvInvoke.CvtColor(videoFrame, grayFrame, ColorConversion.Bgr2Gray);

                if (_previousFrame == null)
                {
                    _width = videoFrame.Width;
                    _height = videoFrame.Height;
                    _previousFrame = grayFrame.Clone();
                    _motionFrame = new Mat(_height, _width, DepthType.Cv8U, 1);
                    _motionFrame.SetTo(new MCvScalar(0));
                    return;
                }

                if ((videoFrame.Width != _width) || (videoFrame.Height != _height))
                {
                    Reset();
                    _width = videoFrame.Width;
                    _height = videoFrame.Height;
                    _previousFrame = grayFrame.Clone();
                    _motionFrame = new Mat(_height, _width, DepthType.Cv8U, 1);
                    _motionFrame.SetTo(new MCvScalar(0));
                    return;
                }

                CvInvoke.AbsDiff(grayFrame, _previousFrame, _motionFrame);
                CvInvoke.Threshold(_motionFrame, _motionFrame, _differenceThreshold, 255, ThresholdType.Binary);

                grayFrame.CopyTo(_previousFrame);

                if (_suppressNoise)
                {
                    CvInvoke.Erode(_motionFrame, _motionFrame, _structuringElement, new Point(-1, -1), 1, BorderType.Default, new MCvScalar(0));
                }

                _pixelsChanged = CvInvoke.CountNonZero(_motionFrame);
            }
        }

        /// <summary>
        /// Reset motion detector to initial state.
        /// </summary>
        public void Reset()
        {
            _previousFrame?.Dispose();
            _previousFrame = null;

            _motionFrame?.Dispose();
            _motionFrame = null;

            _pixelsChanged = 0;
        }
    }
}