# Gift of the Givers - Prototype Web Application

## Running the project

```bash
dotnet restore
dotnet run
```

The app uses SQLite (`gotg.db`, created automatically on first run) so no external database
setup is required to demo the prototype. To move to Azure SQL for later phases, change
`UseSqlite` to `UseSqlServer` in `Program.cs` and update the connection string in
`appsettings.json`.

## Demo accounts (seeded automatically on startup)

| Role     | Email                       | Password      |
|----------|------------------------------|---------------|
| Employee | employee@giftofthegivers.org | Employee@123  |
| Donor    | donor@example.com            | Donor@123     |

You can also register new Donor accounts via the standard ASP.NET Identity "Register" page
(scaffolded Identity UI - `/Identity/Account/Register`).

## How this meets the brief

- **Branding & visuals** - `_Layout.cshtml` provides a consistent header/logo, navbar
  (Home, About, Donate, Volunteer, Contact), Bootstrap 5 styling, and a custom `site.css`
  with the foundation's colour palette.
- **Authentication & roles** - ASP.NET Core Identity, two roles (`Employee`, `Donor`) seeded
  at startup. `EmployeeController` is locked down with `[Authorize(Roles = "Employee")]`.
- **Donation features** - `DonationController`/`Views/Donation` support one-time vs recurring,
  ZAR/USD/EUR currency choice, logged-in or anonymous guest donation, and generate a
  placeholder PDF tax certificate via QuestPDF (`Services/TaxCertificateGenerator.cs`).
- **Volunteer section** - simple form (`Views/Volunteer/Register.cshtml`) storing sign-ups
  in the `VolunteerSignups` table, visible to Employees on their dashboard.
- **Employee dashboard** - post relief project updates (shown on the public homepage too)
  and view volunteer sign-ups and recent donations.

## Known prototype limitations (intentional, per brief)

- No real payment gateway integration - donations are dummy records.
- Tax certificate is clearly labelled as a placeholder, not a legally valid document.
- Minimal data validation/workflow beyond what's needed to demonstrate feasibility.
- These will be built out properly in Part 2.

## Suggested screenshots to capture for submission

1. Homepage (branding + latest updates)
2. Login page and Register page
3. Donation form (both logged-in and guest view)
4. Thank-you page + downloaded PDF certificate
5. Volunteer registration form + success message
6. Employee dashboard (post update, volunteer list, donations list)

## Group contribution reflection template

Fill in for your submission:

- **UI/UX & branding (layout, CSS, navbar):** [Name]
- **Authentication & role setup (Identity, seeding, authorization):** [Name]
- **Donation logic & PDF certificate generation:** [Name]
- **Volunteer module:** [Name]
- **Employee dashboard & data models:** [Name]
- **Testing, screenshots, documentation:** [Name]
