using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Antialiasing
{
    /// <summary>
    /// Представляет алгоритмы фильтрации изображения без потерь качества.
    /// </summary>
    internal static unsafe class AntialiasingFilter
    {
        /// <summary>
        /// Усреднённая выборка.
        /// </summary>
        /// <param name="sourceData">Исходное изображение.</param>
        /// <param name="destinationData">Изображение назначения.</param>
        /// <param name="newWidth">Новая ширина.</param>
        /// <param name="newHeight">новая высота.</param>
        private static void NearestNeighbor(UnmanagedBitmap sourceData, UnmanagedBitmap destinationData, int newWidth, int newHeight)
        {
            int pixelSize = Image.GetPixelFormatSize(sourceData.PixelFormat) / 8;
            int srcStride = sourceData.Stride;
            int dstStride = destinationData.Stride;
            double xFactor = (double)sourceData.Width / newWidth;
            double yFactor = (double)sourceData.Height / newHeight;
            byte* baseSrc = (byte*)(void*)sourceData.ImageData;
            byte* baseDst = (byte*)(void*)destinationData.ImageData;
            
            for (int y = 0; y < newHeight; y++)
            {
                byte* dst = baseDst + dstStride * y;
                byte* src = baseSrc + srcStride * ((int)(y * yFactor));
                byte* p;
                
                for (int x = 0; x < newWidth; x++)
                {
                    p = src + pixelSize * ((int)(x * xFactor));
                    
                    for (int i = 0; i < pixelSize; i++, dst++, p++) *dst = *p;
                }
            }
        }

        /// <summary>
        /// Билинейная фильтрация.
        /// </summary>
        /// <param name="sourceData">Исходное изображение.</param>
        /// <param name="destinationData">Изображение назначения.</param>
        /// <param name="newWidth">Новая ширина.</param>
        /// <param name="newHeight">новая высота.</param>
        private static void Bilinear(UnmanagedBitmap sourceData, UnmanagedBitmap destinationData, int newWidth, int newHeight)
        {
            int pixelSize = Image.GetPixelFormatSize(sourceData.PixelFormat) / 8;
            int srcStride = sourceData.Stride;
            int dstOffset = destinationData.Stride - pixelSize * newWidth;
            double xFactor = (double)sourceData.Width / newWidth;
            double yFactor = (double)sourceData.Height / newHeight;
            byte* src = (byte*)(void*)sourceData.ImageData;
            byte* dst = (byte*)(void*)destinationData.ImageData;
            
            double ox, oy, dx1, dy1, dx2, dy2;
            int ox1, oy1, ox2, oy2;
            
            int ymax = sourceData.Height - 1;
            int xmax = sourceData.Width - 1;
            
            byte* tp1, tp2;
            byte* p1, p2, p3, p4;
            
            for (int y = 0; y < newHeight; y++)
            {
                oy = y * yFactor;
                oy1 = (int)oy;
                oy2 = (oy1 == ymax) ? oy1 : oy1 + 1;
                dy1 = oy - oy1;
                dy2 = 1.0 - dy1;
                
                tp1 = src + oy1 * srcStride;
                tp2 = src + oy2 * srcStride;
                
                for (int x = 0; x < newWidth; x++)
                {
                    ox = x * xFactor;
                    ox1 = (int)ox;
                    ox2 = (ox1 == xmax) ? ox1 : ox1 + 1;
                    dx1 = ox - ox1;
                    dx2 = 1.0 - dx1;
                    
                    p1 = tp1 + ox1 * pixelSize;
                    p2 = tp1 + ox2 * pixelSize;
                    p3 = tp2 + ox1 * pixelSize;
                    p4 = tp2 + ox2 * pixelSize;
                    
                    for (int i = 0; i < pixelSize; i++, dst++, p1++, p2++, p3++, p4++)
                        *dst = (byte)(dy2 * (dx2 * (*p1) + dx1 * (*p2)) + dy1 * (dx2 * (*p3) + dx1 * (*p4)));
                }

                dst += dstOffset;
            }
        }

        /// <summary>
        /// Вычисляет бикубический коэффициент.
        /// </summary>
        /// <param name="x">Исходное значение.</param>
        /// <returns></returns>
        private static double BicubicKernel(double x)
        {
            if (x < 0) x = -x;

            double biCoef = 0;

            if (x <= 1)
            {
                biCoef = (1.5 * x - 2.5) * x * x + 1;
            }
            else if (x < 2)
            {
                biCoef = ((-0.5 * x + 2.5) * x - 4) * x + 2;
            }

            return biCoef;
        }

        /// <summary>
        /// Бикубическая фильтрация.
        /// </summary>
        /// <param name="sourceData">Исходное изображение.</param>
        /// <param name="destinationData">Изображение назначения.</param>
        /// <param name="newWidth">Новая ширина.</param>
        /// <param name="newHeight">новая высота.</param>
        private static void Bicubic(UnmanagedBitmap sourceData, UnmanagedBitmap destinationData, int newWidth, int newHeight)
        {
            int pixelSize = sourceData.PixelFormat == PixelFormat.Format8bppIndexed ? 1 : 3;
            int srcStride = sourceData.Stride;
            int dstOffset = destinationData.Stride - pixelSize * newWidth;
            double xFactor = (double)sourceData.Width / newWidth;
            double yFactor = (double)sourceData.Height / newHeight;
            byte* src = (byte*)(void*)sourceData.ImageData;
            byte* dst = (byte*)(void*)destinationData.ImageData;
            
            double ox, oy, dx, dy, k1, k2;
            int ox1, oy1, ox2, oy2;
            
            double r, g, b;
            
            int ymax = sourceData.Height - 1;
            int xmax = sourceData.Width - 1;
            
            byte* p;

            if (destinationData.PixelFormat == PixelFormat.Format8bppIndexed)
            {
                for (int y = 0; y < newHeight; y++)
                {
                    oy = y * yFactor - 0.5;
                    oy1 = (int)oy;
                    dy = oy - oy1;
                    
                    for (int x = 0; x < newWidth; x++, dst++)
                    {
                        ox = x * xFactor - 0.5f;
                        ox1 = (int)ox;
                        dx = ox - ox1;
                        
                        g = 0;
                        
                        for (int n = -1; n < 3; n++)
                        {
                            k1 = BicubicKernel(dy - n);
                            oy2 = oy1 + n;

                            if (oy2 < 0) oy2 = 0;
                            if (oy2 > ymax) oy2 = ymax;
                            
                            for (int m = -1; m < 3; m++)
                            {
                                k2 = k1 * BicubicKernel(m - dx);
                                ox2 = ox1 + m;

                                if (ox2 < 0) ox2 = 0;
                                if (ox2 > xmax) ox2 = xmax;

                                g += k2 * src[oy2 * srcStride + ox2];
                            }
                        }

                        *dst = (byte)Math.Max(0, Math.Min(255, g));
                    }

                    dst += dstOffset;
                }
            }
            else
            {
                for (int y = 0; y < newHeight; y++)
                {
                    oy = y * yFactor - 0.5;
                    oy1 = (int)oy;
                    dy = oy - oy1;
                    
                    for (int x = 0; x < newWidth; x++, dst += 3)
                    {
                        ox = x * xFactor - 0.5;
                        ox1 = (int)ox;
                        dx = ox - ox1;
                        
                        r = g = b = 0;
                        
                        for (int n = -1; n < 3; n++)
                        {
                            k1 = BicubicKernel(dy - n);
                            oy2 = oy1 + n;

                            if (oy2 < 0) oy2 = 0;
                            if (oy2 > ymax) oy2 = ymax;
                            
                            for (int m = -1; m < 3; m++)
                            {
                                k2 = k1 * BicubicKernel(m - dx);
                                ox2 = ox1 + m;

                                if (ox2 < 0) ox2 = 0;
                                if (ox2 > xmax) ox2 = xmax;
                                
                                p = src + oy2 * srcStride + ox2 * 3;

                                r += k2 * p[RGB.R];
                                g += k2 * p[RGB.G];
                                b += k2 * p[RGB.B];
                            }
                        }

                        dst[RGB.R] = (byte)Math.Max(0, Math.Min(255, r));
                        dst[RGB.G] = (byte)Math.Max(0, Math.Min(255, g));
                        dst[RGB.B] = (byte)Math.Max(0, Math.Min(255, b));
                    }

                    dst += dstOffset;
                }
            }
        }

        /// <summary>
        /// Обрабатывает изображение указанным алгоритмом.
        /// </summary>
        /// <param name="sourceData">Исходное изображение.</param>
        /// <param name="destinationData">Изображение назначения.</param>
        /// <param name="newWidth">Новая ширина.</param>
        /// <param name="newHeight">новая высота.</param>
        /// <param name="algorithm">Алгоритм изменения размеров.</param>
        public static void Process(UnmanagedBitmap sourceData, UnmanagedBitmap destinationData, int newWidth, int newHeight, AntialiasingMethod algorithm)
        {
            switch (algorithm)
            {
                default:
                case AntialiasingMethod.NearestNeighbor:
                    NearestNeighbor(sourceData, destinationData, newWidth, newHeight);
                    break;

                case AntialiasingMethod.Bilinear:
                    Bilinear(sourceData, destinationData, newWidth, newHeight);
                    break;

                case AntialiasingMethod.Bicubic:
                    Bicubic(sourceData, destinationData, newWidth, newHeight);
                    break;
            }
        }
    }
}