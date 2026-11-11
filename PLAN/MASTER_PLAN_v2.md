# 🔱 CyhAstro — Complete Master Plan v2.0
## Astro Programmer's Deep Architecture Document

> **Version:** 2.0 | **Date:** November 2026  
> **Stack:** ASP.NET Core 9 | SQL Server | Dapper | Gemini AI | Razor Pages + JS  
> **Purpose:** Yeh file poora project plan hai — koi bhi system/developer isko padhkar project samajh sake aur implement kar sake.

---

## 📋 PART 0 — PHILOSOPHY & RULES

> These rules guide every decision in this project.

1. **Admin = God** — Har cheez Admin dashboard se set hogi. Code deploy kiye bina bhi prices, texts, limits, features ON/OFF ho sakenge.
2. **No Hardcoded Values** — Har number (price, scan limit, mahadasha years, remedy text) database ke `tbl_AppSettings` ya dedicated table se aayega.
3. **Freemium First** — User pehle app se value le, phir paise de. Free mein itna de ki addict ho jaye.
4. **Quota = Currency** — Har feature ke liye credits system. Paid = credits milte hain. Credits khatam = paywall.
5. **Dynamic Content** — Rashifal text, remedy suggestions, oracle answers — sab DB mein store, Admin se edit.
6. **Offline-First** — Jo calculations pure math se ho sakti hain (Kundli, Numerology) wo offline bhi chalegi.

---

## 📋 PART 1 — SUBSCRIPTION TIERS (Admin se Fully Configurable)

```
┌─────────────────────────────────────────────────────────────────────┐
│                     PLAN COMPARISON TABLE                           │
├──────────────────────┬───────────┬────────────┬────────────┬────────┤
│ Feature              │ FREE      │ SILVER     │ GOLD       │DIAMOND │
│                      │ ₹0        │ ₹99/week   │ ₹299/month │₹1999/yr│
├──────────────────────┼───────────┼────────────┼────────────┼────────┤
│ Kundli Calculate     │ ✅ Always │ ✅         │ ✅         │ ✅     │
│ Kundli PDF Download  │ ❌        │ ✅ 2/month │ ✅ Unlim.  │ ✅     │
│ Palm Scan            │ 1 (ever)  │ 5/month    │ Unlimited  │ Unlim. │
│ Face Scan            │ ❌        │ 2/month    │ Unlimited  │ Unlim. │
│ Daily Rashifal       │ 3 rashis  │ All 12     │ All 12     │ All 12 │
│ AI Pandit Ji Chat    │ 3/day     │ 10/day     │ Unlimited  │ Unlim. │
│ Prashna Oracle       │ 3/day     │ 10/day     │ Unlimited  │ Unlim. │
│ Kundli Scorecard     │ Basic     │ Full       │ Full       │ Full   │
│ Vivah Milan (Match)  │ Score only│ Full Report│ PDF Report │ PDF+   │
│ Baby Naming          │ ✅ Free   │ ✅         │ ✅         │ ✅     │
│ Numerology           │ Basic     │ Full       │ Full       │ Full   │
│ Muhurat Finder       │ ❌        │ ✅         │ ✅         │ ✅     │
│ Sade Sati Report     │ Status    │ Full       │ Full+Tips  │ Full   │
│ Annual Jatakam PDF   │ ❌        │ ❌         │ ✅ 1/year  │ ✅     │
│ Family Kundli Vault  │ 1 member  │ 3 members  │ 5 members  │ Unlim. │
│ Puja Booking         │ View only │ ✅ Book    │ ✅ 10% off │ 20%off │
│ Notifications        │ 1/week    │ Daily      │ Realtime   │ Push+  │
│ Lucky Gem Suggest    │ ❌        │ ✅         │ ✅         │ ✅     │
│ Dream Interpreter    │ 2/day     │ 10/day     │ Unlimited  │ Unlim. │
│ WhatsApp Share Cards │ Watermark │ No WM      │ No WM      │ Brand  │
│ Support              │ Email     │ Email      │ Priority   │ Chat   │
└──────────────────────┴───────────┴────────────┴────────────┴────────┘
```

> 🔑 **Admin Control:** Har plan ki limits `tbl_Plans` table mein store hongi. Admin ek click mein change kar sakta hai.

---

## 📋 PART 2 — DATABASE DESIGN (Complete Schema — New Tables)

