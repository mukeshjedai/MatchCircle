-- Add message and status columns to user_interactions table
-- Run this SQL script against your PostgreSQL database

ALTER TABLE best.user_interactions 
ADD COLUMN IF NOT EXISTS message TEXT,
ADD COLUMN IF NOT EXISTS status VARCHAR(20) DEFAULT 'pending';

-- Update existing records to have 'pending' status if they don't have one
UPDATE best.user_interactions 
SET status = 'pending' 
WHERE status IS NULL;

-- Add index on status for better query performance
CREATE INDEX IF NOT EXISTS idx_user_interactions_status 
ON best.user_interactions(status);

-- Add index on interaction_type and status for connect requests
CREATE INDEX IF NOT EXISTS idx_user_interactions_type_status 
ON best.user_interactions(interaction_type, status);

