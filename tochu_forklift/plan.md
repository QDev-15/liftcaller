# Kế hoạch triển khai — IMIP.TochuForkLift (Web)

> Trạng thái: **Draft — chờ review**. Tài liệu này chỉ là **kế hoạch**, chưa có code nào được tạo. Sau khi bạn duyệt/góp ý, sẽ bắt đầu scaffold solution theo đúng cấu trúc dưới đây.

## 0. Quyết định đã chốt (input từ bạn)

| Mục | Quyết định |
|---|---|
| Client | **Web app** (không làm WPF nữa) |
| Runtime | **.NET 10** |
| Backend | **ASP.NET Core 10** |
| Database | **DB hoàn toàn mới**, tên `imip_liftcall`, schema (cấu trúc 3 bảng gốc + bảng `Terminal`) sinh ra bởi EF Core Migrations khi deploy — **không** đụng/dùng chung DB `PLC` cũ |
| Đăng nhập Caller/Receiver | Admin tạo tài khoản (Tên + mã PIN 4 số, mặc định "0000", Admin tự đặt) cho từng Caller/Receiver; tablet nhập 1 lần, trình duyệt tự nhớ (localStorage) cho các lần sau; có autocomplete gợi ý tên khi đăng nhập |
| Đèn báo (LED) | Giữ tính năng, nhưng **chuyển việc gửi HTTP ra đèn từ client (browser) sang server (Api)** — lý do kỹ thuật ở mục 6.1 |
| Âm thanh | 2 âm riêng biệt: 1 âm "có yêu cầu mới" (Lấy sản phẩm + Lấy vật liệu trả về dùng chung), 1 âm Sự cố riêng — **Sự cố lặp lại mỗi 30s** nếu chưa được Respond |
| Thứ tự hàng đợi | **Sự cố luôn ưu tiên lên đầu** (trong nhóm Sự cố, báo trước lên trước); các loại còn lại xếp sau, trong nhóm đó đến trước lên trước — xem mục 6 |
| Tích hợp PLC | Không làm, không thiết kế chỗ mở rộng cho việc này ở giai đoạn này |
| Cấu trúc | Tách **Backend** và **Frontend** rõ ràng |
| Frontend | 2 app: **WebApp** (Blazor WebAssembly — Caller + Receiver) và **Admin** (**Blazor Server** — nhẹ hơn, không cần offline-resilience vì chạy trên PC văn phòng mạng ổn định) |
| Xác thực Admin | 1 tài khoản/mật khẩu dùng chung đơn giản (cookie auth), **tách biệt hoàn toàn** khỏi hệ Tên+PIN của Terminal |
| `alumiLiftCall` (WPF) | Giữ nguyên, **không đụng tới** — là bản demo cho khách, sẽ xoá khỏi repo sau, không liên quan tới dự án web này |
| Vị trí | `D:\Data\Projects\TochuV2\tochu_forklift\` |
| Tiền tố tên project | `IMIP.TochuForkLift.*` |

Toàn bộ các quyết định còn lại trong tài liệu này là **đề xuất của tôi dựa trên bối cảnh dự án** (tài liệu đề xuất gốc, code `alumiLiftCall` đã review, và các bug đã phát hiện). Vòng review đầu đã chốt xong ở mục 11 (10/10 câu hỏi đã có câu trả lời) — nếu đọc lại phát hiện thêm điểm cần đổi, cứ góp ý tiếp trước khi bắt đầu code.

---

## 1. Bối cảnh & mục tiêu

Hệ thống gọi lift nhà máy: 5 terminal **Caller** (máy 1–5) gọi lift để Lấy sản phẩm / Lấy vật liệu trả về / báo Sự cố; 1 terminal **Receiver** (tương lai 2–3) hiển thị hàng đợi, Phản hồi, Hoàn thành; cả hai phía thấy được trạng thái lift Đang chạy/Đang dừng.

Bản `alumiLiftCall` (WPF + polling, đã review chi tiết — xem `research_02112026.md`) là **nguồn tham khảo chức năng chính xác nhất** vì nó đã được code thật, không phải chỉ là đề xuất giấy. Kế hoạch này **port lại đúng hành vi đó sang web**, đồng thời vá các vấn đề đã phát hiện khi review (mục 9).

**Không trong phạm vi lần này**:
- Bảng `Setting` (cấu hình nghiệp vụ lưu trong DB — Trouble-priority vẫn đi qua `appsettings.json`, xem mục 6)
- Bảng `AuditEvent`
- Tích hợp PLC (không thiết kế, không ghi chú gì thêm ở đây — loại hẳn khỏi phạm vi plan này)

---

## 2. Kiến trúc tổng thể

```
                          FACTORY LAN
┌──────────────────┐                      ┌──────────────────┐
│  Tablet Caller    │                      │  Tablet Receiver  │
│  ×5 (Edge kiosk)  │                      │  ×1→3 (Edge kiosk)│
│  WebApp: /caller  │                      │  WebApp: /receiver│
└─────────┬─────────┘                      └─────────┬─────────┘
          │  HTTPS: REST + SignalR (WebSocket)        │
          └───────────────────┬────────────────────────┘
                               ▼
                    ┌─────────────────────┐        ┌──────────────────┐
                    │   IIS (1 site)       │◀──────▶│  PC văn phòng     │
                    │                       │  HTTPS │  Admin app        │
                    │  /          → Api     │        │  (xem trạng thái, │
                    │  /app       → WebApp  │        │   lịch sử, CSV)   │
                    │  /admin     → Admin   │        └──────────────────┘
                    │  ASP.NET Core 10      │
                    │  Minimal API + SignalR│
                    └──────────┬────────────┘
                               ▼
                   SQL Server — DB mới: imip_liftcall
                   (EF Migrations tự tạo khi deploy)
                   LiftState / LiftCall / LiftCallHistory / Terminal
```

**Giao tiếp real-time**: SignalR (đẩy full snapshot mỗi khi có thay đổi) là kênh chính, **polling `GET /api/state` là cơ chế dự phòng/đồng bộ lại khi reconnect** — đúng theo khuyến nghị đã có trong tài liệu kiến trúc gốc (`CLAUDE.md` §5.4), và là điểm nâng cấp rõ nét nhất so với bản WPF (vốn chỉ polling thuần 2s). Lý do chọn đẩy full snapshot thay vì diff: state rất nhỏ (tối đa vài chục cuộc gọi đang active), gửi toàn bộ đơn giản & tự sửa lỗi hơn (không bao giờ bị "lệch state" giữa các client).

---

## 3. Cấu trúc Solution & Project

```
tochu_forklift/
├── IMIP.TochuForkLift.sln
├── src/
│   ├── IMIP.TochuForkLift.Api/              ← Backend (API + SignalR + EF Core)
│   ├── IMIP.TochuForkLift.Shared/           ← DTO, enum, HttpClient dùng chung
│   ├── IMIP.TochuForkLift.WebApp/           ← Frontend: Caller + Receiver (Blazor WASM)
│   └── IMIP.TochuForkLift.Admin/            ← Frontend: Admin (Blazor WASM)
├── tests/
│   └── IMIP.TochuForkLift.Api.Tests/        ← Integration test (xUnit, optional cho v1)
└── sql/
    ├── 01_CreateTables.sql                  ← Copy nguyên từ alumiLiftCall, không đổi schema
    └── 02_AddHistory.sql
