ALTER DATABASE CURRENT
SET CHANGE_TRACKING = ON
(CHANGE_RETENTION = 2 DAYS, AUTO_CLEANUP = ON)

CREATE TABLE Hotels (
    [_id] nvarchar(450) NOT NULL PRIMARY KEY,
    [BaseRate] float NULL,
    [Category] nvarchar(max) NULL,
    [Description] nvarchar(max) NULL,
    [Description_fr] nvarchar(max) NULL,
    [HotelName] nvarchar(max) NULL,
    [Amenities] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [LastRenovationDate] DateTime NULL,
    [ParkingIncluded] bit NULL,
    [Rating] int NULL,
    [SmokingAllowed] bit NULL,
    [Location] geography NULL,
)
GO

ALTER TABLE Hotels
ENABLE CHANGE_TRACKING
WITH (TRACK_COLUMNS_UPDATED = ON)

INSERT INTO Hotels (_id, BaseRate, Category, Description, description_fr, HotelName, Amenities, IsDeleted, LastRenovationDate, ParkingIncluded, Rating, SmokingAllowed, Location)
VALUES (N'1', 199, N'Luxury', N'Best hotel in town', N'Meilleur hôtel en ville', N'Fancy Stay', N'["pool", "view", "wifi", "concierge"]', 0, N'2010-06-27', 0, 5, 0, geography::Point(47.678581, -122.131577, 4326))

INSERT INTO Hotels (_id, BaseRate, Category, Description, description_fr, HotelName, Amenities, IsDeleted, LastRenovationDate, ParkingIncluded, Rating, SmokingAllowed, Location)
VALUES (N'2', 79.99, N'Budget', N'Cheapest hotel in town', N'Hôtel le moins cher en ville', N'Roach Motel', N'["motel", "budget"]', 0, N'1982-04-28', 1, 1, 1, geography::Point(49.678581, -122.131577, 4326))

INSERT INTO Hotels (_id, BaseRate, Category, Description, description_fr, HotelName, Amenities, IsDeleted, LastRenovationDate, ParkingIncluded, Rating, SmokingAllowed, Location)
VALUES (N'3', 129.99, N'Suite', N'Close to town hall and the river', NULL, N'Arcadia Resort & Restaurant', NULL, 0, NULL, NULL, NULL, NULL, NULL)
GO
