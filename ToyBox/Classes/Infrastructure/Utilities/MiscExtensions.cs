using System.Collections;
using Unity.Collections;
using UnityEngine.Rendering;
using UnityEngine;

namespace ToyBox.Infrastructure.Utilities;
public static class MiscExtensions {
    // Credits to https://github.com/microsoftenator2022
    /// <summary>
    /// Divides input sequence into chunks of at most <paramref name="chunkSize"/>
    /// </summary>
    /// <returns>Sequence of sequences of <paramref name="chunkSize"/> elements. The last chunk will contain at most <paramref name="chunkSize"/> elements</returns>
    public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> source, int chunkSize) {
        var chunk = new T[chunkSize];
        var i = 0;

        foreach (var element in source) {
            chunk[i] = element;

            i++;
            if (i == chunkSize) {
                yield return chunk;
                chunk = new T[chunkSize];
                i = 0;
            }
        }

        if (i > 0 && i < chunkSize) yield return chunk.Take(i);
    }
    public static string Format(this string s, object arg0) {
        return string.Format(s, arg0);
    }
    public static string Format(this string s, object arg0, object arg1) {
        return string.Format(s, arg0, arg1);
    }
    public static string Format(this string s, object arg0, object arg1, object arg2) {
        return string.Format(s, arg0, arg1, arg2);
    }
    public static string Format(this string s, params object[] args) {
        return string.Format(s, args);
    }
    public static string ToContentString(this IEnumerable enumerable) {
        return InternalToContentString(enumerable);
    }
    private static string InternalToContentString(object? obj) {
        if (obj == null) {
            return "null";
        }

        if (obj is string str) {
            return $"\"{str}\"";
        }

        if (obj is IEnumerable enumerable and not IDictionary) {
            var elements = new List<string>();

            foreach (var item in enumerable) {
                elements.Add(InternalToContentString(item));
            }

            return "[" + string.Join(", ", elements) + "]";
        }

        if (obj is IDictionary dictionary) {
            var elements = new List<string>();

            foreach (DictionaryEntry entry in dictionary) {
                elements.Add($"{InternalToContentString(entry.Key)}: {InternalToContentString(entry.Value)}");
            }

            return "{" + string.Join(", ", elements) + "}";
        }

        return obj.ToString();
    }
    public enum SaveTextureFileFormat {
        PNG,
        JPG,
        EXR,
        TGA
    }
    public static void SaveTextureToFile(this Texture source, string filePath, int width, int height, SaveTextureFileFormat fileFormat = SaveTextureFileFormat.PNG, int jpgQuality = 95, bool asynchronous = true, Action<bool>? done = null) {
        // check that the input we're getting is something we can handle:
        if (source is not (Texture2D or RenderTexture)) {
            done?.Invoke(false);
            return;
        }

        // use the original texture size in case the input is negative:
        if (width < 0 || height < 0) {
            width = source.width;
            height = source.height;
        }

        // resize the original image:
        var resizeRT = RenderTexture.GetTemporary(width, height, 0);
        Graphics.Blit(source, resizeRT);

        // create a native array to receive data from the GPU:
        var narray = new NativeArray<byte>(width * height * 4, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

        // request the texture data back from the GPU:
        var request = AsyncGPUReadback.RequestIntoNativeArray(ref narray, resizeRT, 0, (AsyncGPUReadbackRequest request) => {
            // if the readback was successful, encode and write the results to disk
            if (!request.hasError) {
                NativeArray<byte> encoded;

                switch (fileFormat) {
                    case SaveTextureFileFormat.EXR:
                        encoded = ImageConversion.EncodeNativeArrayToEXR(narray, resizeRT.graphicsFormat, (uint)width, (uint)height);
                        break;
                    case SaveTextureFileFormat.JPG:
                        encoded = ImageConversion.EncodeNativeArrayToJPG(narray, resizeRT.graphicsFormat, (uint)width, (uint)height, 0, jpgQuality);
                        break;
                    case SaveTextureFileFormat.TGA:
                        encoded = ImageConversion.EncodeNativeArrayToTGA(narray, resizeRT.graphicsFormat, (uint)width, (uint)height);
                        break;
                    case SaveTextureFileFormat.PNG:
                        encoded = ImageConversion.EncodeNativeArrayToPNG(narray, resizeRT.graphicsFormat, (uint)width, (uint)height);
                        break;
                    default:
                        encoded = ImageConversion.EncodeNativeArrayToPNG(narray, resizeRT.graphicsFormat, (uint)width, (uint)height);
                        break;
                }

                File.WriteAllBytes(filePath, [.. encoded]);
                encoded.Dispose();
            }

            narray.Dispose();

            // notify the user that the operation is done, and its outcome.
            done?.Invoke(!request.hasError);
        });

        if (!asynchronous) {
            request.WaitForCompletion();
        }
    }
}
