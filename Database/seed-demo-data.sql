USE `hmrs`;

START TRANSACTION;

INSERT INTO `Employees`
(`employeeCode`, `name`, `department`, `dateOfBirth`, `gender`, `phone`, `email`,
 `employmentStatus`, `age`, `address`, `salary`, `jobTitle`)
VALUES
('DEMO-001', 'Nguyễn Minh An',  'Ban Giám đốc', '1985-04-12', 'Nam',  '0900000001', 'demo001@hrms-demo.local', 'Active',   '41', 'Thành phố Hồ Chí Minh', 35000000, 'Giám đốc vận hành'),
('DEMO-002', 'Trần Thu Bình',   'Nhân sự',      '1992-08-21', 'Nữ',   '0900000002', 'demo002@hrms-demo.local', 'Active',   '34', 'Thành phố Hồ Chí Minh', 18000000, 'Trưởng phòng nhân sự'),
('DEMO-003', 'Lê Quốc Cường',   'Công nghệ',    '1996-02-15', 'Nam',  '0900000003', 'demo003@hrms-demo.local', 'Active',   '30', 'Thành phố Thủ Đức',     22000000, 'Lập trình viên'),
('DEMO-004', 'Phạm Ngọc Dung',  'Kế toán',      '1994-11-03', 'Nữ',   '0900000004', 'demo004@hrms-demo.local', 'Active',   '31', 'Quận Bình Thạnh',        17000000, 'Kế toán tổng hợp'),
('DEMO-005', 'Hoàng Gia Huy',   'Kinh doanh',   '1998-06-27', 'Nam',  '0900000005', 'demo005@hrms-demo.local', 'Active',   '28', 'Quận Gò Vấp',            15000000, 'Chuyên viên kinh doanh'),
('DEMO-006', 'Võ Khánh Linh',   'Marketing',    '1999-01-19', 'Nữ',   '0900000006', 'demo006@hrms-demo.local', 'Active',   '27', 'Quận 7',                 16000000, 'Chuyên viên nội dung'),
('DEMO-007', 'Đặng Tuấn Long',  'Công nghệ',    '1995-09-08', 'Nam',  '0900000007', 'demo007@hrms-demo.local', 'Active',   '31', 'Thành phố Thủ Đức',     24000000, 'Kỹ sư hệ thống'),
('DEMO-008', 'Bùi Mai Phương',  'Chăm sóc KH',  '1997-12-30', 'Nữ',   '0900000008', 'demo008@hrms-demo.local', 'Active',   '28', 'Quận Tân Bình',          14000000, 'Chuyên viên CSKH'),
('DEMO-009', 'Đỗ Anh Quân',     'Kho vận',      '1993-03-11', 'Nam',  '0900000009', 'demo009@hrms-demo.local', 'Active',   '33', 'Huyện Bình Chánh',       14500000, 'Điều phối kho'),
('DEMO-010', 'Hồ Thảo Vy',      'Nhân sự',      '2000-07-05', 'Nữ',   '0900000010', 'demo010@hrms-demo.local', 'OnLeave',  '26', 'Quận Phú Nhuận',         13000000, 'Chuyên viên tuyển dụng'),
('DEMO-011', 'Ngô Đức Thành',   'Bảo trì',      '1991-10-14', 'Nam',  '0900000011', 'demo011@hrms-demo.local', 'Active',   '34', 'Thành phố Dĩ An',        15500000, 'Kỹ thuật viên'),
('DEMO-012', 'Dương Mỹ Hạnh',   'Kinh doanh',   '1996-05-24', 'Nữ',   '0900000012', 'demo012@hrms-demo.local', 'Inactive', '30', 'Thành phố Thuận An',    15000000, 'Chuyên viên kinh doanh')
ON DUPLICATE KEY UPDATE
  `name` = VALUES(`name`),
  `department` = VALUES(`department`),
  `dateOfBirth` = VALUES(`dateOfBirth`),
  `gender` = VALUES(`gender`),
  `phone` = VALUES(`phone`),
  `email` = VALUES(`email`),
  `employmentStatus` = VALUES(`employmentStatus`),
  `age` = VALUES(`age`),
  `address` = VALUES(`address`),
  `salary` = VALUES(`salary`),
  `jobTitle` = VALUES(`jobTitle`);

INSERT INTO `EmployeeCards` (`cardUid`, `employeeId`, `isActive`)
SELECT CONCAT('CARD-DEMO-', LPAD(SUBSTRING(`employeeCode`, 6), 3, '0')), `id`,
       IF(`employmentStatus` = 'Inactive', 0, 1)
FROM `Employees`
WHERE `employeeCode` LIKE 'DEMO-%'
ON DUPLICATE KEY UPDATE
  `employeeId` = VALUES(`employeeId`),
  `isActive` = VALUES(`isActive`);

