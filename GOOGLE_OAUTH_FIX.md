# Fix Google OAuth Redirect URI Mismatch Error

## Error
**Error 400: redirect_uri_mismatch**

The redirect URI `http://127.0.0.1:5179/signin-google` is not registered in your Google Cloud Console.

## Solution

You need to add the following redirect URIs to your Google OAuth 2.0 Client ID in Google Cloud Console:

### Step 1: Go to Google Cloud Console

1. Visit [Google Cloud Console](https://console.cloud.google.com/)
2. Select your project
3. Navigate to **APIs & Services** > **Credentials**
4. Click on your OAuth 2.0 Client ID (the one with Client ID: `1058124648124-bnoig638h4v8bcagu26avep1fc64ugqh`)

### Step 2: Add Authorized Redirect URIs

In the **Authorized redirect URIs** section, add ALL of the following:

```
http://localhost:5179/signin-google
http://127.0.0.1:5179/signin-google
https://localhost:7019/signin-google
http://localhost:22849/signin-google
https://localhost:44339/signin-google
```

**Important Notes:**
- Add ALL of these URIs (both localhost and 127.0.0.1 versions)
- The path `/signin-google` must match exactly
- Include both HTTP and HTTPS versions if you use both
- Include all ports your app might run on

### Step 3: Save and Wait

1. Click **Save**
2. Wait 1-2 minutes for changes to propagate
3. Try signing in again

### Step 4: Verify

After adding the redirect URIs, the Google sign-in should work. If you still get errors:

1. Make sure you saved the changes in Google Cloud Console
2. Clear your browser cache and cookies
3. Try again after waiting a few minutes

## For Production

When deploying to production, add your production redirect URI:
```
https://yourdomain.com/signin-google
```

## Quick Checklist

- [ ] Added `http://localhost:5179/signin-google`
- [ ] Added `http://127.0.0.1:5179/signin-google`
- [ ] Added `https://localhost:7019/signin-google` (if using HTTPS)
- [ ] Saved changes in Google Cloud Console
- [ ] Waited 1-2 minutes for propagation
- [ ] Cleared browser cache
- [ ] Tried signing in again

