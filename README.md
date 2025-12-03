# BioIsac Admin Portal

A simple admin-only website for managing contacts and sending emails.

## Features

1. **Admin-only access** with 2FA authentication
2. **Contact management** - Add, edit, delete contacts with names, emails, and work fields
3. **Category-based organization** - Contacts are automatically sorted by work field
4. **Two-factor authentication** - TOTP-based 2FA using authenticator apps
5. **Email sending** - Send emails to categories or specific individuals
6. **In-app email composition** - Write and send emails directly from the application

## Setup Instructions

### Backend Setup

1. Navigate to the `api` folder:
   ```bash
   cd api
   ```

2. Restore NuGet packages:
   ```bash
   dotnet restore
   ```

3. Update `appsettings.json` with your email settings:
   ```json
   "EmailSettings": {
     "SmtpServer": "smtp.gmail.com",
     "SmtpPort": 587,
     "SenderEmail": "your-email@gmail.com",
     "SenderPassword": "your-app-password"
   }
   ```
   
   **Note for Gmail**: You'll need to use an App Password, not your regular password. Enable 2FA on your Google account and generate an app password.

4. Run the API:
   ```bash
   dotnet run
   ```
   
   The API will run on `http://localhost:5266` (HTTP) or `https://localhost:7203` (HTTPS).

### Frontend Setup

1. Open `Client/Resources/index.html` in a web browser, or serve it using a local web server.

2. If the API is running on a different port, update the `API_BASE` constant in `Client/Resources/Scripts/index.js`.

### Default Login Credentials

- **Username**: `admin`
- **Password**: `admin123`

**Important**: On first login, you'll be prompted to set up 2FA. Scan the QR code with an authenticator app (Google Authenticator, Microsoft Authenticator, etc.) and enter the 6-digit code to complete login.

## Usage

1. **Login**: Use the default credentials above. On first login, set up 2FA.

2. **Manage Contacts**: 
   - Click "Add Contact" to add new contacts
   - Contacts are automatically organized by work field
   - Edit or delete contacts as needed

3. **Send Emails**:
   - Go to the "Send Email" tab
   - Choose to send to a category or specific person
   - Write your email (HTML supported)
   - Click "Send Email"

## Technology Stack

- **Frontend**: HTML, CSS, JavaScript (vanilla), Bootstrap 5
- **Backend**: .NET 8.0 Web API (C#)
- **Database**: SQLite
- **Email**: MailKit
- **2FA**: OTP.NET (TOTP)

## Notes

- The database file (`database.db`) will be created automatically in the `api` folder on first run
- Make sure to configure email settings before sending emails
- The application uses session tokens stored in localStorage for authentication

