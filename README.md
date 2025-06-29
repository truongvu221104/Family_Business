# Family Business Financial Management System

## Table of Contents
1. [Objectives & Expected Outcomes](#objectives--expected-outcomes)  
2. [Scope & Technical Requirements](#scope--technical-requirements)  
3. [Implementation Plan (3 Weeks)](#implementation-plan-3-weeks)  
4. [Resources & Tools](#resources--tools)  
5. [Risk Assessment](#risk-assessment)  
6. [References](#references)

---

## Objectives & Expected Outcomes

### Objectives
- Develop a system that enables family-owned businesses to track income and expenses accurately and visualize cash flow by **day**, **week**, **month**, and **year**.  
- Provide rapid analysis of income/expense fluctuations to support informed financial decisions.

### Expected Outcomes
- A simple, user-friendly WPF interface.  
- **Core features**:
  - CRUD operations for income and expense transactions  
  - Detailed transaction-history view  
  - Cash-flow reports by day, week, month, and year  
  - Exportable reports in **Excel** or **PDF**  
- A PowerShell script that automatically backs up `.bak` files to Google Drive.

---

## Scope & Technical Requirements

### In-Scope
- Local data storage on **SQL Server Express**  
- CRUD operations on transactions and aggregation of cash-flow reports  
- Exporting reports to **Excel** and **PDF**  
- Backup via a PowerShell script uploading `.bak` files to Google Drive

### Out-of-Scope
- Direct bank integration or online payment processing  
- Live cloud data storage (Google Drive only for backups)  
- Advanced financial analytics (forecasting, modeling)  
- Automated notifications (email/SMS)

### Technical Requirements
- **Language & Framework:** C# on .NET 8  
- **UI:** WPF using MVVM pattern  
- **Database:** SQL Server Express  
- **Backup & Sync:** Google Drive for Desktop (`SQL_Backups`)  
- **Version Control & CI/CD:** GitHub + GitHub Actions  
- **Automation Scripting:** PowerShell  
- **Target OS:** Windows 10 or later

---

## Implementation Plan (3 Weeks)

### Week 1 – Phase 1: Initiation & Setup
- Finalize project proposal and core requirements (login, trip search, coach/seat view, arrival times, manager CRUD)  
- Create GitHub repository and configure CI/CD with Actions  
- Install/configure Visual Studio & SQL Server Express

### Week 2 – Phase 2: Development
- Design basic database schema (Users, Trains, Coaches, Seats, Trips, Routes, Stations)  
- Implement backend using Entity Framework Core + LINQ  
- Build WPF UI (XAML) for login, search, seat/coach view, arrival times, CRUD  
- Use delegates for event handling  
- Connect front-end to back-end

### Week 3 – Phase 3: Integration, Testing & Finalization
- Populate sample data (users, trains, trips…)  
- End-to-end testing of core features  
- Bug fixing and package as executable  
- Prepare user documentation and final report

---

## Resources & Tools

- **Visual Studio 2022 Community Edition**  
- **SQL Server Express Edition**  
- **Git & GitHub** (Actions)  
- **Google Drive for Desktop**  
- **PowerShell**

**Initial Setup**  
1. Create GitHub repo + CI/CD workflow  
2. Install Google Drive for Desktop; share `SQL_Backups` folder  
3. Install Visual Studio (“.NET desktop development” workload)  
4. Install SQL Server Express; create `FamilyFinanceDB` + `Transactions` table

---

## Risk Assessment

| Risk                                      | Impact   | Mitigation                                                   |
|-------------------------------------------|----------|--------------------------------------------------------------|
| Learning curve with WPF/MVVM              | Moderate | Follow tutorials, build small prototypes, adhere to MVVM    |
| Backup script or Drive sync failures      | Moderate | Test PowerShell locally, verify `.bak` uploads              |
| Data loss or accidental deletion          | High     | Use Google Drive version history; keep daily/weekly backups  |
| Tight 3-week timeline                     | High     | Prioritize core features; allocate buffer for testing/fixes  |
| Insufficient testing across environments  | Moderate | Test on ≥2 Windows 10 machines; bundle .NET runtime if needed|

---

## References (Optional)

- Microsoft Docs – WPF & MVVM  
- Microsoft Docs – Entity Framework Core  
- Microsoft Learn – PowerShell Scripting Fundamentals  
- Google Drive for Desktop Help Center  
- GitHub Docs – GitHub Actions for .NET Projects  
