using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace MathQuestion
{
    public class LocalQuestionSource : IQuestionSource
    {
        private readonly string _rootPath;
        private readonly System.Random _random = new();
        private LocalQuestionEntry[] _entries;

        public LocalQuestionSource()
            : this(Path.Combine(Application.streamingAssetsPath, LocalQuestionIndex.FolderName))
        {
        }

        public LocalQuestionSource(string rootPath)
        {
            _rootPath = rootPath;
        }

        public async Awaitable<IReadOnlyList<Question>> GetQuestions(QuestionFilter filter, int count)
        {
            _entries ??= await LoadIndex();

            string grade = filter.Grade.ToString();
            string publisher = filter.Publisher.ToString();
            var candidates = _entries
                .Where(e => e.grade == grade && e.publisher == publisher)
                .OrderBy(_ => _random.Next());

            var questions = new List<Question>();
            foreach (var entry in candidates)
            {
                if (questions.Count >= count)
                    break;

                var question = await Load(entry, filter);
                if (question != null)
                    questions.Add(question);
            }
            return questions;
        }

        private async Awaitable<LocalQuestionEntry[]> LoadIndex()
        {
            byte[] bytes = await StreamingFile.Read(Path.Combine(_rootPath, LocalQuestionIndex.IndexFileName));
            if (bytes == null)
            {
                Debug.LogWarning($"[MathQuestion] 找不到本機題庫：{_rootPath}");
                return Array.Empty<LocalQuestionEntry>();
            }

            var index = JsonUtility.FromJson<LocalQuestionIndex>(System.Text.Encoding.UTF8.GetString(bytes));
            return index?.questions ?? Array.Empty<LocalQuestionEntry>();
        }

        private async Awaitable<Question> Load(LocalQuestionEntry entry, QuestionFilter filter)
        {
            string imagesPath = Path.Combine(_rootPath, LocalQuestionIndex.ImagesFolderName);
            byte[] questionBytes = await StreamingFile.Read(Path.Combine(imagesPath, entry.question));
            if (questionBytes == null)
                return null;
            byte[] optionsBytes = await StreamingFile.Read(Path.Combine(imagesPath, entry.options));
            if (optionsBytes == null)
                return null;

            return ImageLoader.CreateQuestion(entry.id, questionBytes, optionsBytes, entry.choices, entry.answer,
                filter.Grade, filter.Publisher);
        }
    }
}
