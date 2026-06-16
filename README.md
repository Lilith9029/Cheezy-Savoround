# Cheezy Savoround

**Cheezy Savoround** là một tựa game giải đố thuộc thể loại Sorting/Merge với chủ đề về những lát bánh pizza. Người chơi sẽ vào vai một đầu bếp, sắp xếp và ghép các lát bánh pizza còn thiếu trên một bàn lưới để tạo thành những đĩa bánh hoàn chỉnh.

---

## 1. Sơ Đồ Hoạt Động Hệ Thống (System Flow Diagram)

Dưới đây là sơ đồ luồng hoạt động chính của core gameplay, mô tả sự tương tác giữa Khối Đầu Vào (Input), Xử Lý Logic (Logic Core), Hệ Thống Truyền Tin (Event System) và Giao Diện Đầu Ra (Visual/Audio Output):

### Sơ đồ SFD:
![System Flow Diagram](docs/Cheezy%20Savoround.drawio.png)

---

## 2. Kiến Trúc Hệ Thống (System Architecture)

Dự án được thiết kế theo mô hình hướng sự kiện (Event-driven) độc lập, giảm thiểu sự phụ thuộc trực tiếp (Tight Coupling) giữa các Class thông qua Observer Pattern và các mô hình State:

### Thứ Tự Khởi Tạo (Execution Order)

Thứ tự `[DefaultExecutionOrder]` được áp dụng để đảm bảo các dependency đã sẵn sàng trước khi subscribe:

| Ưu tiên | Class | Lý do |
|---------|-------|-------|
| `-10` | `UserDataManager` | Load save file trước mọi manager khác |
| `-10` | `GameManager` | Broadcast state ban đầu sau khi data đã load |
| `-5` | `AchievementManager` | Cần `UserDataManager.Instance` để `EnsureSaveDataForAll()` |
| `0` | `UIManager`, `ShopManager`, v.v. | Subscribe event sau khi tất cả core đã init |

### Khối Đầu Vào (Input)
- **`DraggablePlate.cs`**: Thực hiện Raycast phát hiện điểm chạm trên bàn chơi 3D. Hỗ trợ Snap tự động khi di chuyển chậu/đĩa pizza gần ô lưới.
- **`GhostPlatePreview.cs`**: Hiển thị mô hình đĩa bán trong suốt (Ghost Material) và nâng cao gạch lát nền (Tile) để làm nổi bật ô lưới dự kiến thả.

### Khối Logic (Logic Core)
- **`GameManager.cs`**: Quản lý Finite State Machine (FSM) kiểm soát các trạng thái: `Menu`, `Playing`, `CheckingCombo`, `Animating`, `GameOver`.
- **`MergeManager.cs`**: Thực hiện quét 4 hướng lân cận khi đặt đĩa bánh xuống, tính toán ưu tiên gom/trừ và nổ hoa (Bloom/Explode) bằng luật ưu tiên.

### Hệ Thống Phụ Trợ & Hỗ Trợ (Meta & Boosters)
- **`UserDataManager.cs`**: Đọc và ghi dữ liệu người chơi (Vàng, Skin đã mua, tiến độ nhiệm vụ, số lượng vật phẩm hỗ trợ) ra tệp JSON (`userdata.json`) tại thư mục lưu trữ cục bộ (`Application.persistentDataPath`).
- **`BoosterManager.cs`**: Quản lý logic kích hoạt và trừ lượt sử dụng của 4 loại Power-ups/Vật phẩm hỗ trợ (Dao Cắt, Chai Sốt, Thớt Gỗ, Thùng Rác) khi người chơi chọn công cụ và click vào ô lưới tương ứng trên bàn chơi 3D.
- **`ShopManager.cs`** & **`ShopUI.cs`**: Quản lý cửa hàng đổi màu sắc/skin đĩa bánh pizza và mua sắm thêm vật phẩm hỗ trợ.
- **`DailyRewardManager.cs`** & **`DailyRewardUI.cs`**: Điểm danh nhận vàng mỗi 24 giờ. Bảo mật kiểm tra thời gian thực qua **WorldTimeAPI (UTC)** và cơ chế đối chiếu lịch sử phiên chơi (last session) nhằm chống hack đổi giờ trên thiết bị.
- **`AchievementManager.cs`** & **`AchievementUI.cs`**: Hệ thống nhiệm vụ trọn đời hoạt động hoàn toàn hướng sự kiện. Danh sách nhiệm vụ được đọc hoàn toàn từ `Resources/achievements.json` — không hardcode trong code. Lắng nghe các sự kiện nổ hoa (`PizzaPlate.OnPlateCleared`), combo (`ComboAudioPlayer.OnComboAchieved`), vàng (`GameManager.OnGoldChanged`) và level (`GameManager.OnLevelProgressChanged`) để cập nhật tiến độ.

