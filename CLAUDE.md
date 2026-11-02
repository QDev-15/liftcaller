# CLAUDE.md — Dự án TochuV2 (Factory Lift Call System)

> File này tóm tắt chi tiết toàn bộ tài liệu hiện có trong `Documents/10.2026/`. Thư mục dự án hiện **chưa có code** (không phải git repo) — đây mới chỉ là giai đoạn tài liệu/đề xuất (proposal). Dùng file này làm bối cảnh khi bắt đầu triển khai code thực tế.

## 1. Dự án là gì

**Hệ thống gọi lift (thang nâng/xe nâng) trong nhà máy** — cho phép công nhân tại 5 máy sản xuất gọi người vận hành lift để: **lấy sản phẩm (Product Pickup)**, **lấy vật liệu trả về (Return Material Pickup)**, hoặc báo **sự cố (Trouble)**. Người vận hành lift cần thấy danh sách cuộc gọi theo thứ tự và phản hồi (Respond). Cả hai phía cần biết lift đang chạy hay dừng.

Khách hàng yêu cầu ban đầu dùng **WPF (C#)** cho 2 ứng dụng desktop trên tablet Windows. Sau khi phân tích, đơn vị tư vấn (tác giả các tài liệu `.md`) đề xuất phương án thay thế **đơn giản hơn**: **một web app** chạy từ server tại chỗ, tablet chạy **Microsoft Edge ở chế độ kiosk** — vẫn giữ nguyên stack khách hàng yêu cầu (.NET, SQL Server, IIS).

## 2. Cấu trúc thư mục tài liệu

```
Documents/10.2026/
├── EN/                                                              (bản tiếng Anh)
│   ├── Prompt.txt                                                   — yêu cầu gốc của khách hàng (raw request)
│   ├── Factory Lift Call System Proposal.docx                       — đề xuất ban đầu, bản 1 & bản 2 (dựa trên WPF)
│   ├── Lift Call System - Architecture and Estimate (Detailed).md/.pdf  — kiến trúc + ước tính chi tiết (đề xuất MỚI, khuyến nghị)
│   ├── Lift Call System - Architecture and Estimate (Summary).md/.pdf   — bản tóm tắt của tài liệu trên
│   ├── Caller.png                                                   — mockup màn hình máy gọi (caller)
│   └── Lift Side.png                                                — mockup màn hình phía lift
├── JA/                                                               (bản tiếng Nhật — nội dung tương đương EN, chỉ khác ngôn ngữ)
│   ├── リフト呼出システム アーキテクチャと見積(詳細版).md/.pdf
│   └── リフト呼出システム アーキテクチャと見積(概要版).md/.pdf
└── Request/                                                          (bản tiếng Nhật gốc, đầu vào ban đầu)
    ├── プロンプト.txt                                                — yêu cầu gốc (= Prompt.txt, tiếng Nhật)
    ├── 工場リフト呼出システム 提案書.docx                              — đề xuất ban đầu (= bản EN .docx, tiếng Nhật)
    ├── 呼出元.png                                                    — mockup màn hình caller (tiếng Nhật)
    └── 呼出先.png                                                    — mockup màn hình lift (tiếng Nhật)
```

Đã đối chiếu: cặp file EN/JA là **bản dịch song song của cùng nội dung** (số dòng và cấu trúc khớp nhau), không có thông tin lệch nhau.

## 3. Yêu cầu gốc của khách hàng (Prompt.txt / プロンプト.txt)

**Môi trường phát triển:**
- Cả máy gọi (caller) và máy nhận (lift) đều là **tablet Windows**
- Ngôn ngữ phát triển: **C# WPF**
- Database: xây nếu cần, ứng viên **SQL Server**
- REST API: xây nếu cần, môi trường **IIS**
- **Không** tích hợp PLC ở giai đoạn này
- Caller và lift cùng mạng Wi-Fi nội bộ; nếu cần IIS thì đặt cùng môi trường đó

**Tính năng cần có:**
- 5 terminal gọi: Máy 1 → Máy 5
- 1 terminal phía lift hiện tại (tương lai sẽ lên 2–3 terminal, nhưng thử nghiệm trước với 1)
- **Caller:** chỉ thao tác bằng nút bấm — 3 nút gọi (**Lấy sản phẩm / Lấy vật liệu trả về / Sự cố**) + nút **Hủy gọi**. Ngoài ra cần hiển thị: đang được phản hồi ("Responding") hay chưa, và lift đang chạy hay dừng.
- **Phía lift (lift side):** khách hàng yêu cầu đơn vị tư vấn **tự đề xuất màn hình tối ưu**, chỉ nêu yêu cầu thông tin: máy nào gọi vì lý do gì, thứ tự các cuộc gọi, cách báo cho caller biết đang được phản hồi, cách báo cho caller biết lift chạy/dừng. Toàn bộ thao tác bằng chạm (touch).

## 4. Đề xuất ban đầu — *Factory Lift Call System Proposal* (bản 1 & bản 2, dựa trên WPF)

Đây là file `.docx` (đã dịch & tóm tắt từ nội dung trích xuất), gồm 2 phiên bản gộp trong 1 file.

### 4.1 Bản 1 (lần đề xuất đầu tiên)

- **Kết luận kiến trúc:** 3 tầng — `ASP.NET Core Web API + SQL Server + 2 loại client WPF (caller / lift)`.
  - Lý do: trạng thái của 5 terminal caller + lift quản lý tập trung ở 1 nơi (DB) → thứ tự & trạng thái phản hồi không bị lệch; thêm/bớt terminal không cần đổi server; dữ liệu dùng được cho phân tích lịch sử sau này.
- **Kiến trúc hệ thống:** Caller (WPF C#) & Lift (WPF C#) → HTTP + SignalR → IIS (ASP.NET Core Web API + SignalR Hub) → SQL Server.
- **Phương thức giao tiếp:** so sánh Polling (1–2s, đơn giản nhưng là phương án tối thiểu) vs **SignalR (khuyến nghị)** — đẩy tức thời từ server; cần tự động fallback về polling khi mất kết nối vì Wi-Fi nhà máy hay bị gián đoạn chớp nhoáng.
- **Thiết kế DB (bản nháp):**
  - `CallRequest`: CallId (PK), StationNo (1–5), CallType (1: Lấy sản phẩm, 2: Lấy vật liệu trả về, 3: Sự cố), Status (0: Đang gọi, 1: Đang phản hồi, 2: Hoàn thành, 9: Đã hủy), CalledAt, AcceptedAt, ClosedAt.
  - `LiftStatus`: 1 dòng duy nhất — IsRunning, UpdatedAt.
  - `Station`: master danh sách máy — số máy, tên, thời gian liên lạc cuối (giám sát sống/chết terminal).
  - **Quy tắc nghiệp vụ:** không cho gọi trùng (cùng máy, cùng loại) để chống bấm liên tục; thứ tự gọi theo CallId tăng dần (đến trước phục vụ trước), nhưng cần xác nhận có ưu tiên cho Trouble hay không; chỉ máy gọi mới được hủy cuộc gọi của chính nó (nên có hộp thoại xác nhận khi hủy cuộc gọi đang được phản hồi).
- **API/SignalR (bản nháp):** `POST /api/calls`, `POST /api/calls/{id}/cancel`, `POST /api/calls/{id}/accept`, `POST /api/calls/{id}/complete`, `GET /api/calls/active`, `PUT /api/lift/status`. SignalR broadcast `CallsChanged`, `LiftStatusChanged`.
- **Đề xuất màn hình:**
  - *Caller:* header luôn hiển thị (số máy, trạng thái lift, giờ); 3 nút lớn (Product Pickup / Return Material Pickup / Trouble — màu đỏ); khung trạng thái hiện tại (vd "Product Pickup: Calling…" → "Lift has responded" nhấp nháy xanh); nút Cancel Call ở dưới cùng. Nút tối thiểu 80×80px (bấm được bằng tay đeo găng); nút đang gọi đổi màu + nhấp nháy; trạng thái phản hồi 3 cấp màu (Idle xám → Calling vàng → Responding xanh); khi lift dừng, header chuyển đỏ "Stopped" (vẫn cho gọi được — cần xác nhận); mất kết nối → hiện to chữ "Communication Error".
  - *Lift side:* toggle Running⇄Stopped ở đầu; danh sách dạng thẻ/hàng "thứ tự, máy, lý do, thời gian đã trôi qua" + nút Respond; phân biệt lý do bằng màu + icon + chữ (hỗ trợ người khó phân biệt màu); thời gian trôi qua hiển thị để cuộc gọi bị bỏ quên nổi bật (nhấp nháy sau 3 phút); cuộc gọi mới có âm thanh + thông báo toàn màn hình (vì người vận hành không thể nhìn màn hình liên tục khi lái lift); nhấn Respond phản ánh ngay cho caller; "Complete" đóng cuộc gọi; toggle chạy/dừng dùng slide hoặc long-press để tránh thao tác nhầm.
- **Phương pháp phát triển:** WPF theo MVVM (CommunityToolkit.Mvvm), server theo kiến trúc phân lớp; DB access bằng EF Core (hoặc Dapper nếu cần nhẹ); DTO/enum dùng chung qua class library; log bằng Serilog; triển khai qua ClickOnce/MSIX hoặc share folder + auto-update; tiến trình Agile (3–4 sprint × 2 tuần).
- **Các bước phát triển đề xuất:** review mockup giấy/XAML dummy với người hiện trường → dựng khung DB+API+SignalR và kiểm tra kết nối → làm màn hình caller trước rồi đến lift → test trên tablet thật (mất kết nối, bấm đồng thời) → go-live, màn hình báo cáo lịch sử để sau.
- **Phi chức năng/vận hành:** kiosk mode (khóa toàn màn hình, không sleep, tự khởi động); khi server down → hiện lỗi rõ ràng, cần quy tắc liên lạc bằng lời tạm thời; lưu lịch sử 1–3 năm; bảo mật LAN nội bộ + nhận diện terminal bằng số máy + API key; bắt đầu với 1 server (SQL Server + IIS chung máy), kể cả SQL Server Express cũng đủ cho quy mô 5 terminal.
- **Các mục cần xác nhận trước:** ưu tiên khi nhiều cuộc gọi đồng thời (Trouble trước hay đến trước phục vụ trước?); trạng thái chạy/dừng của lift là chuyển thủ công hay lấy tự động từ thiết bị (PLC)?; chỉ 1 terminal lift hay nhiều (nhiều terminal cần kiểm soát xung đột phản hồi); môi trường Wi-Fi nhà máy và vị trí đặt server; xử lý khi phía lift bỏ sót cuộc gọi không phản hồi (gửi lại thông báo, leo thang)?

### 4.2 Bản 2 (sau khi khách hàng xác nhận một số điểm)

Các mục đã chốt từ bản 1, ảnh hưởng đến thiết kế:

| Mục đã chốt | Ảnh hưởng thiết kế |
|---|---|
| Không tích hợp PLC | Trạng thái chạy/dừng của lift chuyển thủ công trên terminal phía lift |
| Cùng Wi-Fi; IIS đặt cùng môi trường | Tự chứa hoàn toàn trong LAN nội bộ, không public ra ngoài → đơn giản hóa kiến trúc |
| Phía lift: 1 terminal hiện tại (tương lai 2–3) | Giữ đơn giản hiện tại, nhưng thiết kế có tính đến đa terminal |

- **Kiến trúc khuyến nghị (bản 2):** `ASP.NET Core Web API + SignalR (IIS) + SQL Server Express + WPF client (1 app dùng chung)`.
  - Lý do: server là nguồn sự thật duy nhất → thứ tự gọi, trạng thái phản hồi, trạng thái chạy đồng nhất trên mọi terminal; SignalR giúp "Respond" phản ánh gần như tức thì sang caller; SQL Server Express (miễn phí) đủ cho quy mô 5+1 terminal; thêm terminal lift sau này hầu như không cần sửa server.
  - **Ghi chú kiến trúc:** IIS và SQL Server có thể chung 1 máy server; server dùng IP tĩnh; dùng **1 EXE duy nhất** cho tablet, chuyển vai trò "caller (số máy)" hay "lift side" qua cấu hình khởi động (dễ bảo trì hơn); đã cân nhắc phương án đơn giản hơn (chia sẻ DB qua thư mục mạng / kết nối SQL trực tiếp) nhưng khuyến nghị đi qua API vì tốt hơn khi Wi-Fi chớp nhoáng, bảo mật, và mở rộng tương lai.
- **Phương thức giao tiếp:** Polling (1s, đơn giản nhất nhưng trễ tới 1s + tăng tải mạng) vs **SignalR (khuyến nghị)**. Biện pháp vận hành cho Wi-Fi nhà máy: tự động reconnect + lấy lại toàn bộ trạng thái ngay sau khi kết nối lại (tránh bỏ sót cập nhật); khi mất kết nối hiện to "Communication Error"; **heartbeat 5–10s** để giám sát sống/chết terminal, phía lift hiện "Máy ○ mất kết nối".
- **Thiết kế DB (hoàn thiện hơn):** `CallRequest` thêm cột `AcceptedBy` (ID terminal đã phản hồi — phục vụ đa terminal); `LiftStatus` thêm `UpdatedBy`; `Station` là bảng master terminal.
  - **Quy tắc nghiệp vụ:** không cho gọi trùng cùng 1 máy (chống bấm liên tục/nhầm); thứ tự theo CallId tăng dần; **khi phản hồi, terminal nào bấm trước thắng** (kiểm soát độc quyền qua `UPDATE ... WHERE Status = 0` — chống xung đột khi có nhiều terminal lift); chỉ hủy được từ máy gọi, hủy khi đang phản hồi cần hộp thoại xác nhận.
- **API:** thêm `POST /api/heartbeat` (thông báo sống còn terminal), `GET/PUT /api/lift/status`.
- **Màn hình:** tương tự bản 1, bổ sung chi tiết: nút active hiện rõ "đã gọi" bằng màu+chữ, các nút khác bị vô hiệu hóa; khi mất kết nối: **làm xám toàn màn hình + hiện "Communication Error"**; nút hủy chỉ bật khi đang có cuộc gọi; phía lift — thẻ màu phân biệt theo loại (đỏ=Trouble, xanh dương=Product Pickup, cam=Return Material Pickup); toàn bộ thẻ là vùng chạm được; xử lý "Trouble" (cần xác nhận): mặc định ưu tiên hiển thị trên đầu, nhưng nếu cần giữ nguyên thứ tự đến trước thì chỉ đổi màu.
- **Phương pháp phát triển:** Server dùng **.NET 8 LTS**, hosting in-process trên IIS (đây là lựa chọn ban đầu — sau này tài liệu kiến trúc mới đã đổi sang .NET 10, xem mục 5).
- **Các mục còn cần xác nhận (bản 2):** có cần ưu tiên Trouble hay chỉ đến trước phục vụ trước?; có cần bước "Complete" sau khi phản hồi hay chỉ phản hồi là xong?; xử lý khi cuộc gọi không được phản hồi lâu (gửi lại thông báo, lặp âm thanh)?; server dùng máy có sẵn hay máy chuyên dụng?; ai chịu trách nhiệm chuyển trạng thái chạy/dừng phía lift (chính người vận hành)?; có cần báo cáo/thống kê lịch sử cuộc gọi không (có thể để sau)?

## 5. Tài liệu kiến trúc & ước tính công sức MỚI (khuyến nghị cuối cùng) — `Lift Call System - Architecture and Estimate`

> Đây là **tài liệu quan trọng nhất và mới nhất** (ngày 2026-10-28, trạng thái "Draft for review"), dựa trên đề xuất gốc (bản 1 & 2) + các mockup màn hình. Có 2 bản: **Detailed** (đầy đủ, dùng làm nguồn chính) và **Summary** (rút gọn, ước tính theo tuần thay vì ngày-người). Nội dung EN và JA tương đương nhau.

### 5.1 Tóm tắt & 3 thay đổi chính so với đề xuất gốc

Thay vì 2 app desktop WPF, tài liệu đề xuất: **1 web app** chạy từ server tại chỗ nhỏ, tablet chạy **Microsoft Edge ở chế độ kiosk Windows**. Vẫn giữ nguyên stack khách hàng chọn (**.NET, SQL Server, IIS**) và dùng license SQL Server sẵn có của khách hàng. Thay đổi 3 điều:

1. **Không cần cài/update gì trên tablet** — version mới chỉ deploy 1 lần trên server.
2. **Ít thành phần di chuyển hơn** — 1 codebase, 1 deployable, màn hình đã thiết kế sẵn dạng web page.
3. **Không bị khóa vào tablet Windows** — nếu sau này dùng tablet Android hoặc màn hình treo tường, chỉ cần mở cùng URL.

**Ước tính nhanh:** ~**59 ngày-dev** + **24 ngày-tester** (kịch bản likely). 1 dev + 1 tester bán thời gian → go-live trong ~**12 tuần** (gồm 2 tuần pilot tại xưởng). Nếu dùng AI hỗ trợ code (vd Claude Code): ~**8–9 tuần**.

### 5.2 Yêu cầu tóm tắt (recap)

| Hạng mục | Yêu cầu |
|---|---|
| Caller | 5 tablet (Máy 1–5), chỉ chạm, 3 nút gọi + 1 nút Cancel |
| Hiển thị phía caller | Đang được phản hồi hay không; lift chạy hay dừng |
| Phía lift | 1 tablet hiện tại, 2–3 trong tương lai. Hiện máy nào gọi, lý do, thứ tự gọi |
| Hành động phía lift | Phản hồi cuộc gọi; chuyển lift chạy/dừng |
| Mạng | Tất cả thiết bị cùng Wi-Fi nhà máy. Chỉ LAN nội bộ |
| Tích hợp | Không tích hợp PLC. Operator tự set trạng thái lift bằng tay |
| Stack ưu tiên | **.NET/C#** xuyên suốt, **SQL Server** (khách hàng đã có license), IIS. WPF là lựa chọn client ban đầu |

### 5.3 Kiến trúc đề xuất

```
 FACTORY WI-FI (chỉ LAN nội bộ)

 Caller tablets ×5                    Lift tablet ×1 (→2–3)
 Windows 11 + Edge kiosk              Windows 11 + Edge kiosk
 /caller/{machineNo}                  /lift
            │  HTTPS: REST commands + SignalR (WebSocket)
            └──────────────┬─────────────────┘
                            ▼
        On-site server (mini PC hoặc VM, LAN có dây)
          IIS
           └─ ASP.NET Core (.NET 10 LTS)
                ├─ Blazor WebAssembly UI (static)
                ├─ REST API (commands)
                ├─ SignalR hub (live state push)
                └─ Admin pages (terminals, history)
          SQL Server, license khách hàng (EF Core)
          Nightly backup (SQL Server Agent job)
                            │ optional, phase 2
                            ▼
              Network signal tower / buzzer gần lift (LAN-controlled)
```

**Luồng 1 cuộc gọi:**
1. Công nhân máy 3 bấm **Product Pickup** → tablet gửi `POST /api/calls` kèm request ID tự sinh (để retry không tạo trùng).
2. Server lưu cuộc gọi trong 1 transaction, đẩy **state snapshot** mới (toàn bộ cuộc gọi active + trạng thái lift) tới mọi màn hình qua SignalR.
3. Tablet lift phát âm thanh, hiện cuộc gọi ở cuối hàng đợi, lặp lại cảnh báo đến khi có người phản hồi.
4. Operator bấm **Respond** → server chỉ chấp nhận nếu cuộc gọi vẫn ở trạng thái `Calling` (nên nếu có 2–3 terminal lift thì chỉ người bấm đầu tiên thắng). Snapshot mới chuyển màn hình máy 3 sang xanh "Responding".
5. Sau khi lấy hàng xong, operator bấm **Complete** → cuộc gọi chuyển vào lịch sử.

### 5.4 Các quyết định thiết kế chính

| Quyết định | Lựa chọn | Lý do |
|---|---|---|
| Loại client | **Web app ở Edge kiosk mode** thay vì WPF | Update chỉ cần làm trên server. Windows có sẵn kiosk mode (Assigned Access), browser tự khởi động lại nếu crash. Cũng hoạt động trên thiết bị khác sau này |
| UI framework | **Blazor WebAssembly** (C#) | Team ở lại C# xuyên suốt. UI chạy trong browser nên rớt Wi-Fi không mất UI, có thể tự hiện trạng thái lỗi riêng. So sánh các lựa chọn .NET client khác ở §5.3 (của tài liệu gốc) |
| Cập nhật real-time | **SignalR (WebSockets)**, gửi **full snapshot** mỗi lần thay đổi, **polling tự động làm fallback** | State rất nhỏ (tối đa vài chục cuộc gọi), gửi toàn bộ đơn giản & đáng tin cậy hơn gửi phần thay đổi. Sau reconnect, màn hình đúng ngay khi nhận snapshot tiếp theo |
| Command | **REST với request ID** đảm bảo retry an toàn (idempotency) | Retry sau khi rớt Wi-Fi không tạo cuộc gọi thứ 2 |
| Concurrency | Conditional update (`WHERE Status = 'Calling'`) + row version | Nhiều terminal lift không thể cùng phản hồi 1 cuộc gọi |
| Database | **SQL Server** dưới license sẵn có của khách hàng, qua EF Core | Không tốn thêm license, IT khách hàng đã biết vận hành & backup. Có thể dùng instance có sẵn trên mạng hoặc cài trên app server. Khối lượng dự kiến chỉ vài trăm dòng/ngày |
| Hosting | **IIS**, ASP.NET Core in-process | Theo đúng yêu cầu khách hàng. Cần bật WebSockets & cấu hình app pool cho kết nối dài hạn (xem §6) |
| Runtime | **.NET 10 LTS** | .NET 8 (đề xuất gốc dùng) hết hỗ trợ **tháng 11/2026**. .NET 10 được hỗ trợ đến tháng 11/2028 |
| Terminal identity | Mỗi tablet có URL khởi động kiosk riêng kèm **terminal token**, vd `/caller/3?t=…` | Không cần màn hình login tại xưởng nhưng mọi request vẫn truy vết được về 1 tablet cụ thể. Token có thể thu hồi trên trang admin |
| Phát hiện offline | **Presence lấy từ chính kết nối SignalR**, + client heartbeat | Màn hình lift hiện "Machine 4 offline" mà không cần hệ thống giám sát riêng |

### 5.5 Tech stack

| Lớp | Công nghệ |
|---|---|
| Tablet OS/shell | Windows 11 Pro, Assigned Access chạy **Microsoft Edge kiosk mode** |
| Front end | Blazor WebAssembly (.NET 10), CSS dựa trên mockup có sẵn, resource file tiếng Nhật + Anh |
| Real-time | ASP.NET Core SignalR (WebSockets, fallback tự động) |
| Back end | ASP.NET Core 10 Web API + SignalR hub, host trên IIS |
| Data | SQL Server (license khách hàng, 2019+), EF Core 10 với migrations |
| Logging | Serilog ghi rolling file + bảng audit trong DB |
| Health | ASP.NET Core health checks (`/health`) |
| Tests | xUnit, `WebApplicationFactory` integration tests, bUnit cho UI component, simulator terminal giả cho soak test |
| CI/CD | GitHub Actions hoặc Azure DevOps: build, test, publish zip. Deploy là copy script vào IIS (server không cần internet) |
| Backup | SQL Server Agent job backup full hằng đêm lên network share, hoặc quy trình backup sẵn có của khách hàng |

### 5.6 Thiết kế chức năng

**Vòng đời cuộc gọi (Call lifecycle):**
```
            Respond               Complete
 Calling ───────────► Responding ───────────► Completed
    │                     │
    │ Cancel (caller)     │ Cancel (caller, kèm xác nhận)
    ▼                     ▼
 Cancelled            Cancelled
```

**Màn hình:**

| Màn hình | Nội dung |
|---|---|
| **Caller** (`/caller/{n}`) | Số máy, trạng thái lift (Running/Stopped), trạng thái phản hồi (Idle, Calling, Responding), 3 nút gọi lớn, nút Cancel Call (chỉ bật khi đang có cuộc gọi), overlay toàn màn hình "Connection error" khi offline |
| **Lift** (`/lift`) | Toggle Running/Stopped (long-press + xác nhận), hàng đợi cuộc gọi (số thứ tự, máy, lý do với màu/icon/chữ, thời gian trôi qua, nút Respond), khu vực "In progress" với nút Complete, dải trạng thái online/offline terminal. Âm thanh cảnh báo khi có cuộc gọi mới, lặp lại mỗi N giây đến khi phản hồi, thẻ nhấp nháy sau 3 phút |
| **Admin** (`/admin`, chỉ PC văn phòng) | Danh sách terminal (online/offline, lần thấy cuối, reset token), lịch sử cuộc gọi có filter + export CSV, cài đặt (chu kỳ cảnh báo lại, ngưỡng quá hạn, bật/tắt ưu tiên Trouble) |

**Data model (tối thiểu):**

| Bảng | Cột chính |
|---|---|
| `Terminal` | Id, Name, Role (Caller/Lift), MachineNo, TokenHash, LastSeenAt, IsActive |
| `Call` | Id, MachineNo, CallType, Status, CreatedAt, RespondedAt, RespondedByTerminalId, CompletedAt, CancelledAt, RequestId (unique), RowVersion |
| `LiftState` | 1 dòng duy nhất: IsRunning, UpdatedAt, UpdatedByTerminalId |
| `Setting` | Key, Value |
| `AuditEvent` | Id, At, TerminalId, Action, CallId, Details |

**API surface:**

| Method | Endpoint | Mục đích |
|---|---|---|
| `POST` | `/api/calls` | Tạo cuộc gọi (kèm request ID để retry an toàn) |
| `POST` | `/api/calls/{id}/respond` | Phản hồi (phía lift) |
| `POST` | `/api/calls/{id}/complete` | Hoàn thành (phía lift) |
| `POST` | `/api/calls/{id}/cancel` | Hủy (chỉ máy gọi) |
| `PUT` | `/api/lift/state` | Set chạy/dừng |
| `GET` | `/api/state` | Snapshot đầy đủ, dùng khi khởi động/reconnect |
| `GET` | `/api/history` | Lịch sử có filter + export CSV (admin) |
| Hub | `/hubs/state` → `StateChanged(snapshot)` | Đẩy trạng thái live tới mọi màn hình |

**Mặc định đề xuất cho các câu hỏi mở** (cần khách hàng xác nhận, hầu hết cấu hình được qua settings):

| Câu hỏi mở | Mặc định đề xuất |
|---|---|
| Trouble có nhảy cóc hàng đợi không? | **Có**, hiện đầu danh sách màu đỏ. Có setting tắt được |
| Có cần bước "Complete" không? | **Có**, vì cho dữ liệu thời gian phản hồi/xử lý. Cuộc gọi còn mở quá timeout cấu hình sẽ tự complete |
| Mỗi máy được bao nhiêu cuộc gọi active? | **1 cuộc/loại gọi.** 1 máy có thể vừa yêu cầu product pickup vừa báo trouble cùng lúc |
| Cuộc gọi không ai trả lời | Lặp âm thanh mỗi 30s, thẻ nhấp nháy sau 3 phút, ghi log sự kiện. Leo thang (tháp đèn/supervisor) là tùy chọn phase 2 |
| Gọi khi lift đang dừng | **Cho phép.** Caller thấy header đỏ "Lift stopped" để biết có thể chờ lâu |
| Ai chuyển chạy/dừng? | Operator phía lift, trên tablet lift |
| Báo cáo | Export CSV lịch sử có sẵn. Dashboard KPI là tùy chọn phase 2 |

### 5.7 Các phương án đã cân nhắc (Alternatives Considered)

**Thay đổi so với đề xuất gốc:**

| Chủ đề | Đề xuất gốc | Đề xuất này | Lý do |
|---|---|---|---|
| Client | 2 app WPF (hoặc 1 app với switch khởi động) | 1 web app ở Edge kiosk mode | Không cần cài/update client; có sẵn kiosk & tự phục hồi khi crash |
| Runtime | .NET 8 LTS | .NET 10 LTS | .NET 8 hết hỗ trợ tháng 11/2026 |
| Database | Gợi ý SQL Server Express | SQL Server dưới license sẵn có của khách hàng | Đã trả tiền sẵn. Có SQL Server Agent để backup & maintain |
| Live update | SignalR events + fallback polling | SignalR full snapshot; `GET /api/state` khi reconnect | Đơn giản hơn & tự sửa lỗi |
| Chống trùng | Chưa chỉ rõ | Request ID trên mọi command | Retry Wi-Fi không tạo cuộc gọi trùng |
| Admin | Chưa đề cập | Trang admin cơ bản (terminal, lịch sử, export CSV, settings) | Cần để vận hành hằng ngày |
| Cảnh báo | Âm thanh + màn hình | Âm thanh lặp lại + tháp đèn tùy chọn | Operator lái lift không thể canh tablet |

**Nếu bắt buộc phải dùng WPF**, kiến trúc server vẫn giữ nguyên, chỉ client thay đổi — effort thêm ở §5.11 (Variants).

**So sánh kênh cập nhật real-time (§5.2 gốc):**

| Phương án | Phù hợp? | Chi phí dev | Bảo trì | Kết luận |
|---|---|---|---|---|
| **SignalR** | Push tới browser, tự reconnect, client C# cho Blazor, trạng thái online/offline từ chính kết nối | Baseline | Thấp | **Khuyến nghị** |
| **Polling** (`GET /api/state` mỗi 1–2s) | 8 client × 1 req/s không đáng kể | −1 đến −2 ngày | Thấp nhất | **Giữ làm fallback tự động** |
| **Server-Sent Events** | Push 1 chiều qua HTTP thường, hỗ trợ sẵn trong .NET 10 | ±0 đến +1 ngày | Thấp | Khả thi nhưng không có lợi thế rõ so với SignalR cho team C# |
| **Raw WebSockets** | Phải tự viết reconnect/heartbeat | +3 đến +4 ngày | Trung bình | Không đáng — SignalR đã là wrapper tốt |
| **Webhooks** | Browser/tablet không nhận được HTTP gọi vào | n/a | n/a | **Không áp dụng** cho cập nhật màn hình. Có thể dùng sau cho cảnh báo ra ngoài (Teams, email) |
| **Message queue** (RabbitMQ/Azure Service Bus/Kafka) | Dành cho nhiều service backend & khối lượng cao | +6 đến +10 ngày | Cao | **Thừa thãi** cho 1 server + 8 client |
| **MQTT** (Mosquitto + MQTTnet) | Nhẹ, phổ biến trong IIoT, phù hợp nếu thêm nút bấm vật lý/tháp đèn/PLC sau này | +5 đến +8 ngày | Trung bình | **Phương án tương lai** nếu mở rộng phần cứng |

Phần khó nhất của hệ thống là **business rule** (thứ tự gọi, chỉ 1 Respond/cuộc gọi, không trùng cuộc gọi) — được thực thi bằng **database transaction** trên server, nên queue/broker không loại bỏ được công việc này mà chỉ thêm hạ tầng.

**So sánh client .NET (§5.3 gốc):**

| Phương án | Chi phí dev | Bảo trì | Khi nào hợp lý |
|---|---|---|---|
| **Blazor WebAssembly** (khuyến nghị) | Baseline | Thấp | Mặc định. UI vẫn chạy khi rớt Wi-Fi, tự hiện trạng thái lỗi |
| **Blazor Server** | −5 đến −8 ngày (không cần tầng API riêng, live update có sẵn) | Thấp | Chỉ nên nếu khảo sát Wi-Fi cho thấy phủ sóng ổn định — UI phụ thuộc kết nối liên tục, rớt mạng sẽ đóng băng màn hình "Reconnecting…" |
| **WPF** (như đề xuất gốc) | +8 đến +14 ngày | Cao hơn (cần installer + auto-update mỗi tablet) | Nếu bắt buộc app native, vd cần phần cứng local hoặc khớp các app WPF có sẵn |

**Kết luận (§5.4 gốc):** 1) Blazor WebAssembly trên ASP.NET Core + SQL Server có license + host trên IIS. 2) Giữ SignalR, polling làm fallback tự động. 3) Không dùng webhook/message queue trong core — webhook có thể là add-on nhỏ phase 2 cho Teams/email. 4) Giữ MQTT trong đầu nếu sau này thêm nút vật lý/tháp đèn/PLC. 5) Blazor Server có thể tiết kiệm ~1 tuần dev nhưng chỉ nếu Wi-Fi khảo sát tốt.