### Existing Tables (v1.0):
- `tbl_Users` — User accounts
- `tbl_Subscriptions` — User subscription + scan quotas
- `tbl_UserLedger` — Financial transaction log
- `tbl_KundliProfiles` — Calculated kundli data
- `tbl_ScanHistory` — Palm + Face scan history
- `tbl_AppVersions` — App version management
- `tbl_AppSettings` — Key-value settings store

### NEW Tables (v2.0):

```sql
-- ══════════════════════════════════════════════════════
-- TABLE A: tbl_Plans  (Admin se Fully Configurable)
-- ══════════════════════════════════════════════════════
CREATE TABLE tbl_Plans (
    PlanId              NVARCHAR(20) PRIMARY KEY,  -- 'Free','Silver','Gold','Diamond'
    DisplayName         NVARCHAR(100) NOT NULL,
    DisplayNameHindi    NVARCHAR(100) NOT NULL,
    PriceINR            DECIMAL(10,2) NOT NULL DEFAULT 0,
    DurationDays        INT NOT NULL DEFAULT 0,     -- 0 = Forever (Free)
    BadgeColor          NVARCHAR(20) DEFAULT '#94a3b8',
    BadgeIcon           NVARCHAR(50) DEFAULT '⭐',
    IsActive            BIT NOT NULL DEFAULT 1,
    SortOrder           INT NOT NULL DEFAULT 0,
    -- QUOTA LIMITS (all configurable from Admin)
    PalmScansPerMonth   INT NOT NULL DEFAULT 1,
    FaceScansPerMonth   INT NOT NULL DEFAULT 0,
    AiChatPerDay        INT NOT NULL DEFAULT 3,
    PrashnaPerDay       INT NOT NULL DEFAULT 3,
    KundliPdfPerMonth   INT NOT NULL DEFAULT 0,
    MilanPdfPerMonth    INT NOT NULL DEFAULT 0,
    FamilyMembersMax    INT NOT NULL DEFAULT 1,
    DreamInterpPerDay   INT NOT NULL DEFAULT 2,
    -- FEATURE FLAGS (all configurable — 1=ON, 0=OFF)
    HasDailyRashifal    BIT NOT NULL DEFAULT 1,
    RashifalLimit       INT NOT NULL DEFAULT 3,     -- 3 rashis free, 12 paid
    HasMuhurat          BIT NOT NULL DEFAULT 0,
    HasNumerologyFull   BIT NOT NULL DEFAULT 0,
    HasGemstones        BIT NOT NULL DEFAULT 0,
    HasSadeSatiReport   BIT NOT NULL DEFAULT 0,
    HasAnnualJatakam    BIT NOT NULL DEFAULT 0,
    HasPujaBooking      BIT NOT NULL DEFAULT 0,
    HasShareNoWatermark BIT NOT NULL DEFAULT 0,
    PujaDiscountPct     INT NOT NULL DEFAULT 0,
    UpdatedAt           DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- ══════════════════════════════════════════════════════
-- TABLE B: tbl_HoroscopeContent  (Admin se Edit Rashifal)
-- ══════════════════════════════════════════════════════
CREATE TABLE tbl_HoroscopeContent (
    HoroId          UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    RashiName       NVARCHAR(50) NOT NULL,  -- 'Mesh','Vrishabha',...'Meena'
    RashiIndex      INT NOT NULL,           -- 0-11
    Period          NVARCHAR(10) NOT NULL,  -- 'Daily','Weekly','Monthly','Yearly'
    ForDate         DATE NOT NULL,          -- The date this content is for
    SummaryText     NVARCHAR(MAX) NOT NULL,
    CareerText      NVARCHAR(MAX) NULL,
    LoveText        NVARCHAR(MAX) NULL,
    WealthText      NVARCHAR(MAX) NULL,
    HealthText      NVARCHAR(MAX) NULL,
    RemedyText      NVARCHAR(MAX) NULL,
    LuckyNumber     INT NULL,
    LuckyColor      NVARCHAR(50) NULL,
    LuckyGem        NVARCHAR(50) NULL,
    StarRating      DECIMAL(2,1) NULL,      -- 1.0 to 5.0
    IsPublished     BIT NOT NULL DEFAULT 0,
    CreatedBy       NVARCHAR(100) NULL,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
CREATE UNIQUE INDEX UX_Horo_RashiPeriodDate ON tbl_HoroscopeContent(RashiIndex, Period, ForDate);

-- ══════════════════════════════════════════════════════
-- TABLE C: tbl_OracleAnswers  (Prashna Oracle answers pool)
-- ══════════════════════════════════════════════════════
CREATE TABLE tbl_OracleAnswers (
    OracleId        UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ModValue        INT NOT NULL,           -- 0,1,2 (number % 3)
    Category        NVARCHAR(50) NOT NULL,  -- 'marriage','career','money','general','health','travel'
    ResultType      NVARCHAR(10) NOT NULL,  -- 'Yes','No','Wait'
    Emoji           NVARCHAR(10) NOT NULL,
    HeadlineHindi   NVARCHAR(200) NOT NULL,
    DetailHindi     NVARCHAR(1000) NOT NULL,
    RemedyHindi     NVARCHAR(500) NULL,
    IsActive        BIT NOT NULL DEFAULT 1,
    SortOrder       INT NOT NULL DEFAULT 0
);

-- ══════════════════════════════════════════════════════
-- TABLE D: tbl_FamilyProfiles  (Parivar ki Kundli)
-- ══════════════════════════════════════════════════════
CREATE TABLE tbl_FamilyProfiles (
    FamilyId        UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    OwnerUserId     UNIQUEIDENTIFIER NOT NULL REFERENCES tbl_Users(UserId),
    RelationLabel   NVARCHAR(50) NOT NULL,   -- 'Spouse','Son','Mother','Father' etc.
    FullName        NVARCHAR(150) NOT NULL,
    Gender          NVARCHAR(10) NOT NULL,
    DateOfBirth     DATE NOT NULL,
    TimeOfBirth     TIME NULL,
    BirthCity       NVARCHAR(150) NULL,
    BirthLatitude   DECIMAL(9,6) NULL,
    BirthLongitude  DECIMAL(9,6) NULL,
    KundliJson      NVARCHAR(MAX) NULL,      -- Calculated kundli stored as JSON
    IsActive        BIT NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_Family_OwnerUserId ON tbl_FamilyProfiles(OwnerUserId);

-- ══════════════════════════════════════════════════════
-- TABLE E: tbl_MilanRequests  (Kundli Matching History)
-- ══════════════════════════════════════════════════════
CREATE TABLE tbl_MilanRequests (
    MilanId         UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    RequestedByUser UNIQUEIDENTIFIER NOT NULL REFERENCES tbl_Users(UserId),
    Boy_Name        NVARCHAR(150) NOT NULL,
    Boy_DOB         DATE NOT NULL,
    Boy_TOB         TIME NULL,
    Boy_City        NVARCHAR(150) NULL,
    Boy_Lat         DECIMAL(9,6) NULL,
    Boy_Lng         DECIMAL(9,6) NULL,
    Girl_Name       NVARCHAR(150) NOT NULL,
    Girl_DOB        DATE NOT NULL,
    Girl_TOB        TIME NULL,
    Girl_City       NVARCHAR(150) NULL,
    Girl_Lat        DECIMAL(9,6) NULL,
    Girl_Lng        DECIMAL(9,6) NULL,
    TotalGunas      INT NULL,
    ResultJson      NVARCHAR(MAX) NULL,      -- Full 36 guna breakdown
    PdfPath         NVARCHAR(500) NULL,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- ══════════════════════════════════════════════════════
-- TABLE F: tbl_PujaServices  (Admin se Add/Edit Pujas)
-- ══════════════════════════════════════════════════════
CREATE TABLE tbl_PujaServices (
    PujaId          UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PujaName        NVARCHAR(200) NOT NULL,
    PujaNameHindi   NVARCHAR(200) NOT NULL,
    Description     NVARCHAR(MAX) NULL,
    PriceINR        DECIMAL(10,2) NOT NULL,
    DurationText    NVARCHAR(100) NULL,     -- e.g. '2-3 hours'
    Category        NVARCHAR(50) NULL,      -- 'Navagraha','Shiva','Devi','Pitru'
    Emoji           NVARCHAR(10) NULL,
    ImageUrl        NVARCHAR(500) NULL,
    Benefits        NVARCHAR(MAX) NULL,     -- JSON array of benefit strings
    IsActive        BIT NOT NULL DEFAULT 1,
    SortOrder       INT NOT NULL DEFAULT 0,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- ══════════════════════════════════════════════════════
-- TABLE G: tbl_PujaBookings  (Puja booking orders)
-- ══════════════════════════════════════════════════════
CREATE TABLE tbl_PujaBookings (
    BookingId       UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId          UNIQUEIDENTIFIER NOT NULL REFERENCES tbl_Users(UserId),
    PujaId          UNIQUEIDENTIFIER NOT NULL REFERENCES tbl_PujaServices(PujaId),
    BookingDate     DATE NULL,
    AmountPaid      DECIMAL(10,2) NOT NULL,
    DiscountApplied DECIMAL(10,2) NOT NULL DEFAULT 0,
    Status          NVARCHAR(20) NOT NULL DEFAULT 'Pending',
    -- Status values: Pending, Confirmed, Completed, Cancelled
    UserName        NVARCHAR(150) NOT NULL,
    UserPhone       NVARCHAR(20) NOT NULL,
    UserAddress     NVARCHAR(500) NULL,
    SpecialRequest  NVARCHAR(500) NULL,
    AdminNotes      NVARCHAR(500) NULL,
    PaymentTxnId    NVARCHAR(300) NULL,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_PujaBook_UserId ON tbl_PujaBookings(UserId);
CREATE INDEX IX_PujaBook_Status ON tbl_PujaBookings(Status);

-- ══════════════════════════════════════════════════════
-- TABLE H: tbl_NumerologyCache
-- ══════════════════════════════════════════════════════
CREATE TABLE tbl_NumerologyCache (
    NumeroCacheId   UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId          UNIQUEIDENTIFIER NOT NULL REFERENCES tbl_Users(UserId),
    InputType       NVARCHAR(20) NOT NULL,  -- 'DOB','Name','Mobile','Vehicle'
    InputValue      NVARCHAR(200) NOT NULL,
    ResultJson      NVARCHAR(MAX) NOT NULL,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- ══════════════════════════════════════════════════════
-- TABLE I: tbl_DreamDatabase  (Admin se manage karo)
-- ══════════════════════════════════════════════════════
CREATE TABLE tbl_DreamDatabase (
    DreamId         UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Keywords        NVARCHAR(500) NOT NULL,  -- 'saanp,snake,naag' (comma-separated)
    Title           NVARCHAR(200) NOT NULL,
    TitleHindi      NVARCHAR(200) NOT NULL,
    Meaning         NVARCHAR(MAX) NOT NULL,
    IsGood          BIT NULL,                -- NULL=neutral, 1=good, 0=bad
    Remedy          NVARCHAR(500) NULL,
    Category        NVARCHAR(50) NULL,       -- 'Animals','Water','Deity','Family','Nature'
    IsActive        BIT NOT NULL DEFAULT 1
);
CREATE FULLTEXT INDEX ON tbl_DreamDatabase(Keywords, TitleHindi);

-- ══════════════════════════════════════════════════════
-- TABLE J: tbl_Notifications
-- ══════════════════════════════════════════════════════
CREATE TABLE tbl_Notifications (
    NotifId         UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId          UNIQUEIDENTIFIER NULL,  -- NULL = broadcast to all
    PlanFilter      NVARCHAR(20) NULL,      -- 'Free','Silver','Gold','Diamond' or NULL=all
    Title           NVARCHAR(200) NOT NULL,
    Body            NVARCHAR(500) NOT NULL,
    DeepLink        NVARCHAR(200) NULL,     -- e.g., '/App?tab=rashifal'
    IsRead          BIT NOT NULL DEFAULT 0,
    SentAt          DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- ══════════════════════════════════════════════════════
-- TABLE K: tbl_FeatureUsageLog  (Daily quota tracking)
-- ══════════════════════════════════════════════════════
CREATE TABLE tbl_FeatureUsageLog (
    LogId       BIGINT IDENTITY PRIMARY KEY,
    UserId      UNIQUEIDENTIFIER NOT NULL,
    FeatureKey  NVARCHAR(50) NOT NULL,
    -- Keys: 'ai_chat','prashna','palm_scan','face_scan','dream_search','kundli_pdf','milan_pdf'
    UsedAt      DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ForDate     DATE NOT NULL DEFAULT CAST(GETUTCDATE() AS DATE)
);
CREATE INDEX IX_Usage_UserDate ON tbl_FeatureUsageLog(UserId, FeatureKey, ForDate);
```

