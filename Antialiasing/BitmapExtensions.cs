using System.Drawing;
using System.Drawing.Imaging;

namespace Antialiasing
{
    /// <summary>
    /// Представляет расширения для <see cref="Bitmap"/>.
    /// </summary>
    public static unsafe class BitmapExtensions
    {
        /// <summary>
        /// Изменяет размеры исходного изображения без потерь качества.
        /// </summary>
        /// <param name="sourceData">Исходное изображение.</param>
        /// <param name="width">Новая ширина.</param>
        /// <param name="height">Новая высота.</param>
        /// <param name="algorithm">Алгоритм изменения размера.</param>
        /// <returns></returns>
        public static Bitmap Resize(this BitmapData sourceData, int width, int height, AntialiasingMethod algorithm)
        {
            Bitmap destinationImage = sourceData.PixelFormat == PixelFormat.Format8bppIndexed
                    ? UnmanagedBitmap.CreateGrayscaleImage(width, height)
                    : new Bitmap(width, height, sourceData.PixelFormat);
            
            BitmapData destinationData = destinationImage.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, sourceData.PixelFormat);

            try
            {
                using (UnmanagedBitmap unmSource = new UnmanagedBitmap(sourceData))
                using (UnmanagedBitmap unmDestination = new UnmanagedBitmap(destinationData))
                    AntialiasingFilter.Process(unmSource, unmDestination, width, height, algorithm);
            }
            finally
            {
                destinationImage.UnlockBits(destinationData);
            }

            destinationImage.SetResolution(300, 300);

            return destinationImage;
        }

        /// <summary>
        /// Изменяет размеры исходного изображения без потерь качества.
        /// </summary>
        /// <param name="sourceData">Исходное изображение.</param>
        /// <param name="newSize">Новые размеры.</param>
        /// <param name="algorithm">Алгоритм изменения размера.</param>
        /// <returns></returns>
        public static Bitmap Resize(this BitmapData sourceData, Size newSize, AntialiasingMethod algorithm)
            => Resize(sourceData, newSize.Width, newSize.Height, algorithm);

        /// <summary>
        /// Изменяет размеры исходного изображения без потерь качества.
        /// </summary>
        /// <param name="sourceData">Исходное изображение.</param>
        /// <param name="width">Новая ширина.</param>
        /// <param name="height">Новая высота.</param>
        /// <returns></returns>
        public static Bitmap Resize(this BitmapData sourceData, int width, int height) => Resize(sourceData, width, height, AntialiasingMethod.NearestNeighbor);

        /// <summary>
        /// Изменяет размеры исходного изображения без потерь качества.
        /// </summary>
        /// <param name="sourceData">Исходное изображение.</param>
        /// <param name="newSize">Новые размеры.</param>
        /// <returns></returns>
        public static Bitmap Resize(this BitmapData sourceData, Size newSize) => Resize(sourceData, newSize, AntialiasingMethod.NearestNeighbor);

        /// <summary>
        /// Изменяет размеры исходного изображения без потерь качества.
        /// </summary>
        /// <param name="sourceImage">Исходное изображение.</param>
        /// <param name="width">Новая ширина.</param>
        /// <param name="height">Новая высота.</param>
        /// <param name="algorithm">Алгоритм изменения размера.</param>
        /// <returns></returns>
        public static Bitmap Resize(this Bitmap sourceImage, int width, int height, AntialiasingMethod algorithm)
        {
            BitmapData sourceData = sourceImage.LockBits(new Rectangle(0, 0, sourceImage.Width, sourceImage.Height), ImageLockMode.ReadOnly, sourceImage.PixelFormat);

            try
            {
                return Resize(sourceData, width, height, algorithm);
            }
            finally
            {
                sourceImage.UnlockBits(sourceData);
            }
        }

        /// <summary>
        /// Изменяет размеры исходного изображения без потерь качества.
        /// </summary>
        /// <param name="sourceImage">Исходное изображение.</param>
        /// <param name="newSize">Новые размеры.</param>
        /// <param name="algorithm">Алгоритм изменения размера.</param>
        /// <returns></returns>
        public static Bitmap Resize(this Bitmap sourceImage, Size newSize, AntialiasingMethod algorithm)
            => Resize(sourceImage, newSize.Width, newSize.Height, algorithm);

        /// <summary>
        /// Изменяет размеры исходного изображения без потерь качества.
        /// </summary>
        /// <param name="sourceImage">Исходное изображение.</param>
        /// <param name="width">Новая ширина.</param>
        /// <param name="height">Новая высота.</param>
        /// <returns></returns>
        public static Bitmap Resize(this Bitmap sourceImage, int width, int height) => Resize(sourceImage, width, height, AntialiasingMethod.NearestNeighbor);