### 5.8 Triển khai & Vận hành

**Phần cứng:**

| Hạng mục | SL | Ghi chú |
|---|---|---|
| Server | 1 | Mini PC công nghiệp hoặc VM trên server sẵn có. Windows Server/Windows 11 Pro, **LAN có dây**, IP tĩnh hoặc DNS, có **UPS**. DB có thể dùng instance SQL Server sẵn có trên mạng thay vì cài local |
| Tablet caller | 5 (+1 dự phòng) | Windows 11 Pro (Home không có Assigned Access). Màn hình chạm thân thiện với găng tay. Gắn tường/cánh tay, nguồn điện cố định |
| Tablet lift | 1 (+1 dự phòng) | Như trên, thêm **loa ngoài có nguồn** vì loa tablet quá nhỏ trong xưởng |
| Wi-Fi | — | **Khảo sát site** tại mọi vị trí tablet + dọc tuyến di chuyển lift. 1 SSID riêng cho các thiết bị này |
| Tháp tín hiệu (tùy chọn) | 1 | Đèn + còi điều khiển qua LAN gần lift (phase 2) |

**Checklist cấu hình server:** bật tính năng IIS **WebSocket Protocol** (nếu không, SignalR fallback về transport chậm hơn); app pool: Start Mode **AlwaysRunning**, Idle Time-out **0**, tắt recycle định kỳ hoặc dời sang đêm; bật preload; SQL Server backup full hằng đêm qua SQL Server Agent job (hoặc quy trình sẵn có), lưu network share giữ 14 ngày, lịch sử cuộc gọi giữ 1–3 năm (có job purge); dùng HTTPS cert từ CA nội bộ nếu có (HTTP thường vẫn chấp nhận được trên LAN cô lập nhưng khuyến nghị HTTPS).

