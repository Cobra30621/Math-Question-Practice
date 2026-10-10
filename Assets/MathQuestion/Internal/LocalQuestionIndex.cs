using System;

namespace MathQuestion
{
    // 欄位名稱就是 index.json 的格式，JsonUtility 不支援最外層是陣列，所以包一層
    [Serializable]
    internal class LocalQuestionIndex
    {
        public const string FolderName = "Questions";
        public const string IndexFileName = "index.json";
        public const string ImagesFolderName = "images";

        public LocalQuestionEntry[] questions;
    }

    [Serializable]
    internal class LocalQuestionEntry
    {
        public string id;
        public string publisher;
        public string grade;
        public int chapter;
        public int difficulty;
        public string question;
        public string options;
        public int answer;
        public int choices;
    }
}