---

## 📋 PART 3 — USER PANEL SCREEN MAP

```
USER APP (Mobile UI — /App page)
│
├── 🔐 AUTH SCREENS (unauthenticated)
│   ├── Login Screen (Email/Phone + Password)
│   └── Register Screen (Name, Email/Phone, DOB, TOB, City)
│
└── 📱 MAIN APP (authenticated — 6 Tab Bottom Nav)
    │
    ├── 🏠 TAB 1: HOME
    │   ├── 🌅 Daily Briefing Card (personalized by rashi+dasha — from API)
    │   │   ├── Career status (🟢🟡🔴)
    │   │   ├── Wealth / Health / Love status
    │   │   ├── Shubh time range + Rahu Kaal
    │   │   └── Lucky Number + Color
    │   ├── ⚡ Quick Feature Cards (2-column grid, locked/unlocked by plan)
    │   │   ├── 🔮 Prashna Oracle (NEW)
    │   │   ├── 🌙 Rashifal
    │   │   ├── 📊 Kundli Scorecard (NEW)
    │   │   ├── 🗓️ Muhurat Finder (NEW — Silver+)
    │   │   ├── 🤖 AI Pandit Ji (Premium)
    │   │   └── 🔢 Numerology (NEW)
    │   └── ✦ More Features
    │       ├── 👶 Baby Naming (Free)
    │       ├── 💍 Vivah Milan 36 Guna
    │       ├── 🌍 Lucky Direction/City
    │       ├── 🌙 Dream Interpreter
    │       └── 🛕 Online Puja Booking
    │
    ├── 🌙 TAB 2: RASHIFAL
    │   ├── 12 Rashi horizontal selector
    │   ├── Period: Aaj | Hafte | Mahine | Saal
    │   ├── Star rating display
    │   ├── Content sections: Samanya, Vyavsay, Prem, Dhan, Swasthya
    │   ├── Remedy box
    │   └── Lucky Row (number, color, gem)
    │
    ├── 🔮 TAB 3: KUNDLI
    │   ├── Sub-tab: मेरी कुंडली
    │   │   ├── Scorecard circle (X/100)
    │   │   ├── 5 category progress bars
    │   │   ├── Share + PDF buttons
    │   │   ├── D1 North Indian Chart (SVG)
    │   │   ├── Mahadasha timeline
    │   │   └── Dosha boxes (Manglik/SadeSati/Kalsarp)
    │   ├── Sub-tab: विवाह मिलान
    │   │   ├── Boy + Girl DOB/TOB/City inputs
    │   │   ├── 36 Guna breakdown table
    │   │   └── PDF Report (Paid)
    │   └── Sub-tab: परिवार की कुंडली
    │       ├── Add member form (plan-limited count)
    │       └── Member cards → their kundli view
    │
    ├── 🛠️ TAB 4: TOOLS
    │   ├── 🔮 Prashna Oracle (interactive number grid)
    │   ├── 🗓️ Muhurat Finder (by event type + date)
    │   ├── 🔢 Numerology (DOB/Name/Mobile/Vehicle)
    │   ├── 🌙 Dream Interpreter (keyword search)
    │   └── 🌍 Lucky Direction + City
    │
    ├── 🤖 TAB 5: AI PANDIT JI
    │   ├── Suggested questions pills
    │   ├── Chat conversation (kundli-context aware)
    │   ├── Message input + Send
    │   └── Daily quota counter (Free: 3/day)
    │
    └── 👤 TAB 6: PROFILE
        ├── Avatar, Name, Plan badge
        ├── Stats (scans used, chats used)
        ├── Upgrade Banner (if Free/Silver)
        ├── Subscription details
        └── Menu: Meri Kundli | Parivar | Reports | Puja Bookings
                   Notifications | Language | Logout
```