**Checklist cấu hình tablet (kiosk):** **Assigned Access** single-app kiosk chạy Edge, start URL riêng mỗi tablet, Edge tự mở lại trang nếu đóng/crash; policy Edge: cho phép **autoplay** cho URL app (để âm thanh cảnh báo phát không cần chạm trước), tắt popup dịch & first-run experience; cài đặt nguồn: màn hình không bao giờ tắt, thiết bị không sleep, tắt power saving Wi-Fi adapter; dùng **giới hạn sạc pin** của hãng (vd 80%) để tránh phồng pin khi cắm điện liên tục; Windows Update active hours đặt ngoài giờ sản xuất; có **hướng dẫn provisioning** để setup tablet dự phòng dưới 30 phút.

**Hành vi khi lỗi:**

| Lỗi | Người dùng thấy gì | Khôi phục |
|---|---|---|
| Tablet mất Wi-Fi | "Connection error" toàn màn hình, nút bị vô hiệu hóa để không ai tưởng nhầm đã gửi cuộc gọi | Tự reconnect & resync |
| Server/IIS restart | Mọi màn hình hiện "Connection error" vài giây | Tự reconnect, cuộc gọi active được khôi phục từ DB |
| Server down lâu | "Connection error" mọi màn hình | Dùng fallback đã thống nhất (điện thoại/bộ đàm). Runbook khôi phục + spare server/VM image |
| Browser crash | — | Kiosk mode tự khởi động lại Edge |

