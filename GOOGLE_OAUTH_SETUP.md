# Google OAuth Setup Guide

This application supports Google OAuth login. Follow these steps to configure it:

## Step 1: Create Google OAuth Credentials

1. Go to the [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select an existing one
3. Navigate to **APIs & Services** > **Credentials**
4. Click **Create Credentials** > **OAuth client ID**
5. If prompted, configure the OAuth consent screen:
   - Choose **External** user type (unless you have a Google Workspace)
   - Fill in the required information (App name, User support email, Developer contact)
   - Add scopes: `email`, `profile`, `openid`
   - Add test users if needed (for testing before verification)
6. Create OAuth Client ID:
   - Application type: **Web application**
   - Name: Your app name (e.g., "MatchCircle")
   - Authorized JavaScript origins: 
     - `http://localhost:5179`
     - `http://127.0.0.1:5179`
     - `https://localhost:7019` (if using HTTPS)
     - `https://yourdomain.com` (for production)
   - Authorized redirect URIs (IMPORTANT - Add ALL of these):
     - `http://localhost:5179/signin-google`
     - `http://127.0.0.1:5179/signin-google`
     - `https://localhost:7019/signin-google` (if using HTTPS)
     - `http://localhost:22849/signin-google` (for IIS Express)
     - `https://localhost:44339/signin-google` (for IIS Express HTTPS)
     - `https://yourdomain.com/signin-google` (for production)
   
   **Note:** You must add BOTH `localhost` and `127.0.0.1` versions, as browsers may use either one.

## Step 2: Configure appsettings.json

Update your `appsettings.json` file with your Google OAuth credentials:

```json
{
  "Authentication": {
    "Google": {
      "ClientId": "YOUR_GOOGLE_CLIENT_ID.apps.googleusercontent.com",
      "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET"
    }
  }
}
```

Replace:
- `YOUR_GOOGLE_CLIENT_ID` with your actual Client ID from Google Cloud Console
- `YOUR_GOOGLE_CLIENT_SECRET` with your actual Client Secret

## Step 3: Test the Integration

1. Start your application
2. Navigate to the login page
3. Click "Sign in with Google"
4. You should be redirected to Google's login page
5. After authentication, you'll be redirected back to your app

## Features

- **Persistent Sessions**: User sessions persist across browser restarts (30 days)
- **Cookie-based Authentication**: Secure cookie-based authentication
- **Session Restoration**: Automatic session restoration from persistent cookies
- **Google OAuth**: Seamless Google account integration

## Security Notes

- Never commit your `appsettings.json` with real credentials to version control
- Use environment variables or Azure Key Vault for production
- Keep your Client Secret secure
- Regularly rotate your OAuth credentials

