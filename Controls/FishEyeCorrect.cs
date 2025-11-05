using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System;

namespace iSpyApplication.Controls
{
    public class FishEyeCorrect
    {
        public Mat CameraMatrix { get; private set; }
        public Mat DistortionCoeff { get; private set; }
        public Mat NewCameraMatrix { get; private set; }

        private int _w = -1, _h = -1;

        public void Init(int w, int h, double focalLength, double scale, int offx, int offy)
        {
            if (_w == w && _h == h) return;

            _w = w;
            _h = h;

            var cameraMatrix = new Matrix<double>(3, 3);
            cameraMatrix.SetZero();
            cameraMatrix[0, 0] = focalLength;
            cameraMatrix[1, 1] = focalLength;
            cameraMatrix[0, 2] = w / 2d;
            cameraMatrix[1, 2] = h / 2d;
            cameraMatrix[2, 2] = 1.0;
            CameraMatrix = cameraMatrix.Mat;

            DistortionCoeff = new Mat(4, 1, DepthType.Cv64F, 1);
            DistortionCoeff.SetTo(new MCvScalar(0));

            var newCameraMatrix = cameraMatrix.Clone();

            // Adjust the new camera matrix for scaling and offset
            var newFocalLength = focalLength * scale;
            newCameraMatrix[0, 0] = newFocalLength;
            newCameraMatrix[1, 1] = newFocalLength;
            newCameraMatrix[0, 2] = (w / 2d) + offx - (w / 2d);
            newCameraMatrix[1, 2] = (h / 2d) + offy - (h / 2d);
            NewCameraMatrix = newCameraMatrix.Mat;
        }

        public void Correct(Mat img)
        {
            // This method is no longer used with CvInvoke.Remap, but kept for compatibility.
        }
    }
}