### 5.9 Kế hoạch triển khai (Delivery Plan)

Agile, sprint 2 tuần, review tại xưởng cuối mỗi sprint.

| Tuần | Hoạt động |
|---|---|
| 1–2 | Workshop yêu cầu, chốt các câu hỏi mở, review mockup clickable tại chỗ, thiết kế kỹ thuật |
| 3–4 | Sprint 1: setup project, CI, data model, state machine cuộc gọi, REST API, SignalR snapshot, màn hình caller |
| 5–6 | Sprint 2: màn hình lift (hàng đợi, timer, cảnh báo âm thanh, Respond/Complete, toggle chạy/dừng), phát hiện offline |
| 7 | Sprint 3 (ngắn): trang admin, lịch sử + export CSV, settings, text UI tiếng Nhật/Anh |
| 8 | Script setup server + kiosk, test hệ thống chu kỳ 1 |
| 9 | Test tại chỗ trên tablet/Wi-Fi thật, sửa lỗi, test regression chu kỳ 2 |
| 10–11 | UAT + **pilot tại xưởng** (dùng thật có fallback), điều chỉnh |
| 12 | Go-live, bàn giao hướng dẫn vận hành & admin |

Tester bắt đầu ~50% từ tuần 3 (lập kế hoạch test), gần full-time tuần 8–11.