---

## 📋 PART 4 — ADMIN PANEL SCREEN MAP

```
ADMIN PORTAL (/Admin)
│
├── 📊 DASHBOARD (Enhanced)
│   ├── Live stats: Users, Revenue Today, Scans, Chats, Puja Bookings
│   ├── Revenue chart (last 30 days) — Chart.js
│   ├── Feature usage breakdown chart
│   └── Plan distribution pie chart
│
├── 👥 USERS
│   ├── List (search, filter by plan, paginated)
│   └── Details (profile, sub, scans, ledger, chat history, block/grant credits)
│
├── 💰 PLANS MANAGER (NEW)
│   └── Edit all 4 plans: price, duration, all quotas, all feature flags
│
├── 🌙 RASHIFAL MANAGER (NEW — KEY FEATURE)
│   ├── Calendar view (green = published, red = missing)
│   ├── Add/Edit for any rashi + date + period
│   └── Publish/Unpublish toggle
│
├── 🔮 ORACLE MANAGER (NEW)
│   ├── All answers list (by category)
│   ├── Add/Edit: category, yes/no/wait, text, remedy
│   └── Live test tool
│
├── 🛕 PUJA MANAGER (NEW)
│   ├── Services: Add/Edit puja (name, price, benefits, image)
│   └── Bookings: List, update status, admin notes
│
├── 🌙 DREAM DATABASE (NEW)
│   ├── All entries (searchable)
│   └── Add/Edit: keywords, meaning, category, remedy
│
├── 🔔 NOTIFICATIONS (NEW)
│   ├── Send: target (all/plan/user), title, body, deeplink
│   └── History log
│
├── 💳 LEDGER & REVENUE (Enhanced)
│   ├── All transactions + revenue summary by plan
│   └── Export CSV
│
├── ⚙️ APP SETTINGS (Enhanced)
│   ├── AI prompts (Gemini palm + face prompts)
│   ├── Maintenance mode toggle
│   ├── Support email, App name, Language
│   └── Feature global ON/OFF switches
│
└── 📱 APP VERSIONS (Existing)
```

