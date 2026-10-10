# Math Question Practice

小學生數學答題的練習作品「餵小怪獸」，不上架。目的是驗證兩件事：答題模組能否乾淨地用在新遊戲，以及靠 AI coding 能多快做出一個小遊戲。

- 企劃與範圍：[Docs/Prototype.md](Docs/Prototype.md)
- 答題模組設計與 eMath API：[Docs/QuestionModule.md](Docs/QuestionModule.md)

動手前先讀這兩份。範圍以 Prototype.md 的「要做／不做」為準，不要自行加功能。

## 環境

- Unity 6（6000.6.4f1），URP
- 輸入：只啟用新版 Input System。UI 拖曳用 UGUI 的 `IBeginDragHandler` / `IDragHandler` / `IEndDragHandler`，EventSystem 使用 `InputSystemUIInputModule`。
- 目標平台：Android 平板，橫向

## 程式規範

- 保持簡單。這是小專案，不要引入 DI 框架、事件匯流排、Odin、UniRx 等套件。
- 不使用 Singleton。需要的參照用 Inspector 指定，或由場景中的一個進入點建立後傳下去。
- 非同步一律用 async/await 與 Unity 6 的 `Awaitable`，不用 Coroutine。
- 一個類別只做一件事。MonoBehaviour 只處理畫面與輸入，遊戲規則放在一般 C# 類別。
- 命名：類別與公開成員 PascalCase，私有欄位 `_camelCase`，序列化欄位用 `[SerializeField] private`。
- 註解用繁體中文，只寫「為什麼」，不要逐行解釋程式碼在做什麼。

## 答題模組（`Assets/MathQuestion/`）

- 有自己的 `MathQuestion.asmdef`，**不可以引用遊戲程式碼**，遊戲只能透過 `QuestionProvider` 使用它。
- 模組不處理 UI、獎勵、存檔。
- 修改公開介面時，要同步更新 `Docs/QuestionModule.md`。
- eMath 的 Token 只能從 `EMathConfig` 讀取。不要寫進程式碼、不要印在 Log、不要提交到 git。

## 遊戲（`Assets/Game/`）

- 遊戲規則（出題順序、計分、飽足度、成長階段）放在一般 C# 類別，方便單獨測試。
- 存檔用 `PlayerPrefs` 或 `Application.persistentDataPath` 下的 JSON，不用額外套件。

## 參考專案

舊專案 `../MathQuestionGame`（Unity 2021）的 `Assets/Scripts/Question/` 只能當作題目格式與 API 的參考，**不要複製它的程式碼或架構**。它依賴 Singleton、GameManager 與 Odin，正是這次要避免的寫法。

## 開發紀錄

每完成 Prototype.md 的一個里程碑，提醒開發者在「開發紀錄」表格記下花費時間與卡住的地方。