### 5.10 Ước tính công sức (Effort Estimate) — bản Detailed

Đơn vị: **ngày-người (person-days)**. Khoảng Low/Likely/High. Giả định dev mid-to-senior C# có kinh nghiệm ASP.NET Core.

**Developer:**

| Nhóm việc | Low | Likely | High |
|---|---:|---:|---:|
| Discovery & design (workshop, mockup, thiết kế kỹ thuật) | 5 | 7 | 11 |
| Back end (setup/CI, state machine, SignalR, token, log/health/audit) | 8.5 | 11.5 | 17 |
| Front end (shell, caller, lift, admin) | 12 | 16 | 21 |
| Deployment (server + kiosk setup) | 3 | 4 | 6 |
| Quality & handover (test, soak simulator, bug fix, on-site support, UAT/pilot, docs) | 14 | 20 | 29 |
| **Tổng Developer** | **42.5** | **58.5** | **84** |

**Tester:**

| Nhóm việc | Low | Likely | High |
|---|---:|---:|---:|
| Toàn bộ (test plan, functional, resilience, multi-terminal, on-site, regression, soak, UAT) | 16 | 23.5 | 31.5 |

**Tổng:**

| Vai trò | Low | Likely | High |
|---|---:|---:|---:|
| Developer | 42.5 | 58.5 | 84 |
| Tester | 16 | 23.5 | 31.5 |
| **Tổng** | **58.5** | **82** | **115.5** |