---

## 📋 PART 5 — NEW API ENDPOINTS

```
/api/
├── user/
│   ├── GET  profile
│   ├── PUT  profile
│   └── GET  dashboard          ← 1 call returns home screen data
│
├── rashifal/
│   ├── GET  daily/{rashiIndex}
│   ├── GET  weekly/{rashiIndex}
│   └── GET  monthly/{rashiIndex}
│
├── oracle/
│   ├── GET  categories
│   └── POST ask                ← input: number + category → answer
│
├── tools/
│   ├── POST numerology/dob
│   ├── POST numerology/name
│   ├── POST numerology/mobile
│   ├── GET  muhurat/{eventType}
│   ├── GET  dream/search?q={keyword}
│   └── GET  panchang/today
│
├── family/
│   ├── GET  list
│   ├── POST add
│   ├── DELETE {id}
│   └── GET  kundli/{id}
│
├── ai/
│   └── POST chat               ← kundli-aware AI conversation
│
├── puja/
│   ├── GET  services
│   ├── POST book
│   └── GET  my-bookings
│
├── subscription/
│   ├── GET  plans              ← from tbl_Plans DB (not hardcoded!)
│   ├── GET  my-plan
│   └── POST upgrade
│
└── notifications/
    ├── GET  my
    └── PUT  mark-read
```

