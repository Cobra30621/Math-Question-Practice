using System.Collections.Generic;
using UnityEngine;

namespace MathQuestion
{
    public class EMathQuestionSource : IQuestionSource
    {
        // 不是每個章節與難度的組合都有題目，每題多給幾次機會
        private const int AttemptsPerQuestion = 4;

        private readonly EMathConfig _config;
        private readonly EMathClient _client;
        private readonly System.Random _random = new();

        public EMathQuestionSource(EMathConfig config)
        {
            _config = config;
            _client = new EMathClient(config);
        }

        public async Awaitable<IReadOnlyList<Question>> GetQuestions(QuestionFilter filter, int count)
        {
            var questions = new List<Question>();
            if (_config == null || !_config.HasToken)
            {
                Debug.LogWarning("[MathQuestion] 沒有設定 EMathConfig 的 Token，略過線上題庫");
                return questions;
            }

            // 明確沒網路時直接放棄，不要讓玩家等逾時
            if (Application.internetReachability == NetworkReachability.NotReachable)
                return questions;

            var seenLinks = new HashSet<string>();
            try
            {
                for (int attempt = 0; attempt < count * AttemptsPerQuestion && questions.Count < count; attempt++)
                {
                    string serial = EMathSerial.Build(filter.Publisher, filter.Grade,
                        EMathSerial.RandomChapter(_random), EMathSerial.RandomDifficulty(_random));
                    var problems = await _client.GetProblems(serial, 1);
                    if (problems.Length == 0 || !seenLinks.Add(problems[0].problemLink))
                        continue;

                    var question = await Download(problems[0], filter);
                    if (question != null)
                        questions.Add(question);
                }
            }
            catch (EMathUnavailableException e)
            {
                // 保留已取到的題目，不足的交給本機題庫補
                Debug.LogWarning($"[MathQuestion] 線上題庫無法使用：{e.Message}");
            }
            return questions;
        }

        private async Awaitable<Question> Download(EMathProblem problem, QuestionFilter filter)
        {
            if (!int.TryParse(problem.answer, out int answer) || !int.TryParse(problem.choices, out int choices))
                return null;

            byte[] questionBytes = await _client.GetImage(problem.problemLink);
            if (questionBytes == null)
                return null;
            byte[] optionsBytes = await _client.GetImage(problem.ansLink);
            if (optionsBytes == null)
                return null;

            return ImageLoader.CreateQuestion(problem.problemLink, questionBytes, optionsBytes, choices, answer,
                filter.Grade, filter.Publisher);
        }
    }
}
