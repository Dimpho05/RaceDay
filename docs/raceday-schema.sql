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

CREATE TABLE Categories (
    CategoryID INT IDENTITY(1,1) PRIMARY KEY,
    EventID INT NOT NULL,
    CategoryName VARCHAR(50) NOT NULL,
    MinimumAge INT NULL,
    MaximumAge INT NULL,
    CategoryDistance DECIMAL(6,2) NULL,
    CONSTRAINT FK_Categories_Events FOREIGN KEY (EventID) REFERENCES Events(EventID)
);

CREATE TABLE Enrolments (
    EnrolmentID INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantID INT NOT NULL,
    EventID INT NOT NULL,
    CategoryID INT NOT NULL,
    EnrolmentDate DATETIME NOT NULL DEFAULT GETDATE(),
    EnrolmentStatus VARCHAR(20) NOT NULL DEFAULT 'Active',
    CONSTRAINT FK_Enrolments_Users FOREIGN KEY (ParticipantID) REFERENCES Users(UserID),
    CONSTRAINT FK_Enrolments_Events FOREIGN KEY (EventID) REFERENCES Events(EventID),
    CONSTRAINT FK_Enrolments_Categories FOREIGN KEY (CategoryID) REFERENCES Categories(CategoryID),
    CONSTRAINT UQ_Enrolments_Participant_Event UNIQUE (ParticipantID, EventID)
);

CREATE TABLE Results (
    ResultID INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentID INT NOT NULL UNIQUE,
    FinishTime TIME NOT NULL,
    FinishingPosition INT NOT NULL,
    ResultDate DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Results_Enrolments FOREIGN KEY (EnrolmentID) REFERENCES Enrolments(EnrolmentID)
);

-- ============================================
-- Seed Data
-- ============================================

-- EventTypes
INSERT INTO EventTypes (TypeName) VALUES
('Run'), ('Walk'), ('Cycle');

-- Users: 2 Organisers, 2 Participants
-- Note: PasswordHash values below are placeholder bcrypt-style hashes representing "Password123!"
INSERT INTO Users (FirstName, LastName, Email, PasswordHash, Role, PhoneNumber) VALUES
('John', 'Mokoena', 'john.mokoena@raceday.co.za', '$2a$11$KIXQ7JZ8vQ3nZ5x8yQ8yMuHASHPLACEHOLDER01', 'Organiser', '0821234567'),
('Sarah', 'van der Merwe', 'sarah.vdm@raceday.co.za', '$2a$11$KIXQ7JZ8vQ3nZ5x8yQ8yMuHASHPLACEHOLDER02', 'Organiser', '0827654321'),
('Thabo', 'Nkosi', 'thabo.nkosi@example.com', '$2a$11$KIXQ7JZ8vQ3nZ5x8yQ8yMuHASHPLACEHOLDER03', 'Participant', '0731122334'),
('Emma', 'Botha', 'emma.botha@example.com', '$2a$11$KIXQ7JZ8vQ3nZ5x8yQ8yMuHASHPLACEHOLDER04', 'Participant', '0739988776');

-- Events: EventTypeID 1=Run, 2=Walk, 3=Cycle | OrganiserID 1=John, 2=Sarah
INSERT INTO Events (EventName, Description, EventDate, Location, Distance, EventTypeID, OrganiserID) VALUES
('Tshwane Half Marathon', 'Annual road race through the streets of Pretoria.', '2026-10-18 06:00:00', 'Pretoria, Gauteng', 21.10, 1, 1),
('Joburg Charity Walk', 'Community fundraising walk in support of local schools.', '2026-11-01 07:00:00', 'Johannesburg, Gauteng', 5.00, 2, 2),
('Cape Winelands Cycle Challenge', 'Scenic road cycling race through the Cape Winelands.', '2026-11-22 06:30:00', 'Stellenbosch, Western Cape', 100.00, 3, 1);

-- Categories: EventID 1=Tshwane Half Marathon, 2=Joburg Charity Walk, 3=Cape Winelands Cycle Challenge
INSERT INTO Categories (EventID, CategoryName, MinimumAge, MaximumAge, CategoryDistance) VALUES
(1, '21km Open', 18, NULL, 21.10),
(1, '10km Fun Run', NULL, NULL, 10.00),
(2, '5km Walk', NULL, NULL, 5.00),
(2, 'Under 20', NULL, 19, 5.00),
(3, '50km Road', 16, NULL, 50.00),
(3, '100km Road', 18, NULL, 100.00);

-- Enrolments: ParticipantID 3=Thabo, 4=Emma | CategoryID per table above
INSERT INTO Enrolments (ParticipantID, EventID, CategoryID, EnrolmentStatus) VALUES
(3, 1, 1, 'Active'),  -- Thabo -> Tshwane Half Marathon, 21km Open
(4, 1, 2, 'Active'),  -- Emma  -> Tshwane Half Marathon, 10km Fun Run
(4, 2, 3, 'Active'),  -- Emma  -> Joburg Charity Walk, 5km Walk
(3, 3, 6, 'Active');  -- Thabo -> Cape Winelands Cycle Challenge, 100km Road

-- Results: EnrolmentID 1=Thabo/Tshwane, 3=Emma/Joburg Walk
INSERT INTO Results (EnrolmentID, FinishTime, FinishingPosition) VALUES
(1, '01:38:42', 14),
(3, '00:42:10', 8);
