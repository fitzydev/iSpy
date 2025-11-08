using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;

namespace iSpyApplication.Vision
{
    /// <summary>
    /// Motion processing algorithm, which highlights motion areas.
    /// </summary>
    public class MotionAreaHighlighting : IMotionProcessing
    {
        private Color _highlightColor = Color.Red;

        public Color HighlightColor
        {
            get { return _highlightColor; }
            set { _highlightColor = value; }
        }

        public MotionAreaHighlighting() { }

        public MotionAreaHighlighting(Color highlightColor)
        {
            _highlightColor = highlightColor;
        }

        /// <summary>
        /// Process video and motion frames.
        /// </summary>
        public void ProcessFrame(Mat videoFrame, Mat motionFrame)
        {
            if (videoFrame.NumberOfChannels == 1)
            {
                // Grayscale case
                byte fillG = (byte)(0.2125 * _highlightColor.R +
                                      0.7154 * _highlightColor.G +
                                      0.0721 * _highlightColor.B);

                videoFrame.SetTo(new MCvScalar(fillG), motionFrame);
            }
            else
            {
                // Color case
                var color = new Bgr(_highlightColor).MCvScalar;
                videoFrame.SetTo(color, motionFrame);
            }
        }

        public void Reset()
        {
        }
    }
}