---

## 3. Tối Ưu Hóa Hiệu Năng (Performance Optimization)

1. **Lưu đệm Ô Lân Cận (Cached Neighbors)**: Các ô lân cận trong `GridManager` được tính toán trước 1 lần duy nhất tại `Start` và lưu trữ trong `Cell.neighbors`. Hàm `GetNeighbors` chỉ trả về tham chiếu danh sách có sẵn thay vì cấp phát mới `new List<Cell>()` và mảng hướng `int[,] dirs` mỗi khung hình.
2. **Quét Logic Không Cấp Phát (Non-Alloc Unique Types)**: Thay thế việc sử dụng `HashSet<string>` trong quá trình phân tích đĩa bánh bằng phương thức `GetUniqueTypesNonAlloc` sử dụng mảng đệm tĩnh (`_uniqueTypesBuffer`) giúp loại bỏ hoàn toàn rác thải bộ nhớ khi kiểm tra merge.
3. **Tối Ưu Hóa Vòng Lặp Merge**: Loại bỏ hoàn toàn việc khởi tạo đối tượng `MoveResult` cho các ứng viên (candidates) trong quá trình quét logic. Chỉ một đối tượng `MoveResult` duy nhất được tạo ra khi xác định được nước đi tốt nhất.
4. **Lưu Đệm Hiệu Ứng Particle (Cached Particle Systems)**: Trong `ObjectPooler.cs`, danh sách các `ParticleSystem` của các Prefab được lưu trữ trước trong component `PooledObject`. Tránh gọi hàm `GetComponentsInChildren<ParticleSystem>()` vốn gây phân mảnh bộ nhớ trên heap khi combo nổ liên tục.

---

## 4. Hướng Dẫn Cấu Hình Dữ Liệu JSON

Tất cả dữ liệu cấu hình được đặt trong thư mục `Assets/Resources/` và được nạp động qua `Resources.Load<TextAsset>()`.

### Cấu Hình Lưới Màn Chơi (`GridConfig.json`)
```json
{
  "width": 3,
  "height": 3,
  "spacing": 2.0
}
```
| Trường | Kiểu | Mô tả |
|--------|------|-------|
| `width` | `int` | Số cột của lưới bàn chơi |
| `height` | `int` | Số hàng của lưới bàn chơi |
| `spacing` | `float` | Khoảng cách (đơn vị Unity) giữa các ô lưới |

---

### Cấu Hình Cửa Hàng Skins (`ShopConfig.json`)
```json
{
  "skins": [
    {
      "id": "default",
      "displayName": "Đĩa Sứ Trắng",
      "cost": 0,
      "colorHex": "#FFFFFF",
      "metallic": 0.1,
      "smoothness": 0.8
    },
    {
      "id": "gold",
      "displayName": "Đĩa Hoàng Gia",
      "cost": 500,
      "colorHex": "#FFD700",
      "metallic": 0.9,
      "smoothness": 0.9
    }
  ]
}
```
| Trường | Kiểu | Mô tả |
|--------|------|-------|
| `id` | `string` | ID nội bộ duy nhất (không đổi) |
| `displayName` | `string` | Tên hiển thị trong cửa hàng |
| `cost` | `int` | Giá mua bằng vàng (0 = miễn phí) |
| `colorHex` | `string` | Mã màu HEX áp lên Material đĩa |
| `metallic` | `float` | Độ kim loại (0–1) |
| `smoothness` | `float` | Độ bóng (0–1) |