---

## 📋 PART 6 — QUOTA ENGINE

```
HOW QUOTA WORKS:

Every protected API call → QuotaService.CheckAndLog(userId, featureKey)

1. User ka plan get karo (from tbl_Subscriptions)
2. Plan limits get karo (from tbl_Plans — cached 5 min)
3. Today's usage count karo (from tbl_FeatureUsageLog)
4. If usage < limit → Allow → Log usage → Return 200 OK
5. If usage >= limit → Return 402 PaymentRequired
   {
     "error": "quota_exceeded",
     "feature": "ai_chat",
     "limit": 3,
     "resetIn": "17h 23m",
     "upgradeToPlans": ["Silver", "Gold"],
     "paywallTitle": "AI Pandit Ji unlock karein",
     "paywallBody": "Silver plan mein 10 sawaal/din milenge"
     ← this text from tbl_AppSettings, editable by Admin!
   }

QUOTA RESET:
  Daily features → reset at midnight IST
  Monthly features → reset on 1st of each month
```

---

## 📋 PART 7 — PAYWALL UI STRATEGY

```
When API returns 402 → show PaywallModal:

┌─────────────────────────────────────────────┐
│  🔮 [Feature Name] unlock karein            │
│     (title from API response — DB se)       │
│                                             │
│  ⭐ Silver Plan — ₹99/week                  │
│  ✅ [Benefit 1 — from DB]                   │
│  ✅ [Benefit 2 — from DB]                   │
│  ✅ [Benefit 3 — from DB]                   │
│                                             │
│  [⬆️ ₹99 mein Upgrade Karen]               │
│  [🔄 Baad mein dekhenge]                   │
└─────────────────────────────────────────────┘

NOTE: Har text DB se aata hai.
Admin ek click mein change kare → instantly user ko dikhega.
```

