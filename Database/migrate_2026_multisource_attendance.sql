USE `hmrs`;

-- Các helper giúp migration chạy lại an toàn nếu lần chạy trước bị dừng giữa chừng.
DROP PROCEDURE IF EXISTS `hrms_add_column_if_missing`;
DROP PROCEDURE IF EXISTS `hrms_add_unique_index_if_missing`;

DELIMITER $$
CREATE PROCEDURE `hrms_add_column_if_missing`(
  IN p_table VARCHAR(64), IN p_column VARCHAR(64), IN p_definition TEXT)
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = DATABASE()
      AND table_name = p_table
      AND column_name = p_column
  ) THEN
    SET @hrms_ddl = CONCAT('ALTER TABLE `', p_table, '` ADD COLUMN `', p_column, '` ', p_definition);
    PREPARE hrms_statement FROM @hrms_ddl;
    EXECUTE hrms_statement;
    DEALLOCATE PREPARE hrms_statement;
  END IF;
END$$

CREATE PROCEDURE `hrms_add_unique_index_if_missing`(
  IN p_table VARCHAR(64), IN p_index VARCHAR(64), IN p_columns VARCHAR(255))
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.statistics
    WHERE table_schema = DATABASE()
      AND table_name = p_table
      AND index_name = p_index
  ) THEN
    SET @hrms_ddl = CONCAT('ALTER TABLE `', p_table, '` ADD UNIQUE KEY `', p_index, '` (', p_columns, ')');
    PREPARE hrms_statement FROM @hrms_ddl;
    EXECUTE hrms_statement;
    DEALLOCATE PREPARE hrms_statement;
  END IF;
END$$
DELIMITER ;

CALL `hrms_add_column_if_missing`('Employees', 'employeeCode', 'VARCHAR(30) NULL AFTER `name`');
CALL `hrms_add_column_if_missing`('Employees', 'department', 'VARCHAR(100) NULL AFTER `employeeCode`');
CALL `hrms_add_column_if_missing`('Employees', 'dateOfBirth', 'DATE NULL AFTER `department`');
CALL `hrms_add_column_if_missing`('Employees', 'gender', 'VARCHAR(20) NULL AFTER `dateOfBirth`');
CALL `hrms_add_column_if_missing`('Employees', 'phone', 'VARCHAR(20) NULL AFTER `gender`');
CALL `hrms_add_column_if_missing`('Employees', 'email', 'VARCHAR(150) NULL AFTER `phone`');
CALL `hrms_add_column_if_missing`('Employees', 'employmentStatus', 'VARCHAR(30) NULL DEFAULT ''Active'' AFTER `email`');
CALL `hrms_add_unique_index_if_missing`('Employees', 'UX_Employees_EmployeeCode', '`employeeCode`');
CALL `hrms_add_unique_index_if_missing`('Employees', 'UX_Employees_Email', '`email`');

CREATE TABLE IF NOT EXISTS `UserRoles` (
  `username` VARCHAR(100) NOT NULL,
  `role` ENUM('Admin', 'HR', 'Payroll', 'Timekeeper', 'Viewer') NOT NULL DEFAULT 'Viewer',
  `isActive` TINYINT(1) NOT NULL DEFAULT 1,
  `createdTime` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updatedTime` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`username`)
) ENGINE=InnoDB;

INSERT IGNORE INTO `UserRoles` (`username`, `role`) VALUES
  ('root', 'Admin'),
  ('hrms_app', 'Admin');

CREATE TABLE IF NOT EXISTS `EmployeeCards` (
  `cardUid` VARCHAR(100) NOT NULL,
  `employeeId` INT NOT NULL,
  `isActive` TINYINT(1) NOT NULL DEFAULT 1,
  `createdTime` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updatedTime` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`cardUid`),
  UNIQUE KEY `UX_EmployeeCards_Employee` (`employeeId`),
  CONSTRAINT `FK_EmployeeCards_Employees`
    FOREIGN KEY (`employeeId`) REFERENCES `Employees` (`id`)
    ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `AttendanceEvents` (
  `id` BIGINT NOT NULL AUTO_INCREMENT,
  `employeeId` INT NOT NULL,
  `eventTime` DATETIME NOT NULL,
  `source` VARCHAR(30) NOT NULL,
  `externalEventId` VARCHAR(160) NOT NULL,
  `deviceName` VARCHAR(100) NULL,
  `rawData` TEXT NULL,
  `createdBy` VARCHAR(100) NULL,
  `createdTime` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `UX_AttendanceEvents_Source_External` (`source`, `externalEventId`),
  KEY `IX_AttendanceEvents_Employee_Time` (`employeeId`, `eventTime`),
  CONSTRAINT `FK_AttendanceEvents_Employees`
    FOREIGN KEY (`employeeId`) REFERENCES `Employees` (`id`)
    ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `SystemAuditLogs` (
  `id` BIGINT NOT NULL AUTO_INCREMENT,
  `username` VARCHAR(100) NULL,
  `action` VARCHAR(50) NOT NULL,
  `entityType` VARCHAR(50) NULL,
  `entityId` VARCHAR(100) NULL,
  `details` VARCHAR(1000) NULL,
  `createdTime` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `IX_SystemAuditLogs_CreatedTime` (`createdTime`),
  KEY `IX_SystemAuditLogs_Username` (`username`)
) ENGINE=InnoDB;

-- AttendanceService cần một bản ghi tổng hợp cho mỗi nhân viên/ngày.
-- Nếu lệnh này báo dữ liệu trùng, hãy đối soát/gộp các dòng trùng rồi chạy lại toàn bộ file.
CALL `hrms_add_unique_index_if_missing`(
  'Timekeeping', 'UX_Timekeeping_Employee_Date', '`employeeId`, `workDate`');

DROP PROCEDURE IF EXISTS `hrms_add_column_if_missing`;
DROP PROCEDURE IF EXISTS `hrms_add_unique_index_if_missing`;
