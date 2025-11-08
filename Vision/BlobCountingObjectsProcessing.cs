using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util; // For VectorOfVectorOfPoint
using System.Collections.Generic; // For List

namespace iSpyApplication.Vision
{
    /// <summary>
    /// Motion processing algorithm, which counts separate moving objects and highlights them.
    /// </summary>
    public class BlobCountingObjectsProcessing : IMotionProcessing
    {
        private Color _highlightColor = Color.Red;
        private bool _highlightMotionRegions = true;

        private VectorOfVectorOfPoint _contours;
        private Mat _hierarchy;

        private int _minObjectsWidth = 10;
        private int _minObjectsHeight = 10;
        private Rectangle[] _objectRectangles = new Rectangle[0];

        public bool HighlightMotionRegions
        {
            get { return _highlightMotionRegions; }
            set { _highlightMotionRegions = value; }
        }

        public Color HighlightColor
        {
            get { return _highlightColor; }
            set { _highlightColor = value; }
        }

        public int MinObjectsWidth
        {
            get { return _minObjectsWidth; }
            set { _minObjectsWidth = value; }
        }

        public int MinObjectsHeight
        {
            get { return _minObjectsHeight; }
            set { _minObjectsHeight = value; }
        }

        public int ObjectsCount { get; private set; }

        public Rectangle[] ObjectRectangles
        {
            get { return _objectRectangles; }
        }

        public BlobCountingObjectsProcessing() : this(10, 10) { }

        public BlobCountingObjectsProcessing(bool highlightMotionRegions) : this(10, 10, highlightMotionRegions) { }

        public BlobCountingObjectsProcessing(int minWidth, int minHeight) :
            this(minWidth, minHeight, Color.Red)
        { }

        public BlobCountingObjectsProcessing(int minWidth, int minHeight, Color highlightColor)
        {
            _minObjectsWidth = minWidth;
            _minObjectsHeight = minHeight;
            _highlightColor = highlightColor;
            _contours = new VectorOfVectorOfPoint();
            _hierarchy = new Mat();
        }

        public BlobCountingObjectsProcessing(int minWidth, int minHeight, bool highlightMotionRegions)
            : this(minWidth, minHeight)
        {
            _highlightMotionRegions = highlightMotionRegions;
        }

        /// <summary>
        /// Process video and motion frames.
        /// </summary>
        public void ProcessFrame(Mat videoFrame, Mat motionFrame)
        {
            if (motionFrame.Depth != DepthType.Cv8U || motionFrame.NumberOfChannels != 1)
            {
                //throw new InvalidImagePropertiesException( "Motion frame must be 8 bpp image." );
                return;
            }

            // Find contours
            CvInvoke.FindContours(motionFrame, _contours, _hierarchy, RetrType.External, ChainApproxMethod.ChainApproxSimple);

            var rects = new List<Rectangle>();
            if (_contours.Size > 0)
            {
                for (int i = 0; i < _contours.Size; i++)
                {
                    Rectangle rect = CvInvoke.BoundingRectangle(_contours[i]);
                    if (rect.Width >= _minObjectsWidth && rect.Height >= _minObjectsHeight)
                    {
                        rects.Add(rect);
                    }
                }
            }
            _objectRectangles = rects.ToArray();
            ObjectsCount = rects.Count;

            if (_highlightMotionRegions && ObjectsCount > 0)
            {
                // highlight each moving object
                var color = new Bgr(_highlightColor).MCvScalar;
                foreach (Rectangle rect in _objectRectangles)
                {
                    CvInvoke.Rectangle(videoFrame, rect, color, 1);
                }
            }
        }

        public void Reset()
        {
            _objectRectangles = new Rectangle[0];
            ObjectsCount = 0;
            _contours?.Dispose();
            _hierarchy?.Dispose();
            _contours = new VectorOfVectorOfPoint();
            _hierarchy = new Mat();
        }
    }
}