- **Thời gian lịch:** ~**12 tuần** với 1 dev + 1 tester (bán thời gian đầu, gần full sau). Với 2 dev: ~**8–9 tuần**.
- **Chưa bao gồm:** quản lý dự án & điều phối khách hàng (cộng thêm 10–15%), mua/lắp phần cứng, khảo sát Wi-Fi, đi lại, license Windows. License SQL Server do khách hàng cung cấp.

**Phương án mở rộng (variants/add-on):**

| Hạng mục | Developer | Tester | Ghi chú |
|---|---:|---:|---|
| **Dùng client WPF thay vì web** (server giữ nguyên) | +8–14 ngày | +2–4 ngày | 2 UI XAML, đóng gói MSIX/ClickOnce + auto-update, kiosk Shell Launcher, test cài đặt/nâng cấp |
| Tháp tín hiệu/còi gần lift | 3–5 ngày | 1–2 ngày | Đèn + âm thanh khi có cuộc gọi mới |
| Dashboard KPI (thời gian phản hồi; số cuộc gọi theo máy/loại/giờ) | 5–8 ngày | 2–3 ngày | Dùng dữ liệu lịch sử sẵn có |
| Tích hợp PLC cho trạng thái chạy/dừng tự động | 5–10 ngày | 2–4 ngày | Tùy model/protocol PLC. Cần spike kỹ thuật trước |
| Leo thang supervisor (email/Teams) cho Trouble không ai trả lời | 2–3 ngày | 1 ngày | Cần mail server hoặc internet |

