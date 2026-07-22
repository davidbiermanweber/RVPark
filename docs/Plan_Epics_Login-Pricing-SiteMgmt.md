# Implementation Plan — Three Epics

**Project:** Hill AFB FamCamp RV Park Reservation System (ASP.NET Core MVC, EF Core, .NET 10, SQL Server/Azure)
**Scope of this plan:** three epics, mapped to the requirements backlog:

| Epic | Requirement stories |
|---|---|
| **1. Login & Browse Available Sites** | G1 (register + military affiliation + email verify), G2 (sign-in / profile), G3 (search by date + type), G9 (real-time mobile availability) |
| **2. Pricing & Rule Management** | A2 (pricing by type + Oct-1 scheduled change + event/holiday/premium), A3 (stay rules), A4 (cancellation policy) |
| **3. Site & Availability Management** | S2 (block/unblock sites), site CRUD + attributes, admin availability view |

> Enforcement stories (SYS1 peak limit, SYS2 away-rule, SYS3 double-booking, SYS4 eligibility) and the booking/payment flow (G5/G6) are **out of scope here** but are consumers of the data/config these epics create. Where a decision affects them, it's noted.

---

## 0. Foundation work (do this FIRST — it unblocks Epics 1 & 3)

These are not optional; the three epics cannot be built correctly without them.

### 0.1 Add `Reservation.SiteId` FK  ⚠️ critical
`Reservation` currently has no link to `Site`. The existing `EditReservation` action encodes the assigned site into the `ReservationStatus` string (e.g. `"Active - Site 12"`) and even writes site status into `Site.Description`. This makes availability un-queryable.
- Add `public int SiteId { get; set; }` + `public Site? Site { get; set; }` to `Reservation`.
- Migration `AddReservationSiteId`. Backfill or accept nulls for legacy rows (dev DB, low risk).
- Refactor `EditReservation` to set `res.SiteId` instead of the `ReservationStatus` string hack, and stop writing status into `Site.Description`.

### 0.2 Password hashing (NFR-3)  ⚠️ security
Plaintext `Employee.Password` and no customer credential store. Introduce hashing before adding customer auth.
- Recommended: `Microsoft.AspNetCore.Identity.PasswordHasher<T>` (available without adopting full Identity) or `BCrypt.Net-Next`.
- Add `PasswordHash` to the customer account (see 1.1). Migrate `Employee` to hashed passwords (re-seed admin with a hash; update Login to verify hash).

### 0.3 Shared `IAvailabilityService`  🔑 linchpin
Both the customer browse (Epic 1) and admin availability view (Epic 3) need the same "is this site free?" logic. Build it once.
```csharp
public interface IAvailabilityService
{
    // free = no active reservation overlaps AND no SiteBlock overlaps
    Task<bool> IsSiteAvailableAsync(int siteId, DateTime start, DateTime end, int? ignoreReservationId = null);
    Task<List<Site>> AvailableSitesAsync(DateTime start, DateTime end, int? categoryId = null, int? minRvLength = null);
}
```
Overlap rule: `existing.StartDate < end && existing.FinishDate > start`, restricted to non-cancelled reservations, plus no overlapping `SiteBlock` (see 3.1). Register in DI (`builder.Services.AddScoped<IAvailabilityService, AvailabilityService>()`).

### 0.4 Role / identity decision
Today: only `Employee` logs in (AccessLevel 1=host?/2=staff/3=admin via `AdminOnlyAttribute`). Customers have no auth. **Recommendation:** keep the existing home-grown cookie scheme (low risk mid-project) and add a **customer login path** that issues a `Role=Customer` claim, rather than adopting full ASP.NET Identity now. Add named authorization policies (`AdminOnly`, `StaffOrAdmin`, `Customer`) so we stop hand-checking `AccessLevel` strings in each action.

---

## Epic 1 — Login & Browse Available Sites

