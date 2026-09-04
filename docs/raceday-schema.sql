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

CREATE TABLE Events (
    EventID INT IDENTITY(1,1) PRIMARY KEY,
    EventName VARCHAR(100) NOT NULL,
    Description VARCHAR(500) NULL,
    EventDate DATETIME NOT NULL,
    Location VARCHAR(150) NOT NULL,
    Distance DECIMAL(6,2) NOT NULL,
    EventTypeID INT NOT NULL,
    OrganiserID INT NOT NULL,
    CreateAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Events_EventTypes FOREIGN KEY (EventTypeID) REFERENCES EventTypes(EventTypeID),
    CONSTRAINT FK_Events_Users FOREIGN KEY (OrganiserID) REFERENCES Users(UserID)
);
