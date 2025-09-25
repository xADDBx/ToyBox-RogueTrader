using System.IO;
using UnityEngine;
using ModKit;
using HarmonyLib;
using System;

namespace ToyBox {
    class AssetLoader {
        private static Lazy<Func<Texture2D, byte[], Texture2D>> LoadImage = new(() => AccessTools.MethodDelegate<Func<Texture2D, byte[], Texture2D>>(AccessTools.Method(typeof(ImageConversion), nameof(ImageConversion.LoadImage), [typeof(Texture2D), typeof(byte[])])));
        public static Sprite LoadInternal(string folder, string file, Vector2Int size) {
            return Image2Sprite.Create($"{Mod.modEntry.Path}Assets{Path.DirectorySeparatorChar}{folder}{Path.DirectorySeparatorChar}{file}", size);
        }
        // Loosely based on https://forum.unity.com/threads/generating-sprites-dynamically-from-png-or-jpeg-files-in-c.343735/
        public static class Image2Sprite {
            public static string icons_folder = "";
            public static Sprite Create(string filePath, Vector2Int size) {
                var bytes = File.ReadAllBytes(icons_folder + filePath);
                var texture = new Texture2D(size.x, size.y, TextureFormat.ARGB32, false);
                // After the latest Unity update, UnityEngine.ImageConversionModule is targetting .NET standard 2.1 and using System.ReadOnlySpan<T>
                // Referencing System.Memory does not help; so I just created this runtime delegate. Since this method is (as of now) unused anyways it's a good enough fix.
                _ = LoadImage.Value(texture, bytes);
                return Sprite.Create(texture, new Rect(0, 0, size.x, size.y), new Vector2(0, 0));
            }
        }
    }
}
