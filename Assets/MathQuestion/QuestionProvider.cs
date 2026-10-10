using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MathQuestion
{
    public class QuestionProvider
    {
        private readonly IQuestionSource _online;
        private readonly IQuestionSource _local;

        // online 可以是 null，例如沒有 Token 時只用本機題庫
        public QuestionProvider(IQuestionSource online, IQuestionSource local)
        {
            _online = online;
            _local = local;
        }

        // 回傳最多 count 題；兩個來源都不足時會少於 count，由遊戲端決定怎麼處理
        public async Awaitable<IReadOnlyList<Question>> GetQuestions(QuestionFilter filter, int count)
        {
            var questions = new List<Question>(await TryGet(_online, filter, count));
            if (questions.Count >= count)
                return questions;

            var usedIds = new HashSet<string>(questions.Select(q => q.Id));
            foreach (var question in await TryGet(_local, filter, count))
            {
                if (questions.Count < count && usedIds.Add(question.Id))
                    questions.Add(question);
                else
                    question.Release();
            }
            return questions;
        }

        // 一個來源出錯不該讓整局卡住，改由下一個來源補
        private static async Awaitable<IReadOnlyList<Question>> TryGet(IQuestionSource source, QuestionFilter filter,
            int count)
        {
            if (source == null)
                return Array.Empty<Question>();

            try
            {
                return await source.GetQuestions(filter, count);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MathQuestion] {source.GetType().Name} 取題失敗：{e.Message}");
                return Array.Empty<Question>();
            }
        }
    }
}
