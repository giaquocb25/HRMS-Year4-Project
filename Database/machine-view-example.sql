-- Chạy trong database của máy chấm công và đổi tên bảng/cột cho đúng thiết bị.
-- HRMS chỉ đọc view này; không ghi vào database của máy.
CREATE OR REPLACE VIEW hrms_attendance_export AS
SELECT
  CAST(employee_id AS CHAR(30)) AS employee_code,
  CAST(card_uid AS CHAR(100)) AS card_uid,
  event_time,
  CAST(event_id AS CHAR(160)) AS event_id
FROM attendance_logs;
