namespace Antialiasing
{
    /// <summary>
    /// Алгоритм изменения размеров исходного изображения.
    /// </summary>
    public enum AntialiasingMethod
    {
        /// <summary>
        /// Высокая скорость, низкое качество.
        /// </summary>
        NearestNeighbor,
        /// <summary>
        /// Оптимальное соотношение скорости и качества.
        /// </summary>
        Bilinear,
        /// <summary>
        /// Низкая скорость, высокое качество.
        /// </summary>
        Bicubic,
    }
}