### Data model
- **`User`** (the customer/guest) gains auth + eligibility fields:
  - `PasswordHash` (string)
  - `MilitaryAffiliation` (enum: ActiveDuty/Reserve/Guard/Retired/Veteran/DoDCivilian/Other) and `MilitaryStatus`
  - `IsEmailVerified` (bool), `EmailVerificationToken` (Guid/string), `TokenExpiresUtc` (DateTime?)
  - Optional: `PasswordResetToken`, `ResetExpiresUtc`
- **`Site`** gains `MaxRvLength` (int) — needed so G3 results can show "max RV length" and to feed G4 matching later. (Shared with Epic 3.)

### Controllers & views
- **`CustomerAccountController`** (new; keep `AccountController` for employees):
  - `Register` GET/POST — captures name, contact, affiliation/status; creates inactive account; generates verification token; sends email.
  - `VerifyEmail(token)` — activates account.
  - `Login` / `Logout` — verify hash, require `IsEmailVerified`, issue `Role=Customer` cookie claim.
  - `Profile` GET/POST — edit contact info + military status; password-reset request + reset actions.
- **`IEmailSender`** abstraction (new): dev implementation logs the verification link to console / writes to a file under `App_Data`; SMTP config deferred. This keeps G1 testable without a live mail server.
- **`AvailabilityController`** (public, no login required to browse):
  - `Search` GET — form: arrival, departure, site type (Category), optional RV length.
  - `Results` — calls `IAvailabilityService.AvailableSitesAsync`; shows type, current price (from `CategoryPrice`), and max RV length. Excludes booked/blocked sites.
- **Views:** `Register`, `VerifyEmailSent`, customer `Login`, `Profile`, `Search`, `Results` — all **mobile-responsive** (NFR-1). Reuse the existing Bootstrap layout.

