# Checklist triển khai và demo HRMS

## 1. Chuẩn bị môi trường

- [ ] Cài Visual Studio 2019/2022, .NET Framework 4.7.2 Targeting Pack và DevExpress WPF 22.2.
- [ ] Cài MySQL Server 8.x và MySQL Workbench.
- [ ] Với Android: cài Node.js 18, JDK 11, Android Studio và Android SDK phù hợp React Native 0.71.
- [ ] Máy quản lý và điện thoại nằm cùng mạng LAN riêng.

## 2. Database

- [ ] Sao lưu database hiện tại trước khi thay đổi.
- [ ] Cài mới: chạy `Database/schema.sql`.
- [ ] Nâng cấp database cũ: chạy `Database/migrate_2026_multisource_attendance.sql`; file có thể chạy lại an toàn nếu lần trước bị dừng giữa chừng.
- [ ] Xác nhận có các bảng `UserRoles`, `EmployeeCards`, `AttendanceEvents`, `SystemAuditLogs`.
- [ ] Xác nhận `Timekeeping` có unique index `(employeeId, workDate)`.
- [ ] Chạy `Database/verify-installation.sql`; tất cả dòng kiểm tra chính phải trả về `PASS`.
- [ ] Tạo tài khoản MySQL theo vai trò; tham khảo `Database/role-grants-example.sql`.
- [ ] Không dùng tài khoản `root` khi vận hành thật.

## 3. Kiểm tra ứng dụng WPF

- [ ] Mở `HRMS.sln`, restore NuGet và build cấu hình `Debug | Any CPU`.
- [ ] Đăng nhập bằng tài khoản Admin và kiểm tra vai trò ở thanh trạng thái.
- [ ] Tạo nhân viên có `employeeCode` duy nhất; kiểm tra email, điện thoại và ngày sinh.
- [ ] Đăng nhập lần lượt bằng HR, Payroll, Timekeeper, Viewer để kiểm tra nút/tab bị khóa đúng quyền.
- [ ] Đăng xuất khi dịch vụ QR đang mở; xác nhận quay lại màn hình đăng nhập và token cũ không còn dùng được.
- [ ] Vô hiệu hóa thử một tài khoản trong `UserRoles`; xác nhận tài khoản đó bị từ chối đăng nhập.
- [ ] Thử hạ quyền/vô hiệu hóa quản trị viên cuối cùng; xác nhận hệ thống từ chối để tránh tự khóa toàn bộ phần quản trị.
- [ ] Kiểm tra trường lương và thông tin riêng tư không hiện với Timekeeper/Viewer.

## 4. Chấm công không QR và thẻ từ

- [ ] Mở `Tiện ích > Trung tâm chấm công` bằng Timekeeper/Admin.
- [ ] Nhập ID nội bộ hoặc mã nhân viên rồi Enter; kiểm tra giờ vào.
- [ ] Chọn nhân viên bằng HR/Admin, quét UID thẻ và gán thẻ.
- [ ] Đăng nhập Timekeeper/Admin, quét thẻ để ghi công.
- [ ] Quét lại ngay trong vòng 5 phút; xác nhận nhật ký có sự kiện nhưng chưa tạo giờ ra.
- [ ] Quét lại sau mốc tối thiểu; xác nhận giờ ra được cập nhật.
- [ ] Kiểm tra tab nhật ký đối soát có nguồn, thiết bị và người ghi.
- [ ] Với HR/Admin, điều chỉnh một ngày công kèm lý do; xác nhận giờ thay đổi, audit có giá trị trước/sau và dữ liệu quét gốc vẫn còn.

## 5. Nhập file

- [ ] Sao chép `Database/attendance-import-template.csv` và thêm dữ liệu thử.
- [ ] Thử một dòng dùng `employee_code`, một dòng dùng `card_uid`.
- [ ] Nhập file lần đầu và đối chiếu tổng/đã nhập/lỗi.
- [ ] Nhập lại cùng file; xác nhận các dòng được báo trùng và không tạo thêm công.
- [ ] Lưu cùng dữ liệu thành `.xlsx` và kiểm tra lại luồng Excel.

## 6. Database máy chấm công

- [ ] Tạo tài khoản chỉ có quyền `SELECT` trên database của máy.
- [ ] Tạo view chuẩn theo `machine-view-example.sql` (MySQL) hoặc `machine-view-example-sqlserver.sql` (SQL Server).
- [ ] Đối chiếu `employee_code` của máy với `Employees.employeeCode` trong HRMS.
- [ ] Chọn mốc đồng bộ nhỏ để thử trước, sau đó mới nhập lịch sử dài ngày.
- [ ] Chạy đồng bộ hai lần và xác nhận khóa chống trùng hoạt động.
- [ ] Thử dữ liệu không theo thứ tự thời gian; xác nhận hệ thống vẫn lấy lượt sớm nhất làm giờ vào và lượt muộn nhất hợp lệ làm giờ ra.
- [ ] Không lưu mật khẩu máy chấm công vào source code hoặc file cấu hình được commit.

## 7. Android client–server

- [ ] Chạy URLACL bằng quyền quản trị cho cổng trong `AttendanceApiPort` (mặc định 8080).
- [ ] Chỉ mở inbound firewall TCP 8080 trên profile Private.
- [ ] Mở `Chấm công bằng QR` trên WPF; quét QR thiết lập bằng Android.
- [ ] Truy cập `GET http://IP_MAY_CHU:8080/timekeeping/` để kiểm tra health endpoint.
- [ ] Quét QR nhân viên; xác nhận điện thoại nhận kết quả ghi công trực tiếp.
- [ ] Thử token sai và token hết hạn; API phải trả lỗi 401.
- [ ] Đóng cửa sổ chấm công; xác nhận token cũ không còn dùng được.

## 8. Lương, báo cáo và audit

- [ ] Tạo dữ liệu đủ/thiếu 8 giờ và kiểm tra trạng thái công.
- [ ] Tính lương tháng; nhân viên `Inactive` không có bảng lương mới.
- [ ] Nhân viên đang hoạt động nhưng không có ngày công vẫn có dòng lương 0.
- [ ] Xuất báo cáo công CSV và mở bằng Excel để kiểm tra tiếng Việt.
- [ ] Xuất phiếu lương và đối chiếu ngày công/số tiền.
- [ ] Mở `Cài đặt > Nhật ký hệ thống`; kiểm tra login, sửa hồ sơ, phân quyền, tính lương và xuất báo cáo.

## 9. Trước khi bảo vệ đồ án

- [ ] Chuẩn bị database demo riêng, ít nhất 5 nhân viên và 2 tháng dữ liệu.
- [ ] Có sẵn một file CSV/XLSX hợp lệ và một file có lỗi để trình bày validation.
- [ ] Thử demo khi mất mạng, sai mật khẩu, trùng sự kiện và thẻ chưa gán.
- [ ] Tắt thông báo/ứng dụng không liên quan, kiểm tra firewall và nguồn điện.
- [ ] Không đưa mật khẩu, dữ liệu cá nhân thật hoặc dump production lên GitHub.
