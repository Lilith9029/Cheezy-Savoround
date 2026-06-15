# Cheezy Savoround

**Cheezy Savoround** là một tựa game giải đố thuộc thể loại Sorting/Merge với chủ đề về những lát bánh pizza. Người chơi sẽ vào vai một đầu bếp, sắp xếp và ghép các lát bánh pizza còn thiếu trên một bàn lưới để tạo thành những đĩa bánh hoàn chỉnh.

---

## 1. Sơ Đồ Hoạt Động Hệ Thống (System Flow Diagram)

Dưới đây là sơ đồ luồng hoạt động chính của core gameplay, mô tả sự tương tác giữa Khối Đầu Vào (Input), Xử Lý Logic (Logic Core), Hệ Thống Truyền Tin (Event System) và Giao Diện Đầu Ra (Visual/Audio Output):

### Bản Xem Trước Sơ Đồ (Sử dụng đường dẫn tương đối cho Git / Relative Path):
![System Flow Diagram](docs/Cheezy%20Savoround.drawio.png)

### Bản Xem Trước Cục Bộ (Sử dụng đường dẫn tuyệt đối cho IDE / Absolute Path):
![System Flow Diagram Local](file:///d:/Unity/Project/Cheezy%20Savoround/docs/Cheezy%20Savoround.drawio.png)

---

## 2. Kiến Trúc Hệ Thống (System Architecture)

Dự án được thiết kế theo mô hình hướng sự kiện (Event-driven) độc lập, giảm thiểu sự phụ thuộc trực tiếp (Tight Coupling) giữa các Class thông qua Observer Pattern và các mô hình State:

- **Khối Đầu Vào (Input)**:
  - `DraggablePlate.cs`: Thực hiện Raycast phát hiện điểm chạm trên bàn chơi 3D. Hỗ trợ Snap tự động khi di chuyển chậu/đĩa pizza gần ô lưới.
  - `GhostPlatePreview.cs`: Hiển thị mô hình đĩa bán trong suốt (Ghost Material) và nâng cao gạch lát nền (Tile) để làm nổi bật ô lưới dự kiến thả.
- **Khối Logic (Logic Core)**:
  - `GameManager.cs`: Quản lý Finite State Machine (FSM) kiểm soát các trạng thái: `Menu`, `Playing`, `CheckingCombo`, `Animating`, `GameOver`.
  - `MergeManager.cs`: Thực hiện quét 4 hướng lân cận khi đặt đĩa bánh xuống, tính toán ưu tiên gom/trừ và nổ hoa (Bloom/Explode) bằng luật ưu tiên.
- **Hệ Thống Phụ Trợ & Hỗ Trợ (Meta & Boosters)**:
  - `UserDataManager.cs`: Đọc và ghi dữ liệu người chơi (Vàng, Skin đã mua, tiến độ nhiệm vụ, số lượng vật phẩm hỗ trợ) ra tệp JSON (`userdata.json`) tại thư mục lưu trữ cục bộ.
  - `BoosterManager.cs`: Quản lý logic kích hoạt và trừ lượt sử dụng của 4 loại Power-ups/Vật phẩm hỗ trợ (Dao Cắt, Chai Sốt, Thớt Gỗ, Thùng Rác) khi người chơi chọn công cụ và click vào ô lưới tương ứng trên bàn chơi 3D.
  - `ShopManager.cs` & `ShopUI.cs`: Quản lý cửa hàng đổi màu sắc/skin đĩa bánh pizza và mua sắm thêm vật phẩm hỗ trợ.
  - `DailyRewardManager.cs` & `DailyRewardUI.cs`: Điểm danh nhận vàng mỗi 24 giờ. Bảo mật kiểm tra thời gian thực qua **WorldTimeAPI (UTC)** và cơ chế đối chiếu lịch sử phiên chơi (last session) nhằm chống hack đổi giờ trên thiết bị.
  - `AchievementManager.cs` & `AchievementUI.cs`: Hệ thống nhiệm vụ trọn đời hoạt động hoàn toàn hướng sự kiện (lắng nghe các sự kiện nổ hoa, nhận vàng từ gameplay để cập nhật tiến độ).

---

## 3. Tối Ưu Hóa Hiệu Năng (Performance Optimization)

Game được tối ưu hóa sâu để chạy mượt mà trên các thiết bị di động cấu hình yếu:

### A. Zero GC Alloc trong Gameplay Loops
1. **Lưu đệm Ô Lân Cận (Cached Neighbors)**: Các ô lân cận trong `GridManager` được tính toán trước 1 lần duy nhất tại `Start` và lưu trữ trong `Cell.neighbors`. Hàm `GetNeighbors` chỉ trả về tham chiếu danh sách có sẵn thay vì cấp phát mới `new List<Cell>()` và mảng hướng `int[,] dirs` mỗi khung hình.
2. **Quét Logic Không Cấp Phát (Non-Alloc Unique Types)**: Thay thế việc sử dụng `HashSet<string>` trong quá trình phân tích đĩa bánh bằng phương thức `GetUniqueTypesNonAlloc` sử dụng mảng đệm tĩnh (`_uniqueTypesBuffer`) giúp loại bỏ hoàn toàn rác thải bộ nhớ khi kiểm tra merge.
3. **Tối Ưu Hóa Vòng Lặp Merge**: Loại bỏ hoàn toàn việc khởi tạo đối tượng `MoveResult` cho các ứng viên (candidates) trong quá trình quét logic. Chỉ một đối tượng `MoveResult` duy nhất được tạo ra khi xác định được nước đi tốt nhất.
4. **Lưu Đệm Hiệu Ứng Particle (Cached Particle Systems)**: Trong `ObjectPooler.cs`, danh sách các `ParticleSystem` của các Prefab được lưu trữ trước trong component `PooledObject`. Tránh gọi hàm `GetComponentsInChildren<ParticleSystem>()` vốn gây phân mảnh bộ nhớ trên heap khi combo nổ liên tục.

### B. Tối Ưu Hóa Draw Calls & Render
1. **Sprite Atlas**: Gom toàn bộ tài nguyên UI phẳng (nút bấm, khung viền, icon) vào một tệp `CheezyUI.spriteatlas` duy nhất để Unity tự động gộp Draw Calls.
2. **GPU Instancing**: Kích hoạt trên tất cả các Material mô hình 3D trong game (đĩa bánh, lát pizza) giúp render hàng trăm đối tượng cùng loại chỉ bằng 1 Draw Call duy nhất.
3. **Static Batching**: Thiết lập cờ `BatchingStatic` cho các vật thể môi trường cố định (Bàn ăn, Sàn nhà, Khung cảnh phòng chờ, Ô lưới Grid) để Unity tự động dựng lô tĩnh.
4. **Cách Ly Canvas (Canvas Isolation)**: Tách riêng Canvas tĩnh và Canvas động (điểm số, thanh trượt cấp độ). Thay đổi điểm số sẽ chỉ khiến sub-canvas tương ứng dựng lại (rebuild) thay vì toàn bộ giao diện game.

---

## 4. Hướng Dẫn Cấu HÌnh Dữ Liệu JSON

### Cấu Hình Lưới Màn Chơi (Grid Config)
Tệp `GridConfig.json` nằm trong thư mục `Assets/Resources/` để cấu hình động kích thước lưới bàn chơi:
```json
{
  "width": 3,
  "height": 3,
  "spacing": 2.0
}
```

### Cấu Cấu Hình Cửa Hàng Skins (Shop Config)
Tệp `ShopConfig.json` nằm trong thư mục `Assets/Resources/` cấu hình danh sách đĩa skin có thể mua:
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

---

## 5. Cơ Chế Hoạt Động Của 4 Vật Phẩm Hỗ Trợ (Power-ups / Boosters)

Người chơi có thể kích hoạt các vật thể hỗ trợ từ thanh công cụ cuối màn hình để giải cứu đĩa bánh kẹt hoặc tăng tốc hoàn thành:
1. **Dao Cắt (Cutter)**: Click vào đĩa bánh đang thiếu lát. Hệ thống sẽ tự động tìm một ô trống lân cận và tạo ra một đĩa bánh mới chứa đúng số lượng và loại lát pizza còn thiếu để ghép hoàn chỉnh đĩa bánh đó ngay lập tức thông qua cơ chế merge.
2. **Chai Sốt (Sauce Bottle)**: Click vào đĩa bánh đang thiếu lát. Hệ thống sẽ tự động thêm đầy các lát pizza cùng loại vào các ô trống còn lại trên đĩa bánh đó, kích hoạt nổ đĩa (bloom) và ghi điểm ngay lập tức.
3. **Thớt Gỗ (Wooden Board)**: Click chọn đĩa bánh thứ nhất, sau đó click chọn đĩa bánh thứ hai trên lưới. Hệ thống sẽ thực hiện hoán đổi vị trí của 2 đĩa bánh cho nhau và tự động quét kiểm tra các liên kết ghép bánh mới tại cả 2 vị trí vừa hoán đổi.
4. **Thùng Rác (Trash Can)**: Click vào một đĩa bánh bất kỳ trên lưới để loại bỏ đĩa bánh đó khỏi bàn chơi (hủy đối tượng và giải phóng ô lưới), tạo không gian trống để xếp các đĩa bánh mới.

---

## 6. Hướng Dẫn Vận Hành & Build Trong Unity Editor

Dự án cung cấp bộ công cụ tự động hóa thiết lập nhanh trên thanh Menu Unity Editor:

1. **Thiết lập giao diện tự động (Setup UI)**:
   - Click menu **Cheezy Savoround -> Setup UI** trên thanh công cụ Editor.
   - Script sẽ tự tạo phân cấp Canvas chuẩn, nạp tài nguyên hình ảnh UI, cấu hình các popups (Shop, Daily, Achievements), liên kết toàn bộ tham chiếu tới `UIManager` và thiết lập các trình quản lý core gameplay tự động.
2. **Tối ưu hóa tài nguyên build (Optimize Build Assets)**:
   - Click menu **Cheezy Savoround -> Optimize Build Assets** trên thanh công cụ Editor.
   - Script sẽ tự động:
     - Tạo tệp `CheezyUI.spriteatlas` và thêm thư mục `Assets/UI` để đóng gói Draw Calls.
     - Bật tính năng GPU Instancing trên toàn bộ file Material (.mat) trong thư mục Assets.
     - Đánh dấu cờ Static Batching cho các đối tượng môi trường cố định trong scene hiện tại.
