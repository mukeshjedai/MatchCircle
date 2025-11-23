# Fix Google OAuth Redirect URI Error - Step by Step

## The Problem
You're getting this error because `http://127.0.0.1:5179/signin-google` is not registered in your Google Cloud Console OAuth credentials.

## Solution: Add Redirect URI to Google Cloud Console

### Step 1: Open Google Cloud Console
1. Go to: https://console.cloud.google.com/
2. Make sure you're signed in with the Google account that created the OAuth credentials
3. Select the correct project (the one where you created the OAuth Client ID)

### Step 2: Navigate to Credentials
1. In the left sidebar, click **APIs & Services**
2. Click **Credentials** (or go directly to: https://console.cloud.google.com/apis/credentials)

### Step 3: Find Your OAuth 2.0 Client ID
1. Look for your OAuth 2.0 Client ID in the list
2. Your Client ID should be: `1058124648124-bnoig638h4v8bcagu26avep1fc64ugqh`
3. Click on the **pencil icon** (Edit) or click on the Client ID name to edit it

### Step 4: Add Authorized Redirect URIs
1. Scroll down to the **Authorized redirect URIs** section
2. Click **+ ADD URI** button
3. Add each of these URIs one by one (click ADD URI for each):

```
http://127.0.0.1:5179/signin-google
http://localhost:5179/signin-google
https://localhost:7019/signin-google
http://localhost:22849/signin-google
https://localhost:44339/signin-google
```

**Important:**
- Copy and paste each URI exactly as shown
- The path `/signin-google` must match exactly (case-sensitive)
- Add ALL of them to avoid future issues
- Make sure there are no extra spaces

### Step 5: Save Changes
1. Scroll to the bottom of the page
2. Click **SAVE** button
3. Wait for the confirmation message

### Step 6: Wait for Propagation
- Google's changes can take **1-5 minutes** to propagate
- Don't try to sign in immediately after saving
- Wait at least 2 minutes, then try again

### Step 7: Clear Browser Cache (Optional but Recommended)
1. Clear your browser cache and cookies for the site
2. Or try in an incognito/private window
3. This ensures you're not using cached OAuth settings

### Step 8: Test Again
1. Restart your application (if it's running)
2. Go to the login page
3. Click "Sign in with Google"
4. It should work now!

## Troubleshooting

### Still Getting the Error?
1. **Double-check the URI**: Make sure you copied it exactly, including `http://` and the port number
2. **Check you're editing the right Client ID**: Verify the Client ID matches `1058124648124-bnoig638h4v8bcagu26avep1fc64ugqh`
3. **Wait longer**: Sometimes it takes up to 10 minutes for changes to propagate
4. **Check for typos**: Make sure there are no spaces or typos in the redirect URI
5. **Try a different browser**: Sometimes browser cache can cause issues

### Common Mistakes
- ❌ Adding `http://127.0.0.1:5179` (missing `/signin-google`)
- ❌ Adding `http://localhost:5179/signin-google` but not `http://127.0.0.1:5179/signin-google`
- ❌ Using HTTPS when the app runs on HTTP
- ❌ Wrong port number
- ❌ Extra spaces or typos

### Verify Your Settings
After adding, your **Authorized redirect URIs** section should look like this:

```
http://127.0.0.1:5179/signin-google
http://localhost:5179/signin-google
https://localhost:7019/signin-google
http://localhost:22849/signin-google
https://localhost:44339/signin-google
```

## Quick Reference

**Your OAuth Client ID:** `1058124648124-bnoig638h4v8bcagu26avep1fc64ugqh`

**Redirect URIs to Add:**
- `http://127.0.0.1:5179/signin-google` ← **This is the one causing the error**
- `http://localhost:5179/signin-google`
- `https://localhost:7019/signin-google`
- `http://localhost:22849/signin-google`
- `https://localhost:44339/signin-google`

## Direct Link (if you're logged in)
https://console.cloud.google.com/apis/credentials

