using System.Runtime.CompilerServices;

// API 存取與題庫格式只開放給下載工具與測試，遊戲端只看得到 QuestionProvider
[assembly: InternalsVisibleTo("MathQuestion.Editor")]
[assembly: InternalsVisibleTo("MathQuestion.Tests")]
