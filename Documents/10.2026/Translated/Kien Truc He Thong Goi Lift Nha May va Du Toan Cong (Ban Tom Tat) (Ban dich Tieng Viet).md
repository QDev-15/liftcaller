# Hệ thống Gọi Lift Nhà máy: Kiến trúc Đề xuất & Dự toán Công việc

| | |
|---|---|
| **Tài liệu** | Đề xuất kiến trúc và dự toán sơ bộ (bản tóm tắt) |
| **Dựa trên** | *Đề xuất Hệ thống Gọi Lift Nhà máy* (bản 1 & 2) và các mockup màn hình |
| **Trạng thái** | Bản thảo để xem xét |
| **Ngày** | 2026-10-28 |

---

## 1. Tóm tắt

Công nhân tại năm máy sản xuất cần một cách đơn giản để gọi người điều khiển lift cho **Lấy sản phẩm**, **Lấy vật liệu trả về** hoặc **Sự cố**. Người điều khiển cần thấy các lượt gọi theo thứ tự và phản hồi. Cả hai bên cần biết được lift đang chạy hay không.

Đề xuất ban đầu dùng **hai ứng dụng desktop WPF gốc (native)** trên máy tính bảng. Tài liệu này đề xuất một phương án đơn giản hơn: **một ứng dụng web** được phục vụ từ một server nhỏ tại hiện trường, với các máy tính bảng chạy **Microsoft Edge ở chế độ kiosk trên Windows**. Phương án này vẫn hoàn toàn nằm trong hệ công nghệ mà khách hàng đã chọn (**.NET, SQL Server và IIS**) và dùng license SQL Server hiện có của khách hàng. Nó thay đổi ba điều:

- **Không cần cài đặt hay cập nhật gì trên máy tính bảng.** Phiên bản mới được triển khai một lần, trên server.
- **Ít thành phần di động hơn.** Chỉ có một codebase và một bản deploy duy nhất, và các màn hình đã được thiết kế sẵn dưới dạng trang web.
- **Không bị ràng buộc vào máy tính bảng Windows.** Nếu sau này dùng máy tính bảng Android hoặc màn hình treo tường, chỉ cần mở cùng URL.

**Dự toán sơ bộ:** một developer cùng một tester bán thời gian sẽ hoàn thành go-live trong khoảng **12 tuần**, bao gồm 2 tuần pilot tại nhà máy. Với phát triển có hỗ trợ AI, khoảng **8–9 tuần**.

---

## 2. Tóm lược Yêu cầu

| Hạng mục | Yêu cầu |
|---|---|
| Bên gọi | 5 máy tính bảng (Máy 1–5), chỉ cảm ứng, có 3 nút gọi và 1 nút Hủy |
| Hiển thị bên gọi | Lượt gọi đang được phản hồi hay không, và lift đang chạy hay dừng |
| Bên lift | 1 máy tính bảng hiện tại, 2–3 máy sau này. Hiển thị máy nào gọi, lý do và thứ tự các lượt gọi |
| Hành động bên lift | Phản hồi một lượt gọi, và chuyển trạng thái lift giữa chạy và dừng |
| Mạng | Tất cả thiết bị trên cùng Wi-Fi nhà máy. Chỉ LAN nội bộ |
| Tích hợp | Không tích hợp PLC. Người điều khiển tự tay thiết lập trạng thái lift |
| Hệ công nghệ ưu tiên | **.NET / C#** xuyên suốt, **SQL Server** (khách hàng đã có license), IIS. WPF là lựa chọn client ban đầu |

---

## 3. Kiến trúc Đề xuất

### 3.1 Tổng quan

```
 WI-FI NHÀ MÁY (chỉ LAN nội bộ)

 ┌──────────────────────────┐            ┌──────────────────────────┐
 │ Máy tính bảng bên gọi ×5 │            │ Máy tính bảng lift ×1    │
 │ Windows 11 + Edge kiosk  │            │ (→2–3) Windows 11 +      │
 │ /caller/{machineNo}      │            │ Edge kiosk  /lift        │
 └────────────┬─────────────┘            └─────────────┬────────────┘
              │  HTTPS: lệnh REST + SignalR (WebSocket)
              └──────────────────┬─────────────────────┘
                                 ▼
          ┌──────────────────────────────────────────────┐
          │ Server tại hiện trường (mini PC hoặc VM,      │
          │ LAN có dây)                                   │
          │                                              │
          │  IIS                                         │
          │   └─ ASP.NET Core (.NET 10 LTS)              │
          │        ├─ Blazor WebAssembly UI (tĩnh)       │
          │        ├─ REST API (lệnh)                    │
          │        ├─ SignalR hub (đẩy trạng thái realtime)│
          │        └─ Trang Admin (thiết bị, lịch sử)    │
          │                                              │
          │  SQL Server, license của khách hàng (EF Core)│
          │  Backup hàng đêm (SQL Server Agent job)      │
          └──────────────────────────────────────────────┘
                                 │ tùy chọn, giai đoạn 2
                                 ▼
                   Đèn tháp báo hiệu / chuông qua mạng
                   gần lift (điều khiển qua LAN)
```

