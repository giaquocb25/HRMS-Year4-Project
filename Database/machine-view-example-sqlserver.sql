-- Chạy trong database SQL Server của máy chấm công và đổi bảng/cột theo thiết bị.
-- HRMS chỉ đọc view này; không ghi vào database của máy.
CREATE OR ALTER VIEW dbo.hrms_attendance_export AS
SELECT
  CONVERT(varchar(30), employee_id) AS employee_code,
  CONVERT(varchar(100), card_uid) AS card_uid,
  event_time,
  CONVERT(varchar(160), event_id) AS event_id
FROM dbo.attendance_logs;
