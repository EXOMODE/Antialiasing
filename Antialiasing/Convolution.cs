using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Security;

namespace Antialiasing
{
    /// <summary>
    /// Представляет алгоритмы конволюции.
    /// </summary>
    internal unsafe class Convolution
    {
        /// <summary>
        /// Матрица Лапласа.
        /// </summary>
        private int[,] kernel;

        /// <summary>
        /// Делитель.
        /// </summary>
        private int divisor = 1;

        /// <summary>
        /// Размер матрицы.
        /// </summary>
        private int size;

        /// <summary>
        /// Возвращает или задаёт карту Лапласа.
        /// </summary>
        public int[,] Kernel
        {
            get => kernel;

            set
            {
                int s = value.GetLength(0);

                if ((s != value.GetLength(1)) || (s < 3) || (s > 99) || (s % 2 == 0)) throw new ArgumentException("Invalid kernel size.");

                kernel = value;
                size = s;
            }
        }

        /// <summary>
        /// Возвращает или задаёт делитель.
        /// </summary>
        public int Divisor
        {
            get => divisor;

            set
            {
                if (value == 0) throw new ArgumentException("Divisor can not be equal to zero.");

                divisor = value;
            }
        }

        /// <summary>
        /// Разброс.
        /// </summary>
        public int Threshold { get; set; }

        /// <summary>
        /// Динамический коэффициент для усечения граней.
        /// </summary>
        public bool DynamicDivisorForEdges { get; set; }

