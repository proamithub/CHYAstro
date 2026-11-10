# ✦ CyhAstro — AI-Powered Vedic Astrology, Palmistry & Face Reading Platform

> **Modern Enterprise Architecture:** ASP.NET Core 9 | Razor Pages Admin | REST Web API | SQL Server (Stored Procedures) | Dapper (Repository Pattern) | Free Gemini Vision AI | 100% Offline Vedic Math Engine | Offline-First Hybrid Sync | In-App Auto Updates | Subscription & Ledger Engine

---

## 🌟 Highlights

1. **हस्तरेखा (Palmistry Vision AI)**:
   - Left & Right palm scan capture.
   - Detects Life Line (जीवन रेखा), Head Line (मस्तिष्क रेखा), Heart Line (हृदय रेखा), Fate Line (भाग्य रेखा), and Planetary Mounts (गुरु, शुक्र, सूर्य).
   - Powered by Google Gemini 1.5/2.0 Flash (Generous Free Tier: 1,500 scans/day).

2. **सामुद्रिक शास्त्र (Face Reading / Physiognomy)**:
   - Frontal face landmark alignment.
   - 3-Zone Vedic analysis: Forehead (मस्तक - बुद्धि व भाग्य), Eyes/Eyebrows (नेत्र - निर्णय व स्वभाव), Nose/Jaw (नासिका व ठोड़ी - धन व संकल्प शक्ति).

3. **100% Offline वैदिक जन्म पत्रिका (Vedic Kundli)**:
   - NASA/Swiss Ephemeris mathematical principles implemented directly in C#.
   - Calculates Lagna (Ascendant), Moon Sign (चंद्र राशि), Sun Sign, 27 Nakshatras & Padas, Vimshottari Mahadasha, Manglik Dosha, and Sade Sati without requiring an internet connection or paid external APIs.

4. **Offline-First Hybrid Architecture & Auto-Sync**:
   - `OfflineSyncEngine` allows users to capture scans in remote areas without internet.
   - Saves scans locally in a pending queue; automatically syncs and retrieves AI predictions once network connectivity is restored.

5. **In-App Auto-Update System**:
   - Checks server via `/api/app/check-update`.
   - Supports direct APK downloads, mandatory update enforcement, and release notes without requiring manual Play Store visits.

6. **Web Admin Portal & Financial Ledger (Razor Pages)**:
   - Live Dashboard (Users, Scans, Active Subscribers, Revenue).
   - Full User Management & Detail Inspection (Profiles, Scans, Actions).
   - Double-entry styled audit ledger (`tbl_UserLedger`).
   - Dynamic settings (update plan prices and AI prompts from the Admin UI without redeploying code).

7. **Database Integrity with Atomic Stored Procedures**:
   - All critical mutations execute through SQL Server Stored Procedures (`BEGIN TRANSACTION...COMMIT`).
   - Strict "All or Nothing" safety ensures quota deductions, plan upgrades, and ledger logs are never out of sync.

---

## 🏗️ Solution Structure

