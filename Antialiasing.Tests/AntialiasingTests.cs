using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace Antialiasing.Tests
{
    /// <summary>
    /// ������������ ��������� ����� ������ <see cref="BitmapExtensions"/>.
    /// </summary>
    [TestClass]
    public class AntialiasingTests
    {
        /// <summary>
        /// ��������������� ����� ��� ����������.
        /// </summary>
        /// <param name="sourceImage">�������� �����������.</param>
        /// <param name="width">����� ������.</param>
        /// <param name="height">����� ������.</param>
        private void ResizeTestHelper(Bitmap sourceImage, int width, int height)
        {
            BitmapData sourceData = sourceImage.LockBits(new Rectangle(0, 0, sourceImage.Width, sourceImage.Height), ImageLockMode.ReadWrite, sourceImage.PixelFormat);

            using (Bitmap resultImage = sourceData.Resize(width, height))
            {
                string[] data = new[]
                {
                    "Source:",
                    $"\tWidth: {sourceImage.Width}",
                    $"\tHeight: {sourceImage.Height}",
                    $"\tDPI: {sourceImage.HorizontalResolution}",
                    "",
                    "Result:",
                    $"\tWidth: {resultImage.Width}",
                    $"\tHeight: {resultImage.Height}",
                    $"\tDPI: {resultImage.HorizontalResolution}",
                };

                resultImage.Save($"result_{resultImage.Width}_{resultImage.Height}.jpg", ImageFormat.Jpeg);
                File.WriteAllLines($"result_{resultImage.Width}_{resultImage.Height}.txt", data);
            }

            sourceImage.UnlockBits(sourceData);
        }

        /// <summary>
        /// �������� ������ ������ � ����������.
        /// </summary>
        /// <param name="sourceFolder">�������� ����������.</param>
        /// <param name="filters">������ �������� ������.</param>
        /// <param name="searchOption">����� ������.</param>
        /// <returns></returns>
        private static string[] GetFiles(string sourceFolder, string filters, SearchOption searchOption)
            => filters.Split('|').SelectMany(filter => Directory.GetFiles(sourceFolder, filter, searchOption)).ToArray();

        /// <summary>
        /// ��������� ����� <see cref="BitmapExtensions.Resize(Bitmap, int, int)"/>.
        /// </summary>
        [TestMethod]
        public void ResizeWHTest()
        {
            using (Bitmap sourceImage = new Bitmap("test.jpg"))
            {
                ResizeTestHelper(sourceImage, sourceImage.Width, sourceImage.Height);

                try
                {
                    ResizeTestHelper(sourceImage, sourceImage.Width * 2, sourceImage.Height * 2);
                }
                catch { }

                try
                {
                    ResizeTestHelper(sourceImage, sourceImage.Width * 4, sourceImage.Height * 4);
                }
                catch { }

                ResizeTestHelper(sourceImage, sourceImage.Width / 2, sourceImage.Height / 2);
                ResizeTestHelper(sourceImage, sourceImage.Width / 4, sourceImage.Height / 4);
            }
        }

        /// <summary>
        /// ��������� ����� <see cref="BitmapExtensions.HasAliasing(Bitmap, out float)"/>.
        /// </summary>
        [TestMethod]
        public void AliasingDetectionTest()
        {
            using (Bitmap sourceImage = new Bitmap("test.jpg"))
            {
                Assert.IsTrue(sourceImage.HasAliasing(out float max));
            }
        }

        /// <summary>
        /// ��������� ����� <see cref="BitmapExtensions.HasAliasing(Bitmap, out float)"/>.
        /// </summary>
        [TestMethod]
        public void AliasingDetectionMultiplyTest()
        {
            if (File.Exists("results.txt")) File.Delete("results.txt");

            string[] images = GetFiles("./", "*.jpg|*.png", SearchOption.TopDirectoryOnly);

            foreach (string image in images)
            {
                using (Bitmap sourceImage = new Bitmap(image))
                {
                    bool hasAliasing = sourceImage.HasAliasing(out float max);
                    File.AppendAllText("results.txt", $"{image} - {hasAliasing} - {max}\r\n");
                }
            }
        }
    }
}