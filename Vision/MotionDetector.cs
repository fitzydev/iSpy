using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace iSpyApplication.Vision
{
    public class MotionDetector
    {
        private IMotionDetector _detector;
        private IMotionProcessing _processor;

        // motion detection zones
        private Rectangle[] _motionZones;
        // image of motion zones
        private Mat _zonesFrame;
        // size of video frame
        private int _videoWidth, _videoHeight;

        // dummy object to lock for synchronization
        private readonly object _sync = new object();

        public IMotionDetector MotionDetectionAlgorithm
        {
            get { return _detector; }
            set
            {
                lock (_sync)
                {
                    _detector = value;
                }
            }
        }

        public IMotionProcessing MotionProcessingAlgorithm
        {
            get { return _processor; }
            set
            {
                // lock ( _sync )
                {
                    _processor = value;
                }
            }
        }

        public Rectangle[] MotionZones
        {
            get { return _motionZones; }
            set
            {
                _motionZones = value;
                if (value != null)
                    CreateMotionZonesFrame();
            }
        }

        public MotionDetector(IMotionDetector detector) : this(detector, null) { }

        public MotionDetector(IMotionDetector detector, IMotionProcessing processor)
        {
            _detector = detector;
            _processor = processor;
        }

        public float ProcessFrame(Bitmap videoFrame)
        {
            // Use .ToMat() from Emgu.CV.Bitmap
            using (Mat mat = videoFrame.ToMat())
            {
                return ProcessFrame(mat);
            }
        }

        public float ProcessFrame(Mat videoFrame)
        {
            lock (_sync)
            {
                if (_detector == null)
                    return 0;

                _videoWidth = videoFrame.Width;
                _videoHeight = videoFrame.Height;

                if (_area == 0)
                    _area = _videoWidth * _videoHeight;

                // call motion detection
                _detector.ProcessFrame(videoFrame);
                var motionLevel = _detector.MotionLevel;

                // check if motion zones are specified
                if (_detector.MotionFrame != null && _motionZones != null)
                {
                    if (_zonesFrame == null)
                    {
                        CreateMotionZonesFrame();
                    }

                    if (_zonesFrame != null && (_videoWidth == _zonesFrame.Width) && (_videoHeight == _zonesFrame.Height))
                    {
                        CvInvoke.BitwiseAnd(_detector.MotionFrame, _zonesFrame, _detector.MotionFrame);
                        motionLevel = (float)CvInvoke.CountNonZero(_detector.MotionFrame) / _area;
                    }
                }

                // call motion post processing
                ApplyOverlay(videoFrame);
                return motionLevel;
            }
        }

        public void ApplyOverlay(Mat videoFrame)
        {
            if ((_processor != null) && (_detector?.MotionFrame != null))
            {
                // This call is now correct because IMotionProcessing expects a Mat
                _processor.ProcessFrame(videoFrame, _detector.MotionFrame);
            }
        }

        public void Reset()
        {
            // lock ( _sync )
            {
                _detector?.Reset();
                _processor?.Reset();

                _videoWidth = 0;
                _videoHeight = 0;

                _zonesFrame?.Dispose();
                _zonesFrame = null;
            }
        }

        private int _area;

        // Create motion zones' image
        private void CreateMotionZonesFrame()
        {
            lock (_sync)
            {
                _area = 0;
                // free previous motion zones frame
                _zonesFrame?.Dispose();
                _zonesFrame = null;

                if ((_motionZones != null) && (_motionZones.Length != 0) && (_videoWidth != 0))
                {
                    _zonesFrame = new Mat(_videoHeight, _videoWidth, DepthType.Cv8U, 1);
                    _zonesFrame.SetTo(new MCvScalar(0)); // Set to black

                    var imageRect = new Rectangle(0, 0, _videoWidth, _videoHeight);

                    // draw all motion zones on motion frame
                    foreach (Rectangle rect in _motionZones)
                    {
                        rect.Intersect(imageRect);
                        // Draw a filled white rectangle for the zone
                        CvInvoke.Rectangle(_zonesFrame, rect, new MCvScalar(255), -1);
                        _area += rect.Width * rect.Height;
                    }
                }
            }
        }
    }
}