        /// <summary>
        /// Изменяет размеры исходного изображения без потерь качества.
        /// </summary>
        /// <param name="sourceImage">Исходное изображение.</param>
        /// <param name="newSize">Новые размеры.</param>
        /// <returns></returns>
        public static Bitmap Resize(this Bitmap sourceImage, Size newSize) => Resize(sourceImage, newSize, AntialiasingMethod.NearestNeighbor);

        /// <summary>
        /// Вырезает указанную зону изображения.
        /// </summary>
        /// <param name="sourceImage">Исходное изображение.</param>
        /// <param name="rectangle">Участок изображения.</param>
        /// <returns></returns>
        public static Bitmap Crop(this Bitmap sourceImage, Rectangle rectangle)
        {
            if (sourceImage.Width == rectangle.Width && sourceImage.Height == rectangle.Height) return sourceImage;

            BitmapData sourceImageData = sourceImage.LockBits(new Rectangle(0, 0, sourceImage.Width, sourceImage.Height), ImageLockMode.ReadOnly, sourceImage.PixelFormat);
            Bitmap croppedImage = new Bitmap(rectangle.Width, rectangle.Height, sourceImage.PixelFormat);
            BitmapData croppedImageData = croppedImage.LockBits(new Rectangle(0, 0, rectangle.Width, rectangle.Height), ImageLockMode.WriteOnly, croppedImage.PixelFormat);
            int pixelSize = Image.GetPixelFormatSize(croppedImageData.PixelFormat) / 8;
            byte* src = (byte*)(void*)sourceImageData.Scan0 + rectangle.Y * sourceImageData.Stride + rectangle.X * pixelSize;
            byte* dest = (byte*)(void*)croppedImageData.Scan0;

            for (int y = 0; y < rectangle.Height; y++)
            {
                int srcIndex = y * sourceImageData.Stride;
                int croppedIndex = y * croppedImageData.Stride;

                UnmanagedBitmap.CopyUnmanagedMemory(dest + croppedIndex, src + srcIndex, croppedImageData.Stride);
            }

            croppedImage.UnlockBits(croppedImageData);
            sourceImage.UnlockBits(sourceImageData);

            return croppedImage;
        }

        /// <summary>
        /// Вырезает указанную зону изображения.
        /// </summary>
        /// <param name="sourceImage">Исходное изображение.</param>
        /// <param name="x">Позиция по ширине.</param>
        /// <param name="y">Позиция по высоте.</param>
        /// <param name="width">Ширина.</param>
        /// <param name="height">Высота.</param>
        /// <returns></returns>
        public static Bitmap Crop(this Bitmap sourceImage, int x, int y, int width, int height) => Crop(sourceImage, new Rectangle(x, y, width, height));

        /// <summary>
        /// Вырезает указанную зону изображения.
        /// </summary>
        /// <param name="sourceImage">Исходное изображение.</param>
        /// <param name="location">Позиция на изображении.</param>
        /// <param name="size">Размеры выделения.</param>
        /// <returns></returns>
        public static Bitmap Crop(this Bitmap sourceImage, Point location, Size size) => Crop(sourceImage, new Rectangle(location, size));

        /// <summary>
        /// Вырезает указанную зону изображения.
        /// </summary>
        /// <param name="sourceImage">Исходное изображение.</param>
        /// <param name="location">Позиция на изображении.</param>
        /// <param name="width">Ширина.</param>
        /// <param name="height">Высота.</param>
        /// <returns></returns>
        public static Bitmap Crop(this Bitmap sourceImage, Point location, int width, int height) => Crop(sourceImage, location, new Size(width, height));

        /// <summary>
        /// Вырезает указанную зону изображения.
        /// </summary>
        /// <param name="sourceImage">Исходное изображение.</param>
        /// <param name="x">Позиция по ширине.</param>
        /// <param name="y">Позиция по высоте.</param>
        /// <param name="size">Размеры выделения.</param>
        /// <returns></returns>
        public static Bitmap Crop(this Bitmap sourceImage, int x, int y, Size size) => Crop(sourceImage, new Point(x, y), size);

        /// <summary>
        /// Определяет, является ли изображение размытым.
        /// </summary>
        /// <param name="sourceImage">Исходное изображение.</param>
        /// <param name="max">Степень размытия.</param>
        /// <returns></returns>
        public static bool HasAliasing(this Bitmap sourceImage, out float max)
        {
            BitmapData sourceData = sourceImage.LockBits(new Rectangle(0, 0, sourceImage.Width, sourceImage.Height), ImageLockMode.ReadOnly, sourceImage.PixelFormat);

            try
            {
                return !Convolution.Check(sourceData, out max);
            }
            finally
            {
                sourceImage.UnlockBits(sourceData);
            }
        }

        /// <summary>
        /// Определяет, является ли изображение размытым.
        /// </summary>
        /// <param name="sourceImage">Исходное изображение.</param>
        /// <returns></returns>
        public static bool HasAliasing(this Bitmap sourceImage) => HasAliasing(sourceImage, out float max);
    }
}