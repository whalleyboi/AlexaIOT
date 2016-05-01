using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.Storage;

namespace AlexaIOT.Utils
{
    public static class FileStorageAdapter
    {
        async public static Task<System.IO.Stream> LoadData<Stream>(string filename)
        {
            var semaphore = getSemaphore(filename);
            await semaphore.WaitAsync();

            try
            {
                var storageFile = await ApplicationData.Current.LocalFolder.GetFileAsync(filename);

                using (var stream = await storageFile.OpenStreamForReadAsync())
                {
                    return stream;
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        async public static Task SaveData<T>(T data, string filename)
        {
            var semaphore = getSemaphore(filename);
            await semaphore.WaitAsync();

            try
            {
                var storageFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);

                using (var stream = await storageFile.OpenStreamForWriteAsync())
                {
                    var serializer = new XmlSerializer(typeof(T));
                    serializer.Serialize(stream, data);
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        private static SemaphoreSlim getSemaphore(string filename)
        {
            if (_semaphores.ContainsKey(filename))
                return _semaphores[filename];

            var semaphore = new SemaphoreSlim(1);
            _semaphores[filename] = semaphore;
            return semaphore;
        }

        private static readonly Dictionary<string, SemaphoreSlim> _semaphores = new Dictionary<string, SemaphoreSlim>();
    }
}
