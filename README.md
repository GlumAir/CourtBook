# CourtBook — Badminton & Pickleball Court Reservation System

A web-based court reservation platform built with ASP.NET Core MVC,
Entity Framework Core, SQL Server, and Bootstrap 5.

---

## Prerequisites

Make sure you have the following installed before running the project:

- [Visual Studio 2022](https://visualstudio.microsoft.com/)
  with the **ASP.NET and web development** workload installed
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server Express](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
  or SQL Server LocalDB (included with Visual Studio)
- [SQL Server Management Studio (SSMS)](https://aka.ms/ssmsfullsetup)
  (optional but recommended)
- [Git](https://git-scm.com/)

---

## Getting Started

### Step 1 — Clone the Repository

Open a terminal or command prompt and run:
git clone https://github.com/yourusername/CourtBook.git

Then open the solution file in Visual Studio:
CourtBook.sln

---

### Step 2 — Restore NuGet Packages

Visual Studio should restore packages automatically when you open the project.

If it does not, open **Tools → NuGet Package Manager → Package Manager Console** and run:
Update-Package -reinstall

Or right click the solution in Solution Explorer and click
**Restore NuGet Packages**.

---

### Step 3 — Configure the Database

Open `appsettings.json` and verify the connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;
      Database=CourtBookDb;
      Trusted_Connection=True;
      MultipleActiveResultSets=true"
  }
}
```

If you are using a named SQL Server instance instead of LocalDB,
update the `Server` value to match your setup.

---

### Step 4 — Apply Database Migrations

Open **Tools → NuGet Package Manager → Package Manager Console** and run:
Add-Migration InitialCreate

Then run:
Update-Database

This will create the database and all tables automatically.

---

### Step 5 — Run the Application

Press **F5** or click the green **Run** button in Visual Studio.

The application will open in your browser at `https://localhost:xxxx`.

---

## Default Admin Account

The system automatically seeds a default admin account on first run:

| Field    | Value                    |
|----------|--------------------------|
| Email    | admin@courtbook.com      |
| Password | Admin@123456             |

---

## Default Courts

The following courts are seeded automatically:

| Court Name          | Sport      | Hours              | Price    |
|---------------------|------------|--------------------|----------|
| Badminton Court A   | Badminton  | 6:00 AM - 10:00 PM | ₱150/hr  |
| Badminton Court B   | Badminton  | 6:00 AM - 10:00 PM | ₱150/hr  |
| Pickleball Court A  | Pickleball | 7:00 AM - 9:00 PM  | ₱200/hr  |
| Pickleball Court B  | Pickleball | 7:00 AM - 9:00 PM  | ₱200/hr  |

---

## Project Structure
```text
CourtBook/
├── Controllers/        — All MVC controllers
├── Data/               — DbContext and DatabaseSeeder
├── Models/             — Entity models and enums
├── Services/           — TimeSlotService
├── ViewModels/         — ViewModels for all views
├── Views/              — Razor views (.cshtml)
├── wwwroot/css/        — site.css (custom styles)
├── appsettings.json    — Configuration
└── Program.cs          — App entry point and configuration
```

---

## Tech Stack

- ASP.NET Core MVC (.NET 8)
- Entity Framework Core
- SQL Server / LocalDB
- ASP.NET Core Identity
- Bootstrap 5
- Bootstrap Icons
- Inter (Google Fonts)

---

## Common Issues

**Migration already exists error**
If you get an error saying the migration already exists, run:
Update-Database
without running `Add-Migration` again.

**LocalDB not found**
Install SQL Server Express LocalDB or update the connection string
in `appsettings.json` to point to your SQL Server instance.

**Port already in use**
Change the port in `Properties/launchSettings.json` or close
the application that is using that port.

---

## Git Workflow

Each team member should work on a separate branch:
git checkout -b feature/your-feature-name

When done, push your branch and open a pull request:
git add .
git commit -m "Description of what you built"
git push origin feature/your-feature-name

Never commit directly to `main`.