```

**Vì sao chỉ 4 project chính (không tách Domain/Application/Infrastructure riêng)?** Hệ thống có 3 bảng, ~10 endpoint, nghiệp vụ không phức tạp. Bản `alumiLiftCall.Api` hiện tại gộp Program.cs + repository trong 1 project và hoạt động tốt. Tách thêm Clean Architecture layers (Domain/Application/Infrastructure) ở quy mô này sẽ là over-engineering — thêm ceremony mà không có lợi ích rõ ràng. Thay vào đó, **tổ chức theo folder trong nội bộ `Api` project** (mục 4.1) để vẫn giữ code sạch mà không phải tách project.

---

## 4. Chi tiết từng project

### 4.1 `IMIP.TochuForkLift.Api` (Backend)

Target: `net10.0`. ASP.NET Core 10 Minimal API, hosting in-process trên IIS.

```
IMIP.TochuForkLift.Api/
├── Program.cs                      ← đăng ký DI, map endpoint groups, map SignalR hub
├── Data/
│   ├── ForkLiftDbContext.cs        ← EF Core DbContext (4 DbSet)
│   ├── Entities/
│   │   ├── LiftState.cs
│   │   ├── LiftCall.cs
│   │   ├── LiftCallHistory.cs
│   │   └── Terminal.cs             ← MỚI: Tên, Role, UnitNo, PinHash — xem mục 5.1
│   └── Configurations/             ← Fluent API, khớp 100% với DDL hiện có
│       ├── LiftStateConfiguration.cs
│       ├── LiftCallConfiguration.cs
│       ├── LiftCallHistoryConfiguration.cs
│       └── TerminalConfiguration.cs
├── Services/
│   ├── ICallService.cs / CallService.cs        ← nghiệp vụ: tạo/hủy/phản hồi/hoàn thành cuộc gọi
│   ├── ILiftStateService.cs / LiftStateService.cs
│   ├── ITerminalAuthService.cs / TerminalAuthService.cs   ← MỚI: xác thực Tên+PIN, CRUD terminal (Admin)
│   └── ISignalLightNotifier.cs / SignalLightNotifier.cs   ← MỚI: gửi HTTP ra đèn, xem mục 6.1
├── Endpoints/
│   ├── CallEndpoints.cs            ← MapGroup("/api/calls")
│   ├── LiftStateEndpoints.cs       ← MapGroup("/api/lift-state")
│   ├── StateEndpoints.cs           ← GET /api/state (snapshot)
│   ├── HistoryEndpoints.cs         ← GET /api/history (admin, filter + CSV)
│   ├── AuthEndpoints.cs            ← MỚI: POST /api/auth/login
│   └── AdminTerminalEndpoints.cs   ← MỚI: CRUD Terminal cho Admin
├── Hubs/
│   └── StateHub.cs                 ← SignalR hub, broadcast StateChanged(snapshot)
├── appsettings.json
└── appsettings.Development.json
```

**NuGet packages chính**:
- `Microsoft.EntityFrameworkCore.SqlServer` (10.x)
- `Microsoft.AspNetCore.SignalR` (built-in trong ASP.NET Core, không cần package riêng)
- `Serilog.AspNetCore` + `Serilog.Sinks.File` (log, kế thừa ý định ban đầu dù `alumiLiftCall` chưa làm)
- `Swashbuckle.AspNetCore` (Swagger, giữ như bản cũ)
- `Microsoft.AspNetCore.Mvc.Testing` (chỉ ở project Tests)

### 4.2 `IMIP.TochuForkLift.Shared`

Target: `net10.0`. Không phụ thuộc ASP.NET Core — chỉ DTO + enum + 1 HTTP client helper, để cả 2 app Blazor (WebApp, Admin) dùng chung, tương tự vai trò của `LiftCall.Shared` trong bản WPF.

```
IMIP.TochuForkLift.Shared/
├── Contracts/
│   ├── CallType.cs / PaletteType.cs       ← y hệt enum hiện có (giữ nguyên giá trị số 1/2/3, 0/1/2/3)
│   ├── CallDto.cs
│   ├── LiftStateDto.cs
│   ├── StateSnapshotDto.cs                ← gộp LiftState + danh sách Call (dùng cho GET /api/state và SignalR)
│   ├── TerminalRole.cs                    ← MỚI: enum Caller = 1, Receiver = 2
│   ├── TerminalDto.cs                     ← MỚI: Id, Name, Role, UnitNo, IsActive (không có PinHash — không trả PIN ra client)
│   └── Requests/ (SetLiftStateRequest, PutCallRequest, SetRespondingRequest { IsResponding, RespondedBy },
│                   CompleteCallRequest, LoginRequest, CreateTerminalRequest, UpdateTerminalRequest)
└── Client/
    ├── ForkLiftApiClient.cs               ← wrapper HttpClient + HubConnection (SignalR client)
    └── TerminalIdentity.cs                ← MỚI: model lưu trong localStorage (TerminalId, Name, Role, UnitNo)
