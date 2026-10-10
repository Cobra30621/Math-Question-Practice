using UnityEngine;

namespace MathQuestion
{
    internal static class ImageLoader
    {
        // 兩張圖都解得開才建立題目，避免只留下一半的圖片沒人釋放
        public static Question CreateQuestion(string id, byte[] questionBytes, byte[] optionsBytes, int choices,
            int answer, Grade grade, Publisher publisher)
        {
            var questionImage = ToSprite(questionBytes);
            var optionsImage = ToSprite(optionsBytes);
            if (questionImage == null || optionsImage == null)
            {
                Destroy(questionImage);
                Destroy(optionsImage);
                return null;
            }
            return new Question(id, questionImage, optionsImage, choices, answer, grade, publisher);
        }

        public static void Destroy(Sprite sprite)
        {
            if (sprite == null)
                return;
            DestroyObject(sprite.texture);
            DestroyObject(sprite);
        }

        private static Sprite ToSprite(byte[] bytes)
        {
            var texture = new Texture2D(2, 2);
            if (!texture.LoadImage(bytes))
            {
                DestroyObject(texture);
                return null;
            }
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }

        // 編輯模式（測試、下載工具）不能用 Object.Destroy
        private static void DestroyObject(Object obj)
        {
            if (Application.isPlaying)
                Object.Destroy(obj);
            else
                Object.DestroyImmediate(obj);
        }
    }
}
