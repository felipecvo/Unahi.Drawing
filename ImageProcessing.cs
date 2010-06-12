using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.IO;

namespace Unahi.Drawing {
    public class ImageProcessing : IDisposable {
        private Image CurrentImage { get; set; }

        public ImageDisposition Disposition {
            get {
                if (CurrentImage.Width > CurrentImage.Height) {
                    return ImageDisposition.Landscape;
                } else if (CurrentImage.Height > CurrentImage.Width) {
                    return ImageDisposition.Portrait;
                } else {
                    return ImageDisposition.Square;
                }
            }
        }

        public ImageProcessing(Stream stream) {
            CurrentImage = new Bitmap(stream);
        }

        public ImageProcessing(Image original) {
            CurrentImage = original;
        }

        public void ResizeWithMax(int maxWidth, int maxHeight) {
            //int width, height;
            //switch (Disposition)
            //{
            //    case ImageDisposition.Portrait:
            //        height = maxHeight;
            //        width = (int)(((float)maxHeight / CurrentImage.Height) * (float)CurrentImage.Width);
            //        break;
            //    case ImageDisposition.Landscape:
            //    default:
            //        width = maxWidth;
            //        height = (int)(((float)maxWidth / CurrentImage.Width) * (float)CurrentImage.Height);
            //        break;
            //}
            //Resize(width, height);
            var resized = ResizeWithMax(CurrentImage, maxWidth, maxHeight);
            CurrentImage.Dispose();
            CurrentImage = resized;
        }

        public void Resize(int width, int height) {
            var resized = Resize(CurrentImage, width, height);
            CurrentImage.Dispose();
            CurrentImage = resized;
        }

        private Image ResizeWithMax(Image source, int maxWidth, int maxHeight) {
            int width, height;
            switch (Disposition) {
                case ImageDisposition.Portrait:
                    height = maxHeight;
                    width = (int)(((float)maxHeight / CurrentImage.Height) * (float)CurrentImage.Width);
                    break;
                case ImageDisposition.Landscape:
                default:
                    width = maxWidth;
                    height = (int)(((float)maxWidth / CurrentImage.Width) * (float)CurrentImage.Height);
                    break;
            }
            return Resize(source, width, height);
        }

        private Image Resize(Image source, int width, int height) {
            var resized = new Bitmap(width, height);
            using (var graphics = Graphics.FromImage(resized)) {
                SetHighQuality(graphics);
                graphics.DrawImage(source, 0, 0, width, height);
            }
            return resized;
        }

        public void AddFooter(Image footer) {
            AddFooter(footer, FooterPosition.Left);
        }

        public void AddFooter(Image footer, FooterPosition position) {
            int x;

            switch (position) {
                case FooterPosition.Right:
                    x = CurrentImage.Width - footer.Width;
                    break;
                case FooterPosition.Center:
                    x = (CurrentImage.Width / 2) - (footer.Width / 2);
                    break;
                case FooterPosition.Left:
                default:
                    x = 0;
                    break;
            }

            var newImage = new Bitmap(CurrentImage.Width, CurrentImage.Height + footer.Height);
            using (var graphics = Graphics.FromImage(newImage)) {
                SetHighQuality(graphics);
                graphics.DrawImage(CurrentImage, 0, 0, CurrentImage.Width, CurrentImage.Height);
                graphics.DrawImage(footer, x, CurrentImage.Height + 1, footer.Width, footer.Height);
            }
            CurrentImage.Dispose();
            CurrentImage = newImage;
        }

        public void AddWaterMark(Image waterMark, WaterMarkPosition position) {
            AddWaterMark(waterMark, position, 0);
        }

        public void AddText(string text, Font font, int x, int y) {
            using (var graphics = Graphics.FromImage(CurrentImage)) {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.DrawString(text, font, new SolidBrush(Color.Black), new Point(x + 1, y + 1));
                graphics.DrawString(text, font, new SolidBrush(Color.White), new Point(x, y));
            }
        }

        public void AddWaterMark(Image waterMark, WaterMarkPosition position, float angle) {
            if (angle != 0) {
                Rotate(ref waterMark, angle);
            }

            var attributes = new ImageAttributes();

            float[][] colorMatrixElements = { 
												new float[] {1.0f,  0.0f,  0.0f,  0.0f, 0.0f},       
												new float[] {0.0f,  1.0f,  0.0f,  0.0f, 0.0f},        
												new float[] {0.0f,  0.0f,  1.0f,  0.0f, 0.0f},        
												new float[] {0.0f,  0.0f,  0.0f,  0.3f, 0.0f},        
												new float[] {0.0f,  0.0f,  0.0f,  0.0f, 1.0f}};
            ColorMatrix wmColorMatrix = new ColorMatrix(colorMatrixElements);

            attributes.SetColorMatrix(wmColorMatrix, ColorMatrixFlag.Default,
                ColorAdjustType.Bitmap);

            int xPosOfWm, yPosOfWm;
            switch (position) {
                case WaterMarkPosition.Center:
                    xPosOfWm = (CurrentImage.Width / 2) - (waterMark.Width / 2);
                    yPosOfWm = (CurrentImage.Height / 2) - (waterMark.Height / 2);
                    break;
                case WaterMarkPosition.RightTop:
                default:
                    xPosOfWm = CurrentImage.Width - waterMark.Width - 10;
                    yPosOfWm = 10;
                    break;
            }

            using (var graphics = Graphics.FromImage(CurrentImage)) {
                SetHighQuality(graphics);
                graphics.DrawImage(waterMark,
                    new Rectangle(xPosOfWm, yPosOfWm, waterMark.Width, waterMark.Height),  //Set the detination Position
                    0,                  // x-coordinate of the portion of the source image to draw. 
                    0,                  // y-coordinate of the portion of the source image to draw. 
                    waterMark.Width,    // Watermark Width
                    waterMark.Height,   // Watermark Height
                    GraphicsUnit.Pixel, // Unit of measurment
                    attributes);   //ImageAttributes Object
            }
        }