```

### 4.3 `IMIP.TochuForkLift.WebApp` (Caller + Receiver)

Target: `net10.0`. **Blazor WebAssembly (standalone)** — không dùng Blazor Server.

**Vì sao WASM chứ không phải Blazor Server?** Đây là điểm đã được phân tích kỹ trong tài liệu kiến trúc gốc (`CLAUDE.md` §5.4, bảng so sánh client .NET): Blazor Server giữ UI qua 1 SignalR circuit liên tục — nếu Wi-Fi nhà máy chập chờn (rủi ro đã nêu nhiều lần trong tài liệu gốc), toàn màn hình bị đóng băng "Reconnecting…". Blazor WASM chạy hẳn trong trình duyệt, tự quản lý trạng thái hiển thị, nên khi mất kết nối chỉ riêng phần dữ liệu bị ảnh hưởng (hiện overlay lỗi tự thiết kế) còn UI/nút bấm vẫn phản hồi ngay khi có mạng lại. Đánh đổi: build/deploy phức tạp hơn 1 chút (cần publish static assets), chấp nhận được.

```
IMIP.TochuForkLift.WebApp/
├── Pages/
│   ├── Login.razor             ← route "/" — màn hình nhập Tên + PIN (xem mục 6.3)
│   ├── Caller.razor            ← route "/caller" — tự lấy UnitNo từ identity đã đăng nhập
│   └── Receiver.razor          ← route "/receiver"
├── Components/
│   ├── Login/
│   │   ├── NameInput.razor          ← ô nhập tên (hoặc chọn từ danh sách gợi ý, xem mục 6.3)
│   │   └── PinPad.razor             ← bàn phím số cỡ lớn, touch-friendly, cùng phong cách call-btn trong prototype
│   ├── Caller/
│   │   ├── StatusHeader.razor       ← tên máy, trạng thái lift, trạng thái phản hồi
│   │   ├── PaletteSelector.razor    ← 3 nút Pallet A/B/C
│   │   ├── CallButtons.razor        ← Lấy sản phẩm / Lấy vật liệu trả về / Sự cố
│   │   └── CancelButton.razor
│   └── Receiver/
│       ├── LiftToggle.razor         ← Đang chạy ⇄ Đang dừng
│       ├── CallQueue.razor          ← khu vực "Hàng đợi": chỉ các cuộc gọi `IsResponding=false`, mọi Receiver thấy giống nhau
│       ├── InProgressList.razor     ← MỚI: khu vực "Đang xử lý": chỉ cuộc gọi `RespondedBy == mình` (lọc client-side, mục 6.4)
│       ├── CallCard.razor           ← dùng chung cho cả 2 khu vực, nút khác nhau (Phản hồi vs Hoàn thành)
│       └── ConnectionErrorOverlay.razor  ← dùng chung cho cả Caller & Receiver
├── Services/
│   ├── StateSyncService.cs     ← quản lý HubConnection + polling fallback + resync khi reconnect
│   ├── AuthStateService.cs     ← MỚI: đọc/ghi `TerminalIdentity` vào localStorage, redirect nếu chưa đăng nhập
│   └── AlertSoundService.cs    ← MỚI: phát âm thanh qua JS interop, xem mục 6.2
├── wwwroot/
│   ├── css/app.css             ← kế thừa đúng design token từ prototype cũ (xem mục 6.3): nền #0a0e18,
│   │                              accent #4da3ff, good #22c57f, warn #f5a623, bad #ef4444,
│   │                              font Oswald (tiêu đề) + IBM Plex Sans (nội dung) + IBM Plex Mono (số)
│   ├── js/sound.js             ← MỚI: play(name) gọi từ Blazor qua JS interop
│   └── sounds/
│       ├── request.mp3         ← MỚI: âm "có yêu cầu mới" — dùng chung cho Lấy sản phẩm + Lấy vật liệu trả về
│       └── trouble.mp3         ← MỚI: âm báo Sự cố riêng
├── Program.cs
└── appsettings.json            ← ApiBaseUrl
```

**Route & nhận diện thiết bị (đã đổi so với bản nháp trước)**: không còn `/caller/{unitNo}` tĩnh — mọi tablet (Caller lẫn Receiver) vào cùng 1 URL gốc. Lần đầu vào, `AuthStateService` không thấy `TerminalIdentity` trong localStorage → chuyển tới `Login.razor`. Sau khi xác thực Tên+PIN thành công qua `POST /api/auth/login`, lưu `{terminalId, name, role, unitNo}` vào localStorage rồi điều hướng: `role = Caller` → `/caller` (đọc `unitNo` từ identity đã lưu, không còn phụ thuộc URL/launch argument như bản WPF); `role = Receiver` → `/receiver`. Các lần mở lại sau, `AuthStateService` thấy identity hợp lệ trong localStorage → bỏ qua `Login.razor`, vào thẳng màn hình tương ứng — đúng yêu cầu "lần sau không phải nhập lại". Có 1 nút nhỏ "Đổi máy" ở góc màn hình Caller/Receiver để xóa identity và quay lại `Login.razor`, phòng trường hợp tablet bị đổi sang trạm khác.

### 4.4 `IMIP.TochuForkLift.Admin`

Target: `net10.0`. **Blazor Web App, render mode Interactive Server** (không phải WASM) — chạy hẳn trên server, trình duyệt chỉ nhận HTML qua 1 SignalR circuit nội bộ của Blazor. Đây là 1 project ASP.NET Core thật (có `Program.cs` tự host, không phải static files như `WebApp`), nên deploy/App Pool cũng khác (xem mục 8).

**Vì sao đổi từ WASM sang Server (theo yêu cầu)?** Admin chạy trên 1-2 PC văn phòng, mạng LAN ổn định, không có nhu cầu chống chịu mất kết nối Wi-Fi tablet như WebApp — nên không cần trả giá "build/publish static assets, tự quản lý state offline" của WASM. Đổi lại: code gọn hơn nhiều (không cần tách riêng tầng gọi API qua HTTP từ trình duyệt — code Admin chạy trên server nên **gọi thẳng `Api` qua HttpClient server-to-server**, không có CORS, không cần expose thêm endpoint public nào ngoài những gì Admin cần), và tận dụng được `[Authorize]`/cookie auth kiểu ASP.NET Core cổ điển rất đơn giản.

```
IMIP.TochuForkLift.Admin/
├── Components/
│   ├── Pages/
│   │   ├── Login.razor             ← đăng nhập Admin, tài khoản/mật khẩu dùng chung — TÁCH BIỆT hệ Tên+PIN của Terminal
│   │   ├── Dashboard.razor         ← [Authorize] trạng thái sống: lift đang chạy/dừng, cuộc gọi đang active
│   │   ├── Terminals.razor         ← [Authorize] danh sách + tạo/sửa Caller & Receiver (Tên, Role, UnitNo, PIN)
│   │   └── History.razor           ← [Authorize] bảng LiftCallHistory, filter theo ngày/máy/loại, nút Export CSV
│   └── Terminals/
│       ├── TerminalList.razor
│       └── TerminalFormModal.razor   ← form tạo/sửa: Tên, Role, UnitNo, ô nhập PIN 4 số (mặc định hiện sẵn "0000")
├── Services/
│   ├── AdminTerminalClient.cs  ← HttpClient server-to-server gọi riêng các endpoint Admin-only (`/api/admin/terminals`, `/api/history`)
│   └── AdminAuthService.cs     ← MỚI: kiểm tra username/password, issue cookie (`CookieAuthenticationDefaults`)
├── Program.cs                  ← AddAuthentication().AddCookie(), AddInteractiveServerComponents()
└── appsettings.json            ← ApiBaseUrl, AdminAuth:Username, AdminAuth:PasswordHash
```

**`Dashboard.razor` lấy dữ liệu "sống" qua SignalR (đã chốt, đổi so với bản nháp trước dùng polling).** Vì `Admin` chạy Blazor Server — tức là code C# chạy **trên server**, không phải trong trình duyệt — nó hoàn toàn có thể mở 1 `HubConnection` (gói `Microsoft.AspNetCore.SignalR.Client`) kết nối ra `Api`'s `/hubs/state` giống hệt `WebApp`, chỉ khác là kết nối này chạy **server-to-server** (tiến trình `Admin` ↔ tiến trình `Api`) chứ không phải browser-to-server, nên không có vấn đề CORS/mixed-content gì cả (cùng lý do đã phân tích ở mục 6.1 cho đèn báo — server gọi server luôn đơn giản hơn browser gọi ra ngoài).

Nhờ vậy, **`Admin` dùng lại nguyên `ForkLiftApiClient` trong `Shared`** (cùng class đã viết cho `WebApp`) thay vì phải viết riêng 1 `AdminApiClient` chỉ biết polling — giảm trùng lặp code, không phải duy trì 2 cách lấy state khác nhau cho 2 app. Luồng cụ thể:
1. Khi Blazor Server circuit của Admin khởi tạo (người dùng mở `Dashboard.razor`), service gọi `GET /api/state` 1 lần để có dữ liệu ban đầu, đồng thời mở `HubConnection` tới `/hubs/state`.
2. Mỗi lần `Api` broadcast `StateChanged` (vì có Caller/Receiver nào đó thao tác), `Admin` nhận được ngay, cập nhật state cục bộ rồi gọi `StateHasChanged()` để Blazor Server tự động render lại — **tự nhiên có độ trễ gần như tức thời**, tốt hơn hẳn polling 5s, mà code lại ít hơn vì tái dùng được `Shared`.
3. `HubConnection` tự có cơ chế reconnect built-in của SignalR; nếu rớt kết nối tạm thời, Dashboard hiện 1 banner nhỏ "Mất kết nối, đang thử lại…" rồi tự đồng bộ lại khi nối lại được (gọi `GET /api/state` ngay sau khi `Connected`, giống hệt cơ chế resync của `WebApp`).

Hệ quả: tính năng **WebSocket Protocol phải bật trên IIS** (đã ghi ở mục 8) giờ không chỉ phục vụ tablet Caller/Receiver mà còn phục vụ cả Admin — nếu quên bật, Dashboard vẫn chạy được (SignalR tự fallback Long Polling) nhưng mất lợi thế "gần tức thời".

**Terminal (Caller/Receiver) ≠ tài khoản Admin — 2 khái niệm xác thực tách biệt hoàn toàn.** `Terminal` (bảng mới, mục 5.1) chỉ dùng để tablet Caller/Receiver tự nhận diện mình, không có khái niệm "quyền hạn"; đăng nhập Admin dùng 1 tài khoản/mật khẩu dùng chung cấu hình trong `appsettings.json` (`AdminAuth:Username`/`AdminAuth:PasswordHash`, hash bằng BCrypt như PIN), set cookie khi đăng nhập đúng, mọi trang Admin gắn `[Authorize]`. Đơn giản, đủ dùng nội bộ, không cần bảng `AdminUser` hay ASP.NET Core Identity đầy đủ.

### 4.5 `IMIP.TochuForkLift.Api.Tests` (khuyến nghị, không bắt buộc cho v1)

`WebApplicationFactory` + SQLite in-memory hoặc `InMemoryLiftCallRepository` tương đương, test state machine (đặc biệt bug re-call ở mục 9.1) và concurrency (2 Receiver cùng bấm Respond).

---

## 5. Thiết kế dữ liệu

**Lấy đúng cấu trúc 3 bảng từ `Sql/01_CreateTables.sql`/`02_AddHistory.sql` của `alumiLiftCall`** (copy 2 file này vào `sql/` của solution mới chỉ để tham chiếu/đối chiếu — **không chạy trực tiếp 2 file này nữa**), nhưng **tạo một DB hoàn toàn mới tên `imip_liftcall`**, không dùng chung/không đụng tới DB `PLC` hiện có. EF Core entity map đúng theo cấu trúc cột/kiểu dữ liệu/constraint trong 2 file DDL đó (đã đọc & xác nhận khi review `alumiLiftCall` trước đó):

```csharp
// Data/Entities/LiftState.cs
public class LiftState
{
    public int Id { get; set; }            // PK, CHECK (Id = 1)
    public bool IsRunning { get; set; }
    public DateTime UpdatedAt { get; set; } // default SYSDATETIME()
    public string? UpdatedBy { get; set; }
}

