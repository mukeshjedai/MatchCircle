# Database Update Instructions

## Problem
The application is trying to access `message` and `status` columns in the `user_interactions` table that don't exist yet.

## Solution
Run the SQL script `add_connect_request_columns.sql` against your PostgreSQL database.

## Option 1: Using psql (Command Line)

If you have `psql` installed, you can run:

```bash
psql "Host=travelapp-mukeshbcc56-f406.g.aivencloud.com;Port=23377;Database=defaultdb;Username=avnadmin;Password=AVNS_BKQddwFKbOE_3CQpyf4;SSL Mode=Require;" -f add_connect_request_columns.sql
```

## Option 2: Using pgAdmin or DBeaver

1. Connect to your PostgreSQL database using the connection string from `appsettings.json`
2. Open a new SQL query window
3. Copy and paste the contents of `add_connect_request_columns.sql`
4. Execute the script

## Option 3: Using Aiven Console

1. Log into your Aiven account
2. Navigate to your PostgreSQL service
3. Open the "SQL" or "Query" tab
4. Copy and paste the contents of `add_connect_request_columns.sql`
5. Execute the script

## What the Script Does

The script will:
- Add `message` column (TEXT) to store optional messages in connect requests
- Add `status` column (VARCHAR(20)) with default value 'pending' to track request status
- Update existing records to have 'pending' status
- Create indexes for better query performance

## Verification

After running the script, you can verify the columns were added by running:

```sql
SELECT column_name, data_type, column_default 
FROM information_schema.columns 
WHERE table_schema = 'best' 
  AND table_name = 'user_interactions'
  AND column_name IN ('message', 'status');
```

You should see both `message` and `status` columns listed.

