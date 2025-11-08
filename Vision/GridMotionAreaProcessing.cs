using System;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace iSpyApplication.Vision
{
    /// <summary>
    /// Motion processing algorithm, which performs grid processing of motion frame.
    /// </summary>
    public class GridMotionAreaProcessing : IMotionProcessing
    {
        private Color _highlightColor = Color.Red;
        private bool _highlightMotionGrid;

        private float _motionAmountToHighlight;

        private int _gridWidth;
        private int _gridHeight;

        private float[,] _motionGrid;

        public Color HighlightColor
        {
            get { return _highlightColor; }
            set { _highlightColor = value; }
        }

        public bool HighlightMotionGrid
        {
            get { return _highlightMotionGrid; }
            set { _highlightMotionGrid = value; }
        }

        public float MotionAmountToHighlight
        {
            get { return _motionAmountToHighlight; }
            set { _motionAmountToHighlight = value; }
        }

        public float[,] MotionGrid => _motionGrid;

        public int GridWidth
        {
            get { return _gridWidth; }
            set
            {
                _gridWidth = Math.Min(64, Math.Max(2, value));
                _motionGrid = new float[_gridHeight, _gridWidth];
            }
        }

        public int GridHeight
        {
            get { return _gridHeight; }
            set
            {
                _gridHeight = Math.Min(64, Math.Max(2, value));
                _motionGrid = new float[_gridHeight, _gridWidth];
            }
        }

        public GridMotionAreaProcessing() : this(16, 16) { }

        public GridMotionAreaProcessing(int gridWidth, int gridHeight) : this(gridWidth, gridHeight, true) { }

        public GridMotionAreaProcessing(int gridWidth, int gridHeight, bool highlightMotionGrid)
            : this(gridWidth, gridHeight, highlightMotionGrid, 0.15f) { }

        public GridMotionAreaProcessing(int gridWidth, int gridHeight, bool highlightMotionGrid, float motionAmountToHighlight)
        {
            _gridWidth = Math.Min(64, Math.Max(2, gridWidth));
            _gridHeight = Math.Min(64, Math.Max(2, gridHeight));

            _motionGrid = new float[gridHeight, gridWidth];

            _highlightMotionGrid = highlightMotionGrid;
            _motionAmountToHighlight = motionAmountToHighlight;
        }

        /// <summary>
        /// Process video and motion frames using Emgu.CV
        /// </summary>
        public void ProcessFrame(Mat videoFrame, Mat motionFrame)
        {
            if (motionFrame.Depth != DepthType.Cv8U || motionFrame.NumberOfChannels != 1)
            {
                throw new Exception("Motion frame must be 8 bpp grayscale image.");
            }

            if ((videoFrame.Depth != DepthType.Cv8U) ||
                 (videoFrame.NumberOfChannels != 1 && videoFrame.NumberOfChannels != 3))
            {
                throw new Exception("Video frame must be 8 bpp grayscale image or 24 bpp color image.");
            }

            int width = videoFrame.Width;
            int height = videoFrame.Height;

            if ((motionFrame.Width != width) || (motionFrame.Height != height))
                return;

            int cellWidth = width / _gridWidth;
            int cellHeight = height / _gridHeight;

            // process motion frame calculating amount of changed pixels
            // in each grid's cell
            for (int y = 0; y < _gridHeight; y++)
            {
                for (int x = 0; x < _gridWidth; x++)
                {
                    var rect = new Rectangle(x * cellWidth, y * cellHeight, cellWidth, cellHeight);
                    rect.Intersect(new Rectangle(0, 0, width, height));

                    if (rect.Width == 0 || rect.Height == 0)
                    {
                        _motionGrid[y, x] = 0;
                        continue;
                    }

                    // Create a Region of Interest (ROI) for the current cell
                    using (Mat cellMotion = new Mat(motionFrame, rect))
                    {
                        // Count non-zero (motion) pixels in the cell
                        int motionPixels = CvInvoke.CountNonZero(cellMotion);
                        _motionGrid[y, x] = (float)motionPixels / (rect.Width * rect.Height);
                    }
                }
            }


            if (_highlightMotionGrid)
            {
                // highlight motion grid - cells, which have enough motion
                var highlight = new Bgr(_highlightColor).MCvScalar;

                for (int y = 0; y < _gridHeight; y++)
                {
                    for (int x = 0; x < _gridWidth; x++)
                    {
                        if (_motionGrid[y, x] > _motionAmountToHighlight)
                        {
                            var rect = new Rectangle(x * cellWidth, y * cellHeight, cellWidth, cellHeight);
                            rect.Intersect(new Rectangle(0, 0, width, height));

                            if (rect.Width > 0 && rect.Height > 0)
                            {
                                // Draw a non-filled rectangle to highlight the grid cell
                                CvInvoke.Rectangle(videoFrame, rect, highlight, 1);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Reset internal state of motion processing algorithm.
        /// </summary>
        public void Reset()
        {
            _motionGrid = new float[_gridHeight, _gridWidth];
        }
    }
}