// Data/Entities/LiftCall.cs
public class LiftCall
{
    public int UnitNo { get; set; }         // PK
    public byte CallType { get; set; }      // CHECK IN (1,2,3)
    public byte Palette { get; set; }       // CHECK 0..3, default 0
    public DateTime CalledAt { get; set; }  // default SYSDATETIME()
    public bool IsResponding { get; set; }
    public DateTime? RespondedAt { get; set; }
    public string? RespondedBy { get; set; } // MỚI (cột thêm, xem mục 5.2) — Terminal.Name của Receiver đã claim cuộc gọi, NULL khi IsResponding=false
}

// Data/Entities/LiftCallHistory.cs
public class LiftCallHistory
{
    public long HistoryId { get; set; }      // IDENTITY PK
    public int UnitNo { get; set; }
    public byte CallType { get; set; }
    public byte Palette { get; set; }
    public DateTime CalledAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public string? CompletedBy { get; set; }
    public int? DurationSeconds { get; set; }       // computed column, PERSISTED — EF: ValueGeneratedOnAddOrUpdate + HasComputedColumnSql
    public int? ResponseWaitSeconds { get; set; }   // computed column, PERSISTED
}
```

`CallType`/`Palette` map sang `enum CallType : byte { ProductPickup = 1, ReturnMaterial = 2, Trouble = 3 }` và `enum PaletteType : byte { None = 0, A = 1, B = 2, C = 3 }` trong `Shared` — JSON serialize dạng string (`"ProductPickup"`) để API dễ đọc hơn so với số, khác 1 chút so với DB (số) nhưng đúng kiểu mapping EF Core converter chuẩn, không đổi DB.

**Migration**: dùng EF Core Migrations để tạo schema trong DB mới `imip_liftcall`, **giữ đúng tên bảng/cột/kiểu dữ liệu/CHECK constraint/index** như trong `Sql/01_CreateTables.sql`/`02_AddHistory.sql` gốc (cùng tên bảng/cột, cùng CHECK constraint, cùng index `IX_LiftCall_CalledAt`, `IX_LiftCallHistory_CompletedAt`, `IX_LiftCallHistory_UnitNo`) — mục tiêu là cấu trúc **giống hệt** bản gốc, chỉ khác ở chỗ đây là DB mới tinh, không có data cũ. **`Api` tự áp dụng migration khi khởi động** (`dbContext.Database.Migrate()` trong `Program.cs`, chạy 1 lần lúc app pool start) — khi deploy lần đầu, DB `imip_liftcall` sẽ tự được tạo đầy đủ bảng mà không cần ai chạy tay file `.sql` nào (khác hẳn quy trình thủ công của `alumiLiftCall`).

### 5.1 Bảng mới: `Terminal` (đăng nhập Caller/Receiver bằng Tên + PIN)

Đây là bảng **duy nhất** thêm ngoài 3 bảng gốc của `alumiLiftCall`, phục vụ đúng 1 yêu cầu: Admin tạo trước tài khoản cho từng Caller/Receiver (Tên + PIN), tablet nhập 1 lần rồi tự nhớ.

```sql
CREATE TABLE dbo.Terminal
(
    Id         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Terminal PRIMARY KEY,
    Name       NVARCHAR(50)  NOT NULL,                 -- tên hiển thị, vd "Máy 1", "Lift A"
    Role       TINYINT       NOT NULL                  -- 1 = Caller, 2 = Receiver
                   CONSTRAINT CK_Terminal_Role CHECK (Role IN (1, 2)),
    UnitNo     INT           NULL,                      -- chỉ có giá trị khi Role = Caller, map sang LiftCall.UnitNo
    PinHash    NVARCHAR(200) NOT NULL,                  -- PIN đã hash (BCrypt), KHÔNG lưu plaintext
    IsActive   BIT           NOT NULL CONSTRAINT DF_Terminal_IsActive DEFAULT (1),
    CreatedAt  DATETIME2(3)  NOT NULL CONSTRAINT DF_Terminal_CreatedAt DEFAULT (SYSDATETIME()),
    UpdatedAt  DATETIME2(3)  NULL,
    CONSTRAINT UQ_Terminal_Name UNIQUE (Name),
    CONSTRAINT CK_Terminal_UnitNo_RequiredForCaller CHECK (Role = 2 OR UnitNo IS NOT NULL)
);
GO

-- Một UnitNo (số máy) chỉ được gán cho đúng 1 Caller đang active tại một thời điểm
CREATE UNIQUE INDEX UQ_Terminal_UnitNo_Active ON dbo.Terminal (UnitNo)
    WHERE Role = 1 AND IsActive = 1;
