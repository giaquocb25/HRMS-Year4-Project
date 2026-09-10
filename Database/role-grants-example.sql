-- Chạy bằng tài khoản quản trị MySQL sau khi đã tạo các tài khoản bên dưới.
-- Thay tên tài khoản/host theo môi trường; không ghi mật khẩu thật vào file này.

-- Admin ứng dụng
GRANT SELECT, INSERT, UPDATE, DELETE ON hmrs.* TO 'hrms_admin'@'localhost';

-- Nhân sự: hồ sơ, ảnh, thẻ; xem và điều chỉnh bảng công ngày; chỉ xem lương
GRANT SELECT ON hmrs.* TO 'hrms_hr'@'localhost';
GRANT INSERT, UPDATE, DELETE ON hmrs.Employees TO 'hrms_hr'@'localhost';
GRANT INSERT, UPDATE, DELETE ON hmrs.EmployeeCards TO 'hrms_hr'@'localhost';
GRANT INSERT, UPDATE ON hmrs.Timekeeping TO 'hrms_hr'@'localhost';
GRANT INSERT ON hmrs.SystemAuditLogs TO 'hrms_hr'@'localhost';

-- Chấm công: đọc nhân viên/thẻ, ghi sự kiện và bảng công ngày
GRANT SELECT ON hmrs.Employees TO 'hrms_timekeeper'@'localhost';
GRANT SELECT ON hmrs.EmployeeCards TO 'hrms_timekeeper'@'localhost';
GRANT SELECT, INSERT ON hmrs.AttendanceEvents TO 'hrms_timekeeper'@'localhost';
GRANT SELECT, INSERT, UPDATE ON hmrs.Timekeeping TO 'hrms_timekeeper'@'localhost';
GRANT SELECT ON hmrs.UserRoles TO 'hrms_timekeeper'@'localhost';
GRANT INSERT ON hmrs.SystemAuditLogs TO 'hrms_timekeeper'@'localhost';

-- Lương: đọc hồ sơ/công, quản lý bảng lương
GRANT SELECT ON hmrs.Employees TO 'hrms_payroll'@'localhost';
GRANT SELECT ON hmrs.Timekeeping TO 'hrms_payroll'@'localhost';
GRANT SELECT, INSERT, UPDATE, DELETE ON hmrs.Payroll TO 'hrms_payroll'@'localhost';
GRANT SELECT ON hmrs.UserRoles TO 'hrms_payroll'@'localhost';
GRANT INSERT ON hmrs.SystemAuditLogs TO 'hrms_payroll'@'localhost';

-- Chỉ xem nhân viên
GRANT SELECT ON hmrs.Employees TO 'hrms_viewer'@'localhost';
GRANT SELECT ON hmrs.UserRoles TO 'hrms_viewer'@'localhost';
GRANT INSERT ON hmrs.SystemAuditLogs TO 'hrms_viewer'@'localhost';

INSERT INTO hmrs.UserRoles (username, role) VALUES
  ('hrms_admin', 'Admin'),
  ('hrms_hr', 'HR'),
  ('hrms_timekeeper', 'Timekeeper'),
  ('hrms_payroll', 'Payroll'),
  ('hrms_viewer', 'Viewer')
ON DUPLICATE KEY UPDATE role = VALUES(role), isActive = 1;
