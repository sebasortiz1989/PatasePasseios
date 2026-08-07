<div align="center">

# Patas & Passeios

**Serviços para cães** — a cross-platform pet-sitting app built to learn [Dapper](https://github.com/DapperLib/Dapper) over SQLite.

<img src="images/screenshots/login.png" alt="Login — Patas & Passeios" width="280">

</div>

Identifiers, comments and docs are English; the UI is Brazilian Portuguese.

---

## What it is

**Patas & Passeios** helps a dog sitter run the day-to-day of the business:

| Area | What you can do |
| --- | --- |
| **Agenda** | Browse walks, sitting, hotel and day-care by day / week / month, filter by type, open a booking |
| **Cães / Tutores** | Register dogs and tutors, photos, breed, address, phone |
| **Novo serviço** | Book passeio, pet sitting, hotel (with optional extra charge) or day-care |
| **Pagamentos** | Record payments on the tutor screen — the ledger settles services and banks leftover as credit |
| **Perfil** | Income by month/type, Pix key, password, PNG month summary, backup export/import |
| **Extras** | Mark work as done, export a booking to Google Calendar, hide money figures |

Payment is **not** toggled on the agenda. Paid/unpaid tags there are display-only; money is entered from the tutor detail screen.

Seeded demo login: `test@test.com` / `8998`.

---

## Screens

<p align="center">
  <img src="images/tabs/agenda.png" height="40" alt="">
  <img src="images/tabs/dogs.png" height="40" alt="">
  <img src="images/tabs/tutors.png" height="40" alt="">
  <img src="images/tabs/services.png" height="40" alt="">
  <img src="images/tabs/perfil.png" height="40" alt="">
</p>

<p align="center">
  <img src="images/screenshots/agenda.png" width="160" alt="Agenda">
  &nbsp;
  <img src="images/screenshots/dogs.png" width="160" alt="Cachorros">
  &nbsp;
  <img src="images/screenshots/tutors.png" width="160" alt="Tutores">
</p>
<p align="center">
  <em>Agenda · Cães · Tutores</em>
</p>

<p align="center">
  <img src="images/screenshots/services.png" width="160" alt="Novo serviço">
  &nbsp;
  <img src="images/screenshots/perfil.png" width="160" alt="Perfil">
</p>
<p align="center">
  <em>Novo serviço · Perfil / faturamento</em>
</p>

### Tabs

1. **Cães** — dog list; open a dog for photo, tutor, description and bookings.
2. **Tutores** — tutor list; open a tutor for contact details, dogs, unpaid bill, payment history and “registrar pagamento”.
3. **Agenda** — upcoming (and optionally past) services, grouped by dog, with paid/done status tags.
4. **Novo** — create a service for a dog (passeio, pet sitting, hotel, day-care).
5. **Perfil** — account, password, monthly income breakdown, reports and backup.

Login / sign-up and restore-from-backup sit outside the tab shell.

---

## Tech stack

- **.NET 10** (`net10.0`) · **Avalonia 12.1.1** UI
- **Dapper** + **SQLite** (local DB per install)
- Custom DI / MVP navigation from the **AvaloniaFramework** git submodule
- Heads: **Desktop** (Windows/Linux), **MacOS**, **iOS**, **Android** over the same View

```
Repository.Dapper  →  Viewmodel  →  View  →  Infrastructure  →  Desktop / Mac / iOS / Android
                              ↑
                   AvaloniaFramework (submodule)
```

---

## Domain model (as implemented)

Schema names win over older “Client” wording — the code uses **Tutors** / **PetSitterTutors**.

```
PetSitter ──┬── PetSitterTutors ── Tutors ── Dogs
            │                         │
            ├── WalkingService ───────┤
            ├── PetSittingService ────┤
            ├── PetHotelService ──────┤  (+ ExtraCharge, RequiresWalking)
            ├── DayCareService ───────┤  (+ RequiresWalking)
            │                         │
            └── TutorPayments ────────┘
                    └── TutorPaymentAllocations  (Kind + ServiceId)
```

Each service row carries independent **`ServiceDone`** (work happened) and **`ServicePaid`** (money settled) flags, plus settlement columns (`AmountSettled`, `CreditApplied`). Hotel total = nights × daily rate + extra charge. Tutor **credit** is spent automatically when new bookings are created.

---

## Getting started

```bash
git clone --recursive git@github.com:sebasortiz1989/DapperDemo.git
cd DapperDemo

# if the clone was not recursive:
git submodule update --init --recursive

cd DapperDemo
dotnet build app/DapperDemo.Desktop/DapperDemo.Desktop.csproj
dotnet run --project app/DapperDemo.Desktop/DapperDemo.Desktop.csproj
dotnet test tests/Tests.Dapper/Tests.Dapper.csproj
```

Prefer building the **Desktop** head on Linux/CI: the iOS/Android/MacOS projects need mobile workloads a plain SDK image does not have. Stop a running head before rebuilding — it locks `bin/` (`MSB3027` / `MSB3021`).

---

## Repository layout

```
<repo>/
  README.md                 ← this file
  CLAUDE.md / .cursor/      ← agent guidance (skills + rules)
  images/                   ← logo, tab icons, README screenshots
  external/AvaloniaFramework/   ← git submodule
  DapperDemo/
    src/Repository.Dapper/  ← SQLite schema, DTOs, repositories, backup
    app/DapperDemo.Viewmodel/
    app/DapperDemo.View/    ← .axaml screens (pt-BR)
    app/DapperDemo.Infrastructure/
    app/DapperDemo.Desktop|MacOS|iOS|Android/
    tests/Tests.Dapper/     ← data-layer tests
```

Deep topic notes live under `.claude/skills/` (and mirrored Cursor rules): navigation, schema, money/payments, backup, styling canvas, Avalonia docs connector.

---

## References

- [Dapper](https://github.com/DapperLib/Dapper)
- [Avalonia](https://avaloniaui.net/)
- [Dapper tutorial](https://dappertutorial.net/online-examples) · [learndapper.com](https://www.learndapper.com/non-query)
