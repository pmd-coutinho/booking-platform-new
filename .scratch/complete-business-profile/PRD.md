Status: ready-for-agent

# PRD: Complete Business Profile

## Problem Statement

Platform Admins can create Registered Businesses and Business Managers can accept invitations, but there is no way to set the Business Profile. The Business remains Unbookable because OnboardingIncomplete is still a blocker, even when the manager is ready to configure the public-facing identity. Without this slice, the onboarding chain cannot progress, and the Business cannot accumulate the setup needed for scheduling.

This slice must also fix the Bookability Reason model so the platform knows exactly why a Business is not Bookable at each step.

## Solution

Add a `CompleteBusinessProfile` capability for Business Managers.

The Business Manager provides the public-facing business name, Public Booking Slug, Business Contact, Business Address, Business Time Zone, and Business Currency. The Server validates the Business exists, reconstructs state from the Business stream, verifies a manager has accepted an invitation, verifies the profile is not already completed, validates all fields (including DNS label rules for the slug and IANA/ISO formats for timezone and currency), reserves the Public Booking Slug through a strongly consistent database reservation, and appends the `BusinessProfileCompleted` event to the Business stream. If the Business is now Bookable (only missing ProfileIncomplete), it also appends `BusinessBookabilityChanged` with status `Bookable` and no reasons.

The response returns the current Bookability status and the full set of Bookability Reasons.

## User Stories

1. As a Business Manager, I want to complete the Business Profile, so that the business has a public identity.
2. As a Business Manager, I want the public-facing business name to be required, so that customers see a meaningful name on the booking page.
3. As a Business Manager, I want the Public Booking Slug to be required, so that customers can find the business by a unique subdomain.
4. As a Business Manager, I want the slug to be validated as a valid DNS label, so that the booking subdomain works correctly.
5. As a Business Manager, I want the slug to be globally unique, so that no two businesses share the same subdomain.
6. As a Business Manager, I want duplicate slug attempts to be rejected clearly, so that I can choose a different one.
7. As a Business Manager, I want the Business Contact (phone and email) to be required, so that customers can reach the business.
8. As a Business Manager, I want the Business Address (street, city, postal code, country) to be required, so that the business location is known.
9. As a Business Manager, I want the Business Time Zone to be a valid IANA ID, so that appointment times are interpreted correctly.
10. As a Business Manager, I want the Business Currency to be a valid ISO 4217 code, so that appointment prices are shown in the right currency.
11. As a Business Manager, I want invalid timezone or currency values to be rejected, so that the system cannot be configured with bad data.
12. As a Business Manager, I want the profile completion command to be rejected if no Business Manager has accepted an invitation yet, so that unclaimed businesses cannot be configured.
13. As a Business Manager, I want the profile completion command to be rejected if the profile is already completed, so that the event model does not duplicate the same fact.
14. As a Business Manager, I want successful profile completion to clear the `ProfileIncomplete` Bookability Reason, so that the remaining blockers are visible.
15. As a Business Manager, I want the Business to become Bookable if the profile was the only missing onboarding piece, so that customers can schedule appointments immediately.
16. As a Business Manager, I want the Business to remain Unbookable if staff, appointment types, or business hours are still missing, so that the onboarding chain is accurate.
17. As a Business Manager, I want a successful response to include the current Bookability status and reasons, so that the UI shows the next steps immediately.
18. As a product builder, I want the Business stream to record the profile completion event, so that the Business lifecycle remains auditable.
19. As a product builder, I want the slug reservation to be strongly consistent with the event append, so that concurrent completions cannot race for the same slug.
20. As an AFK implementation agent, I want the domain command to validate everything and return events, so that the endpoint remains thin.
21. As an AFK implementation agent, I want the slug reservation to be a small database record, not a domain event, so that the reservation enforces the invariant but does not carry domain history.
22. As an AFK implementation agent, I want the Bookability Reason model to be fixed before adding this slice, so that `OnboardingIncomplete` is replaced with granular reasons.

## Implementation Decisions