### 3.2 Một lượt gọi diễn ra như thế nào

1. Một công nhân tại Máy 3 chạm **Lấy sản phẩm**. Máy tính bảng gửi `POST /api/calls` kèm một request ID do nó tự sinh ra, để một lần gửi lại (retry) không bao giờ tạo ra lượt gọi trùng.
2. Server lưu lượt gọi trong một transaction duy nhất và đẩy **ảnh chụp trạng thái (state snapshot)** mới — gồm tất cả lượt gọi đang hoạt động và trạng thái lift — tới mọi màn hình đang kết nối qua SignalR.
3. Máy tính bảng bên lift phát âm thanh, hiển thị lượt gọi ở cuối hàng đợi, và lặp lại cảnh báo cho đến khi có người phản hồi.
4. Người điều khiển chạm **Phản hồi**. Server chỉ chấp nhận nếu lượt gọi vẫn ở trạng thái `Calling`, nên với 2–3 terminal lift, chỉ lượt chạm đầu tiên thắng. Snapshot mới sẽ chuyển màn hình của Máy 3 sang màu xanh với trạng thái "Đang phản hồi".
5. Sau khi lấy hàng xong, người điều khiển chạm **Hoàn thành**, và lượt gọi chuyển vào lịch sử.

### 3.3 Các quyết định thiết kế chính

