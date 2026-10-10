# 答題模組設計

2026-10-10

## 原則

- **只負責「取題」和「判斷對錯」。** 答題畫面、答對後的獎勵、存檔都由遊戲自己處理。
- **不依賴任何管理器。** 不用 Singleton、不用 Odin、不引用 `GameManager` 之類的類別。放進空的 Unity 專案就能用。
- **有網路用線上題目，沒有就自動改用本機題庫。** 遊戲端不需要知道題目從哪裡來。
- **獨立的 Assembly Definition**（`MathQuestion.asmdef`），確保模組不會不小心引用到遊戲程式碼。

MathQuestionGame 的 `Assets/Scripts/Question/` 只當作參考，不複製程式碼。

## 對外介面

```csharp
// 遊戲端唯一需要用的入口
public class QuestionProvider
{
    // online 可以傳 null（例如沒有 Token），此時只用本機題庫
    public QuestionProvider(IQuestionSource online, IQuestionSource local);

    // 取最多 count 題：先取線上，失敗或題數不足時用本機題庫補齊（依 Id 去重）
    // 兩邊加起來還是不夠時，回傳的題數會少於 count，由遊戲端處理
    public Awaitable<IReadOnlyList<Question>> GetQuestions(QuestionFilter filter, int count);
}

public class QuestionFilter
{
    public Grade Grade;
    public Publisher Publisher = Publisher.Ziyou;   // 原型固定用子由
}

public class Question
{
    public string Id { get; }              // eMath 的 problemLink，用來去重
    public Sprite QuestionImage { get; }   // 題目圖
    public Sprite OptionsImage { get; }    // 選項圖（所有選項在同一張圖）
    public int ChoiceCount { get; }        // 選項數量
    public Grade Grade { get; }
    public Publisher Publisher { get; }

    public bool IsCorrect(int choice);     // choice 從 1 開始，與選項圖上的 ①～④ 一致
    public void Release();                 // 釋放執行期建立的圖片，這題用完後呼叫
}

// 內建兩種來源
public class EMathQuestionSource : IQuestionSource { public EMathQuestionSource(EMathConfig config); }
public class LocalQuestionSource : IQuestionSource
{
    public LocalQuestionSource();                 // 讀 StreamingAssets/Questions
    public LocalQuestionSource(string rootPath);  // 測試或自訂位置用
}

public interface IQuestionSource
{
    Awaitable<IReadOnlyList<Question>> GetQuestions(QuestionFilter filter, int count);
}
```

遊戲端用法：

```csharp
// 進入點建立一次，往下傳
var provider = new QuestionProvider(new EMathQuestionSource(config), new LocalQuestionSource());

var questions = await provider.GetQuestions(new QuestionFilter { Grade = Grade.G2_1 }, 5);
if (questions[0].IsCorrect(selected)) FeedMonster();

// 一局結束
foreach (var q in questions) q.Release();
```

Unity 6 有內建的 `Awaitable`，所以用 async/await 取代原專案的 Coroutine。

## 列舉

沿用原專案的定義，數值順序不能改，因為 API 網址是用它們算出來的。

```csharp
public enum Grade { G1_1, G1_2, G2_1, G2_2, G3_1, G3_2, G4_1, G4_2, G5_1, G5_2, G6_1, G6_2 }
public enum Publisher { Ziyou, Hanlin, KangHsuan, NanYi }   // 子由、翰林、康軒、南一
```

原專案的 `MathType`（數學類型）實際上沒有從 API 取得，原型先不放。

## eMath API

整理自 MathQuestionGame 的 `QuestionDownloader.cs`。

**取一題**

```
GET https://api.emath.math.ncu.edu.tw/problem/serial/{序號}/{數量}
Header: Authorization: <Token>
```

序號由 6 段數字直接串接：

| 段 | 內容 | 計算方式 |
| --- | --- | --- |
| 1 | 出版社 | `(int)publisher + 1`（子由 = 1） |
| 2 | 年級 | `((int)grade + 2) / 2`（1～6） |
| 3 | 學期 | `(int)grade % 2 + 1`（上 = 1、下 = 2） |
| 4 | 章節 | 兩位數，`01`～`12`，大多只到 `08` |
| 5 | 難度 | `1`～`8`，難度 `2` 的題目最多 |
| 6 | 固定 | `001` |

例：子由、二上、第 3 章、難度 2 → `/problem/serial/121032001/1`

回傳格式：

```json
{
  "problem": [
    { "problemLink": "…", "ansLink": "…", "answer": "2", "choices": "4" }
  ]
}
```

**取圖片**

```
GET https://api.emath.math.ncu.edu.tw/problem/{problemLink 或 ansLink}
Header: Authorization: <Token>
```

一題要下載兩張圖：題目圖（`problemLink`）與選項圖（`ansLink`）。原專案每張圖最多重試 3 次，兩張都成功才算取到這一題。

`answer` 從 1 起算，對應選項圖上的 ①～④（例：`"4"` 表示 ④）。

圖片實際上是 JPEG，但工具存成 `.png` 副檔名。`Texture2D.LoadImage` 會依內容判斷格式，所以不影響讀取。

### 連線失敗與逾時

斷網時不能讓玩家乾等，所以線上來源會盡快放棄，改由本機題庫補足：

- `Application.internetReachability` 是 `NotReachable` 時，直接不打 API。
- 每個請求的逾時由 `EMathConfig.TimeoutSeconds` 決定，預設 5 秒。
- 遇到 `ConnectionError`（含逾時）或 HTTP 401／403，就視為整個線上題庫暫時不能用，停止這次取題，但保留已經取到的題目。
- 其他錯誤（例如 404、空陣列、JSON 解析失敗）視為「這個組合沒有題目」，換下一個組合再試。每題最多試 4 個組合。

