-- Kiểm tra sau khi chạy schema.sql hoặc migrate_2026_multisource_attendance.sql.
-- Script chỉ đọc dữ liệu, không sửa hoặc xóa bản ghi.

USE `hmrs`;

SELECT 'required_tables' AS check_name,
       COUNT(*) AS actual_count,
       7 AS expected_count,
       IF(COUNT(*) = 7, 'PASS', 'FAIL') AS result
FROM information_schema.tables
WHERE table_schema = DATABASE()
  AND table_name IN (
    'Employees', 'UserRoles', 'EmployeeCards', 'AttendanceEvents',
    'SystemAuditLogs', 'Timekeeping', 'Payroll'
  );

SELECT 'employee_profile_columns' AS check_name,
       COUNT(*) AS actual_count,
       7 AS expected_count,
       IF(COUNT(*) = 7, 'PASS', 'FAIL') AS result
FROM information_schema.columns
WHERE table_schema = DATABASE()
  AND table_name = 'Employees'
  AND column_name IN (
    'employeeCode', 'department', 'dateOfBirth', 'gender',
    'phone', 'email', 'employmentStatus'
  );

SELECT 'required_unique_indexes' AS check_name,
       COUNT(DISTINCT index_name) AS actual_count,
       4 AS expected_count,
       IF(COUNT(DISTINCT index_name) = 4, 'PASS', 'FAIL') AS result
FROM information_schema.statistics
WHERE table_schema = DATABASE()
  AND non_unique = 0
  AND index_name IN (
    'UX_Employees_EmployeeCode', 'UX_Employees_Email',
    'UX_AttendanceEvents_Source_External', 'UX_Timekeeping_Employee_Date'
  );

SELECT 'active_admins' AS check_name,
       COUNT(*) AS actual_count,
       'at least 1' AS expected_count,
       IF(COUNT(*) >= 1, 'PASS', 'FAIL') AS result
FROM UserRoles
WHERE role = 'Admin' AND isActive = 1;

SELECT 'employee_foreign_keys' AS check_name,
       COUNT(DISTINCT table_name) AS actual_count,
       4 AS expected_count,
       IF(COUNT(DISTINCT table_name) = 4, 'PASS', 'FAIL') AS result
FROM information_schema.key_column_usage
WHERE table_schema = DATABASE()
  AND referenced_table_name = 'Employees'
  AND referenced_column_name = 'id'
  AND table_name IN ('EmployeeCards', 'AttendanceEvents', 'Timekeeping', 'Payroll');

SELECT 'duplicate_employee_work_dates' AS check_name,
       COUNT(*) AS actual_count,
       0 AS expected_count,
       IF(COUNT(*) = 0, 'PASS', 'FAIL') AS result
FROM (
  SELECT employeeId, workDate
  FROM Timekeeping
  GROUP BY employeeId, workDate
  HAVING COUNT(*) > 1
) duplicates;

SELECT 'orphan_employee_cards' AS check_name,
       COUNT(*) AS actual_count,
       0 AS expected_count,
       IF(COUNT(*) = 0, 'PASS', 'FAIL') AS result
FROM EmployeeCards c
LEFT JOIN Employees e ON e.id = c.employeeId
WHERE e.id IS NULL;

SELECT 'orphan_attendance_events' AS check_name,
       COUNT(*) AS actual_count,
       0 AS expected_count,
       IF(COUNT(*) = 0, 'PASS', 'FAIL') AS result
FROM AttendanceEvents a
LEFT JOIN Employees e ON e.id = a.employeeId
WHERE e.id IS NULL;

SELECT 'orphan_timekeeping_rows' AS check_name,
       COUNT(*) AS actual_count,
       0 AS expected_count,
       IF(COUNT(*) = 0, 'PASS', 'FAIL') AS result
FROM Timekeeping t
LEFT JOIN Employees e ON e.id = t.employeeId
WHERE e.id IS NULL;

SELECT 'orphan_payroll_rows' AS check_name,
       COUNT(*) AS actual_count,
       0 AS expected_count,
       IF(COUNT(*) = 0, 'PASS', 'FAIL') AS result
FROM Payroll p
LEFT JOIN Employees e ON e.id = p.employeeId
WHERE e.id IS NULL;

SELECT source, COUNT(*) AS event_count, MIN(eventTime) AS first_event,
       MAX(eventTime) AS last_event
FROM AttendanceEvents
GROUP BY source
ORDER BY source;