```

Lưu ý thiết kế:
- Bảng này **không đụng gì tới 3 bảng gốc** — `LiftCall.UnitNo` vẫn là số nguyên như cũ, `Terminal` chỉ là lớp ánh xạ "số máy này do thiết bị/người nào đang dùng", hoàn toàn tách biệt. Nếu sau này muốn bỏ hệ thống đăng nhập, xoá bảng `Terminal` không ảnh hưởng gì tới `LiftState`/`LiftCall`/`LiftCallHistory`.
- **PIN: đúng 4 chữ số, Admin tự nhập khi tạo Terminal** (không phải hệ thống tự sinh ngẫu nhiên). Form tạo mới ở Admin (`TerminalFormModal.razor`) hiện sẵn giá trị mặc định `"0000"` trong ô PIN để Admin gõ đè lên — validate phía Api: đúng 4 ký tự, toàn số (`^\d{4}$`).
- `PinHash` dùng BCrypt (`BCrypt.Net-Next` NuGet) — **dù PIN chỉ 4 số và mặc định "0000" (đơn giản theo đúng yêu cầu, phù hợp mức bảo mật nội bộ LAN của hệ thống này)**, vẫn không lưu dạng chữ thường trong DB, đúng tinh thần đã nhắc ở `research_02112026.md` về không lưu bí mật dạng plaintext — kể cả 1 bí mật "yếu" cũng nên hash, phòng trường hợp DB bị lộ thì không ai đọc được danh sách PIN hàng loạt.
- Nhiều Receiver (tương lai 2–3 cái) → mỗi cái vẫn là 1 dòng `Terminal` riêng với `Role = 2`, `UnitNo = NULL`. Hành động của Receiver ghi vào `LiftCallHistory.CompletedBy`/`LiftState.UpdatedBy` sẽ dùng `Terminal.Name` thay vì `Environment.MachineName` như bản WPF cũ — rõ ràng hơn nhiều khi xem lịch sử.

### 5.2 Thêm cột `RespondedBy` vào `LiftCall` (lần deviation thứ 2 so với 3 bảng gốc)

Khác với `Terminal` (thêm hẳn 1 bảng mới, không đụng bảng gốc nào), đây là **sửa trực tiếp 1 bảng gốc** — cần nêu rõ vì khác tính chất:

```sql
ALTER TABLE dbo.LiftCall ADD RespondedBy NVARCHAR(50) NULL;  -- Terminal.Name của Receiver đang xử lý, NULL khi chưa ai nhận
```

Lý do bắt buộc phải thêm: bản `alumiLiftCall` gốc không có cột này vì **chưa bao giờ chạy với >1 Receiver** nên không cần biết "ai đang phản hồi" — cứ `IsResponding=true` là đủ. Giờ dự án đã có bảng `Terminal` cho phép nhiều Receiver, nếu không lưu `RespondedBy` thì không thể nào lọc đúng "cuộc gọi này của tôi hay của đồng nghiệp" ở mục 6.4. Cột tương đương `LiftCallHistory.CompletedBy` đã tồn tại sẵn trong bản gốc (dùng cho lúc đã xong) — `RespondedBy` chỉ là bản tương tự cho lúc **đang** xử lý, cùng kiểu dữ liệu, cùng tinh thần thiết kế.

---

## 6. Thiết kế API & SignalR

| Method | Endpoint | Mục đích | Ghi chú so với `alumiLiftCall` |
|---|---|---|---|
| GET | `/api/state` | Snapshot đầy đủ (LiftState + danh sách LiftCall) | = `GET /api/status` cũ, đổi tên cho khớp vai trò "resync khi reconnect SignalR" |
| GET/PUT | `/api/lift-state` | Đọc/set trạng thái chạy-dừng | giữ nguyên |
| PUT | `/api/calls/{unitNo}` | Đăng ký cuộc gọi (ghi đè nếu đã có) | **fix bug 9.1**: chặn ghi đè nếu `IsResponding = true`, trả 409 Conflict thay vì âm thầm reset |
| DELETE | `/api/calls/{unitNo}` | Hủy cuộc gọi | giữ nguyên |
| PUT | `/api/calls/{unitNo}/response` | Set đang/ngừng phản hồi, ghi `RespondedBy` | **đổi**: trả 409 nếu thua cuộc đua claim — chi tiết mục 6.4 |
| POST | `/api/calls/{unitNo}/complete` | Hoàn thành — chuyển sang History (1 transaction) | giữ nguyên |
| GET | `/api/history` | Lịch sử, filter `from/to/unitNo/callType`, `?format=csv` | **mới**, phục vụ Admin |
| Hub | `/hubs/state` → `StateChanged(StateSnapshotDto)` | Đẩy trạng thái mới tới mọi client đang connect | **mới**, thay thế polling-only |
| POST | `/api/auth/login` | `{name, pin}` → `{terminalId, name, role, unitNo}` hoặc 401 | **mới**, đăng nhập Caller/Receiver |
| GET | `/api/auth/suggest` | Danh sách `{name, role}` của các Terminal `IsActive = true` (không có PIN/id nhạy cảm) | **mới**, phục vụ autocomplete ở màn Login (mục 6.3) |
| GET | `/api/admin/terminals` | Danh sách Terminal, chỉ Admin gọi (server-to-server từ `Admin`) | **mới** |
| POST | `/api/admin/terminals` | Tạo Terminal mới — **Admin tự nhập PIN** (4 số, mặc định gợi ý "0000") trong request, không phải server tự sinh | **mới** |
| PUT | `/api/admin/terminals/{id}` | Sửa Tên/UnitNo/IsActive, hoặc đổi PIN mới (Admin nhập) | **mới** |

Mỗi hành động ghi (PUT/DELETE/POST) sau khi commit DB → gọi `hub.Clients.All.SendAsync("StateChanged", snapshot)`. Client (WebApp) vẫn giữ 1 timer polling `GET /api/state` mỗi ~10–15s làm lưới an toàn (không phải 2s như bản cũ, vì giờ có SignalR làm kênh chính) + gọi lại `GET /api/state` ngay sau khi `HubConnection` chuyển sang `Connected` (để không bỏ sót thay đổi xảy ra trong lúc mất kết nối).

**Thứ tự hàng đợi (đã chốt, không còn là flag tắt/mở)**: `GET /api/state` (và snapshot gửi qua SignalR) trả `calls` theo sort 2 tầng — `ORDER BY CASE WHEN CallType = 3 THEN 0 ELSE 1 END, CalledAt ASC`. Nghĩa là: mọi cuộc gọi Sự cố (CallType=3) luôn đứng trước mọi cuộc gọi khác bất kể gọi sau; trong nhóm Sự cố, ai báo trước đứng trước; các cuộc gọi Lấy sản phẩm/Lấy vật liệu trả về xếp sau tất cả Sự cố, và trong nhóm đó cũng theo thứ tự gọi trước-đứng-trước. Logic này nằm trong `CallService`/query ở `StateEndpoints`, không cần bảng `Setting` vì đây giờ là hành vi cố định của hệ thống (không phải tuỳ chọn bật/tắt).

### 6.1 Cách gửi tín hiệu đèn — chi tiết từng bước

Khác biệt cốt lõi so với bản WPF: **trình duyệt (Caller/Receiver) không bao giờ tự gọi ra đèn** — chỉ server (`Api`) mới làm việc đó. Lý do: bản WPF là ứng dụng desktop nên gọi HTTP ra bất kỳ IP nào trong mạng đều được; Blazor WebAssembly chạy **bên trong sandbox của trình duyệt**, nếu JS/WASM tự gọi thẳng `http://192.168.55.7/...` thì trình duyệt sẽ chặn vì đó là 1 origin khác (CORS) và thiết bị đèn (phần cứng đơn giản) chắc chắn không trả header cho phép CORS. Thứ 2, http đèn chạy ở local không có SSL cũng sẽ bị trình duyệt chặn do mixed-content nếu WebApp chạy HTTPS. Chuyển sang gửi từ server giải quyết cả 2 vấn đề cùng lúc vì gọi server-to-server không bị giới hạn bởi trình duyệt.

Luồng cụ thể, lấy ví dụ Caller máy 3 bấm "Lấy sản phẩm":

1. Trình duyệt Caller gửi `PUT /api/calls/3` tới `Api` (same-origin, bình thường, không liên quan đèn).
2. `CallService` validate, mở transaction, ghi/UPSERT vào bảng `LiftCall`, commit.
3. Ngay sau commit, `CallService` gọi `ISignalLightNotifier.NotifyAsync(pattern)` với `pattern` tra theo đúng bảng đã thiết kế sẵn trong `リフト呼出システム_設計書.xlsx` (sheet 10): `00100` cho Lấy sản phẩm thành công, `01000` cho Lấy vật liệu trả về, `10000` cho Sự cố, `11100`/`22200` khi trạng thái phản hồi đổi (phát hiện qua polling nội bộ server hoặc ngay tại điểm set `IsResponding`), `00000` khi hết cuộc gọi.
4. `SignalLightNotifier` (chạy **trong tiến trình server**, trên máy IIS) gọi `HttpClient.GetAsync("http://192.168.55.7/api/control?led={pattern}")` — đây là lời gọi HTTP bình thường giữa 2 máy trong cùng mạng LAN nhà máy, không đi qua trình duyệt nên không bị CORS/mixed-content.
5. Gọi theo kiểu **fire-and-forget có timeout ngắn (2 giây) + try/catch nuốt lỗi** — y hệt triết lý bản cũ ("thất bại cũng không được chặn thao tác nghiệp vụ"): dù đèn không phản hồi, bước 2 (ghi DB) đã xong và bước 6 (SignalR) vẫn chạy bình thường.
6. Song song, `CallService` broadcast `StateChanged` qua SignalR để mọi tablet cập nhật UI — **không phụ thuộc** vào việc gửi đèn có thành công hay không.

