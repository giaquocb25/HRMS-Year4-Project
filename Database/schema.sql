CREATE DATABASE IF NOT EXISTS `hmrs`
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;

USE `hmrs`;

CREATE TABLE IF NOT EXISTS `Employees` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `name` VARCHAR(150) NOT NULL,
  `employeeCode` VARCHAR(30) NULL,
  `department` VARCHAR(100) NULL,
  `dateOfBirth` DATE NULL,
  `gender` VARCHAR(20) NULL,
  `phone` VARCHAR(20) NULL,
  `email` VARCHAR(150) NULL,
  `employmentStatus` VARCHAR(30) NULL DEFAULT 'Active',
  `age` VARCHAR(3) NULL,
  `address` VARCHAR(255) NULL,
  `salary` INT NOT NULL DEFAULT 0,
  `jobTitle` VARCHAR(100) NULL,
  `image` LONGBLOB NULL,
  `createdTime` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updatedTime` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `UX_Employees_EmployeeCode` (`employeeCode`),
  UNIQUE KEY `UX_Employees_Email` (`email`),
  CONSTRAINT `CK_Employees_Salary` CHECK (`salary` >= 0)
) ENGINE=InnoDB;

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

CREATE TABLE IF NOT EXISTS `Timekeeping` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `employeeId` INT NOT NULL,
  `arriveTime` TIME NOT NULL DEFAULT '00:00:00',
  `leaveTime` TIME NOT NULL DEFAULT '00:00:00',
  `workDate` DATE NOT NULL,
  `createdTime` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updatedTime` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `UX_Timekeeping_Employee_Date` (`employeeId`, `workDate`),
  CONSTRAINT `FK_Timekeeping_Employees`
    FOREIGN KEY (`employeeId`) REFERENCES `Employees` (`id`)
    ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `Payroll` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `employeeId` INT NOT NULL,
  `workdays` INT NOT NULL DEFAULT 0,
  `amount` INT NOT NULL DEFAULT 0,
  `month` CHAR(7) NOT NULL,
  `createdTime` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updatedTime` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `UX_Payroll_Employee_Month` (`employeeId`, `month`),
  CONSTRAINT `FK_Payroll_Employees`
    FOREIGN KEY (`employeeId`) REFERENCES `Employees` (`id`)
    ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `CK_Payroll_Workdays` CHECK (`workdays` >= 0),
  CONSTRAINT `CK_Payroll_Amount` CHECK (`amount` >= 0)
) ENGINE=InnoDB;
