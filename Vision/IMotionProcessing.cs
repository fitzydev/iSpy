using Emgu.CV; // Replaced AForge.Imaging with Emgu.CV

namespace iSpyApplication.Vision
{
    /// <summary>
    /// Interface of motion processing algorithm.
    /// </summary>
    public interface IMotionProcessing
    {
        /// <summary>
        /// Process video and motion frames doing further post processing after
        /// performed motion detection.
        /// </summary>
        /// <param name="videoFrame">Original video frame.</param>
        /// <param name="motionFrame">Motion frame provided by motion detection
        /// algorithm (see <see cref="IMotionDetector"/>).</param>
        void ProcessFrame(Mat videoFrame, Mat motionFrame); // Changed from UnmanagedImage

        /// <summary>
        /// Reset internal state of motion processing algorithm.
        /// </summary>
        void Reset();
    }
}