**原專案的取題策略**：章節隨機 1～8；難度有 70% 選 2，30% 在 1～8 之間隨機。並不是每個組合都有題目，所以要能容忍單題失敗、繼續取下一題。

### Token

原專案把 Token 寫死在程式碼裡，而且已經進了 git 歷史。新專案改成：

- Token 放在不進版控的設定檔 `Assets/Config/EMathConfig.asset`（ScriptableObject），已經加進 `.gitignore`。
- 建立方式：在 Project 視窗的 `Assets/Config/` 按右鍵 → Create → Math Question → EMath Config，命名為 `EMathConfig`，然後填入 Token。
- 程式碼只從 `EMathConfig.Token` 讀取 Token，也不會把它印在 Log 裡。
- 打包進 App 的 Token 還是可以被解出來。這個原型不上架，可以接受；正式產品應改由後端代為取題。

## 本機題庫（離線用）

原專案有約 800 張題目與選項圖片（`Assets/Resources/Question/`，78 MB），對應關係存在 `Local Question Data.asset`。這個格式依賴 Odin 序列化，不沿用。

離線題庫要涵蓋**所有 12 個年級**，讓開始畫面的每個選項在離線時都能玩。

新格式：

```
Assets/StreamingAssets/Questions/
├── index.json
└── images/
    ├── <problemLink>.png
    └── <ansLink>.png
```

`index.json`（`JsonUtility` 不支援最外層是陣列，所以包一層 `questions`）：

```json
{
  "questions": [
    { "id": "<problemLink>", "publisher": "Ziyou", "grade": "G2_1", "chapter": 3, "difficulty": 2,
      "question": "<problemLink>.png", "options": "<ansLink>.png", "answer": 2, "choices": 4 }
  ]
}
```

- `id` 和線上題目的 `Question.Id` 相同，線上與離線混用時可以去重。
- `question`、`options` 是 `images/` 底下的檔名。
- `answer` 保留 API 的原始值，從 1 起算。

**建立方式：編輯器工具**（已選定，取代「從舊專案轉換」）

選單 Tools → Math Question → 下載本機題庫：

- 對 12 個年級各下載 N 題（預設 20），已經下載過的題目會保留，只補不足的部分，中途停止也不會白載。
- 每下載完一個年級就寫一次 `index.json`。
- 需要一個填好 Token 的 `EMathConfig`。

容量估算：實測每張圖約 17 KB，一題兩張。12 年級 × 20 題 ≈ 8 MB，對 apk 大小影響不大。

- 有些年級的題目很少（實測 G4_1 只有少數章節有題），可能補不滿，工具會在 Console 提示。重跑工具會接著補。
- 下載工具的逾時是 30 秒，比遊戲中的 5 秒長。連續 3 次連線失敗才中斷，中斷時已下載的題目仍會寫進 index。

### 平台注意：Android 的 StreamingAssets

Android 上的 `StreamingAssets` 包在 apk（壓縮檔）裡，路徑是 `jar:file://…!/assets/…`，`System.IO.File` 讀不到。所以：

- `LocalQuestionSource` 讀 `index.json` 和圖片一律用 `UnityWebRequest`，各平台都走同一條路，不寫平台判斷。
- 本機路徑（編輯器、iOS）要轉成跳脫過的 `file://` URI，因為專案路徑有空白。
- `index.json` 只在第一次取題時讀一次並快取。圖片則是選到哪題才載哪題，避免一次把全部圖片載進記憶體。
- 圖片用 `Texture2D.LoadImage` 從位元組建立。線上與離線共用同一段程式碼，所以題庫存的是原始檔，不重新編碼。

## 資料夾結構

```
Assets/MathQuestion/
├── MathQuestion.asmdef
├── AssemblyInfo.cs                 內部類別只開放給 Editor 與 Tests
├── Question.cs
├── QuestionFilter.cs
├── Grade.cs、Publisher.cs
├── QuestionProvider.cs
├── Sources/
│   ├── IQuestionSource.cs
│   ├── EMathQuestionSource.cs     線上
│   └── LocalQuestionSource.cs     離線
├── Config/
│   └── EMathConfig.cs             Token、網址、逾時
├── Internal/                       （internal，遊戲端看不到）
│   ├── EMathClient.cs             API 請求、圖片重試
│   ├── EMathSerial.cs             序號計算、隨機章節與難度
│   ├── LocalQuestionIndex.cs      index.json 格式
│   ├── StreamingFile.cs           跨平台讀 StreamingAssets
│   └── ImageLoader.cs             位元組 → Sprite、釋放
├── Editor/
│   ├── MathQuestion.Editor.asmdef
│   └── LocalQuestionDownloaderWindow.cs   本機題庫下載工具
└── Tests/                          EditMode 測試
    └── MathQuestion.Tests.asmdef
```

## 待確認

- [x] `answer` 是從 1 還是從 0 起算？→ **從 1 起算**。實測 43 題：沒有任何一題 `answer` 為 0，有 13 題的 `answer` 等於選項數；人工檢查一題（G1_1，answer 4）確認正解是 ④。
- [ ] `/problem/serial/{序號}/{數量}` 的數量大於 1 時，是否一次回傳多題？可以的話能減少請求次數。目前的程式一次只要 1 題。另外確認：同一個序號重複請求，會回傳不同的題目（是隨機抽題），所以重複打同一個組合是有意義的。
- [ ] 取不到題目時 API 回傳什麼（404、空陣列）？決定要怎麼判斷「這個組合沒有題目」。目前的程式把 404、空陣列與解析失敗都當成「沒有題目」，實測後再收緊。