---

## 📋 PART 8 — STEP BY STEP IMPLEMENTATION PLAN

### PHASE 1 — Foundation (Week 1)
```
Step 1.1: DB Migration — 11 new tables SQL script (CyhAstro_Schema_v2.sql)
          + Seed data: tbl_Plans (4 plans), tbl_OracleAnswers (30+ entries)

Step 1.2: Core Layer — New DTOs + Interfaces
          PlanDto, HoroscopeDto, OracleAnswerDto, NumerologyDto
          DashboardDto, FamilyMemberDto, PujaDto, QuotaResultDto

Step 1.3: Infrastructure — Implementations
          PlanRepository, QuotaService, HoroscopeRepository
          NumerologyService (pure C# math), PanchangService

Step 1.4: API — New Controllers
          RashifalController, OracleController, ToolsController, FamilyController
```

### PHASE 2 — Admin Side (Week 2)
```
Step 2.1: Plan Manager page (tbl_Plans CRUD)
Step 2.2: Rashifal Content Manager (calendar + rich edit)
Step 2.3: Oracle Answers Manager
Step 2.4: Dashboard enhancement (charts, new stats)
```

### PHASE 3 — User App New Features (Week 3)
```
Step 3.1: Home screen rebuild (Daily Briefing, feature grid with plan locks)
Step 3.2: Rashifal screen (12 rashi, periods, DB content)
Step 3.3: Kundli Scorecard + Dosha Report
Step 3.4: Tools Tab (Prashna Oracle, Numerology)
```

### PHASE 4 — Monetization (Week 4)
```
Step 4.1: Paywall Modal (dynamic content from DB)
Step 4.2: Payment Integration (Razorpay/PhonePe webhooks)
Step 4.3: Puja Booking system (Admin + User)
Step 4.4: AI Pandit Ji (kundli-aware context)
```

### PHASE 5 — Advanced (Week 5-6)
```
Step 5.1: Family Kundli Vault
Step 5.2: Vivah Milan full UI + PDF
Step 5.3: Dream Interpreter (DB + search)
Step 5.4: Push Notifications
Step 5.5: Muhurat Finder (calendar view)
Step 5.6: Annual Jatakam PDF (premium)
```

---

## 📋 PART 9 — NUMEROLOGY ENGINE (Pure C# Math)

```csharp
public class NumerologyService
{
    // MOOLANK (Birth Number) — just the date
    // E.g., DOB=15 → 1+5=6 → Moolank 6
    public int CalculateMoolank(DateOnly dob)
    {
        return ReduceToSingleDigit(dob.Day);
    }

    // BHAGYANK (Life Path) — full DOB digit sum
    // E.g., 15/08/1995 → 1+5+0+8+1+9+9+5=38 → 3+8=11 → 1+1=2
    public int CalculateBhagyank(DateOnly dob)
    {
        int sum = SumDigits(dob.Day) + SumDigits(dob.Month) + SumDigits(dob.Year);
        return ReduceToSingleDigit(sum);
    }

    // NAME NUMEROLOGY (Cheiro system)
    // A=1 B=2 C=3 D=4 E=5 F=8 G=3 H=5 I=1 J=1 K=2 L=3 M=4
    // N=5 O=7 P=8 Q=1 R=2 S=3 T=4 U=6 V=6 W=6 X=5 Y=1 Z=7
    public int CalculateNameNumber(string name)
    {
        // Map each letter, sum all, reduce to single digit
    }

    // MOBILE NUMBER CHECK
    // Sum all 10 digits → reduce → lookup meaning from DB
    public int CalculateMobileNumber(string mobile)
    {
        int sum = mobile.Where(char.IsDigit).Sum(c => c - '0');
        return ReduceToSingleDigit(sum);
    }

    private int ReduceToSingleDigit(int n)
    {
        while (n > 9) n = SumDigits(n);
        return n;
    }
}
```

---

## 📋 PART 10 — DAILY BRIEFING ALGORITHM

