using MathQuestion;

namespace Game
{
    public static class GradeNames
    {
        private const string Years = "一二三四五六";

        // 數值順序固定是一上、一下、二上…，所以可以直接算
        public static string Of(Grade grade) => $"{Years[(int)grade / 2]}{((int)grade % 2 == 0 ? "上" : "下")}";
    }
}