> Điều kiện hạ tầng cần xác nhận khi lắp đặt thật: máy chủ IIS phải **có đường mạng (LAN) tới được `192.168.55.7`**. Trước đây là tablet gọi tới đèn (cùng khu vực sàn máy, chắc chắn cùng mạng); giờ là **server phòng máy/văn phòng** gọi tới đèn ở khu vực sản xuất — nếu nhà máy có chia VLAN giữa khu server và khu sàn máy thì cần mở route/port giữa 2 bên. Đây là rủi ro hạ tầng thật, nên kiểm tra trước khi lắp đặt (đã thêm vào mục 12 — Rủi ro).

`LedController.Enabled` giữ nguyên ý tưởng (mặc định tắt cho tới khi test với thiết bị thật), chuyển thành `appsettings.json` của `Api`: `"SignalLight": { "Enabled": false, "Host": "192.168.55.7", "TimeoutSeconds": 2 }`.

### 6.2 Âm thanh thông báo — 2 âm riêng biệt

| Nhóm cuộc gọi | File âm thanh | Khi nào phát |
|---|---|---|
| Lấy sản phẩm **+** Lấy vật liệu trả về | `sounds/request.mp3` | Khi Receiver nhận snapshot mới có 1 dòng `Calling` (CallType = 1 hoặc 2) chưa từng thấy trước đó |
| Sự cố | `sounds/trouble.mp3` | Khi Receiver nhận snapshot mới có 1 dòng `Calling` CallType = 3 chưa từng thấy trước đó — âm khác hẳn, mang tính khẩn cấp hơn |

Cách phát hiện "cuộc gọi mới" phía client: `StateSyncService` giữ lại danh sách `CallId`/`(UnitNo, CalledAt)` của snapshot trước đó; mỗi lần nhận snapshot mới (qua SignalR hoặc polling), diff với danh sách cũ — phần tử nào xuất hiện mới và đang ở trạng thái `Calling` thì kích hoạt âm tương ứng qua `AlertSoundService` (gọi JS interop `sound.js` phát thẻ `<audio>` ẩn, vì Web Audio API tự tạo tần số như trong prototype cũ chỉ phù hợp cho demo — âm thanh thật từ file mp3 nghe rõ và "giống còi báo" hơn trên loa tablet/loa ngoài gắn thêm). Chỉ phát ở màn hình Receiver (đúng tinh thần gốc: operator không nhìn màn hình liên tục khi lái lift); Caller không cần âm thanh.

**Lặp lại cảnh báo cho Sự cố (đã chốt)**: nếu có ≥1 cuộc gọi `CallType = Trouble` vẫn ở trạng thái `Calling` (chưa ai bấm Respond), `AlertSoundService` lặp lại `trouble.mp3` **mỗi 30 giây** cho tới khi cuộc gọi đó chuyển sang `Responding` hoặc biến mất (Complete/Cancel). Cài đặt bằng 1 timer phía client (`PeriodicTimer` trong Blazor WASM), không cần đổi gì ở server — mỗi tick 30s, `AlertSoundService` tự kiểm tra lại danh sách `Calling` hiện có trong state cục bộ của `StateSyncService` để quyết định phát lại hay không. **Chỉ áp dụng cho Sự cố** — Lấy sản phẩm/Lấy vật liệu trả về không lặp lại cảnh báo (đúng theo mục đích: Sự cố khẩn cấp hơn, đáng được nhắc dai hơn).

### 6.3 Màn hình đăng nhập (Login) — kế thừa thiết kế từ prototype

Dựa trên bản demo UX đã làm trước đó (`https://claude.ai/artifact/YTML5Ag1GbGPu7nwRvPa5r`), giữ nguyên design token đã dùng: nền tối `#0a0e18`, khối `.bezel`/`.screen` mô phỏng khung tablet, font `Oswald` cho tiêu đề viết hoa, `IBM Plex Sans` cho nội dung, `IBM Plex Mono` cho số/giờ, các màu trạng thái `--good #22c57f` / `--warn #f5a623` / `--bad #ef4444` / `--accent #4da3ff`.

Bố cục `Login.razor` (áp trong khung `.screen` như các màn hình khác, không phải popup/modal):
- Logo + tiêu đề "🚜 LIFT CALL" giống header các màn hình còn lại.
- Ô nhập **Tên**: input text lớn, **có autocomplete ngay từ v1** — gọi `GET /api/auth/suggest` lúc vào trang Login (danh sách nhẹ, chỉ `{name, role}` của Terminal đang active), hiện dạng gợi ý/datalist bên dưới ô nhập để gõ vài ký tự là chọn được, giảm gõ sai tên trên tablet.
- **Bàn phím số chạm lớn** cho PIN (layout 3 cột như lưới nút gọi `call-grid` đã có trong prototype, không dùng bàn phím hệ điều hành) — nhập đủ số chữ số thì tự động submit, không cần nút "Đăng nhập" riêng để giảm thao tác chạm.
- Trạng thái lỗi (sai Tên/PIN) hiện ngay dưới bàn phím, dùng màu `--bad` nhất quán với `.overlay`/`.stopped-banner` đã có.
- Sau khi đăng nhập đúng, có hiệu ứng chuyển nhanh sang `/caller` hoặc `/receiver`, không cần màn hình "chào mừng" rườm rà vì đây là thiết bị dùng lặp lại hằng ngày.

### 6.4 Nhiều Receiver cùng nhận 1 cuộc gọi — tranh chấp & lọc hiển thị (đã chốt)

Tình huống: Receiver A và Receiver B cùng thấy cuộc gọi của máy 3 trong hàng đợi, cả hai cùng bấm "Phản hồi" gần như đồng thời.

**Chặn tranh chấp ở tầng DB (ai bấm trước thắng):**
```sql
UPDATE dbo.LiftCall
   SET IsResponding = 1, RespondedAt = SYSDATETIME(), RespondedBy = @terminalName
 WHERE UnitNo = @unitNo AND IsResponding = 0;
-- @@ROWCOUNT = 1 → thắng cuộc đua, claim thành công
-- @@ROWCOUNT = 0 → đã bị Receiver khác claim mất (hoặc cuộc gọi không còn tồn tại) ngay trước đó
```
Vì đây là 1 câu UPDATE có điều kiện chạy trong transaction, SQL Server tự đảm bảo chỉ 1 trong 2 request đồng thời có thể đổi được dòng — không cần lock thủ công, không có khoảng hở để cả 2 cùng thắng.

**Phản hồi cho Receiver thua cuộc (đã chốt theo đúng yêu cầu — KHÔNG coi là lỗi):**
- API trả `409 Conflict` về mặt kỹ thuật (đúng bản chất: request không thực hiện được vì state đã đổi), nhưng **`WebApp` xử lý mã 409 riêng cho đúng endpoint này như một kết quả bình thường, không phải lỗi**: không hiện khung đỏ/thông báo lỗi, chỉ bật 1 toast trung tính **"Yêu cầu đã được tiếp nhận"**, rồi cập nhật lại state cục bộ ngay (không đợi SignalR) để thẻ cuộc gọi đó **biến mất khỏi màn hình của Receiver vừa thua** — đúng yêu cầu.
- Receiver thắng cuộc thì nhận `204 No Content` như bình thường, broadcast `StateChanged` cho mọi người (bao gồm cả Receiver B vừa thua, cả Caller máy 3) — nhưng vì B đã tự ẩn thẻ đó ngay khi nhận 409 rồi nên snapshot mới tới sau cũng không gây giật màn hình.