---

### Cấu Hình Nhiệm Vụ Thành Tích (`achievements.json`)

Danh sách nhiệm vụ được đọc **hoàn toàn từ JSON** — không hardcode trong code. Thêm, sửa, xóa nhiệm vụ bằng cách chỉnh sửa file này mà không cần recompile.

```json
{
  "achievements": [
    {
      "id": "bloom_pizzas_10",
      "title": "Pizza Rookie",
      "description": "Clear 10 pizza plates",
      "targetValue": 10,
      "reward": 50
    },
    {
      "id": "max_combo_5",
      "title": "Combo King",
      "description": "Achieve a x5 combo",
      "targetValue": 5,
      "reward": 100
    }
  ]
}
```
| Trường | Kiểu | Mô tả |
|--------|------|-------|
| `id` | `string` | ID duy nhất, **phải khớp** với ID được gọi trong `AchievementManager` |
| `title` | `string` | Tiêu đề hiển thị trong UI |
| `description` | `string` | Mô tả mục tiêu |
| `targetValue` | `int` | Giá trị mục tiêu cần đạt |
| `reward` | `int` | Phần thưởng vàng khi hoàn thành |

#### Các ID Nhiệm Vụ Được Hỗ Trợ

| ID | Loại Tracking | Sự Kiện Kích Hoạt |
|----|--------------|------------------|
| `bloom_pizzas_10/50/100/500` | Cộng dồn (`isRelative=true`) | `PizzaPlate.OnPlateCleared` |
| `earn_gold_500/1000/5000` | Tổng vàng cao nhất (`isRelative=false`) | `GameManager.OnGoldChanged` |
| `max_combo_3/5/10` | Giá trị cao nhất (`isRelative=false`) | `ComboAudioPlayer.OnComboAchieved` |
| `reach_level_5/10/25` | Giá trị cao nhất (`isRelative=false`) | `GameManager.OnLevelProgressChanged` |
| `use_booster_10/50` | Cộng dồn (`isRelative=true`) | `AchievementManager.TrackBoosterUsed()` |
| `daily_streak_3/7` | Giá trị cao nhất (`isRelative=false`) | `AchievementManager.TrackDailyStreak(int)` |

> **Lưu ý**: Khi thêm ID nhiệm vụ **mới** (ngoài bảng trên), cần thêm logic gọi `UpdateProgress("id_mới", ...)` tương ứng trong `AchievementManager.cs`.

---

## 5. Cơ Chế Hoạt Động Của 4 Vật Phẩm Hỗ Trợ (Power-ups / Boosters)

Người chơi có thể kích hoạt các vật thể hỗ trợ từ thanh công cụ cuối màn hình để giải cứu đĩa bánh kẹt hoặc tăng tốc hoàn thành:

1. **Dao Cắt (Cutter)**: Click vào đĩa bánh đang thiếu lát. Hệ thống sẽ tự động tìm một ô trống lân cận và tạo ra một đĩa bánh mới chứa đúng số lượng và loại lát pizza còn thiếu để ghép hoàn chỉnh đĩa bánh đó ngay lập tức thông qua cơ chế merge.
2. **Chai Sốt (Sauce Bottle)**: Click vào đĩa bánh đang thiếu lát. Hệ thống sẽ tự động thêm đầy các lát pizza cùng loại vào các ô trống còn lại trên đĩa bánh đó, kích hoạt nổ đĩa (bloom) và ghi điểm ngay lập tức.
3. **Thớt Gỗ (Wooden Board)**: Click chọn đĩa bánh thứ nhất, sau đó click chọn đĩa bánh thứ hai trên lưới. Hệ thống sẽ thực hiện hoán đổi vị trí của 2 đĩa bánh cho nhau và tự động quét kiểm tra các liên kết ghép bánh mới tại cả 2 vị trí vừa hoán đổi.
4. **Thùng Rác (Trash Can)**: Click vào một đĩa bánh bất kỳ trên lưới để loại bỏ đĩa bánh đó khỏi bàn chơi (hủy đối tượng và giải phóng ô lưới), tạo không gian trống để xếp các đĩa bánh mới.