```text
CyhAstro/
├── Database/
│   └── CyhAstro_Schema.sql         # 7 Tables, Indexes, 12 Atomic Stored Procedures
│
├── src/
│   ├── CyhAstro.Core/              # Domain Entities, DTOs, and Repository Interfaces
│   │   ├── Entities/               # User, Subscription, UserLedger, ScanHistory, KundliProfile
│   │   ├── DTOs/                   # Request/Response data models
│   │   └── Interfaces/             # IUserRepository, IScanRepository, IAdminRepository...
│   │
│   ├── CyhAstro.Infrastructure/    # Data Access Layer & External Integrations
│   │   ├── Data/                   # SqlConnectionFactory (Dapper)
│   │   ├── Repositories/           # Stored Procedure DAL implementations
│   │   ├── Services/               # GeminiVisionService, VedicAstroService
│   │   └── DependencyInjection.cs  # Clean IServiceCollection registration extension
│   │
│   ├── CyhAstro.Api/               # ASP.NET Core REST API for Mobile/Hybrid Apps
│   │   ├── Controllers/            # Auth, Kundli, Scan, Subscription, App
│   │   ├── appsettings.json
│   │   ├── appsettings.Development.json
│   │   ├── appsettings.Production.json
│   │   └── Program.cs              # JWT, Swagger, CORS, File Upload Configuration
│   │
│   ├── CyhAstro.Admin/             # Web Admin Management Portal (Razor Pages)
│   │   ├── Pages/
│   │   │   ├── Shared/_AdminLayout.cshtml # Dark Cosmic mystic purple-gold UI
│   │   │   └── Admin/              # Dashboard, Users, Details, Ledger, Scans, Settings, Versions
│   │   └── Program.cs              # Cookie Authentication & Session Management
│   │
│   └── CyhAstro.Client/            # Shared Hybrid Client Library
│       ├── Services/               # AstroApiClient, OfflineSyncEngine, InAppUpdater
│       └── Components/             # HandScanner, FaceScanner, KundliView, PaywallModal
│
├── CyhAstro.sln                    # Standard Solution File
├── CyhAstro.slnx                   # Modern .NET 9 XML Solution
└── .gitignore                      # Configured to ignore secrets, binaries & user uploads
```

---

## 🚀 Getting Started

### 1. Database Setup
1. Open **SQL Server Management Studio (SSMS)**.
2. Connect to your local instance: `VRS-AMITPC\SQLEXPRESS`.
3. Open and execute [`Database/CyhAstro_Schema.sql`](file:///d:/samle%20pro/CyhAstro/Database/CyhAstro_Schema.sql).
4. This will create:
   - Database `CyhAstroDB`
   - 7 Core Tables
   - 12 Production-Ready Stored Procedures
   - Default Administrator account: `admin@cyhastro.com` (initial password: `Admin@123`)

### 2. Configure Gemini Vision API Key (100% Free)
1. Go to [Google AI Studio](https://aistudio.google.com/app/apikey).
2. Sign in with your Google account and click **"Create API Key"**.
3. Copy the key and paste it in `src/CyhAstro.Api/appsettings.Development.json`:
```json
"GeminiSettings": {
  "ApiKey": "YOUR_COPIED_GEMINI_KEY",
  "ModelName": "gemini-1.5-flash"
}
```

### 3. Run the Backend API
```powershell
cd "d:\samle pro\CyhAstro"
dotnet run --project src/CyhAstro.Api
```
- Open your browser to: `https://localhost:5001/swagger` to explore and test the interactive API documentation.

### 4. Run the Web Admin Portal
```powershell
cd "d:\samle pro\CyhAstro"
dotnet run --project src/CyhAstro.Admin
```
- Open your browser to: `https://localhost:5002/Admin`
- Log in with `admin@cyhastro.com` / `Admin@123` to view the Live Dashboard, Users List, Transaction Ledger, and AI Prompt management.

---

## 💳 Freemium Subscription Model

| Feature | Free Tier | Gold / Premium Tier |
|---|---|---|
| **Vedic Kundli** | Basic Lagna, Rashi & Nakshatra | Full planetary degrees, Mahadasha, Remedies |
| **Palm Scan (Hastrekha)** | 1 Free Scan (Teaser overview) | Unlimited Scans, 7 Mounts, Career/Wealth timeline |
| **Face Scan (Samudrik)** | Paywall locked preview | Complete 3-Zone Forehead, Eyes, Nose & Jaw reading |
| **PDF Report** | Locked | Downloadable 25-page branded Kundli & Palmistry PDF |
| **AI Astro Chat** | 3 Questions/day | Unlimited AI Astrologer Q&A |

---

## 🔒 Security & Best Practices
- **Passwords**: Hashed with BCrypt (`BCrypt.Net-Next`).
- **Mobile Auth**: Secure JWT tokens signed with HMAC-SHA256.
- **Admin Auth**: HTTP-only secure encrypted cookies.
- **SQL Injections**: Completely mitigated via parameterized Dapper calls to Stored Procedures.
- **Environment Isolation**: Separate `appsettings.Development.json` and `appsettings.Production.json` configurations.
