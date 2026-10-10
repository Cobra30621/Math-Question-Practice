using System.Linq;
using MathQuestion;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Game.Editor
{
    // 用程式產生場景，比手動拉 UI 再一個個拖參照快，也容易重建
    internal static class GameSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Game.unity";
        private static readonly Vector2 ReferenceResolution = new(1920, 1080);

        // 先用顏色區分食物（蘋果、香蕉、葡萄、草莓），之後換圖
        private static readonly Color[] FoodColors =
        {
            new(0.9f, 0.25f, 0.25f), new(0.98f, 0.85f, 0.3f), new(0.6f, 0.35f, 0.8f), new(1f, 0.5f, 0.6f)
        };

        private static Sprite _circle;
        private static Sprite _roundedRect;
        private static Font _font;

        [MenuItem("Tools/Game/建立遊戲場景")]
        private static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null
                && !EditorUtility.DisplayDialog("建立遊戲場景", $"{ScenePath} 已存在，要覆蓋嗎？", "覆蓋", "取消"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            _circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            _roundedRect = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera();
            CreateEventSystem();
            var canvas = CreateCanvas();

            var startScreen = CreateStartScreen(canvas.transform);
            var gameScreen = CreateGameScreen(canvas.transform);
            var resultScreen = CreateResultScreen(canvas.transform);

            // 只留開始畫面，其他畫面在第一次顯示時才啟用，編輯時不會疊在一起
            gameScreen.gameObject.SetActive(false);
            resultScreen.gameObject.SetActive(false);

            var entry = new GameObject("GameEntry").AddComponent<GameEntry>();
            Assign(entry, ("_eMathConfig", FindConfig()), ("_startScreen", startScreen), ("_gameScreen", gameScreen),
                ("_resultScreen", resultScreen));

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings();
            Debug.Log($"[Game] 已建立 {ScenePath}");
        }

        private static void CreateCamera()
        {
            var camera = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(1f, 0.96f, 0.85f);
        }

        private static void CreateEventSystem()
        {
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        private static Canvas CreateCanvas()
        {
            var canvas = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))
                .GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static StartScreen CreateStartScreen(Transform parent)
        {
            var panel = CreatePanel("StartScreen", parent);

            var title = CreateText("Title", panel, 110, TextAnchor.MiddleCenter);
            Place(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -100), new Vector2(1200, 150));
            title.text = "餵小怪獸";

            var hint = CreateText("Hint", panel, 48, TextAnchor.MiddleCenter);
            Place(hint.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -270), new Vector2(1200, 80));
            hint.text = "選年級";

            // 6 欄是年級、2 列是上下學期，順序與 Grade 的數值一致
            var gradeButtons = new Button[12];
            for (int i = 0; i < gradeButtons.Length; i++)
            {
                int column = i / 2;
                int row = i % 2;
                gradeButtons[i] = CreateButton($"Grade{i}", panel, GradeNames.Of((Grade)i), 56, Color.white);
                Place((RectTransform)gradeButtons[i].transform, new Vector2(0.5f, 0.5f),
                    new Vector2((column - 2.5f) * 240, 90 - row * 160), new Vector2(200, 130));
            }

            var message = CreateText("MessageText", panel, 40, TextAnchor.MiddleCenter);
            Place(message.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 330), new Vector2(1600, 70));
            message.color = new Color(0.8f, 0.2f, 0.2f);

            var startButton = CreateButton("StartButton", panel, "開始", 72, new Color(1f, 0.6f, 0.25f));
            Place((RectTransform)startButton.transform, new Vector2(0.5f, 0), new Vector2(0, 140), new Vector2(420, 150));

            var screen = panel.gameObject.AddComponent<StartScreen>();
            Assign(screen, ("_gradeButtons", gradeButtons), ("_startButton", startButton), ("_messageText", message));
            return screen;
        }

        private static GameScreen CreateGameScreen(Transform parent)
        {
            var panel = CreatePanel("GameScreen", parent);

            var questionImage = CreateImage("QuestionImage", panel, null, Color.white);
            Place(questionImage.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(1500, 260));
            questionImage.preserveAspect = true;

            var optionsImage = CreateImage("OptionsImage", panel, null, Color.white);
            Place(optionsImage.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -320), new Vector2(1500, 110));
            optionsImage.preserveAspect = true;

            var progressText = CreateText("ProgressText", panel, 40, TextAnchor.UpperLeft);
            Place(progressText.rectTransform, new Vector2(0, 1), new Vector2(40, -20), new Vector2(400, 60), new Vector2(0, 1));

            var monster = CreateMonster(panel);
            var foods = CreateFoods(panel);

            var messageText = CreateText("MessageText", panel, 64, TextAnchor.MiddleCenter);
            Place(messageText.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1600, 200));
            messageText.raycastTarget = false;

            var screen = panel.gameObject.AddComponent<GameScreen>();
            Assign(screen, ("_questionImage", questionImage), ("_optionsImage", optionsImage),
                ("_progressText", progressText), ("_messageText", messageText), ("_monster", monster), ("_foods", foods));
            return screen;
        }

        private static ResultScreen CreateResultScreen(Transform parent)
        {
            var panel = CreatePanel("ResultScreen", parent);

            var title = CreateText("Title", panel, 100, TextAnchor.MiddleCenter);
            Place(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -140), new Vector2(1200, 150));
            title.text = "吃飽了！";

            var score = CreateText("ScoreText", panel, 64, TextAnchor.MiddleCenter);
            Place(score.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 80), new Vector2(1400, 100));

            var satiety = CreateText("SatietyText", panel, 64, TextAnchor.MiddleCenter);
            Place(satiety.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(1400, 100));

            var playAgain = CreateButton("PlayAgainButton", panel, "再玩一次", 60, new Color(1f, 0.6f, 0.25f));
            Place((RectTransform)playAgain.transform, new Vector2(0.5f, 0), new Vector2(-260, 160), new Vector2(420, 150));

            var changeGrade = CreateButton("ChangeGradeButton", panel, "換年級", 60, Color.white);
            Place((RectTransform)changeGrade.transform, new Vector2(0.5f, 0), new Vector2(260, 160), new Vector2(420, 150));

            var screen = panel.gameObject.AddComponent<ResultScreen>();
            Assign(screen, ("_scoreText", score), ("_satietyText", satiety), ("_playAgainButton", playAgain),
                ("_changeGradeButton", changeGrade));
            return screen;
        }

        private static MonsterView CreateMonster(Transform parent)
        {
            var body = CreateImage("Monster", parent, _circle, new Color(0.45f, 0.75f, 0.95f));
            Place(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -65), new Vector2(300, 300));

            foreach (float x in new[] { -60f, 60f })
            {
                var eye = CreateImage("Eye", body.transform, _circle, Color.white);
                Place(eye.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(x, 50), new Vector2(70, 70));
                var pupil = CreateImage("Pupil", eye.transform, _circle, Color.black);
                Place(pupil.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(32, 32));
            }

            var mouth = CreateImage("Mouth", body.transform, _circle, new Color(0.35f, 0.1f, 0.15f));
            Place(mouth.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -55), new Vector2(140, 100));

            // 子物件不擋射線，拖曳放開時才不會被眼睛、嘴巴誤判
            foreach (var graphic in body.GetComponentsInChildren<Graphic>())
                graphic.raycastTarget = false;

            var view = body.gameObject.AddComponent<MonsterView>();
            Assign(view, ("_body", body), ("_mouth", mouth.rectTransform));
            return view;
        }

        private static FoodItem[] CreateFoods(Transform parent)
        {
            var row = new GameObject("Foods", typeof(RectTransform)).GetComponent<RectTransform>();
            row.SetParent(parent, false);
            Place(row, new Vector2(0.5f, 0), new Vector2(0, 130), new Vector2(1600, 180));

            return FoodColors.Select((color, i) =>
            {
                var image = CreateImage($"Food{i + 1}", row, _circle, color);
                Place(image.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(170, 170));

                var label = CreateText("Label", image.transform, 80, TextAnchor.MiddleCenter);
                label.rectTransform.anchorMin = Vector2.zero;
                label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.sizeDelta = Vector2.zero;
                label.raycastTarget = false;
                label.text = (i + 1).ToString();

                var food = image.gameObject.AddComponent<FoodItem>();
                Assign(food, ("_label", label));
                return food;
            }).ToArray();
        }

        private static RectTransform CreatePanel(string name, Transform parent)
        {
            var panel = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            panel.SetParent(parent, false);
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = Vector2.one;
            panel.sizeDelta = Vector2.zero;
            return panel;
        }

        private static Button CreateButton(string name, Transform parent, string label, int fontSize, Color color)
        {
            var image = CreateImage(name, parent, _roundedRect, color);
            image.type = Image.Type.Sliced;
            var button = image.gameObject.AddComponent<Button>();

            var text = CreateText("Label", image.transform, fontSize, TextAnchor.MiddleCenter);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.sizeDelta = Vector2.zero;
            text.raycastTarget = false;
            text.text = label;
            return button;
        }

        // 序列化欄位是 private，只能透過 SerializedObject 指定
        private static void Assign(Object target, params (string Field, object Value)[] fields)
        {
            var serialized = new SerializedObject(target);
            foreach (var (field, value) in fields)
            {
                var property = serialized.FindProperty(field);
                if (value is Object[] array)
                {
                    property.arraySize = array.Length;
                    for (int i = 0; i < array.Length; i++)
                        property.GetArrayElementAtIndex(i).objectReferenceValue = array[i];
                }
                else
                {
                    property.objectReferenceValue = (Object)value;
                }
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.sprite = sprite;
            image.color = color;
            return image;
        }

        private static Text CreateText(string name, Transform parent, int fontSize, TextAnchor alignment)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(parent, false);
            text.font = _font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = new Color(0.25f, 0.2f, 0.15f);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            return text;
        }

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size,
            Vector2? pivot = null)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot ?? new Vector2(0.5f, anchor.y);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static EMathConfig FindConfig()
        {
            string guid = AssetDatabase.FindAssets($"t:{nameof(EMathConfig)}").FirstOrDefault();
            return guid == null ? null : AssetDatabase.LoadAssetAtPath<EMathConfig>(AssetDatabase.GUIDToAssetPath(guid));
        }

        private static void AddToBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