- Build the slice around the `CompleteBusinessProfile` command.
- `CompleteBusinessProfile` is initiated by a Business Manager.
- The command input is `businessId`, `publicBusinessName`, `publicBookingSlug`, `contactPhone`, `contactEmail`, `street`, `city`, `postalCode`, `country`, `timeZone`, `currency`.
- Actor metadata is captured as event metadata, not in the event payload.
- The Business aggregate is reconstructed from the Business stream before decisions.
- The command validates that the Business exists (stream has events), that a manager invitation has been accepted, and that the profile is not already completed.
- The command validates all fields: non-empty strings, valid email format for contact email, valid IANA timezone ID, valid ISO 4217 currency code, DNS label rules for the slug.
- DNS label rules for the slug: lowercase letters, numbers, and hyphens only; no leading or trailing hyphen; 1-63 characters.
- Public Booking Slug uniqueness is enforced via a strongly consistent database reservation table alongside the event append.
- The reservation table stores `slug` as primary key and `business_id`.
- The command appends `BusinessProfileCompleted` with all profile fields.
- If the Business is now Bookable (no remaining blockers), it also appends `BusinessBookabilityChanged` with status `Bookable` and empty reasons.
- If the Business remains Unbookable, it also appends `BusinessBookabilityChanged` with status `Unbookable` and the remaining reasons.
- The Bookability Reason model is fixed before this slice: `OnboardingIncomplete` is replaced with `ProfileIncomplete`, `NoStaffMembers`, `NoAppointmentTypes`, `NoStaffCapabilities`, `NoBusinessHours`, `NoStaffAvailability`.
- After manager acceptance, initial reasons are: `ProfileIncomplete`, `NoStaffMembers`, `NoAppointmentTypes`, `NoStaffCapabilities`, `NoBusinessHours`, `NoStaffAvailability`.
- Profile completion clears `ProfileIncomplete`.
- Future slices will clear `NoStaffMembers`, `NoAppointmentTypes`, `NoStaffCapabilities`, `NoBusinessHours`, `NoStaffAvailability` as setup progresses.
- The Business becomes Bookable only when the reason set is empty.
- The response returns `BookabilityStatus` and `BookabilityReasons`.
- The endpoint route is `POST /api/businesses/{businessId}/profile`.
- The endpoint remains thin; domain decisions are in the aggregate.
- The handler coordinates the slug reservation, event append, and response.
- Update the existing `BusinessManagerInvitationAccepted` event application to set initial granular reasons instead of `OnboardingIncomplete`.
- Update the existing `BusinessCreated` event application to set initial reasons to `ManagerNotAccepted` plus all granular onboarding reasons (or just `ManagerNotAccepted` until acceptance clears it and reveals the rest).
- Actually, simpler: after `BusinessCreated`, reasons are `ManagerNotAccepted` only. After `BusinessManagerInvitationAccepted`, reasons become the full set of onboarding blockers (`ProfileIncomplete`, `NoStaffMembers`, etc.). This means the acceptance event changes the reason set.
- Wait, that means accepting the invitation changes bookability even though the business remains Unbookable. That's fine — the reasons change from `ManagerNotAccepted` to the granular list.
- Alternatively, keep the initial reasons as `ManagerNotAccepted` and `ProfileIncomplete` and all others, but `ManagerNotAccepted` hides the rest until it's cleared. After acceptance, the reasons should be `ProfileIncomplete`, `NoStaffMembers`, etc.
- Let's go with: after `BusinessCreated`, reasons are `ManagerNotAccepted`. After `BusinessManagerInvitationAccepted`, reasons are `ProfileIncomplete`, `NoStaffMembers`, `NoAppointmentTypes`, `NoStaffCapabilities`, `NoBusinessHours`, `NoStaffAvailability`. This is a clear transition.
- This requires updating the existing acceptance handler and tests to use the new granular reasons.
- The endpoint returns `200 OK` with the response, or `400 Bad Request` for validation errors, or `404 Not Found` for missing business or not-yet-accepted manager, or `409 Conflict` for duplicate slug or already-completed profile.
- Do not create a read model or projection in this slice.
- Do not implement profile updates in this slice.
- Do not create staff, appointment types, business hours, or availability in this slice.
- Do not send emails or implement real authentication.

## Testing Decisions

- Integration tests should call the endpoint via Alba with a PostgreSQL Testcontainer.
- Integration tests should assert the response contains Bookable status when profile is the only missing piece.
- Integration tests should assert the response contains Unbookable status with remaining reasons when other setup is missing.
- Integration tests should verify the Business stream contains `BusinessProfileCompleted` and optionally `BusinessBookabilityChanged`.
- Integration tests should verify the slug reservation table contains the claimed slug.
- Integration tests should verify duplicate slug claims are rejected (409 Conflict).
- Integration tests should verify invalid slug formats are rejected (400 Bad Request).
- Integration tests should verify invalid timezone or currency are rejected (400 Bad Request).
- Integration tests should verify missing business returns 404.
- Integration tests should verify unaccepted manager returns 404 (or 409, depending on design).
- Integration tests should verify already-completed profile returns 409.
- Unit tests should cover the Business domain decision: profile completion with all fields, validation of each field, clearing of ProfileIncomplete, transition to Bookable when appropriate.
- Unit tests should cover slug DNS label validation.
- Unit tests should cover the updated acceptance event application (new granular reasons).
- Architecture tests should continue to enforce thin endpoints and no direct Marten/JasperFx/Wolverine.Marten dependencies in endpoints.
- Tests should assert externally visible behavior, not implementation details.

## Out of Scope

- Profile updates (editing an already completed profile).
- Custom Booking Domains.
- Staff Members.
- Appointment Types.
- Staff Capabilities.
- Business Hours.
- Staff Availability.
- Public Booking Slug infrastructure setup (DNS, routing).
- Customer booking.
- Read models and projections.
- Real authentication.
- Email delivery.

## Further Notes

- This slice follows the Business Manager Invitation acceptance slice.
- Relevant ADRs: ADR 0003 (event sourcing), ADR 0004 (one Business stream), ADR 0005 (cross-stream uniqueness), ADR 0007 (granular bookability reasons).
- The slug reservation is a consistency mechanism, not a domain event. It should be treated as infrastructure.
