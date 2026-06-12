Status: needs-human

## Parent

PRD: `.scratch/complete-business-profile/PRD.md`

## What to build

Add the `CompleteBusinessProfile` command to the Business aggregate.

The command validates all fields: non-empty public business name, valid DNS label for Public Booking Slug (lowercase letters, numbers, hyphens; no leading/trailing hyphen; 1-63 characters), non-empty Business Contact (phone and email), non-empty Business Address (street, city, postal code, country), valid IANA timezone ID, valid ISO 4217 currency code.

Add the `BusinessProfileCompleted` domain event containing all profile fields.

Compute the new Bookability status after profile completion: if `ProfileIncomplete` was the only missing onboarding reason, emit `BusinessBookabilityChanged` with status `Bookable` and an empty reason set; otherwise, emit `BusinessBookabilityChanged` with status `Unbookable` and the remaining granular reasons.

Add a slug reservation table in the Booking Database (reservation of `slug` with `business_id`). Enforce uniqueness strongly and transactionally alongside the event append. The reservation is a consistency mechanism, not a domain event.

The command is rejected if the business has no accepted manager, if the profile is already completed, or if the slug is already reserved.

## Acceptance criteria

- [ ] `CompleteBusinessProfile` domain command validates all fields and returns a `BusinessProfileCompleted` event.
- [ ] `BusinessProfileCompleted` event contains all profile fields.
- [ ] Bookability transitions correctly after profile completion: `Bookable` if only `ProfileIncomplete` was missing, otherwise `Unbookable` with remaining reasons.
- [ ] Slug reservation table is created and enforces uniqueness at the database level.
- [ ] Concurrent slug claims are rejected with a clear error.
- [ ] Command rejects missing business, unaccepted manager, or already-completed profile.

## Blocked by

- `.scratch/complete-business-profile/issues/01-fix-granular-bookability-reasons.md`
