-- Migration: Add image_urls column to RatingFilm table
-- Description: Add support for storing up to 3 image URLs for movie reviews
-- Date: 2024

USE [ExpressTicketDB]
GO

-- Add image_urls column to RatingFilm table
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.RatingFilm') 
    AND name = 'image_urls'
)
BEGIN
    ALTER TABLE [dbo].[RatingFilm]
    ADD [image_urls] NVARCHAR(MAX) NULL;
    
    PRINT 'Column image_urls added successfully to RatingFilm table';
END
ELSE
BEGIN
    PRINT 'Column image_urls already exists in RatingFilm table';
END
GO