**Lọc hiển thị theo người claim (giải quyết luôn việc "nhấn Hoàn thành nhầm"):** `Api` vẫn gửi **1 snapshot chung duy nhất** cho tất cả client qua SignalR (không tạo nhiều bản snapshot riêng theo từng Receiver — giữ đúng tinh thần đơn giản đã chọn ở mục 2), nhưng **lọc ở phía client `WebApp`** khi render `Receiver.razor`:
  - Cuộc gọi `IsResponding = false` (chưa ai nhận) → hiện ở mọi Receiver, trong khu vực **"Hàng đợi"**.
  - Cuộc gọi `IsResponding = true` **và** `RespondedBy == TerminalIdentity.Name` (của chính Receiver đang xem) → hiện ở khu vực **"Đang xử lý"** (có nút Hoàn thành) — đúng cấu trúc 2 khu vực `queue-list` / `progress-list` đã có sẵn trong prototype cũ.
  - Cuộc gọi `IsResponding = true` **và** `RespondedBy` là người khác → **không hiển thị ở Receiver này** — biến mất hoàn toàn khỏi màn hình, không phải dạng xám/disabled. Receiver khác do đó **không có cách nào bấm nhầm nút Hoàn thành** cho cuộc gọi không phải của mình, vì nút đó (và cả thẻ chứa nó) chưa bao giờ được render ra.
  - Caller không cần biết `RespondedBy` là ai — chỉ quan tâm `IsResponding` để hiện "Đang phản hồi" như thiết kế cũ.

**Khi Receiver đang xử lý muốn huỷ claim (toggle quay lại "Đang chờ")**: chỉ hiện nút này trên chính màn hình Receiver đang giữ cuộc gọi đó (vì Receiver khác không thấy thẻ này ngay từ đầu, nên về mặt UI không ai khác bấm được) — API khi xử lý `isResponding: false` reset `RespondedBy = NULL`. Không cần thêm cơ chế xác thực phức tạp (vd kiểm tra token) cho hành động này vì rủi ro đã được chặn ở tầng hiển thị, phù hợp mức độ tin cậy nội bộ LAN đã áp dụng xuyên suốt plan này (giống cách Admin chỉ dùng 1 mật khẩu chung).

---

## 7. Luồng xử lý chính (ví dụ: Product Pickup)

```
Caller (WebApp)                 Api                           SignalR Hub            Receiver (WebApp)
   │ chọn Palette A                │                                │                        │
   │ bấm "Lấy sản phẩm"             │                                │                        │
   ├─ PUT /api/calls/3 ───────────▶│                                │                        │
   │   {type: ProductPickup,       │ validate → EF transaction      │                        │
   │    palette: A}                │ upsert LiftCall (UnitNo=3)     │                        │
   │                                │ → gọi đèn 00100 (fire-and-forget, mục 6.1)              │
   │                                ├─ broadcast StateChanged ──────▶│                        │
   │                                │                                ├─ push snapshot ───────▶│
   │◀─ 204 No Content ─────────────┤                                │                        │ hiện thẻ "Máy 3"
   │ (đồng thời) ◀──── push snapshot qua chính HubConnection của Caller cũng nhận được ────────┤ ở cuối hàng đợi,
   │ UI tự cập nhật "Đang gọi"      │                                │                        │ phát âm request.mp3
```

Receiver bấm Respond / Complete đi theo đúng pattern tương tự (ghi DB trước → broadcast sau), đảm bảo **tất cả client đều đồng bộ từ 1 nguồn duy nhất là DB**, SignalR chỉ là kênh thông báo "có gì mới, tự lấy lại" chứ bản thân snapshot gửi qua hub luôn được tính lại từ DB ngay trước khi gửi (không cache ở API) — tránh race condition giữa nhiều request đồng thời.

---

## 8. Hosting & triển khai IIS

1 site IIS, 3 application con dưới cùng site (phù hợp quy mô 1 server nhỏ) — lưu ý **2 loại hosting khác nhau** vì `Admin` giờ là Blazor Server (ASP.NET Core thật), không còn static file như `WebApp`:

| Path | Loại | Physical path | App Pool |
|---|---|---|---|
| `/` | ASP.NET Core (in-process) | publish output của `Api` | Riêng, AlwaysRunning |
| `/app` | Static files (Blazor WASM) | publish `wwwroot` của `WebApp` | Không cần (static) |
| `/admin` | **ASP.NET Core (in-process, Blazor Server)** | publish output của `Admin` | Riêng, AlwaysRunning |

Checklist IIS (kế thừa nguyên những gì đã đúc kết trước đó + bổ sung cho SignalR/Blazor Server):
- Cài **ASP.NET Core Hosting Bundle (.NET 10)**.
- **Bật tính năng WebSocket Protocol của IIS** — bắt buộc cho cả `StateHub` của `Api` **lẫn** circuit nội bộ của Blazor Server ở `Admin` (bản WPF cũ không cần vì chỉ polling; đây là khác biệt quan trọng cần nhớ khi setup server).
- App Pool của `/` (Api) **và** `/admin` (Admin): Start Mode = AlwaysRunning, Idle Time-out = 0, tắt recycle định kỳ — cả 2 đều là ASP.NET Core process thật, không chỉ riêng Api.
- `/app` vẫn chỉ serve static file (Blazor WASM) nên không cần App Pool kiểu ASP.NET Core — add `web.config` với MIME type cho `.dll`/`.wasm`/`.blat` theo chuẩn publish Blazor WASM.
- **DB**: không cần bước chạy tay SQL script nữa — lần đầu `Api` khởi động trên server, `dbContext.Database.Migrate()` tự tạo DB `imip_liftcall` + toàn bộ bảng. Chỉ cần đảm bảo connection string trỏ đúng server SQL và tài khoản SQL có quyền `CREATE DATABASE` lần đầu (hoặc Admin DBA tạo sẵn DB rỗng `imip_liftcall`, ứng dụng chỉ cần quyền tạo bảng bên trong).
- CORS: `WebApp` gọi `Api` cùng origin (cùng site) nên không cần bật CORS; `Admin` gọi `Api` **server-to-server** (không phải từ trình duyệt) nên CORS hoàn toàn không liên quan ở chiều này.

---

## 9. Vấn đề kế thừa từ bản WPF cần khắc phục khi viết lại

Lấy từ review code thật (`research_02112026.md`), đưa thẳng vào thiết kế lần này thay vì để lại:

1. **Re-call ghi đè cuộc gọi đang `Responding`** — `UpsertCallAsync` cũ dùng `MERGE` reset `IsResponding=0` vô điều kiện. Thiết kế mới: nếu cuộc gọi hiện tại có `IsResponding = true`, `PUT /api/calls/{unitNo}` trả **409 Conflict** thay vì âm thầm ghi đè; WebApp hiện cảnh báo "Lift đang xử lý cuộc gọi của bạn, không thể gọi lại" thay vì gọi lại im lặng.
2. **Receiver tự ép `IsRunning=false` khi khởi động** — giữ hành vi này nhưng **log rõ** sự kiện này vào Serilog (bản cũ không log gì), để ít nhất truy vết được khi nó gây hiểu nhầm ngoài hiện trường.
3. **Timeout client 5 giây quá ngắn cho Wi-Fi nhà máy chập chờn** — nâng lên 10s cho REST call, và vì có SignalR làm kênh chính, một request REST bị timeout không còn là lỗi "chặn" toàn bộ trải nghiệm như bản cũ (UI vẫn nhận update qua hub).
4. **Không log** — thêm Serilog từ đầu (ghi file + có thể mở rộng sang bảng audit sau nếu cần, nhưng **không** tạo bảng `AuditEvent` ở v1 theo đúng cam kết).

---

## 10. Kế hoạch các bước (gợi ý thứ tự làm, sau khi bạn duyệt plan này)

