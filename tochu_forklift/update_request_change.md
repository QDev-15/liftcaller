# Thống kê thay đổi so với yêu cầu ban đầu của khách hàng

> So sánh `plan.md` (kế hoạch web hiện tại) với 2 nguồn gốc: (A) yêu cầu gốc của khách hàng (`Prompt.txt`, đề xuất WPF bản 1/2) và (B) tài liệu kiến trúc khuyến nghị (`Lift Call System - Architecture and Estimate`). Xem `CLAUDE.md` để đối chiếu chi tiết từng mục.

## A. Thay đổi so với yêu cầu GỐC của khách hàng

| # | Khách hàng yêu cầu ban đầu | Quyết định hiện tại (`plan.md`) | Vì sao đổi |
|---|---|---|---|
| 1 | **WPF (C#)** cho cả 2 app desktop trên tablet | **Web app** (Blazor WebAssembly + Edge kiosk) | Khách hàng đã tự chốt chọn phương án web do đơn vị tư vấn đề xuất (không cần cài/update tablet) |
| 2 | Không nêu rõ runtime .NET cụ thể | **.NET 10 LTS** | .NET 8 (đề xuất gốc) hết hỗ trợ 11/2026; theo đúng tài liệu kiến trúc khuyến nghị |
| 3 | Caller/Lift không có màn hình đăng nhập — chỉ vào thẳng theo máy | **Thêm màn hình đăng nhập Tên + PIN** (Admin tạo trước, tablet nhớ qua localStorage) | Theo yêu cầu bổ sung của khách hàng: cần quản lý/đặt tên + PIN cho từng Caller/Receiver qua trang Admin |
| 4 | Không đề cập trang quản trị (Admin) | **Thêm hẳn 1 app Admin riêng** (Blazor Server) để quản lý Terminal, xem lịch sử, Dashboard sống | Nhu cầu vận hành thực tế: cần nơi tạo/sửa tài khoản Caller/Receiver, theo dõi trạng thái |
| 5 | DB dự kiến dùng chung/mở rộng trên hạ tầng SQL Server sẵn có (DB `PLC` của `alumiLiftCall`) | **Tạo DB hoàn toàn mới** `imip_liftcall`, không đụng DB `PLC` cũ | Khách hàng yêu cầu tách biệt, tránh ảnh hưởng hệ thống demo cũ |
| 6 | Trouble có ưu tiên hàng đợi hay không — **chưa chốt** (câu hỏi mở) | **Chốt cố định: Sự cố luôn ưu tiên đầu hàng đợi**, không làm cấu hình bật/tắt | Khách hàng xác nhận trực tiếp, không cần linh hoạt qua `Setting` |
| 7 | 1 terminal Receiver duy nhất, chưa có cơ chế chống nhiều người cùng nhận 1 cuộc gọi | **Thêm cột `RespondedBy`** + cơ chế khoá tranh chấp (ai bấm trước thắng), ẩn hoàn toàn cuộc gọi khỏi Receiver khác | Chuẩn bị cho tương lai 2–3 Receiver; khách hàng yêu cầu rõ hành vi tránh "bấm Hoàn thành nhầm" |
| 8 | Không đề cập PLC trong giai đoạn này (giữ nguyên) | **Loại bỏ hoàn toàn khỏi phạm vi** — không thiết kế chỗ mở rộng, không ghi chú | Khách hàng xác nhận bỏ qua, không cần note lại trong plan |

## B. Thay đổi/chốt thêm so với tài liệu kiến trúc KHUYẾN NGHỊ (Detailed Architecture and Estimate)

| # | Tài liệu khuyến nghị đề xuất | Quyết định hiện tại (`plan.md`) | Vì sao đổi |
|---|---|---|---|
| 1 | Nhận diện terminal qua **URL + token** (`/caller/3?t=…`), không cần màn hình login | **Màn hình Login Tên + PIN**, lưu localStorage, có autocomplete | Yêu cầu bổ sung từ khách hàng (mục A.3) — ưu tiên theo khách hàng, bỏ cơ chế token |
| 2 | Admin dùng **Blazor WebAssembly** đồng nhất với WebApp | **Admin đổi sang Blazor Server** | Khách hàng chọn: không cần đồng nhất, muốn Admin đơn giản/nhẹ hơn vì chạy trên PC văn phòng mạng ổn định |
| 3 | Data model 5 bảng: `Terminal, Call, LiftState, Setting, AuditEvent` (thiết kế mới hoàn toàn) | Giữ đúng **3 bảng gốc của `alumiLiftCall`** (`LiftState/LiftCall/LiftCallHistory`) + bảng `Terminal` tự thiết kế lại (đơn giản hơn, có PIN) | Ưu tiên "port đúng hành vi đã chạy thật" của `alumiLiftCall` hơn là thiết kế lại từ đầu theo tài liệu khuyến nghị |
| 4 | `Setting` (cấu hình Trouble-priority, ngưỡng timeout…) | **Không làm** — hành vi cố định trong code, không qua bảng DB | Đơn giản hoá, khách hàng không yêu cầu cấu hình động |
| 5 | `AuditEvent` (log chi tiết từng hành động) | **Không làm** ở v1 — chỉ dùng Serilog ghi file | Ngoài phạm vi yêu cầu hiện tại, để dành phase sau nếu cần |
| 6 | Tháp tín hiệu/đèn báo: nêu như hạng mục **phase 2 tuỳ chọn** (+3–5 ngày) | **Giữ nguyên tính năng đèn đã có sẵn trong `alumiLiftCall`**, nhưng chuyển cách gửi tín hiệu từ **client** (browser) sang **server** | Blazor WebAssembly chạy trong sandbox trình duyệt → gọi thẳng ra đèn bị chặn CORS/mixed-content; phải gửi qua server mới gọi được |
| 7 | Âm thanh: gợi ý chung "âm thanh + lặp lại + tháp đèn tuỳ chọn" | **2 âm riêng biệt**: `request.mp3` (Lấy sản phẩm + Lấy vật liệu trả về) và `trouble.mp3` (Sự cố, lặp mỗi 30s) | Khách hàng yêu cầu cụ thể 2 loại âm khác nhau theo mức độ khẩn cấp |
| 8 | Concurrency đa-Receiver: nêu khái niệm "conditional update" ở mức ý tưởng | **Chi tiết hoá đầy đủ**: cột `RespondedBy`, mã lỗi 409 xử lý như thành công (toast "Yêu cầu đã được tiếp nhận"), ẩn hoàn toàn thẻ cuộc gọi khỏi Receiver thua cuộc | Khách hàng đặc tả chính xác hành vi UX mong muốn khi xảy ra tranh chấp |
| 9 | Không đề cập khái niệm "Palette" (A/B/C) cho Lấy sản phẩm | **Giữ nguyên** — phát hiện trong code `alumiLiftCall` đang dùng, không có trong tài liệu gốc | Port đúng theo code thực tế đã chạy, không theo tài liệu giấy |
| 10 | Admin xác thực: không nêu cụ thể | **1 tài khoản/mật khẩu dùng chung** (tách biệt hoàn toàn khỏi hệ Tên+PIN của Terminal) | Khách hàng chọn phương án đơn giản, đủ dùng nội bộ |

## C. Những điểm GIỮ NGUYÊN theo đúng khuyến nghị (không đổi, để đối chiếu)

- SignalR làm kênh real-time chính, polling `GET /api/state` làm dự phòng/resync khi reconnect.
- SQL Server (không đổi sang DB khác loại), EF Core (migrations) để quản lý schema.
- Hosting trên IIS, bật WebSocket Protocol, App Pool AlwaysRunning.
- Giữ 2 bước **Phản hồi → Hoàn thành** (không rút gọn thành 1 bước).
- Không tích hợp PLC, không webhook/message queue ở lõi hệ thống.