INSERT INTO `Timekeeping` (`employeeId`, `arriveTime`, `leaveTime`, `workDate`)
WITH RECURSIVE `demo_dates` AS (
  SELECT DATE('2026-09-01') AS `workDate`
  UNION ALL
  SELECT DATE_ADD(`workDate`, INTERVAL 1 DAY)
  FROM `demo_dates`
  WHERE `workDate` < DATE('2026-09-10')
)
SELECT
  e.`id`,
  ADDTIME('08:00:00', SEC_TO_TIME(CAST(MOD(CRC32(CONCAT(e.`employeeCode`, d.`workDate`, 'IN')), 1201) AS SIGNED) - 600)),
  ADDTIME('17:00:00', SEC_TO_TIME(CAST(MOD(CRC32(CONCAT(e.`employeeCode`, d.`workDate`, 'OUT')), 1801) AS SIGNED) - 600)),
  d.`workDate`
FROM `Employees` e
CROSS JOIN `demo_dates` d
WHERE e.`employeeCode` LIKE 'DEMO-%'
  AND e.`employmentStatus` <> 'Inactive'
  AND DAYOFWEEK(d.`workDate`) NOT IN (1, 7)
ON DUPLICATE KEY UPDATE
  `arriveTime` = VALUES(`arriveTime`),
  `leaveTime` = VALUES(`leaveTime`);

INSERT INTO `AttendanceEvents`
(`employeeId`, `eventTime`, `source`, `externalEventId`, `deviceName`, `rawData`, `createdBy`)
SELECT t.`employeeId`, TIMESTAMP(t.`workDate`, t.`arriveTime`), 'Demo',
       CONCAT('demo:', e.`employeeCode`, ':', DATE_FORMAT(t.`workDate`, '%Y%m%d'), ':in'),
       'Máy chấm công mẫu', '{"kind":"check-in","demo":true}', 'seed-demo'
FROM `Timekeeping` t
JOIN `Employees` e ON e.`id` = t.`employeeId`
WHERE e.`employeeCode` LIKE 'DEMO-%'
ON DUPLICATE KEY UPDATE
  `eventTime` = VALUES(`eventTime`),
  `deviceName` = VALUES(`deviceName`),
  `rawData` = VALUES(`rawData`);

INSERT INTO `AttendanceEvents`
(`employeeId`, `eventTime`, `source`, `externalEventId`, `deviceName`, `rawData`, `createdBy`)
SELECT t.`employeeId`, TIMESTAMP(t.`workDate`, t.`leaveTime`), 'Demo',
       CONCAT('demo:', e.`employeeCode`, ':', DATE_FORMAT(t.`workDate`, '%Y%m%d'), ':out'),
       'Máy chấm công mẫu', '{"kind":"check-out","demo":true}', 'seed-demo'
FROM `Timekeeping` t
JOIN `Employees` e ON e.`id` = t.`employeeId`
WHERE e.`employeeCode` LIKE 'DEMO-%'
ON DUPLICATE KEY UPDATE
  `eventTime` = VALUES(`eventTime`),
  `deviceName` = VALUES(`deviceName`),
  `rawData` = VALUES(`rawData`);

INSERT INTO `Payroll` (`employeeId`, `workdays`, `amount`, `month`)
SELECT
  e.`id`,
  COUNT(DISTINCT t.`workDate`),
  ROUND(e.`salary` * COUNT(DISTINCT t.`workDate`) / 22),
  '2026-09'
FROM `Employees` e
JOIN `Timekeeping` t ON t.`employeeId` = e.`id`
  AND t.`workDate` >= '2026-09-01' AND t.`workDate` < '2026-10-01'
WHERE e.`employeeCode` LIKE 'DEMO-%'
  AND e.`employmentStatus` <> 'Inactive'
GROUP BY e.`id`, e.`salary`
ON DUPLICATE KEY UPDATE
  `workdays` = VALUES(`workdays`),
  `amount` = VALUES(`amount`);

INSERT INTO `SystemAuditLogs`
(`username`, `action`, `entityType`, `entityId`, `details`)
SELECT 'root', 'DEMO_DATA_SEED', 'Database', 'hmrs',
       CONCAT('Employees=', demo.`employeeCount`, '; generated by Database/seed-demo-data.sql')
FROM (
  SELECT COUNT(*) AS `employeeCount`
  FROM `Employees`
  WHERE `employeeCode` LIKE 'DEMO-%'
) demo
WHERE NOT EXISTS (
  SELECT 1 FROM `SystemAuditLogs`
  WHERE `action` = 'DEMO_DATA_SEED' AND `entityId` = 'hmrs'
);

COMMIT;

SELECT 'Employees' AS `dataset`, COUNT(*) AS `rows`
FROM `Employees` WHERE `employeeCode` LIKE 'DEMO-%'
UNION ALL
SELECT 'EmployeeCards', COUNT(*)
FROM `EmployeeCards` WHERE `cardUid` LIKE 'CARD-DEMO-%'
UNION ALL
SELECT 'AttendanceEvents', COUNT(*)
FROM `AttendanceEvents` WHERE `source` = 'Demo'
UNION ALL
SELECT 'Timekeeping', COUNT(*)
FROM `Timekeeping` t JOIN `Employees` e ON e.`id` = t.`employeeId`
WHERE e.`employeeCode` LIKE 'DEMO-%'
UNION ALL
SELECT 'Payroll', COUNT(*)
FROM `Payroll` p JOIN `Employees` e ON e.`id` = p.`employeeId`
WHERE e.`employeeCode` LIKE 'DEMO-%';
