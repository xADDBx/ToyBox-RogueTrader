using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Collections;
using UnityEngine.Rendering;
using UnityEngine;

namespace ModKit.Utility.Extensions {
    public static class MiscExtensions {
        // Takes the last N objects of the source collection
        public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> source, int N) {
            return source.Skip(Math.Max(0, source.Count() - N));
        }
        // Creates readable collection content string
        public static string ToContentString(this IEnumerable enumerable) {
            return InternalToContentString(enumerable);
        }
        private static string InternalToContentString(object obj) {
            if (obj == null) {
                return "null";
            }

            if (obj is string str) {
                return $"\"{str}\"";
            }

            if (obj is IEnumerable enumerable && !(obj is IDictionary)) {
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
        public static void SaveTextureToFile(this Texture source, string filePath, int width, int height, SaveTextureFileFormat fileFormat = SaveTextureFileFormat.PNG, int jpgQuality = 95, bool asynchronous = true, System.Action<bool> done = null) {
            // check that the input we're getting is something we can handle:
            if (!(source is Texture2D || source is RenderTexture)) {
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
                        default:
                            encoded = ImageConversion.EncodeNativeArrayToPNG(narray, resizeRT.graphicsFormat, (uint)width, (uint)height);
                            break;
                    }

                    System.IO.File.WriteAllBytes(filePath, encoded.ToArray());
                    encoded.Dispose();
                }

                narray.Dispose();

                // notify the user that the operation is done, and its outcome.
                done?.Invoke(!request.hasError);
            });

            if (!asynchronous)
                request.WaitForCompletion();
        }
    }
}
