using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace iSpyApplication.Vision
{
    /// <summary>
    /// Motion processing algorithm, which highlights border of motion areas.
    /// </summary>
    public class MotionBorderHighlighting : IMotionProcessing
    {
        private Color _highlightColor = Color.Red;

        public Color HighlightColor
        {
            get { return _highlightColor; }
            set { _highlightColor = value; }
        }

        public MotionBorderHighlighting() { }

        public MotionBorderHighlighting(Color highlightColor)
        {
            _highlightColor = highlightColor;
        }

        /// <summary>
        /// Process video and motion frames.
        /// </summary>
        public void ProcessFrame(Mat videoFrame, Mat motionFrame)
        {
            // Create a copy of motionFrame to avoid modifying the original
            using (Mat motionCopy = motionFrame.Clone())
            using (var contours = new VectorOfVectorOfPoint())
            {
                // Find contours (edges) of the motion mask
                CvInvoke.FindContours(motionCopy, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);

                var color = new Bgr(_highlightColor).MCvScalar;

                // Draw the contours onto the original video frame
                CvInvoke.DrawContours(videoFrame, contours, -1, color, 1);
            }
        }

        public void Reset()
        {
        }
    }
}