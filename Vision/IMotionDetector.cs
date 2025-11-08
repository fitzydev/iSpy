using Emgu.CV; // Replaced AForge.Imaging with Emgu.CV

namespace iSpyApplication.Vision
{
    /// <summary>
    /// Motion detector's interface.
    /// </summary>
    public interface IMotionDetector
    {
        /// <summary>
        /// Motion level, [0, 1].
        /// </summary>
        float MotionLevel { get; }

        /// <summary>
        /// Motion frame.
        /// </summary>
        Mat MotionFrame { get; } // Changed from UnmanagedImage to Mat

        /// <summary>
        /// Process new video frame.
        /// </summary>
        /// <param name="videoFrame">Video frame to process (detect motion in).</param>
        void ProcessFrame(Mat videoFrame); // Changed from UnmanagedImage to Mat

        /// <summary>
        /// Reset motion detector to initial state.
        /// </summary>
        void Reset();
    }
}