```
INPUT: User ki Rashi (Moon Sign) + Current Mahadasha + Today's Date

STEP 1: Today's panchang calculate karo (Tithi, Var, Nakshatra)
STEP 2: DB se aaj ki rashi ka rashifal content lo (tbl_HoroscopeContent)
STEP 3: 4 scores from rashifal content (career_rating, wealth_rating, etc.)
STEP 4: Rahu Kaal calculate karo (day-of-week formula)
         Mon=7:30-9:00, Tue=15:00-16:30, Wed=12:00-13:30,
         Thu=13:30-15:00, Fri=10:30-12:00, Sat=9:00-10:30, Sun=16:30-18:00
STEP 5: Lucky Number = (Moolank + DayOfWeek) % 9 + 1
STEP 6: Lucky Color from tbl_HoroscopeContent.LuckyColor

RETURNS: DashboardDto (1 API call → /api/user/dashboard)
```

---

## 📋 PART 11 — KEY TECHNICAL DECISIONS

| Decision | Choice | Reason |
|----------|--------|--------|
| Rashifal content | DB-driven | No redeployment for content changes |
| Oracle answers | DB pool + category match | Admin can add/modify freely |
| Numerology | Pure C# math | 100% offline, instant, no API cost |
| AI Chat context | User's full Kundli JSON as system prompt | Personalized answers |
| Quota tracking | tbl_FeatureUsageLog per day | Accurate, resettable |
| Plan config | tbl_Plans DB table | Admin changes instantly effective |
| Payment | Razorpay | Most popular in India, good webhook |
| Paywall UI | Modal with DB-driven text | Admin edits upsell message freely |
| PDF Generation | QuestPDF (existing) | Already integrated in project |
| Caching | In-memory 5-min cache for plans | Reduce DB load |

---

## 📋 PART 12 — NEW FILES TO CREATE

```
CyhAstro.Core/DTOs/
  PlanDto.cs, HoroscopeDto.cs, OracleAnswerDto.cs
  NumerologyDto.cs, DashboardDto.cs, FamilyMemberDto.cs
  PujaDto.cs, QuotaResultDto.cs, ChatMessageDto.cs

CyhAstro.Core/Interfaces/
  IPlanRepository.cs, IHoroscopeRepository.cs
  IOracleRepository.cs, IQuotaService.cs, IFamilyRepository.cs
  INumerologyService.cs, IPanchangService.cs

CyhAstro.Infrastructure/Repositories/
  PlanRepository.cs, HoroscopeRepository.cs
  OracleRepository.cs, FamilyRepository.cs

CyhAstro.Infrastructure/Services/
  QuotaService.cs, NumerologyService.cs
  PanchangService.cs, DailyBriefingService.cs

CyhAstro.Api/Controllers/
  RashifalController.cs, OracleController.cs
  ToolsController.cs, FamilyController.cs
  PujaController.cs, AiChatController.cs

CyhAstro.Admin/Pages/Admin/
  Plans/Index.cshtml
  Rashifal/Index.cshtml, Rashifal/Edit.cshtml
  Oracle/Index.cshtml
  Puja/Services.cshtml, Puja/Bookings.cshtml
  Dreams/Index.cshtml
  Notifications/Index.cshtml
```

---

## 📋 PART 13 — CURRENT STATUS (v1.0 Existing Features)

✅ **Already Built & Working:**
- User Registration/Login (JWT auth)
- Vedic Kundli calculate (100% offline C# math)
- Kundli PDF download (QuestPDF)
- 36 Guna Milan (backend ready, UI needed)
- Palm Scan (Gemini Vision AI)
- Face Reading (Gemini Vision AI)
- Offline Sync Engine
- In-App Auto Update
- Admin Dashboard (basic)
- User Management + Ledger
- Subscription (Free/Monthly plans basic)
- tbl_AppSettings (AI prompts, prices)

🔲 **To Build (v2.0):**
- Dynamic Plans system (tbl_Plans)
- Rashifal Content system
- Prashna Oracle
- Numerology Suite
- Daily Briefing Card
- AI Pandit Ji (kundli-aware)
- Family Kundli Vault
- Puja Booking
- Dream Interpreter
- Muhurat Finder
- Push Notifications
- Quota Engine (tbl_FeatureUsageLog)
- Payment Gateway (Razorpay)

---

*Document created by AI Astro Programmer — CyhAstro Project*  
*Start implementation from Phase 1, Step 1.1 — Database Migration*
