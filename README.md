# HRMS

HRMS là đồ án quản lý nhân sự nội bộ theo mô hình client–server, gồm ứng dụng quản lý WPF, cơ sở dữ liệu MySQL và ứng dụng React Native. Phân hệ chấm công dùng một lõi xử lý chung cho QR, nhập tay, thẻ từ, file và database máy chấm công.

## Chức năng

- Đăng nhập bằng tài khoản MySQL được cấp cho người quản lý.
- Xem, tìm kiếm, thêm, sửa và xóa hồ sơ nhân viên.
- Chấm công không cần QR bằng mã nhân viên hoặc đầu đọc thẻ USB giả lập bàn phím.
- Ghi nhận giờ vào/ra từ ứng dụng Android qua API trong mạng LAN; QR là phương án tùy chọn.
- Nhập file công `.xlsx`, `.csv`, `.tsv`, `.txt` (phân cách bằng dấu phẩy, chấm phẩy hoặc tab) và đồng bộ database MySQL/SQL Server của máy chấm công qua view chuẩn.
- Lưu nhật ký sự kiện gốc, nguồn/thiết bị/người thao tác và chống nhập trùng bằng mã sự kiện.
- HR/Admin có thể điều chỉnh giờ vào/ra khi quên chấm hoặc thiết bị sai; lý do và giá trị trước/sau được ghi audit, còn sự kiện quét gốc không bị sửa.
- Phân quyền `Admin`, `HR`, `Payroll`, `Timekeeper`, `Viewer` ở cả giao diện lẫn tầng nghiệp vụ.
- Ghi nhật ký đăng nhập/đăng xuất, thay đổi hồ sơ, gán thẻ, nhập/sync công, điều chỉnh công, phân quyền, tính lương và xuất báo cáo để truy vết thao tác quản trị.
- Tính lương tháng từ các ngày đạt ngưỡng giờ công cấu hình (mặc định 8 giờ).
- Xem biểu đồ lương và xuất phiếu lương.
- Chuyển đổi giao diện tiếng Việt và tiếng Anh.

## Yêu cầu môi trường

- Windows 10 hoặc 11.
- Visual Studio 2019/2022 với .NET Framework 4.7.2 Targeting Pack.
- DevExpress WPF 22.2.
- MySQL Server 8.x.
- Với ứng dụng Android: Node.js 18, JDK 11 và Android Studio/Android SDK tương thích React Native 0.71.

## Khởi tạo cơ sở dữ liệu

1. Cài mới: mở MySQL Workbench và chạy `Database/schema.sql`. Database cũ: sao lưu dữ liệu rồi chạy `Database/migrate_2026_multisource_attendance.sql`.
2. Tạo một tài khoản MySQL riêng cho ứng dụng; không dùng tài khoản `root` trong vận hành thông thường.

```sql
CREATE USER 'hrms_app'@'localhost' IDENTIFIED BY 'change-this-password';
GRANT SELECT, INSERT, UPDATE, DELETE ON hmrs.* TO 'hrms_app'@'localhost';
FLUSH PRIVILEGES;
```

3. Kiểm tra các khóa `DatabaseServer`, `DatabasePort` và `DatabaseName` trong `HRMS/App.config`. Mặc định project kết nối `localhost:3306`, cơ sở dữ liệu `hmrs`.

Các quy tắc có thể cấu hình trong cùng file gồm `MinimumCheckoutMinutes`, `StandardWorkingDays`, `FullWorkdayHours` và `MandatoryInsuranceRate` (tỷ lệ dạng thập phân, ví dụ `0.105`). Giá trị không hợp lệ tự quay về mặc định an toàn.

Sau khi cài hoặc nâng cấp, chạy `Database/verify-installation.sql` để kiểm tra bảng, cột, unique index, Admin đang hoạt động và tính toàn vẹn dữ liệu chấm công.

Project không lưu sẵn tên đăng nhập hoặc mật khẩu. Người quản lý nhập tài khoản MySQL khi mở chương trình.

File chứng thư ClickOnce cũ `HRMS/HRMS_TemporaryKey.pfx` không còn được project tham chiếu. Trước khi đưa repository lên GitHub, hãy loại file này khỏi lịch sử Git và thay chứng thư nếu từng dùng nó để ký bản phát hành.

`schema.sql` tạo sẵn ánh xạ vai trò `Admin` cho `root` và `hrms_app`. Admin quản lý ánh xạ cho các tài khoản MySQL khác tại `Cài đặt > Phân quyền`. Tài khoản chưa được ánh xạ chỉ có quyền `Viewer`.

