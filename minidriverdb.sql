-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- Host: 127.0.0.1
-- Generation Time: May 15, 2026 at 08:43 PM
-- Server version: 10.4.32-MariaDB
-- PHP Version: 8.0.30

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Database: `minidriverdb`
--

-- --------------------------------------------------------

--
-- Table structure for table `sharedfiles`
--

CREATE TABLE `sharedfiles` (
  `FileID` int(11) NOT NULL,
  `FileName` varchar(255) NOT NULL,
  `FileSize` bigint(20) NOT NULL,
  `UploadDate` datetime DEFAULT current_timestamp(),
  `OwnerID` int(11) NOT NULL,
  `LocalPath` varchar(500) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `userpermissions`
--

CREATE TABLE `userpermissions` (
  `PermissionID` int(11) NOT NULL,
  `OwnerID` int(11) DEFAULT NULL,
  `SharedWithID` int(11) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `users`
--

CREATE TABLE `users` (
  `UserID` int(11) NOT NULL,
  `Username` varchar(50) NOT NULL,
  `Password` varchar(255) NOT NULL,
  `UserFolder` varchar(500) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `users`
--

INSERT INTO `users` (`UserID`, `Username`, `Password`, `UserFolder`) VALUES
(1, 'admin', '123', 'User_Admin'),
(2, 'beo', '123', 'tuan_beo_folder'),
(3, 'thong', '123', 'van_thong_folder'),
(4, 'test', '123', 'test_user_folder');

--
-- Indexes for dumped tables
--

--
-- Indexes for table `sharedfiles`
--
ALTER TABLE `sharedfiles`
  ADD PRIMARY KEY (`FileID`),
  ADD KEY `OwnerID` (`OwnerID`);

--
-- Indexes for table `userpermissions`
--
ALTER TABLE `userpermissions`
  ADD PRIMARY KEY (`PermissionID`),
  ADD KEY `OwnerID` (`OwnerID`),
  ADD KEY `SharedWithID` (`SharedWithID`);

--
-- Indexes for table `users`
--
ALTER TABLE `users`
  ADD PRIMARY KEY (`UserID`),
  ADD UNIQUE KEY `Username` (`Username`);

--
-- AUTO_INCREMENT for dumped tables
--

--
-- AUTO_INCREMENT for table `sharedfiles`
--
ALTER TABLE `sharedfiles`
  MODIFY `FileID` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `userpermissions`
--
ALTER TABLE `userpermissions`
  MODIFY `PermissionID` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `users`
--
ALTER TABLE `users`
  MODIFY `UserID` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=5;

--
-- Constraints for dumped tables
--

--
-- Constraints for table `sharedfiles`
--
ALTER TABLE `sharedfiles`
  ADD CONSTRAINT `sharedfiles_ibfk_1` FOREIGN KEY (`OwnerID`) REFERENCES `users` (`UserID`);

--
-- Constraints for table `userpermissions`
--
ALTER TABLE `userpermissions`
  ADD CONSTRAINT `userpermissions_ibfk_1` FOREIGN KEY (`OwnerID`) REFERENCES `users` (`UserID`),
  ADD CONSTRAINT `userpermissions_ibfk_2` FOREIGN KEY (`SharedWithID`) REFERENCES `users` (`UserID`);
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