---

## 6. Hệ Thống Phần Thưởng Hàng Ngày (Daily Reward)

- Chu kỳ 7 ngày liên tiếp, mỗi ngày nhận một phần thưởng khác nhau (Vàng, Booster đơn, Gói toàn bộ Boosters, Skin đặc biệt).
- Thời gian được kiểm tra qua **WorldTimeAPI** để lấy UTC thực — không thể hack bằng cách chỉnh đồng hồ thiết bị.
- Cơ chế đối chiếu `lastSessionTimeUTC` vs `lastClaimedTimeUTC`: nếu phát hiện đồng hồ bị tua ngược, hiển thị cảnh báo `SECURITY WARNING`.
- `DailyRewardUI.cs` cập nhật đồng hồ đếm ngược real-time (chỉ rebuild text khi giây thay đổi — tiết kiệm CPU).

---

## 7. Cấu Trúc Thư Mục Scripts

```
Assets/Scripts/
├── Core/
│   ├── GameManager.cs          # FSM trung tâm, quản lý Gold & Level
│   └── MergeManager.cs         # Logic ghép bánh 4 hướng
├── Gameplay/
│   ├── PizzaPlate.cs           # Entity đĩa bánh, phát OnPlateCleared
│   ├── PizzaSlice.cs           # Entity lát bánh, animation MoveToSlot
│   ├── PlateSpawner.cs         # Tạo đĩa ngẫu nhiên với Object Pool
│   ├── DraggablePlate.cs       # Input drag & snap
│   ├── BoosterManager.cs       # Logic 4 Power-ups
│   ├── HoldSlotsManager.cs     # Quản lý 3 slot giữ đĩa
│   ├── HoldSlot.cs             # Slot đơn lẻ
│   ├── SelectionManager.cs     # Chọn đối tượng (dùng cho Booster)
│   └── PlateSkin.cs            # Áp skin/màu sắc lên đĩa bánh
├── Grid/
│   ├── GridManager.cs          # Khởi tạo lưới & cache neighbors
│   └── Cell.cs                 # Ô lưới, giữ tham chiếu currentPlate
├── Meta/
│   ├── UserDataManager.cs      # Load/Save JSON, quản lý toàn bộ persistence
│   ├── AchievementManager.cs   # Event-driven achievement tracker (JSON config)
│   ├── ShopManager.cs          # Mua skin & booster
│   └── DailyRewardManager.cs   # 7-day streak + anti-cheat time check
├── UI/
│   ├── UIManager.cs            # Điều phối toàn bộ UI panels
│   ├── AchievementUI.cs        # Hiển thị danh sách nhiệm vụ
│   ├── ShopUI.cs               # Giao diện cửa hàng
│   ├── DailyRewardUI.cs        # Giao diện điểm danh
│   ├── DailyRewardItemUI.cs    # Slot ngày trong UI điểm danh
│   └── ScorePopup.cs           # Popup +100 khi nổ bánh
├── Utils/
│   ├── ObjectPooler.cs         # Generic pool cho Particle & Slice
│   └── AutoDeactivate.cs       # Tự tắt GameObject sau N giây
├── Visuals/
│   └── GhostPlatePreview.cs    # Ghost Material + Tile highlight
└── Editor/
    └── UISetupWindow.cs        # Menu tool Setup UI & Optimize Build
```
