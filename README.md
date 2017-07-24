# Antialiasing

Библиотека для изменений размеров изображений без потерь качества, а так же определения размытий и нечёткостей, реализованная на .NET Core.

## Примеры использования:

###### Изменить размер изображения
```csharp
using (Bitmap sourceImage = new Bitmap("test.jpg"))
using (Bitmap resultImage = sourceImage.Resize(width, height))
{
    resultImage.Save($"result_{resultImage.Width}_{resultImage.Height}.jpg", ImageFormat.Jpeg);
}
```

###### Вырезать участок изображения
```csharp
using (Bitmap sourceImage = new Bitmap("test.jpg"))
using (Bitmap resultImage = sourceImage.Crop(x, y, width, height))
{
    resultImage.Save($"result_{resultImage.Width}_{resultImage.Height}.jpg", ImageFormat.Jpeg);
}
```

###### Определение алиасинга
```csharp
using (Bitmap sourceImage = new Bitmap("test.jpg"))
{
    Assert.IsTrue(sourceImage.HasAliasing());
}
```

Так же есть возможность указать алгоритм ресайзинга, передав аргумент в метод Resize().
