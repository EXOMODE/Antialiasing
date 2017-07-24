using System.Drawing;

namespace Antialiasing
{
    /// <summary>
    /// Представляет цветовую модель RGB.
    /// </summary>
    internal struct RGB
    {
        /// <summary>
        /// Позиция байта Red-канала в развёртке.
        /// </summary>
        public const short R = 2;

        /// <summary>
        /// Позиция байта Green-канала в развёртке.
        /// </summary>
        public const short G = 1;

        /// <summary>
        /// Позиция байта Blue-канала в развёртке.
        /// </summary>
        public const short B = 0;

        /// <summary>
        /// Позиция байта Alpha-канала в развёртке.
        /// </summary>
        public const short A = 3;

        /// <summary>
        /// Значение Red-канала.
        /// </summary>
        public byte Red;

        /// <summary>
        /// Значение Green-канала.
        /// </summary>
        public byte Green;

        /// <summary>
        /// Значение Blue-канала.
        /// </summary>
        public byte Blue;

        /// <summary>
        /// Значение Alpha-канала.
        /// </summary>
        public byte Alpha;

        /// <summary>
        /// Возвращает или задаёт цвет.
        /// </summary>
        public Color Color
        {
            get => Color.FromArgb(Alpha, Red, Green, Blue);

            set
            {
                Red = value.R;
                Green = value.G;
                Blue = value.B;
                Alpha = value.A;
            }
        }

        /// <summary>
        /// Инициализирует новый экземпляр структуры <see cref="RGB"/>.
        /// </summary>
        /// <param name="red">Значение Red-канала.</param>
        /// <param name="green">Значение Green-канала.</param>
        /// <param name="blue">Значение Blue-канала.</param>
        /// <param name="alpha">Значение Alpha-канала.</param>
        public RGB(byte red, byte green, byte blue, byte alpha) : this()
        {
            Red = red;
            Green = green;
            Blue = blue;
            Alpha = alpha;
        }

        /// <summary>
        /// Инициализирует новый экземпляр структуры <see cref="RGB"/>.
        /// </summary>
        /// <param name="red">Значение Red-канала.</param>
        /// <param name="green">Значение Green-канала.</param>
        /// <param name="blue">Значение Blue-канала.</param>
        public RGB(byte red, byte green, byte blue) : this(red, green, blue, 255) { }

        /// <summary>
        /// Инициализирует новый экземпляр структуры <see cref="RGB"/>.
        /// </summary>
        /// <param name="color">Значение цвета.</param>
        public RGB(Color color) : this(color.R, color.G, color.B, color.A) { }
    }
}