        private void SetHighQuality(Graphics graphics) {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        }

        private void Rotate(ref Image image, float angle) {
            var matrix = new Matrix();
            matrix.Translate(image.Width / -2, image.Height / -2, MatrixOrder.Append);
            matrix.RotateAt(angle, new Point(0, 0), MatrixOrder.Append);

            using (var graphicsPath = new GraphicsPath()) {  // transform image points by rotation matrix
                graphicsPath.AddPolygon(new Point[] { new Point(0, 0), new Point(image.Width, 0), new Point(0, image.Height) });
                graphicsPath.Transform(matrix);
                PointF[] pts = graphicsPath.PathPoints;

                // create destination bitmap sized to contain rotated source image
                Rectangle bbox = boundingBox(image, matrix);
                var rotateImage = new Bitmap(bbox.Width, bbox.Height);

                using (Graphics gDest = Graphics.FromImage(rotateImage)) {  // draw source into dest
                    SetHighQuality(gDest);
                    Matrix mDest = new Matrix();
                    mDest.Translate(rotateImage.Width / 2, rotateImage.Height / 2, MatrixOrder.Append);
                    gDest.Transform = mDest;
                    gDest.DrawImage(image, pts);
                    //drawAxes(gDest, Color.Red, 0, 0, 1, 100, "");
                    image.Dispose();
                    image = rotateImage;
                }
            }
        }

        private static Rectangle boundingBox(Image img, Matrix matrix) {
            GraphicsUnit gu = new GraphicsUnit();
            Rectangle rImg = Rectangle.Round(img.GetBounds(ref gu));

            // Transform the four points of the image, to get the resized bounding box.
            Point topLeft = new Point(rImg.Left, rImg.Top);
            Point topRight = new Point(rImg.Right, rImg.Top);
            Point bottomRight = new Point(rImg.Right, rImg.Bottom);
            Point bottomLeft = new Point(rImg.Left, rImg.Bottom);
            Point[] points = new Point[] { topLeft, topRight, bottomRight, bottomLeft };
            GraphicsPath gp = new GraphicsPath(points,
                                                                new byte[] { (byte)PathPointType.Start, (byte)PathPointType.Line, (byte)PathPointType.Line, (byte)PathPointType.Line });
            gp.Transform(matrix);
            return Rectangle.Round(gp.GetBounds());
        }

        private Image Crop(Image source, int width, int height) {
            var croped = new Bitmap(width, height);
            using (var graphics = Graphics.FromImage(croped)) {
                SetHighQuality(graphics);
                int x = (source.Width / 2) - (width / 2);
                int y = (source.Height / 2) - (height / 2);
                graphics.DrawImage(source, new Rectangle(0, 0, width, height), x, y, width, height, GraphicsUnit.Pixel);
            }
            return croped;
        }

        private Image ResizeAndCrop(Image source, int width, int height) {
            float relW = (float)width / source.Width;
            float relH = (float)height / source.Height;

            int resizeWidth, resizeHeight;
            if (relW > relH) {
                resizeWidth = (int)(source.Width * relW);
                resizeHeight = (int)(source.Height * relW);
            } else {
                resizeWidth = (int)(source.Width * relH);
                resizeHeight = (int)(source.Height * relH);
            }

            using (var resized = Resize(source, resizeWidth, resizeHeight)) {
                return Crop(resized, width, height);
            }
        }

        public string SaveThumbnail(string fileName, int size) {
            switch (Disposition) {
                case ImageDisposition.Landscape:
                    var width = (int)(((float)size / CurrentImage.Height) * (float)CurrentImage.Width);
                    using (var thumb = Resize(CurrentImage, width, size)) {
                        using (var croped = Crop(thumb, size, size)) {
                            return Save(croped, fileName);
                        }
                    }
                case ImageDisposition.Portrait:
                    var height = (int)(((float)size / CurrentImage.Width) * (float)CurrentImage.Height); ;
                    using (var thumb = Resize(CurrentImage, size, height)) {
                        using (var croped = Crop(thumb, size, size)) {
                            return Save(croped, fileName);
                        }
                    }
                default:
                    using (var thumb = Resize(CurrentImage, size, size)) {
                        return Save(thumb, fileName);
                    }
            }
        }

        public string SaveThumbnail(string fileName, int width, int height) {
            if (width == height) {
                return SaveThumbnail(fileName, width);
            }

            using (var thumb = ResizeAndCrop(CurrentImage, width, height)) {
                return Save(thumb, fileName);
            }
        }

        public string Save(string fileName) {
            return Save(CurrentImage, fileName);
        }

        private string Save(Image image, string fileName) {
            fileName = GetAvaiableFileName(fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(fileName));

            image.Save(fileName, ImageFormat.Jpeg);

            return fileName;
        }

        private string GetAvaiableFileName(string fileName) {
            if (File.Exists(fileName)) {
                return GetAvaiableFileName(Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName) + "-" + DateTime.Now.Second + Path.GetExtension(fileName)));
            }

            return fileName;
        }

        #region IDisposable Members

        public void Dispose() {
            CurrentImage.Dispose();
        }

        #endregion

        public void AddImage(Image pat, int x, int y) {
            using (var graphics = Graphics.FromImage(CurrentImage)) {
                SetHighQuality(graphics);
                graphics.DrawImage(pat, x, y, pat.Width, pat.Height);
            }
        }
    }
}