| Quyết định | Lựa chọn | Vì sao |
|---|---|---|
| Loại client | **Ứng dụng web ở chế độ Edge kiosk** thay cho WPF | Việc cập nhật chỉ diễn ra trên server. Windows có sẵn chế độ kiosk (Assigned Access), và browser tự khởi động lại nếu bị crash. Nó cũng hoạt động được trên các thiết bị khác sau này |
| UI framework | **Blazor WebAssembly** (C#) | Nhóm phát triển vẫn dùng C# từ đầu đến cuối. UI chạy trong browser, nên mất Wi-Fi không làm mất giao diện và nó có thể tự hiển thị trạng thái lỗi của riêng mình. Các lựa chọn client .NET khác được so sánh ở mục 5.3 |
| Cập nhật thời gian thực | **SignalR (WebSockets)**, gửi **toàn bộ snapshot** mỗi khi có thay đổi, với **polling là phương án dự phòng tự động** | Trạng thái rất nhỏ (nhiều nhất vài chục lượt gọi), nên gửi toàn bộ đơn giản và đáng tin cậy hơn là chỉ gửi phần thay đổi. Sau khi kết nối lại, màn hình sẽ đúng ngay khi snapshot kế tiếp đến. Các phương án khác (webhook, message queue, MQTT) được so sánh ở mục 5.2 |
| Lệnh (commands) | **REST với request ID** giúp việc gửi lại an toàn (idempotency) | Một lần gửi lại sau khi Wi-Fi chập chờn không bao giờ tạo ra lượt gọi thứ hai. REST cũng dễ test và dễ log |
| Xử lý tranh chấp (concurrency) | Cập nhật có điều kiện (`WHERE Status = 'Calling'`) cộng với row version | Với nhiều terminal lift, hai người không thể cùng phản hồi một lượt gọi |
| Cơ sở dữ liệu | **SQL Server** dùng license hiện có của khách hàng, truy cập qua EF Core | Không tốn thêm chi phí license, và team IT của khách hàng đã biết cách vận hành và backup. Có thể là một instance hiện có trên mạng hoặc cài trên app server, tùy theo điều khoản license. SQL Server Agent xử lý backup và maintenance. Lượng dữ liệu dự kiến chỉ vài trăm dòng mỗi ngày |
| Hosting | **IIS**, ASP.NET Core chạy in-process | Đây là điều khách hàng yêu cầu. Cần bật WebSocket và cấu hình app pool cho kết nối tồn tại lâu (xem mục 6) |
| Runtime | **.NET 10 LTS** | .NET 8 — mà đề xuất ban đầu dùng — hết hỗ trợ vào **tháng 11/2026**. .NET 10 được hỗ trợ đến tháng 11/2028 |
| Định danh terminal | Mỗi máy tính bảng có URL khởi động kiosk riêng kèm **terminal token**, ví dụ `/caller/3?t=…` | Không có màn hình đăng nhập tại nhà máy, nhưng mọi request đều có thể truy ngược về đúng máy tính bảng. Token có thể bị revoke trên trang admin |
| Phát hiện offline | **Sự hiện diện (presence) lấy từ chính kết nối SignalR**, cộng với heartbeat từ client | Màn hình lift hiển thị "Máy 4 offline" mà không cần một hệ thống giám sát riêng |

### 3.4 Công nghệ sử dụng

| Lớp (Layer) | Công nghệ |
|---|---|
| OS / shell máy tính bảng | Windows 11 Pro, Assigned Access chạy **Microsoft Edge kiosk mode** |
| Front end | Blazor WebAssembly (.NET 10), CSS dựa trên mockup hiện có, resource file tiếng Nhật và tiếng Anh |
| Realtime | ASP.NET Core SignalR (WebSockets, có fallback tự động) |
| Back end | ASP.NET Core 10 Web API và SignalR hub, host trên IIS |
| Dữ liệu | SQL Server (license của khách hàng, bản 2019 hoặc mới hơn), EF Core 10 kèm migrations |
| Logging | Serilog ghi rolling file cộng với bảng audit trong database |
| Health | ASP.NET Core health checks (`/health`) |
| Test | xUnit, integration test bằng `WebApplicationFactory`, bUnit cho UI component, và một bộ simulator nhỏ giả lập terminal để soak test |
| CI/CD | GitHub Actions hoặc Azure DevOps: build, test, publish file zip. Deploy là một bản copy có script lên IIS (server không cần truy cập internet) |
| Backup | SQL Server Agent job backup full hàng đêm lên network share, hoặc theo quy trình backup SQL hiện có của khách hàng |

---

## 4. Thiết kế Chức năng

### 4.1 Vòng đời một lượt gọi

```
            Phản hồi              Hoàn thành
 Calling ───────────► Responding ───────────► Completed
    │                     │
    │ Hủy (bên gọi)       │ Hủy (bên gọi, có xác nhận)
    ▼                     ▼
 Cancelled            Cancelled
```

### 4.2 Các màn hình

| Màn hình | Nội dung |
|---|---|
| **Bên gọi** (`/caller/{n}`) | Số máy, trạng thái lift (Đang chạy/Đã dừng), trạng thái phản hồi (Idle, Calling, Responding), 3 nút gọi lớn, nút Hủy lượt gọi (chỉ bật khi có lượt gọi đang hoạt động), và overlay toàn màn hình "Lỗi kết nối" khi offline |
| **Bên lift** (`/lift`) | Toggle Đang chạy/Đã dừng (nhấn giữ kèm xác nhận), hàng đợi lượt gọi (số thứ tự, máy, lý do kèm màu/icon/text, thời gian đã trôi qua, nút Phản hồi), khu vực "Đang xử lý" kèm nút Hoàn thành, dải trạng thái online/offline của terminal. Có âm thanh báo động khi có lượt gọi mới, lặp lại mỗi N giây cho đến khi có người phản hồi, và card nhấp nháy sau 3 phút |
| **Admin** (`/admin`, chỉ dùng trên PC văn phòng) | Danh sách terminal (online/offline, lần cuối thấy, reset token), lịch sử lượt gọi kèm filter và export CSV, cài đặt (khoảng thời gian báo lại, ngưỡng quá hạn, bật/tắt ưu tiên Sự cố) |

### 4.3 Mô hình dữ liệu (tối giản)

| Bảng | Cột khóa |
|---|---|
| `Terminal` | Id, Name, Role (Caller hoặc Lift), MachineNo, TokenHash, LastSeenAt, IsActive |
| `Call` | Id, MachineNo, CallType, Status, CreatedAt, RespondedAt, RespondedByTerminalId, CompletedAt, CancelledAt, RequestId (unique), RowVersion |
| `LiftState` | Một dòng duy nhất: IsRunning, UpdatedAt, UpdatedByTerminalId |
| `Setting` | Key, Value |
| `AuditEvent` | Id, At, TerminalId, Action, CallId, Details |

### 4.4 Danh sách API

| Method | Endpoint | Mục đích |
|---|---|---|
| `POST` | `/api/calls` | Tạo một lượt gọi (kèm request ID để gửi lại an toàn) |
| `POST` | `/api/calls/{id}/respond` | Phản hồi (bên lift) |
| `POST` | `/api/calls/{id}/complete` | Hoàn thành (bên lift) |
| `POST` | `/api/calls/{id}/cancel` | Hủy (chỉ máy đang gọi) |
| `PUT` | `/api/lift/state` | Thiết lập chạy hoặc dừng |
| `GET` | `/api/state` | Snapshot đầy đủ, dùng khi khởi động và khi kết nối lại |
| `GET` | `/api/history` | Lịch sử kèm filter và export CSV (admin) |
| Hub | `/hubs/state` → `StateChanged(snapshot)` | Đẩy realtime tới mọi màn hình |

### 4.5 Giá trị mặc định đề xuất cho các câu hỏi còn mở

Đây là các giá trị mặc định đề xuất để không làm chậm việc phát triển. Mỗi mục cần khách hàng xác nhận, và hầu hết có thể thay đổi trong phần cài đặt.

| Câu hỏi còn mở | Giá trị mặc định đề xuất |
|---|---|
| Lượt gọi Sự cố có được vượt hàng đợi không? | **Có**, hiển thị ở trên cùng màu đỏ. Có thể tắt bằng cài đặt |
| Có cần bước "Hoàn thành" không? | **Có**, vì nó cho dữ liệu về thời gian phản hồi và thời gian xử lý. Lượt gọi còn mở sau một khoảng timeout có thể cấu hình sẽ được tự động hoàn thành |
| Mỗi máy được phép bao nhiêu lượt gọi đang hoạt động? | **Một lượt cho mỗi loại gọi.** Một máy có thể vừa yêu cầu lấy sản phẩm vừa báo sự cố cùng lúc |
| Lượt gọi không ai trả lời | Lặp âm thanh mỗi 30 giây, card nhấp nháy sau 3 phút, và sự kiện được ghi log. Việc escalation (đèn tháp hoặc báo giám sát) là tùy chọn giai đoạn 2 |
| Lượt gọi khi lift đang dừng | **Được phép.** Bên gọi thấy tiêu đề đỏ "Lift đã dừng", để biết rằng thời gian chờ có thể lâu |
| Ai chuyển trạng thái chạy/dừng? | Người điều khiển lift, trên máy tính bảng lift |
| Báo cáo | Có kèm export CSV lịch sử. Dashboard KPI là tùy chọn giai đoạn 2 |

---

## 5. Các Phương án Đã Xem xét

### 5.1 Thay đổi so với đề xuất ban đầu

| Chủ đề | Đề xuất ban đầu | Đề xuất này | Lý do |
|---|---|---|---|
| Client | 2 ứng dụng WPF (hoặc 1 ứng dụng với switch khi khởi động) | 1 ứng dụng web ở chế độ Edge kiosk | Không cần cài đặt hay cập nhật client; có sẵn kiosk và khả năng tự hồi phục khi crash |
| Runtime | .NET 8 LTS | .NET 10 LTS | .NET 8 hết hỗ trợ vào tháng 11/2026 |
| Database | Đề xuất dùng SQL Server Express | SQL Server dùng license hiện có của khách hàng | Đã trả tiền sẵn rồi. Có kèm SQL Server Agent để backup và maintenance |
| Cập nhật realtime | Sự kiện SignalR, kèm fallback polling | SignalR gửi full snapshot; `GET /api/state` khi kết nối lại | Đơn giản hơn và tự sửa sai |
| Chống trùng lặp | Chưa được quy định | Request ID trên mọi lệnh | Việc gửi lại do Wi-Fi chập chờn không thể tạo lượt gọi trùng |
| Admin | Chưa được đề cập | Trang admin cơ bản (terminal, lịch sử, export CSV, cài đặt) | Cần thiết để vận hành hệ thống hàng ngày |
| Cảnh báo | Âm thanh và màn hình | Âm thanh có lặp lại, cộng thêm đèn tháp tùy chọn | Người điều khiển đang lái lift không thể nhìn máy tính bảng liên tục |

**Nếu WPF là yêu cầu bắt buộc**, thiết kế server vẫn giữ nguyên. Chỉ phần client thay đổi. Công việc phát sinh được nêu ở mục 8.2.

### 5.2 Cách cập nhật đến được máy tính bảng

Hệ thống có 6–8 máy tính bảng trên một LAN và vài trăm lượt gọi mỗi ngày. Một lượt gọi mới chỉ cần xuất hiện trong vòng một hoặc hai giây. Chênh lệch chi phí là so với dự toán ở mục 8.

| Phương án | Mức độ phù hợp | Chi phí dev | Bảo trì | Kết luận |
|---|---|---|---|---|
| **SignalR** | Đẩy tới browser kèm tự kết nối lại, client C# cho Blazor, và trạng thái online/offline lấy từ chính kết nối | Baseline | Thấp. Cài đặt WebSocket và app pool của IIS chỉ làm một lần | **Khuyến nghị** |
| **Polling** (`GET /api/state` mỗi 1–2 giây) | 8 client × 1 request/giây là không đáng kể với server. Mỗi lần poll cũng đóng vai trò heartbeat | Thấp hơn một chút | Thấp nhất. Không có trạng thái kết nối, nên IIS recycle và Wi-Fi rớt không ảnh hưởng | **Giữ làm fallback có sẵn** |
| **Server-Sent Events** | Đẩy một chiều từ server qua HTTP thường, được hỗ trợ trực tiếp trong .NET 10. Phù hợp với thiết kế "lệnh REST + đẩy trạng thái" | Tương đương | Thấp | Khả thi, nhưng không có lợi thế rõ rệt so với SignalR với team C# |
| **Raw WebSocket** | Phải tự viết tay phần kết nối lại, heartbeat và xử lý message | +0.5 – 1 tuần | Trung bình | Không đáng. SignalR là một lớp bọc đã được kiểm thử kỹ quanh WebSocket |
| **Webhook** | Gọi HTTP callback giữa server với server. Browser và máy tính bảng không thể nhận HTTP call đến | n/a | n/a | **Không áp dụng** cho việc cập nhật màn hình. Có thể dùng sau cho cảnh báo gửi ra ngoài (Teams, email) |
| **Message queue** (RabbitMQ, Azure Service Bus, Kafka) | Được thiết kế để tách rời nhiều service backend và đệm khối lượng lớn. Browser không thể kết nối trực tiếp, nên vẫn cần SignalR ở phía trước | +1.5 – 2 tuần | Cao. Thêm một server nữa phải cài, bảo mật, giám sát và backup | **Quá mức cần thiết** với một server và 8 client |
| **MQTT** (ví dụ broker Mosquitto, thư viện MQTTnet cho .NET) | Mô hình publish/subscribe nhẹ, dùng rộng rãi trong nhà máy (IIoT). Browser kết nối qua WebSocket. Nút bấm vật lý, đèn tháp và gateway PLC thường hỗ trợ sẵn | +1 – 1.5 tuần | Trung bình. Phải duy trì broker, quy tắc truy cập và TLS | **Phương án cho tương lai** nếu có thêm nhiều thiết bị phần cứng |

Phần khó của hệ thống này là các quy tắc nghiệp vụ: thứ tự lượt gọi, chỉ một lần Phản hồi cho mỗi lượt gọi, và không có lượt gọi trùng. Chúng được đảm bảo bởi một **transaction database** trên server. Một queue hay broker chỉ chuyển message mà không đảm bảo bất kỳ quy tắc nào trong số này, nên nó chỉ thêm hạ tầng mà không bớt được công việc đó.

Vì server luôn gửi **toàn bộ trạng thái**, kênh đẩy chỉ cần báo "có gì đó đã thay đổi". Việc chuyển sang hoặc dùng polling làm fallback chỉ đơn giản là gọi `GET /api/state` theo định kỳ. Client tự làm việc này khi SignalR không kết nối được, và việc này đã được tính vào dự toán.

### 5.3 Các lựa chọn client .NET

Vì khách hàng muốn giữ nguyên .NET, chỉ các công nghệ client .NET được so sánh. Phía server (ASP.NET Core, SignalR, SQL Server trên IIS) giống nhau cho cả ba phương án.

| Phương án | Chi phí dev | Bảo trì | Khi nào nên dùng |
|---|---|---|---|
| **Blazor WebAssembly** (khuyến nghị) | Baseline | Thấp. Cập nhật chỉ triển khai trên server | Mặc định. UI vẫn tiếp tục chạy trong browser khi Wi-Fi rớt và tự hiển thị trạng thái lỗi riêng |
| **Blazor Server** (UI chạy trên server) | Ít hơn khoảng 1 – 1.5 tuần. Không cần lớp API riêng, và cập nhật realtime có sẵn | Thấp | Chỉ nên dùng nếu khảo sát Wi-Fi cho thấy vùng phủ ổn định. UI phụ thuộc vào kết nối liên tục: mất kết nối sẽ làm màn hình đứng với "Đang kết nối lại…", và mất kết nối lâu sẽ làm mất session. .NET 10 cải thiện điều này, nhưng vẫn là rủi ro với Wi-Fi nhà máy |
| **WPF** (ứng dụng Windows gốc, như đề xuất ban đầu) | +2 – 3 tuần (xem mục 8.2) | Cao hơn. Cần installer và auto-update trên từng máy tính bảng | Nếu buộc phải dùng ứng dụng gốc, ví dụ để dùng phần cứng local hoặc để khớp với các ứng dụng WPF hiện có |

### 5.4 Kết luận

1. **Blazor WebAssembly** trên ASP.NET Core, dùng **SQL Server** có license của khách hàng, host trên IIS.
2. **Giữ SignalR** cho cập nhật realtime, với polling là fallback tự động.
3. **Không dùng webhook hay message queue** trong hệ thống core. Webhook có thể là một add-on nhỏ ở giai đoạn 2 cho cảnh báo Teams hoặc email.
4. **Lưu ý đến MQTT** nếu sau này thêm nút gọi vật lý, đèn tháp hoặc dữ liệu PLC.
5. **Blazor Server** có thể tiết kiệm khoảng một tuần phát triển, nhưng chỉ khi khảo sát Wi-Fi cho thấy vùng phủ ổn định.

---

## 6. Triển khai & Vận hành

### 6.1 Phần cứng

| Mục | Số lượng | Ghi chú |
|---|---|---|
| Server | 1 | Mini PC công nghiệp nhỏ hoặc VM trên server hiện có. Windows Server hoặc Windows 11 Pro, **LAN có dây**, IP tĩnh hoặc tên DNS, chạy trên **UPS**. Database có thể dùng một instance SQL Server hiện có trên mạng của khách hàng thay vì cài local |
| Máy tính bảng bên gọi | 5 (+1 dự phòng) | Windows 11 Pro (bản Home không có Assigned Access). Màn hình cảm ứng thân thiện với găng tay. Gắn tường hoặc gắn cần, có nguồn điện cố định |
| Máy tính bảng lift | 1 (+1 dự phòng) | Như trên, cộng thêm một **loa ngoài có nguồn**, vì loa máy tính bảng quá nhỏ với môi trường nhà máy |
| Wi-Fi | — | Một **khảo sát hiện trường (site survey)** tại từng vị trí máy tính bảng và theo dọc tuyến lift. Một SSID riêng cho các thiết bị này |
| Đèn tháp báo hiệu (tùy chọn) | 1 | Đèn tháp kèm chuông, điều khiển qua LAN, gần lift (giai đoạn 2) |

### 6.2 Danh sách kiểm tra cấu hình server

- Cài tính năng **WebSocket Protocol** của IIS. Nếu thiếu, SignalR sẽ chuyển sang transport chậm hơn.
- Cài đặt app pool: Start Mode **AlwaysRunning**, Idle Time-out **0**, tắt recycle định kỳ hoặc chuyển sang recycle theo lịch vào ban đêm.
- Bật preload cho site để ứng dụng khởi động cùng IIS.
- SQL Server: backup full hàng đêm qua SQL Server Agent job (hoặc quy trình backup hiện có của khách hàng), copy lên network share và giữ trong 14 ngày. Lịch sử lượt gọi giữ trong 1–3 năm (có job purge).
- Dùng chứng chỉ HTTPS từ CA nội bộ, nếu IT có thể cấp. HTTP thường có thể chấp nhận trên LAN cách ly, nhưng HTTPS được khuyến nghị.

### 6.3 Danh sách kiểm tra cấu hình máy tính bảng (kiosk)

- **Assigned Access** kiosk một ứng dụng chạy Microsoft Edge, với URL khởi động riêng cho từng máy tính bảng. Edge tự mở lại trang nếu bị đóng hoặc crash.
- Chính sách Edge:
  - **Cho phép autoplay** với URL ứng dụng, để âm thanh cảnh báo phát được mà không cần chạm trước.
  - Tắt popup dịch và trải nghiệm first-run.
- Cài đặt nguồn điện: màn hình không bao giờ tắt và thiết bị không bao giờ sleep. Tắt tính năng tiết kiệm điện của adapter Wi-Fi.
- Dùng **giới hạn sạc pin** của nhà sản xuất (ví dụ 80%) để tránh phình pin do luôn cắm điện.
- Đặt giờ hoạt động của Windows Update ngoài giờ sản xuất.
- Một **hướng dẫn provisioning** để có thể thiết lập máy tính bảng dự phòng trong vòng dưới 30 phút.

### 6.4 Hành vi khi gặp lỗi

| Lỗi | Người dùng thấy gì | Khắc phục |
|---|---|---|
| Máy tính bảng mất Wi-Fi | "Lỗi kết nối" toàn màn hình. Các nút bị disable để không ai nghĩ rằng lượt gọi đã được gửi | Tự kết nối lại và đồng bộ lại |
| Server hoặc IIS restart | Mọi màn hình hiện "Lỗi kết nối" trong vài giây | Tự kết nối lại. Các lượt gọi đang hoạt động được khôi phục từ DB |
| Server down lâu | "Lỗi kết nối" trên mọi màn hình | Dùng phương án dự phòng đã thống nhất (điện thoại hoặc bộ đàm). Runbook khôi phục cộng với server dự phòng hoặc image VM |
| Browser crash | — | Chế độ kiosk tự khởi động lại Edge |

---

## 7. Kế hoạch Triển khai

Triển khai theo Agile, sprint 2 tuần, có review tại nhà máy vào cuối mỗi sprint.

| Tuần | Hoạt động |
|---|---|
| 1–2 | Workshop yêu cầu, quyết định các câu hỏi còn mở, review mockup có thể click tại hiện trường, thiết kế kỹ thuật |
| 3–4 | Sprint 1: thiết lập project, CI, mô hình dữ liệu, state machine của lượt gọi, REST API, snapshot SignalR, màn hình bên gọi |
| 5–6 | Sprint 2: màn hình lift (hàng đợi, timer, cảnh báo âm thanh, Phản hồi và Hoàn thành, toggle chạy/dừng), phát hiện offline |
| 7 | Sprint 3 (ngắn): trang admin, lịch sử và export CSV, cài đặt, text UI tiếng Nhật và tiếng Anh |
| 8 | Script cài server và kiosk, chu kỳ test hệ thống lần 1 |
| 9 | Test tại hiện trường trên máy tính bảng và Wi-Fi thật, sửa lỗi, chu kỳ regression lần 2 |
| 10–11 | UAT và **pilot tại nhà máy** (dùng thật kèm quy trình dự phòng), điều chỉnh |
| 12 | Go-live, chuyển giao hướng dẫn vận hành và quản trị |

Tester bắt đầu khoảng 50% từ tuần 3 (lập kế hoạch test) và làm gần như full-time trong tuần 8–11.

---

## 8. Dự toán Sơ bộ

Các số liệu là sơ bộ và tính theo **tuần**. Giả định một developer C# trình độ trung-cao có kinh nghiệm ASP.NET Core và một tester tham gia bán thời gian từ tuần 3.

### 8.1 Công việc và tiến độ

| Giai đoạn | Tuần theo lịch | Developer (person-week) | Tester (person-week) |
|---|---|---:|---:|
| Khảo sát & thiết kế | 1–2 | 1.5 | 0.5 |
| Build (3 sprint) | 3–7 | 5.5 | 1.5 |
| Triển khai & test hệ thống | 8–9 | 2 | 2 |
| UAT & pilot tại nhà máy | 10–11 | 2 | 1 |
| Go-live & chuyển giao | 12 | 1 | – |
| **Tổng** | **≈ 12 tuần** | **≈ 12** (khoảng 9–17) | **≈ 5** (khoảng 3–6) |

- **Nhóm:** 1 developer và 1 tester (bán thời gian giai đoạn đầu, gần full-time trong tuần 8–11). Với 2 developer, khoảng **8–9 tuần** để go-live.
- **Không bao gồm:** quản lý dự án và phối hợp với khách hàng (thêm 10–15%), mua sắm và lắp đặt phần cứng, khảo sát hiện trường Wi-Fi, chi phí đi lại, và license Windows. License SQL Server do khách hàng cung cấp.

### 8.2 Các biến thể và hạng mục bổ sung tùy chọn

| Mục | Developer | Tester | Ghi chú |
|---|---|---|---|
| **Dùng client WPF thay cho web** (cùng thiết kế server) | +2 – 3 tuần | +0.5 – 1 tuần | Hai UI XAML, đóng gói và auto-update, thiết lập kiosk, test cài đặt và nâng cấp |
| Đèn tháp báo hiệu hoặc chuông qua mạng gần lift | ≈ 1 tuần | ≈ 0.5 tuần | Đèn và âm thanh khi có lượt gọi mới. Rất hữu ích nếu người điều khiển ở xa máy tính bảng |
| Dashboard KPI (thời gian phản hồi; số lượt gọi theo máy, loại và giờ) | 1 – 1.5 tuần | ≈ 0.5 tuần | Dùng dữ liệu lịch sử hiện có |
| Tích hợp PLC để tự động lấy trạng thái chạy/dừng | 1 – 2 tuần | 0.5 – 1 tuần | Phụ thuộc vào model và protocol của PLC. Cần một spike kỹ thuật trước |
| Escalation cho giám sát (email hoặc Teams) với các lượt gọi Sự cố chưa được trả lời | ≈ 0.5 tuần | < 0.5 tuần | Cần mail server hoặc truy cập internet |

### 8.3 Triển khai có hỗ trợ AI

Nếu developer dùng một AI coding assistant (ví dụ Claude Code) cho phần lớn code, test tự động, script và tài liệu, và review toàn bộ, công việc sẽ giảm như sau. Những công việc gắn với con người, phần cứng hoặc hiện trường nhà máy (workshop, test tại hiện trường, UAT, pilot) thay đổi rất ít.

| | Thông thường | Có hỗ trợ AI |
|---|---:|---:|
| Developer | ≈ 12 person-week | ≈ 7 person-week (khoảng 5–9) |
| Tester | ≈ 5 person-week | ≈ 4 person-week (khoảng 3–5) |
| **Thời gian lịch đến go-live** | **≈ 12 tuần** | **≈ 8–9 tuần** |

- **Chất lượng:** test tự động kỹ lưỡng, bao gồm bộ test end-to-end trên browser, có chi phí rẻ khi dùng AI, nên độ phủ regression cuối cùng cao hơn.
- **Điều kiện:** developer có kinh nghiệm .NET và review toàn bộ code do AI sinh ra. Với một developer ít kinh nghiệm hơn, mức tiết kiệm sẽ giảm và rủi ro lỗi tinh vi sẽ tăng.
- **Độ tin cậy:** các số liệu có hỗ trợ AI là dựa trên đánh giá chủ quan. Hãy đo tiến độ thực tế sau sprint đầu tiên và re-plan nếu cần. Giữ contingency cho Wi-Fi nhà máy và phần cứng thật, nơi các dự án như thế này thường bị trễ.

---

## 9. Rủi ro & Biện pháp Giảm thiểu

| Rủi ro | Tác động | Biện pháp giảm thiểu |
|---|---|---|
| Wi-Fi yếu tại một số vị trí máy hoặc theo tuyến lift | Lượt gọi bị mất hoặc chậm | Khảo sát hiện trường trước go-live. Overlay lỗi kết nối để lỗi hiển thị rõ. Đi khảo sát vùng phủ trong lúc test tại hiện trường |
| Người điều khiển không thấy hoặc không nghe được máy tính bảng khi đang lái lift | Lượt gọi phải chờ quá lâu | Loa ngoài, cảnh báo lặp lại, đèn tháp tùy chọn (mục 8.2) |
| Browser chặn âm thanh cảnh báo | Cảnh báo bị im lặng | Chính sách autoplay của Edge cho URL ứng dụng, kiểm tra trong lúc thiết lập kiosk |
| IIS recycle ứng dụng hoặc rớt WebSocket | Mất kết nối ngắn | Cài đặt app pool ở mục 6.2. Client tự kết nối lại và resync |
| Server duy nhất bị lỗi | Hệ thống ngừng hoạt động | UPS, backup hàng đêm, runbook khôi phục, image VM dự phòng, phương án dự phòng điện thoại hoặc bộ đàm đã thống nhất |
| Máy tính bảng luôn cắm điện: phình pin, sleep, Windows update | Thiết bị ngừng hoạt động | Giới hạn sạc, chính sách nguồn điện, giờ hoạt động update, máy tính bảng dự phòng |
| Các quy tắc nghiệp vụ còn mở chưa được quyết định kịp thời | Phải làm lại | Giá trị mặc định ở mục 4.5, có thể cấu hình. Quyết định được chốt trong tuần 1–2 |
| Yêu cầu bắt buộc phải giữ WPF | Công việc phát sinh thêm | Thiết kế server không đổi. Dự toán ở mục 8.2 |

---

## 10. Giả định

- Khách hàng cung cấp máy tính bảng, server hoặc VM, mạng Wi-Fi, quyền truy cập mạng, và một người liên hệ tại nhà máy cho các buổi review và UAT.
- 5 terminal bên gọi và tối đa 3 terminal lift, tại một site, cho một lift.
- UI bằng tiếng Nhật, có sẵn text tiếng Anh. Tài liệu bằng tiếng Anh (không bao gồm bản dịch tiếng Nhật).
- Không tích hợp hệ thống bên ngoài (ERP, MES, PLC) trong phạm vi cơ bản.
- Nhóm phát triển có thể đến hiện trường để test tại chỗ (tuần 9–11).
