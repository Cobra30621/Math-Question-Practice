namespace MathQuestion
{
    internal static class EMathSerial
    {
        private const int MaxChapter = 8;
        private const int MaxDifficulty = 8;
        private const int CommonDifficulty = 2;

        // 6 段直接串接：出版社、年級、學期、章節（兩位數）、難度、固定的 001
        public static string Build(Publisher publisher, Grade grade, int chapter, int difficulty)
        {
            int publisherCode = (int)publisher + 1;
            int year = ((int)grade + 2) / 2;
            int semester = (int)grade % 2 + 1;
            return $"{publisherCode}{year}{semester}{chapter:D2}{difficulty}001";
        }

        public static int RandomChapter(System.Random random) => random.Next(1, MaxChapter + 1);

        // 難度 2 的題目最多，偏重它可以減少打到空組合的次數
        public static int RandomDifficulty(System.Random random) =>
            random.NextDouble() < 0.7 ? CommonDifficulty : random.Next(1, MaxDifficulty + 1);
    }
}
