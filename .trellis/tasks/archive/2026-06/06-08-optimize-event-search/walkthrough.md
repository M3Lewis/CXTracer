# Walkthrough: Optimize Event Search Performance

We have optimized the event search box performance to resolve UI lag when typing. The solution introduces debouncing, async batching, conditional RawJson searching, and pre-computed lowercase text matching.

## Changes Made

### 1. Pre-computed Lowercase Text Caching
- **File**: [DisplayEvent.cs](file:///k:/Code/ACTIVE/CXTracer/src/CXTracer/Models/DisplayEvent.cs)
- **Change**: Added a lazy-cached, read-only `SearchableText` property that combines `Title` and `Text` in lowercase. Since these values are initialized via `init`, they never change, making this caching safe and extremely fast for subsequent searches.

### 2. Async Event Filtering & Debouncing
- **File**: [MainWindowViewModel.cs](file:///k:/Code/ACTIVE/CXTracer/src/CXTracer/ViewModels/MainWindowViewModel.cs)
- **Change**: 
  - Added a `_eventFilterCts` cancellation token source.
  - Converted the event handlers `OnEventSearchTextChanged`, `OnSelectedFilterChanged`, and `OnShowRawEventsChanged` to trigger `ApplyFilterAsync` asynchronously.
  - Added a 200ms debounce delay to `OnEventSearchTextChanged` to avoid restarting UI rebuilds during active typing.
  - Added immediate cancellation and disposal of existing `CancellationTokenSource` objects before creating new ones in all event and session filter handlers, preventing resource leaks.
  - Converted `ApplyFilter` to an asynchronous batching method `ApplyFilterAsync(CancellationToken ct, bool debounce)`. It yields to the UI thread (via `YieldToUiAsync()`) after every `EventBatchSize` (40) checked events, ensuring the UI remains responsive and loads search results incrementally.

### 3. Conditional RawJson Search & Optimized Loop
- **File**: [MainWindowViewModel.cs](file:///k:/Code/ACTIVE/CXTracer/src/CXTracer/ViewModels/MainWindowViewModel.cs)
- **Change**:
  - Implemented `PassesFilterInternal` to extract the case-insensitive/lowercase parameters once before iterating.
  - Modified the filter check to only search `evt.RawJson` if `ShowRawEvents` is true or `SelectedFilter` key is `"Raw"`.
  - Used cached `evt.SearchableText.Contains(qLower)` for the primary query match, avoiding repeated allocations and expensive ordinal-ignore-case operations.

---

## Verification Results

### Automated Verification
- Ran `dotnet build` successfully:
  ```text
  已还原 K:\Code\ACTIVE\CXTracer\src\CXTracer\CXTracer.csproj (用时 2.09 秒)。
  CXTracer -> K:\Code\ACTIVE\CXTracer\src\CXTracer\bin\Debug\net8.0\CXTracer.dll
  已成功生成。
  1 个警告
  0 个错误
  ```