Hồ sơ nhân viên hỗ trợ mã nhân viên duy nhất, phòng ban, ngày sinh, giới tính, điện thoại, email và trạng thái. Trạng thái dùng một trong `Active`, `Inactive`, `OnLeave`; nhân viên `Inactive` không được tạo bảng lương tháng mới.

| Vai trò | Quyền chính |
|---|---|
| Admin | Toàn bộ chức năng và phân quyền |
| HR | Quản lý hồ sơ, ảnh và thẻ nhân viên; xem và điều chỉnh công |
| Payroll | Xem/tính/xuất lương; xem công |
| Timekeeper | Chấm công, nhập file, đồng bộ máy; xem công |
| Viewer | Chỉ xem danh sách nhân viên |

Ngoài kiểm tra quyền trong ứng dụng, có thể áp dụng quyền tối thiểu ngay tại MySQL bằng mẫu `Database/role-grants-example.sql`. Đây là lớp bảo vệ thứ hai nếu tài khoản database bị dùng ngoài ứng dụng.

## Chạy ứng dụng WPF

1. Mở `HRMS.sln`.
2. Restore NuGet packages nếu Visual Studio yêu cầu.
3. Chọn cấu hình `Debug | Any CPU`, build và chạy project `HRMS`.

### Chạy nhanh bằng VS Code với MySQL cục bộ

1. Mở thư mục project bằng VS Code.
2. Chạy task `Run HRMS` hoặc nhấn `F5` và chọn `HRMS Run (F5)`.
3. Task sẽ kiểm tra/khởi động MySQL cục bộ, build cấu hình Debug rồi mở màn hình đăng nhập.
4. Khi hoàn tất, chạy task `Stop HRMS MySQL`; công cụ sẽ hỏi mật khẩu trước khi tắt database an toàn.

Cấu hình `HRMS Run (F5)` chạy ứng dụng không gắn debugger vì debugger C# của VS Code chỉ hỗ trợ giới hạn đối với project .NET Framework cũ. Khi cần đặt breakpoint và debug sâu, mở solution bằng Visual Studio; chạy và chỉnh sửa thông thường vẫn thực hiện được bằng VS Code.

Cấu hình VS Code không lưu mật khẩu. Instance thử nghiệm chỉ lắng nghe tại `127.0.0.1:3306`; riêng tiến trình chạy cục bộ dùng biến môi trường để tắt TLS và cho phép lấy public key. Không dùng hai tùy chọn này khi kết nối qua LAN hoặc Internet; môi trường triển khai phải giữ TLS và xác minh chứng thư.

Để điện thoại truy cập dịch vụ chấm công, Windows phải cho phép ứng dụng lắng nghe cổng cấu hình bởi `AttendanceApiPort` (mặc định 8080). Chạy lệnh sau một lần trong Command Prompt hoặc PowerShell với quyền quản trị, thay `TEN_NGUOI_DUNG_WINDOWS` bằng tài khoản Windows đang chạy HRMS:

```text
netsh http add urlacl url=http://+:8080/timekeeping/ user=TEN_NGUOI_DUNG_WINDOWS
```

Nếu Windows Firewall đang chặn kết nối nội bộ, tạo inbound rule TCP cho cổng 8080 chỉ trên mạng Private.

Android và iOS được cấu hình cho HTTP nội bộ để tương thích máy chủ WPF hiện tại. Chỉ dùng trên mạng LAN tin cậy; khi triển khai qua Internet cần đặt API sau HTTPS/reverse proxy và bỏ ngoại lệ cleartext trong manifest mobile.

## Chạy ứng dụng Android

```text
cd "App QR/QRScanner-main"
npm install
npm run android
```

Điện thoại và máy quản lý phải cùng mạng LAN. Trên WPF, mở `Tiện ích > Chấm công bằng QR`, quét mã thiết lập bằng điện thoại, sau đó quét mã QR nhân viên. API ghi công ngay và trả JSON kết quả; không cần bấm xác nhận trên máy quản lý. Token hết hạn sau 30 phút và bị hủy khi cửa sổ đóng.

Nếu thiết bị client không quét được mã thiết lập, cửa sổ WPF cũng hiển thị chuỗi `token,IP,cổng` để sao chép và nhập thủ công.

Client khác (đầu đọc mạng, kiosk hoặc ứng dụng riêng) có thể gọi cùng endpoint:

