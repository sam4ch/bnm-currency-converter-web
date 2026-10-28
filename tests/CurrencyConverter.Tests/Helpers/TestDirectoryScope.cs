namespace CurrencyConverter.Tests.Helpers;

/// <summary>
/// Создаёт изолированный временный каталог для теста и гарантированно удаляет его при утилизации.
/// </summary>
public sealed class TestDirectoryScope : IDisposable
{
    public string DirectoryPath { get; }

    public TestDirectoryScope(string prefix = "CurrencyConverterTest_")
    {
        DirectoryPath = Path.Combine(Path.GetTempPath(), $"{prefix}{Guid.NewGuid():N}");
        Directory.CreateDirectory(DirectoryPath);
    }

    public string GetFilePath(string fileName) => Path.Combine(DirectoryPath, fileName);

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(DirectoryPath))
            {
                Directory.Delete(DirectoryPath, recursive: true);
            }
        }
        catch
        {
            // Игнорируем ошибки удаления временных файлов операционной системой
        }
    }
}