1. `dotnet new sln` + scaffold 4 project chính + wire project reference (`Api`→`Shared` không cần vì Shared không phụ thuộc Api; `WebApp`/`Admin`→`Shared`).
2. `IMIP.TochuForkLift.Shared`: enum + DTO (port gần như 1:1 từ `LiftCall.Shared/Contracts.cs`, cộng thêm DTO cho Terminal/Login mục 5.1/6).
3. `Api`: EF Core DbContext + 4 entity (3 bảng gốc + `Terminal`) + Fluent config trỏ tới DB mới `imip_liftcall`; chạy thử `dbContext.Database.Migrate()` trên DB rỗng, đối chiếu schema sinh ra của 3 bảng gốc với `Sql/01_CreateTables.sql` (so sánh từng dòng — `Terminal` là bảng mới nên không cần đối chiếu).
4. `Api`: endpoint REST cho Call/LiftState (port logic từ `SqlServerLiftCallRepository`/`InMemoryLiftCallRepository`, áp fix bug 9.1 + sort 2 tầng ưu tiên Sự cố ở mục 6 + conditional UPDATE/409 cho Respond ở mục 6.4), test bằng Swagger.
5. `Api`: `POST /api/auth/login`, `GET /api/auth/suggest`, CRUD `/api/admin/terminals` (nhận PIN Admin nhập, validate 4 số, băm bằng BCrypt).
6. `Api`: `ISignalLightNotifier` (mục 6.1) + SignalR hub + wiring broadcast sau mỗi lệnh ghi.
7. `WebApp`: `Login.razor` trước tiên (autocomplete tên + PIN pad, vì Caller/Receiver đều phụ thuộc vào nó để biết mình là ai), rồi Caller, rồi Receiver (gồm lọc Hàng đợi/Đang xử lý theo `RespondedBy` ở mục 6.4) + âm thanh 2 loại + lặp cảnh báo Sự cố 30s (mục 6.2).
8. `Admin`: `Login.razor` (cookie auth, `AdminAuthService`) trước, rồi `Terminals.razor` (tạo tài khoản để có cái đăng nhập thử ở bước 7), rồi Dashboard, rồi History + CSV export.
9. Test đa terminal: mở nhiều tab/nhiều tablet thật, test đăng nhập Tên+PIN (kể cả autocomplete), test thứ tự hàng đợi khi có Sự cố xen giữa các cuộc gọi khác, test mất mạng giữa chừng, **test 2 Receiver cùng bấm Phản hồi 1 cuộc gọi** (xác nhận Receiver thua thấy toast "Yêu cầu đã được tiếp nhận" và thẻ biến mất, Receiver kia không thấy thẻ của người khác trong "Đang xử lý").
10. Viết hướng dẫn deploy IIS riêng (dựa mục 8, lưu ý `Admin` giờ cũng cần App Pool ASP.NET Core riêng) + cấu hình kiosk Edge trên tablet thật + xác nhận route mạng server↔đèn báo (rủi ro mục 12) + xác nhận tài khoản SQL có quyền tạo DB `imip_liftcall` lần đầu.

---

## 11. Quyết định đã chốt (vòng 2)

Toàn bộ câu hỏi mở ở vòng review trước đã có câu trả lời — không còn điểm nào cản trở việc bắt đầu code. Tóm tắt lại đây để dễ tra cứu, chi tiết kỹ thuật nằm ở mục tương ứng:

1. **Database**: DB hoàn toàn mới tên `imip_liftcall`, **không** dùng DB `PLC` cũ, schema do EF Migrations tự sinh khi `Api` khởi động lần đầu — chi tiết mục 5, 8.
2. **Trouble priority**: **Bật cố định** (không phải config bật/tắt) — Sự cố luôn lên đầu hàng đợi, trong nhóm Sự cố ai báo trước lên trước; các loại khác xếp sau theo đúng thứ tự gọi trước-lên-trước — chi tiết mục 6.
3. **Bước "Hoàn thành"**: giữ nguyên 2 bước Phản hồi → Hoàn thành như `alumiLiftCall`.
4. **Admin**: **Blazor Server** (không phải WASM) — đơn giản hơn, gọi `Api` trực tiếp server-to-server, phù hợp vì chạy trên PC văn phòng mạng ổn định — chi tiết mục 4.4.
5. **Xác thực Admin**: 1 tài khoản/mật khẩu dùng chung, cấu hình trong `appsettings.json` (hash BCrypt), cookie auth đơn giản — **tách biệt hoàn toàn** khỏi hệ Tên+PIN của Terminal — chi tiết mục 4.4.
6. **Connection string**: qua biến môi trường **`ConnectionStrings__ImipLiftCall`** (đổi tên theo đúng tên DB mới `imip_liftcall` để nhất quán), không hardcode password trong `appsettings.json` commit lên git, đúng khuyến nghị đã ghi trong `research_02112026.md`.
7. **`alumiLiftCall` (WPF)**: giữ nguyên, không đụng tới — là bản demo cho khách, sẽ bị xoá khỏi repo sau, không liên quan/không cần đồng bộ dữ liệu với hệ thống web mới này.
8. **PIN**: đúng 4 chữ số, Admin tự nhập khi tạo Terminal (không tự sinh ngẫu nhiên), form gợi ý sẵn giá trị mặc định `"0000"` — chi tiết mục 5.1.
9. **Cảnh báo lặp lại**: `trouble.mp3` lặp mỗi 30 giây khi còn Sự cố ở trạng thái `Calling` (chưa ai Respond) — làm ngay ở v1, chỉ áp dụng cho Sự cố — chi tiết mục 6.2.
10. **Autocomplete tên đăng nhập**: làm ngay ở v1, qua `GET /api/auth/suggest` — chi tiết mục 6.3.

---

## 12. Rủi ro

| Rủi ro | Giảm thiểu |
|---|---|
| Server (IIS) không có đường mạng tới đèn báo `192.168.55.7` (khác tablet trước đây, server có thể ở VLAN/phòng máy riêng với khu sản xuất) | Kiểm tra route/firewall giữa khu server và khu sàn máy **trước khi lắp đặt** (mục 6.1); nếu bị chặn, cần xin mở port hoặc đặt server trong cùng VLAN với thiết bị đèn |
| Bật WebSocket cho SignalR trên IIS nhà máy — nếu môi trường mạng/firewall chặn WebSocket | SignalR tự fallback xuống Long Polling (built-in, không cần code thêm), vẫn hoạt động dù chậm hơn |
| Tablet dùng Edge kiosk mode chạy Blazor WASM — tải lần đầu nặng hơn 1 trang web tĩnh thông thường | Bật AOT/trim khi publish Release, cache qua Service Worker nếu cần (đánh giá sau khi đo thực tế) |
| EF Migration sinh schema lệch DDL gốc dù chỉ 1 chi tiết nhỏ (vd. collation, computed column) | Bước 3 ở mục 10 bắt buộc đối chiếu thủ công trước khi áp dụng migration lên DB thật |
| `Api` không có quyền `CREATE DATABASE` trên SQL Server thật (nhiều tổ chức khoá quyền này cho tài khoản ứng dụng) → auto-migrate lúc khởi động thất bại | Xác nhận trước với người quản trị SQL: hoặc cấp quyền tạo DB cho tài khoản kết nối, hoặc DBA tạo sẵn DB rỗng `imip_liftcall` và chỉ cấp quyền tạo bảng bên trong (migration vẫn chạy được, chỉ khác bước tạo DB) |
| PIN mặc định `"0000"` giống nhau nếu Admin không đổi khi tạo Terminal — dễ đoán | Chấp nhận được ở mức bảo mật nội bộ LAN đã thống nhất cho toàn hệ thống (không có đăng nhập ở bản gốc); nhắc Admin đổi PIN khỏi mặc định khi đưa vào dùng thật, có thể thêm cảnh báo nhẹ trên UI Admin sau này nếu cần |
| 3 project frontend/backend dùng 2 kiểu hosting khác nhau (`Api`+`Admin` = ASP.NET Core process, `WebApp` = static WASM) trong cùng 1 site IIS | Checklist deploy mục 8 tách rõ App Pool cho từng loại; vẫn ít phức tạp hơn so với đặt 3 site riêng biệt |


