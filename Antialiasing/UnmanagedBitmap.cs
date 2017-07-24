using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Antialiasing
{
    /// <summary>
    /// Представляет <see cref="Bitmap"/> в неуправляемой памяти.
    /// </summary>
    public unsafe class UnmanagedBitmap : ICloneable, IDisposable
    {
        /// <summary>
        /// Для определения избыточных вызовов.
        /// </summary>
        protected bool disposed;

        /// <summary>
        /// Указатель на неуправляемый буфер данных изображения.
        /// </summary>
        public IntPtr ImageData { get; protected set; }

        /// <summary>
        /// Ширина изображения.
        /// </summary>
        public int Width { get; protected set; }

        /// <summary>
        /// Высота изображения.
        /// </summary>
        public int Height { get; protected set; }

        /// <summary>
        /// Ширина развёртки.
        /// </summary>
        public int Stride { get; protected set; }

        /// <summary>
        /// Формат битовой упаковки пикселей.
        /// </summary>
        public PixelFormat PixelFormat { get; protected set; }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="UnmanagedBitmap"/>.
        /// </summary>
        /// <param name="imageData">Указатель на неуправляемый буфер данных изображения.</param>
        /// <param name="width">Ширина изображения.</param>
        /// <param name="height">Высота изображения.</param>
        /// <param name="stride">Ширина развёртки.</param>
        /// <param name="pixelFormat">Формат битовой упаковки пикселей.</param>
        public UnmanagedBitmap(IntPtr imageData, int width, int height, int stride, PixelFormat pixelFormat)
        {
            ImageData = imageData;
            Width = width;
            Height = height;
            Stride = stride;
            PixelFormat = pixelFormat;
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="UnmanagedBitmap"/>.
        /// </summary>
        /// <param name="bitmapData">Экземпляр неуправляемого буфера изображения</param>
        public UnmanagedBitmap(BitmapData bitmapData) : this(bitmapData.Scan0, bitmapData.Width, bitmapData.Height, bitmapData.Stride, bitmapData.PixelFormat) { }

        /// <summary>
        /// Неуправляемый деструктор класса <see cref="UnmanagedBitmap"/>.
        /// </summary>
        ~UnmanagedBitmap() => Dispose(false);

        /// <summary>
        /// Высвобождает ресурсы.
        /// </summary>
        /// <param name="disposing">Определяет, следует ли так же высвободить неуправляемые ресурсы.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // TODO: Добавить освобождение управляемых ресурсов.
            }

            if (disposed && ImageData != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(ImageData);
                GC.RemoveMemoryPressure(Stride * Height);
                ImageData = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Устанавливает значение пикселя.
        /// </summary>
        /// <param name="x">Позиция по ширине.</param>
        /// <param name="y">Позиция по высоте.</param>
        /// <param name="r">Значение Red-канала.</param>
        /// <param name="g">Значение Green-канала.</param>
        /// <param name="b">Значение Blue-канала.</param>
        /// <param name="a">Значение Alpha-канала.</param>
        protected void SetPixel(int x, int y, byte r, byte g, byte b, byte a)
        {
            if (x >= 0 && y >= 0 && x < Width && y < Height)
            {
                int pixelSize = Image.GetPixelFormatSize(PixelFormat) / 8;
                byte* ptr = (byte*)(void*)ImageData + y * Stride + x * pixelSize;
                ushort* ptr2 = (ushort*)ptr;

                switch (PixelFormat)
                {
                    case PixelFormat.Format8bppIndexed:
                        *ptr = (byte)(0.2125 * r + 0.7154 * g + 0.0721 * b);
                        break;

                    case PixelFormat.Format24bppRgb:
                    case PixelFormat.Format32bppRgb:
                        ptr[RGB.R] = r;
                        ptr[RGB.G] = g;
                        ptr[RGB.B] = b;
                        break;

                    case PixelFormat.Format32bppArgb:
                        ptr[RGB.R] = r;
                        ptr[RGB.G] = g;
                        ptr[RGB.B] = b;
                        ptr[RGB.A] = a;
                        break;

                    case PixelFormat.Format16bppGrayScale:
                        *ptr2 = (ushort)((ushort)(0.2125 * r + 0.7154 * g + 0.0721 * b) << 8);
                        break;

                    case PixelFormat.Format48bppRgb:
                        ptr2[RGB.R] = (ushort)(r << 8);
                        ptr2[RGB.G] = (ushort)(g << 8);
                        ptr2[RGB.B] = (ushort)(b << 8);
                        break;

                    case PixelFormat.Format64bppArgb:
                        ptr2[RGB.R] = (ushort)(r << 8);
                        ptr2[RGB.G] = (ushort)(g << 8);
                        ptr2[RGB.B] = (ushort)(b << 8);
                        ptr2[RGB.A] = (ushort)(a << 8);
                        break;

                    default:
                        throw new Exception($"The pixel format is not supported: {PixelFormat}.");
                }
            }
        }

        /// <summary>
        /// Высвобождает ресурсы, занятые экземпляром <see cref="UnmanagedBitmap"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Возвращает полную копию экземпляра <see cref="UnmanagedBitmap"/>.
        /// </summary>
        /// <returns></returns>
        public UnmanagedBitmap Clone()
        {
            IntPtr newImageData = Marshal.AllocHGlobal(Stride * Height);
            GC.AddMemoryPressure(Stride * Height);

            UnmanagedBitmap newImage = new UnmanagedBitmap(newImageData, Width, Height, Stride, PixelFormat)
            {
                disposed = true
            };

            CopyUnmanagedMemory(newImageData, ImageData, Stride * Height);

            return newImage;
        }

        /// <summary>
        /// Возвращает полную копию экземпляра <see cref="UnmanagedBitmap"/>.
        /// </summary>
        /// <returns></returns>
        object ICloneable.Clone() => Clone();

        /// <summary>
        /// Копирует буфер текущего изображения в другой экземпляр <see cref="UnmanagedBitmap"/>.
        /// </summary>
        /// <param name="destImage">Экземпляр <see cref="UnmanagedBitmap"/>, куда следует скопировать буфер текущего.</param>
        public void Copy(UnmanagedBitmap destImage)
        {
            if (Width != destImage.Width || Height != destImage.Height || PixelFormat != destImage.PixelFormat)
                throw new Exception("Destination image has different size or pixel format.");

            if (Stride == destImage.Stride)
            {
                CopyUnmanagedMemory(destImage.ImageData, ImageData, Stride * Height);
            }
            else
            {
                int dstStride = destImage.Stride;
                int copyLength = (Stride < dstStride) ? Stride : dstStride;

                byte* src = (byte*)(void*)ImageData;
                byte* dst = (byte*)(void*)destImage.ImageData;

                for (int i = 0; i < Height; i++)
                {
                    CopyUnmanagedMemory(dst, src, copyLength);

                    dst += dstStride;
                    src += Stride;
                }
            }
        }

        /// <summary>
        /// Возвращает управляемый экземпляр <see cref="Bitmap"/> из текущего экземпляра <see cref="UnmanagedBitmap"/>.
        /// </summary>
        /// <param name="makeCopy">Указывает, следует ли вернуть копию экземпляра.</param>
        /// <returns></returns>
        public Bitmap ToManagedImage(bool makeCopy)
        {
            Bitmap dstImage = null;

            try
            {
                if (!makeCopy)
                {
                    dstImage = new Bitmap(Width, Height, Stride, PixelFormat, ImageData);

                    if (PixelFormat == PixelFormat.Format8bppIndexed) SetGrayscalePalette(dstImage);
                }
                else
                {
                    dstImage = (PixelFormat == PixelFormat.Format8bppIndexed) ? CreateGrayscaleImage(Width, Height) : new Bitmap(Width, Height, PixelFormat);

                    BitmapData dstData = dstImage.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadWrite, PixelFormat);

                    int dstStride = dstData.Stride;
                    int lineSize = Math.Min(Stride, dstStride);

                    byte* dst = (byte*)(void*)dstData.Scan0;
                    byte* src = (byte*)(void*)ImageData;

                    if (Stride != dstStride)
                    {
                        for (int y = 0; y < Height; y++)
                        {
                            CopyUnmanagedMemory(dst, src, lineSize);
                            dst += dstStride;
                            src += Stride;
                        }
                    }
                    else
                    {
                        CopyUnmanagedMemory(dst, src, Stride * Height);
                    }

                    dstImage.UnlockBits(dstData);
                }

                return dstImage;
            }
            catch (Exception)
            {
                if (dstImage != null) dstImage.Dispose();

                throw new Exception("The unmanaged image has some invalid properties, which results in failure of converting it to managed image.");
            }
        }

        /// <summary>
        /// Возвращает управляемый экземпляр <see cref="Bitmap"/> из текущего экземпляра <see cref="UnmanagedBitmap"/>.
        /// <returns></returns>
        public Bitmap ToManagedImage() => ToManagedImage(true);

        /// <summary>
        /// Упаковывает пиксели в <see cref="PixelFormat.Format8bppIndexed"/>.
        /// </summary>
        /// <param name="points">Список исходных пикселей для упаковки.</param>
        /// <returns></returns>
        public byte[] Collect8bppPixelValues(List<Point> points)
        {
            int pixelSize = Image.GetPixelFormatSize(PixelFormat) / 8;

            if (PixelFormat == PixelFormat.Format16bppGrayScale || pixelSize > 4)
                throw new Exception("Unsupported pixel format of the source image. Use Collect16bppPixelValues() method for it.");

            byte[] pixelValues = new byte[points.Count * (PixelFormat == PixelFormat.Format8bppIndexed ? 1 : 3)];

            byte* basePtr = (byte*)(void*)ImageData;
            byte* ptr;

            if (PixelFormat == PixelFormat.Format8bppIndexed)
            {
                int i = 0;

                foreach (Point point in points)
                {
                    ptr = basePtr + Stride * point.Y + point.X;
                    pixelValues[i++] = *ptr;
                }
            }
            else
            {
                int i = 0;

                foreach (Point point in points)
                {
                    ptr = basePtr + Stride * point.Y + point.X * pixelSize;
                    pixelValues[i++] = ptr[RGB.R];
                    pixelValues[i++] = ptr[RGB.G];
                    pixelValues[i++] = ptr[RGB.B];
                }
            }

            return pixelValues;
        }

        /// <summary>
        /// Упаковывает пиксели в <see cref="PixelFormat.Format16bppGrayScale"/>.
        /// </summary>
        /// <param name="points">Список исходных пикселей для упаковки.</param>
        /// <returns></returns>
        public ushort[] Collect16bppPixelValues(List<Point> points)
        {
            int pixelSize = Image.GetPixelFormatSize(PixelFormat) / 8;

            if (PixelFormat == PixelFormat.Format8bppIndexed || pixelSize == 3 || pixelSize == 4)
                throw new Exception("Unsupported pixel format of the source image. Use Collect8bppPixelValues() method for it.");

            ushort[] pixelValues = new ushort[points.Count * (PixelFormat == PixelFormat.Format16bppGrayScale ? 1 : 3)];

            byte* basePtr = (byte*)(void*)ImageData;
            ushort* ptr;

            if (PixelFormat == PixelFormat.Format16bppGrayScale)
            {
                int i = 0;

                foreach (Point point in points)
                {
                    ptr = (ushort*)(basePtr + Stride * point.Y + point.X * pixelSize);
                    pixelValues[i++] = *ptr;
                }
            }
            else
            {
                int i = 0;

                foreach (Point point in points)
                {
                    ptr = (ushort*)(basePtr + Stride * point.Y + point.X * pixelSize);
                    pixelValues[i++] = ptr[RGB.R];
                    pixelValues[i++] = ptr[RGB.G];
                    pixelValues[i++] = ptr[RGB.B];
                }
            }

            return pixelValues;
        }

        /// <summary>
        /// Возвращает список пикселей, вошедших в область выделения.
        /// </summary>
        /// <param name="rect">Область выделения.</param>
        /// <returns></returns>
        public List<Point> CollectActivePixels(Rectangle rect)
        {
            List<Point> pixels = new List<Point>();
            int pixelSize = Image.GetPixelFormatSize(PixelFormat) / 8;

            rect.Intersect(new Rectangle(0, 0, Width, Height));

            int startX = rect.X;
            int startY = rect.Y;
            int stopX = rect.Right;
            int stopY = rect.Bottom;

            byte* basePtr = (byte*)(void*)ImageData;

            if (PixelFormat == PixelFormat.Format16bppGrayScale || pixelSize > 4)
            {
                int pixelWords = pixelSize >> 1;

                for (int y = startY; y < stopY; y++)
                {
                    ushort* ptr = (ushort*)(basePtr + y * Stride + startX * pixelSize);

                    if (pixelWords == 1)
                    {
                        for (int x = startX; x < stopX; x++, ptr++) if (*ptr != 0) pixels.Add(new Point(x, y));
                    }
                    else
                    {
                        for (int x = startX; x < stopX; x++, ptr += pixelWords)
                            if ((ptr[RGB.R] != 0) || (ptr[RGB.G] != 0) || (ptr[RGB.B] != 0)) pixels.Add(new Point(x, y));
                    }
                }
            }
            else
            {
                for (int y = startY; y < stopY; y++)
                {
                    byte* ptr = basePtr + y * Stride + startX * pixelSize;

                    if (pixelSize == 1)
                    {
                        for (int x = startX; x < stopX; x++, ptr++) if (*ptr != 0) pixels.Add(new Point(x, y));
                    }
                    else
                    {
                        for (int x = startX; x < stopX; x++, ptr += pixelSize)
                            if ((ptr[RGB.R] != 0) || (ptr[RGB.G] != 0) || (ptr[RGB.B] != 0)) pixels.Add(new Point(x, y));
                    }
                }
            }

            return pixels;
        }

        /// <summary>
        /// Возвращает список всех пикселей изображения.
        /// </summary>
        /// <returns></returns>
        public List<Point> CollectActivePixels() => CollectActivePixels(new Rectangle(0, 0, Width, Height));

        /// <summary>
        /// Возвращает цвет пикселя по точкам пересечения координат на битовой карте.
        /// </summary>
        /// <param name="x">Позиция по ширине.</param>
        /// <param name="y">Позиция по высоте.</param>
        /// <returns></returns>
        public Color GetPixel(int x, int y)
        {
            if (x < 0 || y < 0) throw new ArgumentOutOfRangeException("x", "The specified pixel coordinate is out of image's bounds.");
            if (x >= Width || y >= Height) throw new ArgumentOutOfRangeException("y", "The specified pixel coordinate is out of image's bounds.");

            Color color = new Color();

            int pixelSize = Image.GetPixelFormatSize(PixelFormat) / 8;
            byte* ptr = (byte*)(void*)ImageData + y * Stride + x * pixelSize;

            switch (PixelFormat)
            {
                case PixelFormat.Format8bppIndexed:
                    color = Color.FromArgb(*ptr, *ptr, *ptr);
                    break;

                case PixelFormat.Format24bppRgb:
                case PixelFormat.Format32bppRgb:
                    color = Color.FromArgb(ptr[RGB.R], ptr[RGB.G], ptr[RGB.B]);
                    break;

                case PixelFormat.Format32bppArgb:
                    color = Color.FromArgb(ptr[RGB.A], ptr[RGB.R], ptr[RGB.G], ptr[RGB.B]);
                    break;

                default:
                    throw new Exception($"The pixel format is not supported: {PixelFormat}.");
            }

            return color;
        }

        /// <summary>
        /// Возвращает цвет пикселя по исходной точке.
        /// </summary>
        /// <param name="point">Исходная точка батовой карты.</param>
        /// <returns></returns>
        public Color GetPixel(Point point) => GetPixel(point.X, point.Y);

        /// <summary>
        /// Задаёт цвет указанному списку пикселей.
        /// </summary>
        /// <param name="coordinates">Список пикселей.</param>
        /// <param name="color">Цвет.</param>
        public void SetPixels(List<Point> coordinates, Color color)
        {
            int pixelSize = Image.GetPixelFormatSize(PixelFormat) / 8;
            byte* basePtr = (byte*)(void*)ImageData;

            byte red = color.R;
            byte green = color.G;
            byte blue = color.B;
            byte alpha = color.A;

            switch (PixelFormat)
            {
                case PixelFormat.Format8bppIndexed:
                    {
                        byte grayValue = (byte)(0.2125 * red + 0.7154 * green + 0.0721 * blue);

                        foreach (Point point in coordinates)
                        {
                            if (point.X >= 0 && point.Y >= 0 && point.X < Width && point.Y < Height)
                            {
                                byte* ptr = basePtr + point.Y * Stride + point.X;
                                *ptr = grayValue;
                            }
                        }
                    }
                    break;

                case PixelFormat.Format24bppRgb:
                case PixelFormat.Format32bppRgb:
                    {
                        foreach (Point point in coordinates)
                        {
                            if (point.X >= 0 && point.Y >= 0 && point.X < Width && point.Y < Height)
                            {
                                byte* ptr = basePtr + point.Y * Stride + point.X * pixelSize;
                                ptr[RGB.R] = red;
                                ptr[RGB.G] = green;
                                ptr[RGB.B] = blue;
                            }
                        }
                    }
                    break;

                case PixelFormat.Format32bppArgb:
                    {
                        foreach (Point point in coordinates)
                        {
                            if (point.X >= 0 && point.Y >= 0 && point.X < Width && point.Y < Height)
                            {
                                byte* ptr = basePtr + point.Y * Stride + point.X * pixelSize;
                                ptr[RGB.R] = red;
                                ptr[RGB.G] = green;
                                ptr[RGB.B] = blue;
                                ptr[RGB.A] = alpha;
                            }
                        }
                    }
                    break;

                case PixelFormat.Format16bppGrayScale:
                    {
                        ushort grayValue = (ushort)((ushort)(0.2125 * red + 0.7154 * green + 0.0721 * blue) << 8);

                        foreach (Point point in coordinates)
                        {
                            if (point.X >= 0 && point.Y >= 0 && point.X < Width && point.Y < Height)
                            {
                                ushort* ptr = (ushort*)(basePtr + point.Y * Stride) + point.X;
                                *ptr = grayValue;
                            }
                        }
                    }
                    break;

                case PixelFormat.Format48bppRgb:
                    {
                        ushort red16 = (ushort)(red << 8);
                        ushort green16 = (ushort)(green << 8);
                        ushort blue16 = (ushort)(blue << 8);

                        foreach (Point point in coordinates)
                        {
                            if (point.X >= 0 && point.Y >= 0 && point.X < Width && point.Y < Height)
                            {
                                ushort* ptr = (ushort*)(basePtr + point.Y * Stride + point.X * pixelSize);
                                ptr[RGB.R] = red16;
                                ptr[RGB.G] = green16;
                                ptr[RGB.B] = blue16;
                            }
                        }
                    }
                    break;

                case PixelFormat.Format64bppArgb:
                    {
                        ushort red16 = (ushort)(red << 8);
                        ushort green16 = (ushort)(green << 8);
                        ushort blue16 = (ushort)(blue << 8);
                        ushort alpha16 = (ushort)(alpha << 8);

                        foreach (Point point in coordinates)
                        {
                            if (point.X >= 0 && point.Y >= 0 && point.X < Width && point.Y < Height)
                            {
                                ushort* ptr = (ushort*)(basePtr + point.Y * Stride + point.X * pixelSize);
                                ptr[RGB.R] = red16;
                                ptr[RGB.G] = green16;
                                ptr[RGB.B] = blue16;
                                ptr[RGB.A] = alpha16;
                            }
                        }
                    }
                    break;

                default:
                    throw new Exception($"The pixel format is not supported: {PixelFormat}.");
            }
        }

        /// <summary>
        /// Задаёт цвет указанным координатам пикселя.
        /// </summary>
        /// <param name="x">Позиция по ширине.</param>
        /// <param name="y">Позиция по высоте.</param>
        /// <param name="value">Значение цвета.</param>
        public void SetPixel(int x, int y, byte value) => SetPixel(x, y, value, value, value, 255);

        /// <summary>
        /// Задаёт цвет указанным координатам пикселя.
        /// </summary>
        /// <param name="x">Позиция по ширине.</param>
        /// <param name="y">Позиция по высоте.</param>
        /// <param name="color">Значение цвета.</param>
        public void SetPixel(int x, int y, Color color) => SetPixel(x, y, color.R, color.G, color.B, color.A);

        /// <summary>
        /// Задаёт цвет указанному пикселю.
        /// </summary>
        /// <param name="point">Исходных пиксель.</param>
        /// <param name="color">Значение цвета.</param>
        public void SetPixel(Point point, Color color) => SetPixel(point.X, point.Y, color);

        /// <summary>
        /// Создаёт изображение формата <see cref="PixelFormat.Format8bppIndexed"/>.
        /// </summary>
        /// <param name="width">Ширина изображения.</param>
        /// <param name="height">Высота изображения.</param>
        /// <returns></returns>
        public static Bitmap CreateGrayscaleImage(int width, int height)
        {
            Bitmap image = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
            SetGrayscalePalette(image);
            return image;
        }

        /// <summary>
        /// Заливает <see cref="Bitmap"/> серым цветом.
        /// </summary>
        /// <param name="image">Исходный экземпляр <see cref="Bitmap"/>.</param>
        public static void SetGrayscalePalette(Bitmap image)
        {
            if (image.PixelFormat != PixelFormat.Format8bppIndexed) throw new Exception("Source image is not 8 bpp image.");

            ColorPalette cp = image.Palette;

            for (int i = 0; i < 256; i++) cp.Entries[i] = Color.FromArgb(i, i, i);

            image.Palette = cp;
        }

        /// <summary>
        /// Копирует неуправляемую память.
        /// </summary>
        /// <param name="dst">Указатель на буфер назначения.</param>
        /// <param name="src">Указатель на исходный буфер.</param>
        /// <param name="count">Число байт копирования.</param>
        /// <returns></returns>
        internal static unsafe byte* CopyUnmanagedMemory(byte* dst, byte* src, int count)
        {
#if WINDOWS
            return CopyMemory(dst, src, count);
#else
            int countUint = count >> 2;
            int countByte = count & 3;

            uint* dstUint = (uint*)dst;
            uint* srcUint = (uint*)src;

            while (countUint-- != 0) *dstUint++ = *srcUint++;

            byte* dstByte = (byte*)dstUint;
            byte* srcByte = (byte*)srcUint;

            while (countByte-- != 0) *dstByte++ = *srcByte++;

            return dst;
#endif
        }

        /// <summary>
        /// Копирует неуправляемую память.
        /// </summary>
        /// <param name="dst">Указатель на буфер назначения.</param>
        /// <param name="src">Указатель на исходный буфер.</param>
        /// <param name="count">Число байт копирования.</param>
        /// <returns></returns>
        internal static IntPtr CopyUnmanagedMemory(IntPtr dst, IntPtr src, int count)
        {
            CopyUnmanagedMemory((byte*)(void*)dst, (byte*)(void*)src, count);
            return dst;
        }

        /// <summary>
        /// Заполняет неуправляемую память заданным значением.
        /// </summary>
        /// <param name="dst">Указатель на буфер назначения.</param>
        /// <param name="filler">Значение для заполнения.</param>
        /// <param name="count">Число байт буфера для заполнения.</param>
        /// <returns></returns>
        internal static unsafe byte* SetUnmanagedMemory(byte* dst, int filler, int count)
        {
#if WINDOWS
            return SetMemory(dst, filler, count);
#else
            int countUint = count >> 2;
            int countByte = count & 3;

            byte fillerByte = (byte)filler;
            uint fiilerUint = (uint)filler | (uint)filler << 8 | (uint)filler << 16;    // | (uint)filler << 24;

            uint* dstUint = (uint*)dst;

            while (countUint-- != 0) *dstUint++ = fiilerUint;

            byte* dstByte = (byte*)dstUint;

            while (countByte-- != 0) *dstByte++ = fillerByte;

            return dst;
#endif
        }

        /// <summary>
        /// Заполняет неуправляемую память заданным значением.
        /// </summary>
        /// <param name="dst">Указатель на буфер назначения.</param>
        /// <param name="filler">Значение для заполнения.</param>
        /// <param name="count">Число байт буфера для заполнения.</param>
        /// <returns></returns>
        internal static IntPtr SetUnmanagedMemory(IntPtr dst, int filler, int count)
        {
            SetUnmanagedMemory((byte*)(void*)dst, filler, count);
            return dst;
        }

        /// <summary>
        /// Получает неуправляемый экземпляр <see cref="UnmanagedBitmap"/> из <see cref="BitmapData"/>.
        /// </summary>
        /// <param name="imageData">Исходный экземпляр <see cref="BitmapData"/>.</param>
        /// <returns></returns>
        public static UnmanagedBitmap FromManagedImage(BitmapData imageData)
        {
            PixelFormat pixelFormat = imageData.PixelFormat;

            if (pixelFormat != PixelFormat.Format8bppIndexed && pixelFormat != PixelFormat.Format16bppGrayScale && pixelFormat != PixelFormat.Format24bppRgb
                && pixelFormat != PixelFormat.Format32bppRgb && pixelFormat != PixelFormat.Format32bppArgb && pixelFormat != PixelFormat.Format32bppPArgb
                && pixelFormat != PixelFormat.Format48bppRgb && pixelFormat != PixelFormat.Format64bppArgb && pixelFormat != PixelFormat.Format64bppPArgb)
                throw new Exception("Unsupported pixel format of the source image.");

            IntPtr dstImageData = Marshal.AllocHGlobal(imageData.Stride * imageData.Height);
            GC.AddMemoryPressure(imageData.Stride * imageData.Height);

            UnmanagedBitmap image = new UnmanagedBitmap(dstImageData, imageData.Width, imageData.Height, imageData.Stride, pixelFormat);
            CopyUnmanagedMemory(dstImageData, imageData.Scan0, imageData.Stride * imageData.Height);
            image.disposed = true;

            return image;
        }

        /// <summary>
        /// Получает неуправляемый экземпляр <see cref="UnmanagedBitmap"/> из управляемого <see cref="Bitmap"/>.
        /// </summary>
        /// <param name="imageData">Исходный экземпляр <see cref="Bitmap"/>.</param>
        /// <returns></returns>
        public static UnmanagedBitmap FromManagedImage(Bitmap image)
        {
            UnmanagedBitmap dstImage = null;
            BitmapData sourceData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, image.PixelFormat);

            try
            {
                dstImage = FromManagedImage(sourceData);
            }
            finally
            {
                image.UnlockBits(sourceData);
            }

            return dstImage;
        }
#if WINDOWS
        /// <summary>
        /// Копирует неуправляемую память.
        /// </summary>
        /// <param name="dst">Указатель на буфер назначения.</param>
        /// <param name="src">Указатель на исходный буфер.</param>
        /// <param name="count">Число байт копирования.</param>
        /// <returns></returns>
        [DllImport("ntdll.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl)]
        protected static unsafe extern byte* CopyMemory(byte* dst, byte* src, int count);

        /// <summary>
        /// Заполняет неуправляемую память заданным значением.
        /// </summary>
        /// <param name="dst">Указатель на буфер назначения.</param>
        /// <param name="filler">Значение для заполнения.</param>
        /// <param name="count">Число байт буфера для заполнения.</param>
        /// <returns></returns>
        [DllImport("ntdll.dll", EntryPoint = "memset", CallingConvention = CallingConvention.Cdecl)]
        protected static unsafe extern byte* SetMemory(byte* dst, int filler, int count);
#endif
        /// <summary>
        /// Создаёт новый экземпляр <see cref="UnmanagedBitmap"/>.
        /// </summary>
        /// <param name="width">Ширина изображения.</param>
        /// <param name="height">Высота изображения.</param>
        /// <param name="pixelFormat">Формат битовой упаковки пикселей.</param>
        /// <returns></returns>
        public static UnmanagedBitmap Create(int width, int height, PixelFormat pixelFormat)
        {
            int bytesPerPixel = Image.GetPixelFormatSize(pixelFormat) / 8;

            //switch (pixelFormat)
            //{
            //    case PixelFormat.Format8bppIndexed:
            //        bytesPerPixel = 1;
            //        break;

            //    case PixelFormat.Format16bppGrayScale:
            //        bytesPerPixel = 2;
            //        break;

            //    case PixelFormat.Format24bppRgb:
            //        bytesPerPixel = 3;
            //        break;

            //    case PixelFormat.Format32bppRgb:
            //    case PixelFormat.Format32bppArgb:
            //    case PixelFormat.Format32bppPArgb:
            //        bytesPerPixel = 4;
            //        break;

            //    case PixelFormat.Format48bppRgb:
            //        bytesPerPixel = 6;
            //        break;

            //    case PixelFormat.Format64bppArgb:
            //    case PixelFormat.Format64bppPArgb:
            //        bytesPerPixel = 8;
            //        break;

            //    default:
            //        throw new Exception("Can not create image with specified pixel format.");
            //}

            if (width <= 0 || height <= 0) throw new Exception("Invalid image size specified.");

            int stride = width * bytesPerPixel;

            if (stride % 4 != 0) stride += (4 - (stride % 4));

            IntPtr imageData = Marshal.AllocHGlobal(stride * height);
            SetUnmanagedMemory(imageData, 0, stride * height);
            GC.AddMemoryPressure(stride * height);

            UnmanagedBitmap image = new UnmanagedBitmap(imageData, width, height, stride, pixelFormat)
            {
                disposed = true
            };

            return image;
        }
    }
}