        /// <summary>
        /// Определяет, следует ли обрабатывать Alpha-канал.
        /// </summary>
        public bool ProcessAlpha { get; set; }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="Convolution"/>.
        /// </summary>
        /// <param name="kernel">Исходная матрица Лапласа.</param>
        public Convolution(int[,] kernel)
        {
            Kernel = kernel;
            divisor = 0;

            for (int i = 0, n = kernel.GetLength(0); i < n; i++)
                for (int j = 0, k = kernel.GetLength(1); j < k; j++)
                    divisor += kernel[i, j];

            if (divisor == 0) divisor = 1;
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="Convolution"/>.
        /// </summary>
        /// <param name="kernel">Исходная матрица Лапласа.</param>
        /// <param name="divisor">Делитель.</param>
        public Convolution(int[,] kernel, int divisor)
        {
            Kernel = kernel;
            Divisor = divisor;
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="Convolution"/>.
        /// </summary>
        public Convolution() : this(new int[3, 3]
        {
            { 0,  1,  0, },
            { 1, -4,  1, },
            { 0,  1,  0, },
        }) { }

        /// <summary>
        /// Обрабатывает битовую карту формата <see cref="PixelFormat.Format8bppIndexed"/>.
        /// </summary>
        /// <param name="src">Указатель на исходный буфер карты.</param>
        /// <param name="dst">Указатель на буфер карты назначения.</param>
        /// <param name="srcStride">Ширина развёртки исходной карты.</param>
        /// <param name="dstStride">Ширина развёртки карты назначения.</param>
        /// <param name="srcOffset">Смещение исходной карты.</param>
        /// <param name="dstOffset">Смещение карты назначения.</param>
        /// <param name="startX">Начальный индекс по ширине карты.</param>
        /// <param name="startY">Начальный индекс по высоте карты.</param>
        /// <param name="stopX">Конечный индекс по ширине карты.</param>
        /// <param name="stopY">Конечный индекс по высоте карты.</param>
        private void Process8bppImage(byte* src, byte* dst, int srcStride, int dstStride, int srcOffset, int dstOffset, int startX, int startY, int stopX, int stopY)
        {
            int i, j, t, k, ir, jr;
            int radius = size >> 1;
            long g, div;
            int kernelSize = size * size;
            int processedKernelSize;

            for (int y = startY; y < stopY; y++)
            {
                for (int x = startX; x < stopX; x++, src++, dst++)
                {
                    g = div = processedKernelSize = 0;

                    for (i = 0; i < size; i++)
                    {
                        ir = i - radius;
                        t = y + ir;

                        if (t < startY) continue;
                        if (t >= stopY) break;

                        for (j = 0; j < size; j++)
                        {
                            jr = j - radius;
                            t = x + jr;

                            if (t < startX) continue;

                            if (t < stopX)
                            {
                                k = kernel[i, j];
                                div += k;
                                g += k * src[ir * srcStride + jr];
                                processedKernelSize++;
                            }
                        }
                    }

                    if (processedKernelSize == kernelSize)
                    {
                        div = divisor;
                    }
                    else
                    {
                        if (!DynamicDivisorForEdges) div = divisor;
                    }

                    if (div != 0) g /= div;

                    g += Threshold;
                    *dst = (byte)((g > 255) ? 255 : ((g < 0) ? 0 : g));
                }

                src += srcOffset;
                dst += dstOffset;
            }
        }

        /// <summary>
        /// Обрабатывает битовую карту формата <see cref="PixelFormat.Format24bppRgb"/>.
        /// </summary>
        /// <param name="src">Указатель на исходный буфер карты.</param>
        /// <param name="dst">Указатель на буфер карты назначения.</param>
        /// <param name="srcStride">Ширина развёртки исходной карты.</param>
        /// <param name="dstStride">Ширина развёртки карты назначения.</param>
        /// <param name="srcOffset">Смещение исходной карты.</param>
        /// <param name="dstOffset">Смещение карты назначения.</param>
        /// <param name="startX">Начальный индекс по ширине карты.</param>
        /// <param name="startY">Начальный индекс по высоте карты.</param>
        /// <param name="stopX">Конечный индекс по ширине карты.</param>
        /// <param name="stopY">Конечный индекс по высоте карты.</param>
        /// <param name="pixelSize">Число байт на один пиксель.</param>
        private void Process24bppImage(byte* src, byte* dst, int srcStride, int dstStride, int srcOffset, int dstOffset, int startX, int startY, int stopX,
            int stopY, int pixelSize)
        {
            int i, j, t, k, ir, jr;
            int radius = size >> 1;
            long r, g, b, div;
            int kernelSize = size * size;
            int processedKernelSize;

            byte* p;

            for (int y = startY; y < stopY; y++)
            {
                for (int x = startX; x < stopX; x++, src += pixelSize, dst += pixelSize)
                {
                    r = g = b = div = processedKernelSize = 0;

                    for (i = 0; i < size; i++)
                    {
                        ir = i - radius;
                        t = y + ir;

                        if (t < startY) continue;
                        if (t >= stopY) break;

                        for (j = 0; j < size; j++)
                        {
                            jr = j - radius;
                            t = x + jr;

                            if (t < startX) continue;

                            if (t < stopX)
                            {
                                k = kernel[i, j];
                                p = &src[ir * srcStride + jr * pixelSize];

                                div += k;

                                r += k * p[RGB.R];
                                g += k * p[RGB.G];
                                b += k * p[RGB.B];

                                processedKernelSize++;
                            }
                        }
                    }

                    if (processedKernelSize == kernelSize)
                    {
                        div = divisor;
                    }
                    else
                    {
                        if (!DynamicDivisorForEdges) div = divisor;
                    }

                    if (div != 0)
                    {
                        r /= div;
                        g /= div;
                        b /= div;
                    }

                    r += Threshold;
                    g += Threshold;
                    b += Threshold;

                    dst[RGB.R] = (byte)((r > 255) ? 255 : ((r < 0) ? 0 : r));
                    dst[RGB.G] = (byte)((g > 255) ? 255 : ((g < 0) ? 0 : g));
                    dst[RGB.B] = (byte)((b > 255) ? 255 : ((b < 0) ? 0 : b));

                    if (pixelSize == 4) dst[RGB.A] = src[RGB.A];
                }

                src += srcOffset;
                dst += dstOffset;
            }
        }

        /// <summary>
        /// Обрабатывает битовую карту формата <see cref="PixelFormat.Format32bppArgb"/>.
        /// </summary>
        /// <param name="src">Указатель на исходный буфер карты.</param>
        /// <param name="dst">Указатель на буфер карты назначения.</param>
        /// <param name="srcStride">Ширина развёртки исходной карты.</param>
        /// <param name="dstStride">Ширина развёртки карты назначения.</param>
        /// <param name="srcOffset">Смещение исходной карты.</param>
        /// <param name="dstOffset">Смещение карты назначения.</param>
        /// <param name="startX">Начальный индекс по ширине карты.</param>
        /// <param name="startY">Начальный индекс по высоте карты.</param>
        /// <param name="stopX">Конечный индекс по ширине карты.</param>
        /// <param name="stopY">Конечный индекс по высоте карты.</param>
        private void Process32bppImage(byte* src, byte* dst, int srcStride, int dstStride, int srcOffset, int dstOffset, int startX, int startY, int stopX, int stopY)
        {
            int i, j, t, k, ir, jr;
            int radius = size >> 1;
            long r, g, b, a, div;
            int kernelSize = size * size;
            int processedKernelSize;

            byte* p;

            for (int y = startY; y < stopY; y++)
            {
                for (int x = startX; x < stopX; x++, src += 4, dst += 4)
                {
                    r = g = b = a = div = processedKernelSize = 0;

                    for (i = 0; i < size; i++)
                    {
                        ir = i - radius;
                        t = y + ir;

                        if (t < startY) continue;
                        if (t >= stopY) break;

                        for (j = 0; j < size; j++)
                        {
                            jr = j - radius;
                            t = x + jr;

                            if (t < startX) continue;

                            if (t < stopX)
                            {
                                k = kernel[i, j];
                                p = &src[ir * srcStride + jr * 4];

                                div += k;

                                r += k * p[RGB.R];
                                g += k * p[RGB.G];
                                b += k * p[RGB.B];
                                a += k * p[RGB.A];

                                processedKernelSize++;
                            }
                        }
                    }

                    if (processedKernelSize == kernelSize)
                    {
                        div = divisor;
                    }
                    else
                    {
                        if (!DynamicDivisorForEdges) div = divisor;
                    }

                    if (div != 0)
                    {
                        r /= div;
                        g /= div;
                        b /= div;
                        a /= div;
                    }

                    r += Threshold;
                    g += Threshold;
                    b += Threshold;
                    a += Threshold;

                    dst[RGB.R] = (byte)((r > 255) ? 255 : ((r < 0) ? 0 : r));
                    dst[RGB.G] = (byte)((g > 255) ? 255 : ((g < 0) ? 0 : g));
                    dst[RGB.B] = (byte)((b > 255) ? 255 : ((b < 0) ? 0 : b));
                    dst[RGB.A] = (byte)((a > 255) ? 255 : ((a < 0) ? 0 : a));
                }

                src += srcOffset;
                dst += dstOffset;
            }
        }

        /// <summary>
        /// Обрабатывает битовую карту формата <see cref="PixelFormat.Format16bppArgb1555"/>.
        /// </summary>
        /// <param name="baseSrc">Указатель на исходный буфер карты.</param>
        /// <param name="baseDst">Указатель на буфер карты назначения.</param>
        /// <param name="srcStride">Ширина развёртки исходной карты.</param>
        /// <param name="dstStride">Ширина развёртки карты назначения.</param>
        /// <param name="startX">Начальный индекс по ширине карты.</param>
        /// <param name="startY">Начальный индекс по высоте карты.</param>
        /// <param name="stopX">Конечный индекс по ширине карты.</param>
        /// <param name="stopY">Конечный индекс по высоте карты.</param>
        private void Process16bppImage(ushort* baseSrc, ushort* baseDst, int srcStride, int dstStride, int startX, int startY, int stopX, int stopY)
        {
            int i, j, t, k, ir, jr;
            int radius = size >> 1;
            long g, div;
            int kernelSize = size * size;
            int processedKernelSize;

            for (int y = startY; y < stopY; y++)
            {
                ushort* src = baseSrc + y * srcStride;
                ushort* dst = baseDst + y * dstStride;

                for (int x = startX; x < stopX; x++, src++, dst++)
                {
                    g = div = processedKernelSize = 0;

                    for (i = 0; i < size; i++)
                    {
                        ir = i - radius;
                        t = y + ir;

                        if (t < startY) continue;
                        if (t >= stopY) break;

                        for (j = 0; j < size; j++)
                        {
                            jr = j - radius;
                            t = x + jr;

                            if (t < startX) continue;

                            if (t < stopX)
                            {
                                k = kernel[i, j];

                                div += k;
                                g += k * src[ir * srcStride + jr];
                                processedKernelSize++;
                            }
                        }
                    }

                    if (processedKernelSize == kernelSize)
                    {
                        div = divisor;
                    }
                    else
                    {
                        if (!DynamicDivisorForEdges) div = divisor;
                    }

                    if (div != 0) g /= div;

                    g += Threshold;
                    *dst = (ushort)((g > 65535) ? 65535 : ((g < 0) ? 0 : g));
                }
            }
        }

        /// <summary>
        /// Обрабатывает битовую карту формата <see cref="PixelFormat.Format48bppRgb"/>.
        /// </summary>
        /// <param name="baseSrc">Указатель на исходный буфер карты.</param>
        /// <param name="baseDst">Указатель на буфер карты назначения.</param>
        /// <param name="srcStride">Ширина развёртки исходной карты.</param>
        /// <param name="dstStride">Ширина развёртки карты назначения.</param>
        /// <param name="startX">Начальный индекс по ширине карты.</param>
        /// <param name="startY">Начальный индекс по высоте карты.</param>
        /// <param name="stopX">Конечный индекс по ширине карты.</param>
        /// <param name="stopY">Конечный индекс по высоте карты.</param>
        /// <param name="pixelSize">Число байт на один пиксель.</param>
        private void Process48bppImage(ushort* baseSrc, ushort* baseDst, int srcStride, int dstStride, int startX, int startY, int stopX, int stopY, int pixelSize)
        {
            int i, j, t, k, ir, jr;
            int radius = size >> 1;
            long r, g, b, div;
            int kernelSize = size * size;
            int processedKernelSize;

            ushort* p;

            for (int y = startY; y < stopY; y++)
            {
                ushort* src = baseSrc + y * srcStride;
                ushort* dst = baseDst + y * dstStride;

                for (int x = startX; x < stopX; x++, src += pixelSize, dst += pixelSize)
                {
                    r = g = b = div = processedKernelSize = 0;

                    for (i = 0; i < size; i++)
                    {
                        ir = i - radius;
                        t = y + ir;

                        if (t < startY) continue;
                        if (t >= stopY) break;

                        for (j = 0; j < size; j++)
                        {
                            jr = j - radius;
                            t = x + jr;

                            if (t < startX) continue;

                            if (t < stopX)
                            {
                                k = kernel[i, j];
                                p = &src[ir * srcStride + jr * pixelSize];

                                div += k;

                                r += k * p[RGB.R];
                                g += k * p[RGB.G];
                                b += k * p[RGB.B];

                                processedKernelSize++;
                            }
                        }
                    }

                    if (processedKernelSize == kernelSize)
                    {
                        div = divisor;
                    }
                    else
                    {
                        if (!DynamicDivisorForEdges) div = divisor;
                    }

                    if (div != 0)
                    {
                        r /= div;
                        g /= div;
                        b /= div;
                    }

                    r += Threshold;
                    g += Threshold;
                    b += Threshold;

                    dst[RGB.R] = (ushort)((r > 65535) ? 65535 : ((r < 0) ? 0 : r));
                    dst[RGB.G] = (ushort)((g > 65535) ? 65535 : ((g < 0) ? 0 : g));
                    dst[RGB.B] = (ushort)((b > 65535) ? 65535 : ((b < 0) ? 0 : b));

                    if (pixelSize == 4) dst[RGB.A] = src[RGB.A];
                }
            }
        }

        /// <summary>
        /// Обрабатывает битовую карту формата <see cref="PixelFormat.Format64bppArgb"/>.
        /// </summary>
        /// <param name="baseSrc">Указатель на исходный буфер карты.</param>
        /// <param name="baseDst">Указатель на буфер карты назначения.</param>
        /// <param name="srcStride">Ширина развёртки исходной карты.</param>
        /// <param name="dstStride">Ширина развёртки карты назначения.</param>
        /// <param name="startX">Начальный индекс по ширине карты.</param>
        /// <param name="startY">Начальный индекс по высоте карты.</param>
        /// <param name="stopX">Конечный индекс по ширине карты.</param>
        /// <param name="stopY">Конечный индекс по высоте карты.</param>
        private void Process64bppImage(ushort* baseSrc, ushort* baseDst, int srcStride, int dstStride, int startX, int startY, int stopX, int stopY)
        {
            int i, j, t, k, ir, jr;
            int radius = size >> 1;
            long r, g, b, a, div;
            int kernelSize = size * size;
            int processedKernelSize;

            ushort* p;

            for (int y = startY; y < stopY; y++)
            {
                ushort* src = baseSrc + y * srcStride;
                ushort* dst = baseDst + y * dstStride;

                for (int x = startX; x < stopX; x++, src += 4, dst += 4)
                {
                    r = g = b = a = div = processedKernelSize = 0;

                    for (i = 0; i < size; i++)
                    {
                        ir = i - radius;
                        t = y + ir;

                        if (t < startY) continue;
                        if (t >= stopY) break;

                        for (j = 0; j < size; j++)
                        {
                            jr = j - radius;
                            t = x + jr;

                            if (t < startX) continue;

                            if (t < stopX)
                            {
                                k = kernel[i, j];
                                p = &src[ir * srcStride + jr * 4];

                                div += k;

                                r += k * p[RGB.R];
                                g += k * p[RGB.G];
                                b += k * p[RGB.B];
                                a += k * p[RGB.A];

                                processedKernelSize++;
                            }
                        }
                    }

                    if (processedKernelSize == kernelSize)
                    {
                        div = divisor;
                    }
                    else
                    {
                        if (!DynamicDivisorForEdges) div = divisor;
                    }

                    if (div != 0)
                    {
                        r /= div;
                        g /= div;
                        b /= div;
                        a /= div;
                    }

                    r += Threshold;
                    g += Threshold;
                    b += Threshold;
                    a += Threshold;

                    dst[RGB.R] = (ushort)((r > 65535) ? 65535 : ((r < 0) ? 0 : r));
                    dst[RGB.G] = (ushort)((g > 65535) ? 65535 : ((g < 0) ? 0 : g));
                    dst[RGB.B] = (ushort)((b > 65535) ? 65535 : ((b < 0) ? 0 : b));
                    dst[RGB.A] = (ushort)((a > 65535) ? 65535 : ((a < 0) ? 0 : a));
                }
            }
        }

        /// <summary>
        /// Обрабатывает битовую карту изображения.
        /// </summary>
        /// <param name="source">Исходное изображение.</param>
        /// <param name="destination">Экземпляр изображения назначения.</param>
        protected void ProcessImage(UnmanagedBitmap source, UnmanagedBitmap destination)
        {
            int pixelSize = Image.GetPixelFormatSize(source.PixelFormat) / 8;
            int startX = 0;
            int startY = 0;
            int stopX = startX + destination.Width;
            int stopY = startY + destination.Height;

            if ((pixelSize <= 4) && (pixelSize != 2))
            {
                int srcStride = source.Stride;
                int dstStride = destination.Stride;

                int srcOffset = srcStride - destination.Width * pixelSize;
                int dstOffset = dstStride - destination.Width * pixelSize;

                byte* src = (byte*)source.ImageData.ToPointer();
                byte* dst = (byte*)destination.ImageData.ToPointer();

                src += (startY * srcStride + startX * pixelSize);
                dst += (startY * dstStride + startX * pixelSize);

                if (destination.PixelFormat == PixelFormat.Format8bppIndexed)
                {
                    Process8bppImage(src, dst, srcStride, dstStride, srcOffset, dstOffset, startX, startY, stopX, stopY);
                }
                else
                {
                    if ((pixelSize == 3) || (!ProcessAlpha))
                    {
                        Process24bppImage(src, dst, srcStride, dstStride, srcOffset, dstOffset, startX, startY, stopX, stopY, pixelSize);
                    }
                    else
                    {
                        Process32bppImage(src, dst, srcStride, dstStride, srcOffset, dstOffset, startX, startY, stopX, stopY);
                    }
                }
            }
            else
            {
                pixelSize /= 2;

                int dstStride = destination.Stride / 2;
                int srcStride = source.Stride / 2;

                ushort* baseSrc = (ushort*)source.ImageData.ToPointer();
                ushort* baseDst = (ushort*)destination.ImageData.ToPointer();

                baseSrc += (startX * pixelSize);
                baseDst += (startX * pixelSize);

                if (source.PixelFormat == PixelFormat.Format16bppGrayScale)
                {
                    Process16bppImage(baseSrc, baseDst, srcStride, dstStride, startX, startY, stopX, stopY);
                }
                else
                {
                    if ((pixelSize == 3) || (!ProcessAlpha))
                    {
                        Process48bppImage(baseSrc, baseDst, srcStride, dstStride, startX, startY, stopX, stopY, pixelSize);
                    }
                    else
                    {
                        Process64bppImage(baseSrc, baseDst, srcStride, dstStride, startX, startY, stopX, stopY);
                    }
                }
            }
        }

        /// <summary>
        /// Определяет максимальную степень градиента граней.
        /// </summary>
        /// <param name="sourceImage">Исходное изображение.</param>
        /// <returns></returns>
        protected static float CalculateMaxGradient(UnmanagedBitmap sourceImage)
        {
            int pixelSize = Image.GetPixelFormatSize(sourceImage.PixelFormat) / 8;
            int srcStride = sourceImage.Stride;
            byte* buffer = (byte*)(void*)sourceImage.ImageData;

            float max = short.MinValue;

            for (int y = 0; y < sourceImage.Height; y++)
            {
                float* src = (float*)(buffer + srcStride * y);

                for (int x = 0; x < sourceImage.Width / pixelSize; x += pixelSize)
                {
                    if (src[x] > max) max = src[x];
                }
            }

            return max;
        }

        /// <summary>
        /// Определяет наличие размытия на изображении.
        /// </summary>
        /// <param name="sourceImage">Исходное изображение.</param>
        /// <param name="max">Степерь размытия.</param>
        /// <returns></returns>
        [SecurityCritical]
        public static bool Check(BitmapData sourceImage, out float max)
        {
            Convolution convolution = new Convolution();

            using (UnmanagedBitmap src = new UnmanagedBitmap(sourceImage))
            using (UnmanagedBitmap tmp = UnmanagedBitmap.Create(sourceImage.Width, sourceImage.Height, sourceImage.PixelFormat))
            {
                convolution.ProcessImage(src, tmp);
                max = CalculateMaxGradient(tmp);
            }

            return max >= 1E+10F;
        }

        /// <summary>
        /// Определяет наличие размытия на изображении.
        /// </summary>
        /// <param name="sourceImage">Исходное изображение.</param>
        /// <returns></returns>
        [SecurityCritical]
        public static bool Check(BitmapData sourceImage) => Check(sourceImage, out float max);
    }
}