```http
POST http://MAY_CHU:8080/timekeeping/
Content-Type: application/json

{
  "employeeId": 12,
  "employeeCode": null,
  "cardUid": null,
  "token": "TOKEN_PHIEN",
  "eventTime": "2026-09-09T08:01:30+07:00",
  "externalEventId": "device-a-987654",
  "deviceName": "Cổng chính"
}
```

Gọi `GET /timekeeping/` để kiểm tra dịch vụ đang hoạt động và đồng hồ máy chủ mà không đọc dữ liệu nhân sự.

Gửi một trong `employeeId`, `employeeCode` hoặc `cardUid`. Server giới hạn body 4 KB, kiểm tra token phiên, quyền của người mở dịch vụ, mã nhân viên/thẻ và khóa chống trùng trước khi ghi. Endpoint hiện dùng HTTP nội bộ; không mở trực tiếp ra Internet. Nếu triển khai nhiều chi nhánh, đặt sau HTTPS reverse proxy hoặc VPN.

## Chấm công không dùng QR

Mở `Tiện ích > Trung tâm chấm công`:

- Nhập mã nhân viên rồi Enter để chấm công thủ công.
- Với đầu đọc thẻ USB dạng keyboard wedge, gán UID thẻ cho nhân viên một lần, sau đó chỉ cần quét thẻ và Enter.
- Chuỗi nhập được dò theo thứ tự thẻ đã gán, mã nhân viên, rồi ID nội bộ; vì vậy UID thẻ chỉ gồm chữ số không bị hiểu nhầm.
- Lần ghi đầu tiên trong ngày là giờ vào; các lần sau cập nhật giờ ra lớn nhất. Sự kiện nhập ngược thời gian vẫn giữ giờ vào nhỏ nhất.
- Các lần quét lại trong khoảng `MinimumCheckoutMinutes` (mặc định 5 phút) vẫn được lưu vào nhật ký nhưng không bị hiểu nhầm thành giờ ra.
- Lượt quét trực tiếp từ mobile dùng thời gian của máy chủ HRMS, tránh sai lệch hoặc việc thay đổi đồng hồ điện thoại. Dữ liệu lịch sử từ file/máy chấm công vẫn giữ thời gian sự kiện gốc.
- Nhân viên có trạng thái `Inactive` không thể tạo thêm lượt chấm công.

## Nhập Excel/CSV và kết nối máy chấm công

File nhập cần dòng tiêu đề và các cột sau:

```text
employee_id,employee_code,card_uid,event_time,event_id
```

Chỉ cần một trong `employee_id` (ID nội bộ), `employee_code` (khuyến nghị khi lấy từ thiết bị) hoặc `card_uid`; `event_time` là bắt buộc. Nên có `event_id` duy nhất từ thiết bị để chạy đồng bộ nhiều lần mà không sinh dữ liệu trùng. File mẫu nằm tại `Database/attendance-import-template.csv`.

Để đọc database máy chấm công MySQL hoặc SQL Server, tạo một tài khoản chỉ có quyền `SELECT`, rồi tạo view bốn cột `employee_code`, `card_uid`, `event_time`, `event_id` theo `Database/machine-view-example.sql` hoặc `Database/machine-view-example-sqlserver.sql`. Trong Trung tâm chấm công, chọn loại database rồi nhập máy chủ, database, tài khoản, tên view và mốc thời gian cần đồng bộ. Với ZKTeco/Ronald Jack chỉ cung cấp SDK/API riêng, bổ sung adapter mới nhưng vẫn đưa dữ liệu về bốn trường chuẩn trên.

Mật khẩu database máy chấm công chỉ tồn tại trong bộ nhớ của cửa sổ đồng bộ, không được ghi vào source code hoặc file cấu hình.

## Mô hình xử lý

```text
Android QR / nhập tay / thẻ từ / Excel-CSV / database máy
                         |
                         v
              AttendanceService (phân quyền)
                         |
            AttendanceEvents (audit, chống trùng)
                         |
             Timekeeping (giờ vào/giờ ra ngày)
                         |
                    Tính lương
```

## Kiểm tra nhanh

```text
npm run lint
npm test -- --runInBand
npx tsc --noEmit
```

Ứng dụng WPF có thể build bằng Visual Studio hoặc MSBuild sau khi cài đầy đủ .NET Framework 4.7.2 Targeting Pack và DevExpress WPF 22.2.

## Lưu ý dữ liệu

Các file dump cũ trong `db/` và `Database/` chỉ nên dùng để tham khảo dữ liệu mẫu. Với cài đặt mới, dùng `Database/schema.sql` để có khóa duy nhất chống chấm công trùng ngày và chống tạo trùng bảng lương theo tháng.