**Ước tính nếu phát triển có AI hỗ trợ (§5.5 "AI-assisted delivery"):** giả định dev dùng AI coding assistant (vd Claude Code) cho phần lớn code/test/script/docs và vẫn tự review toàn bộ. Việc liên quan con người/phần cứng/xưởng gần như không đổi.

| Vai trò | Standard (likely) | AI-assisted (likely) | AI-assisted range |
|---|---:|---:|---:|
| Developer | 58.5 | 33 | 26–45 |
| Tester | 23.5 | 18 | 14–24 |
| **Tổng** | **82** | **51** | **40–69** |

- **Thời gian lịch:** ~**8–9 tuần** thay vì 12. Sau đó giới hạn là quyết định khách hàng, review tại chỗ, pilot 2 tuần — không phải coding.
- **Chất lượng:** test tự động kỹ (kể cả E2E browser suite) rẻ hơn nhiều với AI → regression coverage cao hơn bản standard.
- **Điều kiện:** dev phải có kinh nghiệm .NET thật và review toàn bộ code sinh ra. Dev kém kinh nghiệm hơn → tiết kiệm giảm, rủi ro lỗi tinh vi (concurrency, security) tăng.
- **Độ tin cậy:** số liệu AI-assisted mang tính phán đoán. Nên đo tiến độ thực tế sau sprint đầu và re-plan nếu cần. Giữ contingency vì Wi-Fi nhà máy & phần cứng thật là nơi dự án dạng này hay trễ.

