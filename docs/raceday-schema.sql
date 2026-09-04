-- ============================================
-- RaceDay Database Schema
-- ============================================

CREATE TABLE Users (
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    FirstName VARCHAR(50) NOT NULL,
    LastName VARCHAR(50) NOT NULL,
    Email VARCHAR(100) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    Role VARCHAR(20) NOT NULL CHECK (Role IN ('Organiser', 'Participant')),
    PhoneNumber VARCHAR(20) NULL,
    CreateAt DATETIME NOT NULL DEFAULT GETDATE()
);

CREATE TABLE EventTypes (
    EventTypeID INT IDENTITY(1,1) PRIMARY KEY,
    TypeName VARCHAR(20) NOT NULL UNIQUE
);
