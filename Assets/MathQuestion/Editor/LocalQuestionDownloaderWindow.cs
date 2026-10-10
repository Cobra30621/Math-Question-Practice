using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MathQuestion.Editor
{
    // 用 eMath API 建立離線題庫，產生 StreamingAssets/Questions 下的 index.json 與圖片
    internal class LocalQuestionDownloaderWindow : EditorWindow
    {
        private const string OutputRoot = "Assets/StreamingAssets/" + LocalQuestionIndex.FolderName;

        // 不是每個組合都有題目，每題多給幾次機會
        private const int AttemptsPerQuestion = 6;
        private const int TimeoutSeconds = 30;

        // 偶發的逾時不值得中斷整批下載，連續失敗才代表伺服器真的有問題
        private const int MaxConsecutiveFailures = 3;

        [SerializeField] private EMathConfig _config;
        [SerializeField] private Publisher _publisher = Publisher.Ziyou;
        [SerializeField] private int _questionsPerGrade = 20;

        private readonly System.Random _random = new();
        private bool _isDownloading;
        private bool _cancelRequested;
        private string _status = "";

        [MenuItem("Tools/Math Question/下載本機題庫")]
        private static void Open() => GetWindow<LocalQuestionDownloaderWindow>("本機題庫");

        private void OnEnable()
        {
            if (_config != null)
                return;
            string guid = AssetDatabase.FindAssets($"t:{nameof(EMathConfig)}").FirstOrDefault();
            if (guid != null)
                _config = AssetDatabase.LoadAssetAtPath<EMathConfig>(AssetDatabase.GUIDToAssetPath(guid));
        }

        private void OnGUI()
        {
            using (new EditorGUI.DisabledScope(_isDownloading))
            {
                _config = (EMathConfig)EditorGUILayout.ObjectField("EMath Config", _config, typeof(EMathConfig), false);
                _publisher = (Publisher)EditorGUILayout.EnumPopup("出版社", _publisher);
                _questionsPerGrade = Mathf.Max(1, EditorGUILayout.IntField("每個年級題數", _questionsPerGrade));
                EditorGUILayout.HelpBox("已下載的題目會保留，只補足不夠的題數。", MessageType.None);

                bool canStart = _config != null && _config.HasToken;
                using (new EditorGUI.DisabledScope(!canStart))
                {
                    if (GUILayout.Button("下載所有年級"))
                        Download();
                }
                if (!canStart)
                    EditorGUILayout.HelpBox("需要一個有 Token 的 EMathConfig。", MessageType.Warning);
            }

            if (_isDownloading && GUILayout.Button("停止"))
                _cancelRequested = true;

            EditorGUILayout.LabelField(_status, EditorStyles.wordWrappedLabel);
        }

        // 按鈕事件只能是 async void，所以例外要在這裡接住
        private async void Download()
        {
            _isDownloading = true;
            _cancelRequested = false;
            try
            {
                await DownloadAllGrades();
            }
            catch (Exception e)
            {
                _status = $"中斷：{e.Message}";
                Debug.LogException(e);
            }
            finally
            {
                _isDownloading = false;
                AssetDatabase.Refresh();
                Repaint();
            }
        }

        private async Awaitable DownloadAllGrades()
        {
            string imagesPath = Path.Combine(OutputRoot, LocalQuestionIndex.ImagesFolderName);
            Directory.CreateDirectory(imagesPath);

            var entries = LoadIndex();
            var knownIds = new HashSet<string>(entries.Select(e => e.id));
            var client = new EMathClient(_config, TimeoutSeconds);
            int consecutiveFailures = 0;
            var grades = (Grade[])Enum.GetValues(typeof(Grade));

            // 連續失敗而中斷時，已下載的部分也要寫進 index
            try
            {
                foreach (var grade in grades)
                {
                    int have = entries.Count(e => e.grade == grade.ToString() && e.publisher == _publisher.ToString());
                    int need = _questionsPerGrade - have;

                    for (int attempt = 0; attempt < need * AttemptsPerQuestion && need > 0; attempt++)
                    {
                        if (_cancelRequested)
                        {
                            _status = $"已停止，共 {entries.Count} 題";
                            return;
                        }

                        LocalQuestionEntry entry;
                        try
                        {
                            entry = await DownloadOne(client, grade, imagesPath, knownIds);
                            consecutiveFailures = 0;
                        }
                        catch (EMathUnavailableException e) when (++consecutiveFailures < MaxConsecutiveFailures)
                        {
                            Debug.LogWarning($"[MathQuestion] {e.Message}，重試中（{consecutiveFailures}/{MaxConsecutiveFailures}）");
                            continue;
                        }

                        if (entry != null)
                        {
                            entries.Add(entry);
                            need--;
                        }

                        _status = $"{grade}：還差 {need} 題（嘗試 {attempt + 1} 次），全部共 {entries.Count} 題";
                        Repaint();
                    }

                    // 每個年級存一次，Unity 當掉也不會白載
                    SaveIndex(entries);
                    if (need > 0)
                        Debug.LogWarning($"[MathQuestion] {grade} 只取到 {_questionsPerGrade - need} 題");
                }
            }
            finally
            {
                SaveIndex(entries);
            }

            _status = $"完成，共 {entries.Count} 題";
        }

        private async Awaitable<LocalQuestionEntry> DownloadOne(EMathClient client, Grade grade, string imagesPath,
            HashSet<string> knownIds)
        {
            int chapter = EMathSerial.RandomChapter(_random);
            int difficulty = EMathSerial.RandomDifficulty(_random);

            var problems = await client.GetProblems(EMathSerial.Build(_publisher, grade, chapter, difficulty), 1);
            if (problems.Length == 0)
                return null;

            var problem = problems[0];
            if (knownIds.Contains(problem.problemLink)
                || !int.TryParse(problem.answer, out int answer)
                || !int.TryParse(problem.choices, out int choices))
                return null;

            byte[] questionBytes = await client.GetImage(problem.problemLink);
            byte[] optionsBytes = questionBytes == null ? null : await client.GetImage(problem.ansLink);
            if (optionsBytes == null)
                return null;

            string questionFile = problem.problemLink + ".png";
            string optionsFile = problem.ansLink + ".png";
            File.WriteAllBytes(Path.Combine(imagesPath, questionFile), questionBytes);
            File.WriteAllBytes(Path.Combine(imagesPath, optionsFile), optionsBytes);
            knownIds.Add(problem.problemLink);

            return new LocalQuestionEntry
            {
                id = problem.problemLink,
                publisher = _publisher.ToString(),
                grade = grade.ToString(),
                chapter = chapter,
                difficulty = difficulty,
                question = questionFile,
                options = optionsFile,
                answer = answer,
                choices = choices
            };
        }

        private static List<LocalQuestionEntry> LoadIndex()
        {
            string path = Path.Combine(OutputRoot, LocalQuestionIndex.IndexFileName);
            if (!File.Exists(path))
                return new List<LocalQuestionEntry>();
            var index = JsonUtility.FromJson<LocalQuestionIndex>(File.ReadAllText(path));
            return index?.questions?.ToList() ?? new List<LocalQuestionEntry>();
        }

        private static void SaveIndex(List<LocalQuestionEntry> entries)
        {
            var index = new LocalQuestionIndex { questions = entries.ToArray() };
            File.WriteAllText(Path.Combine(OutputRoot, LocalQuestionIndex.IndexFileName),
                JsonUtility.ToJson(index, true));
        }
    }
}