*(Bản Summary dùng đơn vị tuần: Developer ~12 person-week standard / ~7 AI-assisted (5–9); Tester ~5 / ~4 (3–5); tổng lịch 12 tuần → 8–9 tuần với AI.)*

### 5.11 Rủi ro & Giảm thiểu

| Rủi ro | Tác động | Giảm thiểu |
|---|---|---|
| Wi-Fi yếu ở 1 số vị trí máy hoặc dọc tuyến lift | Bỏ sót/trễ cuộc gọi | Khảo sát site trước go-live. Overlay lỗi kết nối để thấy rõ. Đi bộ kiểm tra phủ sóng khi test tại chỗ |
| Operator không nhìn/nghe được tablet khi đang lái lift | Cuộc gọi chờ quá lâu | Loa ngoài, cảnh báo lặp lại, tháp tín hiệu tùy chọn |
| Browser chặn âm thanh cảnh báo | Cảnh báo im lặng | Policy autoplay của Edge cho URL app, kiểm tra khi setup kiosk |
| IIS recycle app hoặc rớt WebSocket | Gián đoạn ngắn | Cấu hình app pool ở §5.8. Client tự reconnect & resync |
| Server đơn lẻ bị lỗi | Hệ thống ngừng | UPS, backup hằng đêm, runbook khôi phục, VM image dự phòng, fallback điện thoại/bộ đàm đã thống nhất |
| Tablet cắm điện liên tục: phồng pin, sleep, Windows update | Thiết bị downtime | Giới hạn sạc, policy nguồn, update active hours, tablet dự phòng |
| Business rule mở chưa chốt kịp | Phải làm lại | Dùng mặc định ở §5.6, có thể cấu hình. Chốt quyết định ở tuần 1–2 |
| Yêu cầu bắt buộc giữ WPF | Thêm effort | Thiết kế server không đổi. Ước tính ở §5.10 |

### 5.12 Giả định (Assumptions)

- Khách hàng cung cấp tablet, server/VM, mạng Wi-Fi, quyền truy cập mạng, và đầu mối liên hệ tại xưởng để review & UAT.
- 5 terminal caller + tối đa 3 terminal lift, tại 1 site, cho 1 lift.
- UI tiếng Nhật, có sẵn text tiếng Anh. Tài liệu bằng tiếng Anh (chưa bao gồm dịch tiếng Nhật).
- Không tích hợp hệ thống ngoài (ERP, MES, PLC) trong phạm vi cơ bản.
- Team dev có thể đến site để test tại chỗ (tuần 9–11).

## 6. Mockup màn hình (từ hình ảnh)

Có 4 ảnh mockup (2 bản EN + 2 bản JA, cùng thiết kế, chỉ khác ngôn ngữ): `Caller.png`/`呼出元.png` và `Lift Side.png`/`呼出先.png`.

**Màn hình Caller** ("Lift Call" / "リフト呼出"):
- Header: icon xe nâng + tiêu đề "Lift Call", tên máy ("Machine 1" / "1号機") màu xanh dương.
- 2 ô trạng thái cạnh nhau: **Lift Status** (vd "● Running" màu xanh lá) và **Response Status** (vd "● Responding" màu xanh dương).
- 3 nút lớn cùng hàng: **Product Pickup** (xanh lá, icon hộp), **Return Material Pickup** (cam, icon mũi tên xoay), **Trouble** (đỏ, icon cảnh báo tam giác).
- Nút **Cancel Call** full-width phía dưới (xám, icon X).
- Theme màu tối (dark navy background).

**Màn hình Lift Side** ("Lift Call" / "リフト呼出"):
- Header giống caller, bên dưới là thanh trạng thái lift full-width màu xanh lá "▶ Running" ("運転中").
- Danh sách cuộc gọi dạng card, mỗi card viền màu theo loại:
  1. Viền xanh dương: "Machine 1 — Product Pickup", giờ gọi, nút trạng thái "Responding" (đã có người nhận).
  2. Viền vàng: "Machine 3 — Return Material Pickup", giờ gọi.
  3. Viền đỏ: "Machine 2 — Trouble", giờ gọi.
- Mỗi card có số thứ tự trong ô vuông xám đậm bên trái, icon loại cuộc gọi, tên máy, loại cuộc gọi (màu theo loại: xanh lá=Product Pickup, vàng=Return Material Pickup, đỏ=Trouble), timestamp bên phải.

Thiết kế này khớp với mô tả trong tài liệu kiến trúc (§5.6) — layout 3 nút lớn cho caller, danh sách card có màu/icon/giờ cho lift side.

## 7. Trạng thái hiện tại & việc tiếp theo

- **Hiện tại:** chỉ có tài liệu đề xuất (docx gốc + 2 bản kiến trúc .md/.pdf song ngữ Anh/Nhật) + mockup hình ảnh. **Chưa có code, chưa có git repo.**
- **Tài liệu "Detailed" (`EN/Lift Call System - Architecture and Estimate (Detailed).md`) là nguồn tham khảo chính** khi bắt đầu code — chứa đầy đủ data model, API surface, quyết định kiến trúc, và các giá trị mặc định cho câu hỏi mở.
- **Các quyết định còn chờ khách hàng xác nhận** trước khi code (xem bảng ở §5.6 "Mặc định đề xuất") và các câu hỏi mở còn lại trong đề xuất gốc (bản 2, mục 8): có cần ưu tiên Trouble, có cần bước Complete, xử lý cuộc gọi không phản hồi, server dùng máy nào, ai chuyển trạng thái lift, có cần báo cáo lịch sử.
- **Quyết định kiến trúc chủ chốt cần khách hàng chốt:** Web app (Blazor WebAssembly + Edge kiosk, khuyến nghị) **hay** giữ nguyên WPF client như yêu cầu gốc — ảnh hưởng lớn đến effort (+8–14 ngày-dev nếu WPF) và cách triển khai/update sau này.
- Khi bắt đầu implement, nên dùng đúng stack đã chốt trong tài liệu Detailed: **.NET 10 LTS, ASP.NET Core, Blazor WebAssembly, SignalR, EF Core 10, SQL Server, IIS hosting**.