### Migrations
`AddCustomerAuthFields`, `AddSiteMaxRvLength` (can combine with Epic 3's site-attribute migration).

### Tasks (story pts from backlog)
1. `User` auth/eligibility fields + migration — 2
2. Password hashing + email-sender abstraction (dev impl) — 3
3. Register + email verification flow (G1) — 5
4. Customer login/logout + profile/password reset (G2) — 3
5. Availability search form + results, mobile-responsive (G3/G9) — 5 *(depends on 0.1, 0.3)*

---

## Epic 2 — Pricing & Rule Management  *(mostly independent — good parallel track)*

### Data model
- **Pricing (A2):** `CategoryPrice` already supports dated prices (`StartDate`/`EndDate`, null end = current). Reservations already snapshot `DailyRate`, so scheduling a future rate set won't disturb existing bookings. Add:
  - `PriceType` enum on `CategoryPrice` (Standard / Event / Holiday / Premium) + optional `Label`. Premium capability exists but stays inactive (Want).
- **Stay rules (A3):** new single-row **`ParkPolicy`** (settings) table:
  - `BookingWindowMonths` (6), `PeakStartMonth`/`PeakEndMonth` (Apr–Oct) or explicit dates, `PeakMaxStayDays` (14), `LongTermStart` (Oct 15), `LongTermEnd` (Apr 1), `AwayBeforeReturnDays` (14).
- **Cancellation policy (A4):** fold into `ParkPolicy` or a sibling `CancellationPolicy` row:
  - `StandardFee` (10.00), `StandardThresholdDays` (3), late/holiday = one-day charge flag.
  - Note for later G8: snapshot the fee onto the cancellation record so policy edits don't retroactively change completed cancellations.

### Controllers & views
- **Pricing:** extend the existing `Categories/Edit` page (already hosts `CategoryPrices` CRUD) to support scheduling a rate set effective Oct 1 and tagging event/holiday rates. Reuse `CategoryPricesController`.
- **`ParkPolicyController`** (new, admin-only): `Edit` GET/POST for stay rules and cancellation policy (single settings screen or two tabs).

### Migrations
`AddParkPolicy`, `AddPriceTypeToCategoryPrice`. Seed one `ParkPolicy` row with the requirement defaults.

### Tasks
1. `ParkPolicy` model + seed + migration — 2
2. Stay-rules admin edit screen (A3) — 3
3. Cancellation-policy admin edit screen (A4) — 3
4. `PriceType` on CategoryPrice + Oct-1 scheduled rate-set UI + event/holiday rates (A2) — 5

---

## Epic 3 — Site & Availability Management

### Data model
- **`SiteBlock`** (new): `SiteId` FK, `StartDate`, `EndDate`, `Reason`, `CreatedByEmployeeId`, `CreatedAtUtc`. Cleanly implements S2 date-ranged block/unblock — replaces the `Site.Description` hack.
- **`Site`** attributes: `MaxRvLength` (shared w/ Epic 1), optional `IsActive` (indefinite maintenance), optional `Length`/hookup attributes.

### Controllers & views
- **Extend `SitesController`:**
  - Real `Block(siteId, start, end, reason)` / `Unblock(blockId)` actions writing `SiteBlock`; remove the Description-status hack in `EditReservation`.
  - Add `MaxRvLength`/attributes to Create/Edit forms.
- **Admin availability view** (`AvailabilityController` admin action or a `Sites/Calendar` view): grid of sites × dates showing **Open / Reserved / Blocked**, sourced from `IAvailabilityService` — gives staff the real-time picture that replaces the bulletin board (NFR-5).
- **Cleanup (tech debt):** de-duplicate the repeated Cancel/UnCancel blocks in `EditReservation`; rename `Models/Categoy.cs` → `Category.cs`.

### Migrations
`AddSiteBlock`, `AddSiteAttributes` (combine `MaxRvLength` here or with Epic 1).

### Tasks
1. `SiteBlock` model + migration — 2
2. Block/Unblock actions + views (S2) — 3
3. Site CRUD attribute additions (MaxRvLength etc.) — 2
4. Admin availability grid using `IAvailabilityService` — 5
5. Refactor `EditReservation` off the Description/status-string hack (uses 0.1) — 3

---

## Recommended build order

```
Foundation (0.1 SiteId FK, 0.2 hashing, 0.3 AvailabilityService, 0.4 roles/policies)
        │
        ├─► Epic 3  (Site & Availability Mgmt — produces real data to browse)
        │
        ├─► Epic 1  (Login & Browse — consumes AvailabilityService)
        │
        └─► Epic 2  (Pricing & Rules — largely independent; can run in parallel from the start)
```
Epic 2 has almost no dependency on the foundation and is a good parallel track for a second sub-team. Epics 1 and 3 both depend on 0.1 and 0.3, so those foundation items are the highest priority.

## Cross-cutting risks / call-outs
- ⚠️ **`Reservation.SiteId` missing** — must land first; everything availability-related is blocked on it.
- ⚠️ **Plaintext passwords** — fix with hashing as part of foundation (NFR-3).
- **Two person-models** (`Employee` vs `User`) with separate/again-no auth — plan keeps them separate to limit blast radius; a future unification into one `Account`+`Role` is possible but not recommended mid-project.
- **`AppDbContext` suppresses `PendingModelChangesWarning`** — watch for drift between the model snapshot and migrations; run `dotnet ef migrations add` deliberately.
- **Tech debt to clean while in the code:** duplicated Cancel/UnCancel logic in `EditReservation`, `Categoy.cs` filename typo, site-status-in-Description hack.

## Suggested verification per epic
- Epic 1: register → receive (logged) verify link → verify → login → search a date range → confirm booked/blocked sites are excluded and prices/max-length show.
- Epic 2: edit a rate effective Oct 1, confirm existing reservation `DailyRate` unchanged; edit stay/cancellation policy and confirm persisted defaults.
- Epic 3: block a site for a range → confirm it disappears from both admin grid and customer search for that range → unblock → reappears.
