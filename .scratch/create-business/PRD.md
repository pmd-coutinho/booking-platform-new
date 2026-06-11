Status: ready-for-agent

# PRD: Create Business

## Problem Statement

Platform Admins need a way to create the first domain object in the booking platform: a Registered Business shell with an initial Business Manager Invitation. Today the Server only has a stub booking endpoint and no event-sourced business lifecycle. Without this slice, there is no place to start Business Onboarding, no accepted Business Manager path, and no explicit Bookability status for downstream scheduling work.

This first slice should establish the event-sourced foundation without implementing the full booking platform. It should prove that the platform can append domain events for Business setup, expose the behavior through the Server, and persist those events in the Booking Database.

## Solution

Add a `CreateBusiness` capability for Platform Admins.

The Platform Admin provides a business name, manager email, and invitation expiry. The Server generates a Business identity and a Business Manager Invitation identity, then appends three events to a new Business stream in one transaction:

- `BusinessCreated`
- `BusinessManagerInvited`
- `BusinessBookabilityChanged` with status `Unbookable` and structured Bookability Reasons

The response returns the generated Business id, Invitation id, current Bookability status, and current Bookability Reasons. The created Business is not Bookable yet because no Business Manager has accepted the invitation and Business Onboarding is incomplete.

## User Stories

1. As a Platform Admin, I want to create a Business shell, so that the platform has a Registered Business to onboard.
2. As a Platform Admin, I want to provide the Business name during creation, so that the Registered Business can be identified internally before its full Business Profile exists.
3. As a Platform Admin, I want to invite an initial Business Manager during Business creation, so that someone can later accept responsibility for Business Onboarding.
4. As a Platform Admin, I want to choose the invitation expiry, so that the invitation is not valid indefinitely.
5. As a Platform Admin, I want invitation expiry to be validated against platform bounds, so that I cannot accidentally create unsafe long-lived invitations.
6. As a Platform Admin, I want the Server to generate the Business id, so that callers do not control domain identities.
7. As a Platform Admin, I want the Server to generate the Business Manager Invitation id, so that each invitation can be accepted, expired, or audited independently.
8. As a Platform Admin, I want duplicate Business names to be allowed, so that real-world businesses with the same name can still be onboarded.
9. As a Platform Admin, I want Business creation and Business Manager invitation to succeed or fail together, so that the platform does not create managerless shell records accidentally.
10. As a Platform Admin, I want the created Business to be explicitly Unbookable, so that no customers can schedule appointments before Business Onboarding is complete.
11. As a Platform Admin, I want the Unbookable status to include structured Bookability Reasons, so that the UI can explain what remains incomplete.
12. As a Platform Admin, I want the initial reasons to include that no Business Manager has accepted yet, so that the next onboarding action is clear.
13. As a Platform Admin, I want the initial reasons to include that Business Onboarding is incomplete, so that setup requirements are visible immediately.
14. As a Platform Admin, I want Business creation to be event-sourced, so that the Business lifecycle is auditable from domain facts.
15. As a Platform Admin, I want actor role and actor identity to be captured as event metadata, so that later audits can distinguish Platform Admin actions from Business Manager actions.
16. As a Platform Admin, I want invalid manager emails to be rejected, so that unusable invitations are not created.
17. As a Platform Admin, I want blank Business names to be rejected, so that Registered Businesses have a meaningful internal name.
18. As a Platform Admin, I want invitation expiry in the past to be rejected, so that already-expired invitations are not created.
19. As a Platform Admin, I want invitation expiry beyond the platform maximum to be rejected, so that invitation lifetime stays bounded.
20. As a Platform Admin, I want a successful response to include the generated ids, so that follow-up commands can address the Business and invitation.
21. As a Platform Admin, I want a successful response to include the current Bookability status, so that the caller does not need a second read to know the created state.
22. As a Platform Admin, I want a successful response to include the current Bookability Reasons, so that the caller can immediately show onboarding progress.
23. As an AFK implementation agent, I want the first slice to be small and vertically complete, so that it can be implemented safely before the larger booking lifecycle.
24. As an AFK implementation agent, I want the command to respect the Business stream boundary ADR, so that Business onboarding starts consistently with the documented architecture.
25. As an AFK implementation agent, I want the command to respect the domain-event-sourcing ADR, so that the write model does not drift into CRUD for core domain state.
26. As an AFK implementation agent, I want the HTTP endpoint to stay thin, so that domain decisions remain testable outside transport code.
27. As an AFK implementation agent, I want the event stream to contain separate events for creation, invitation, and Bookability, so that each domain fact can evolve independently.
28. As a future Business Manager, I want my invitation to be represented by its own invitation id, so that acceptance and expiry target the correct invitation.
29. As a future Business Manager, I want my invitation to belong to the Business lifecycle, so that onboarding state is visible from the Business stream.
30. As a future product builder, I want this slice to leave room for multiple Business Managers later, so that accepting one invitation does not imply a one-manager limit.

## Implementation Decisions

- Build the first slice around the `CreateBusiness` command.
- `CreateBusiness` is initiated by a Platform Admin.
- The command input is `businessName`, `managerEmail`, and `invitationExpiresAt`.
- Platform Admin identity is not part of the request body; it is actor context and should be recorded as event metadata.
- The Server generates `businessId` and `invitationId`.
- Business names are not globally unique.
- Manager email must be syntactically valid enough for invitation workflows.
- Invitation expiry is admin-chosen but must be within platform-defined bounds.
- Recommended invitation expiry bounds are default 7 days and maximum 30 days. This slice only needs to validate the provided expiry against the maximum and reject past values.
- Append `BusinessCreated`, `BusinessManagerInvited`, and `BusinessBookabilityChanged` to one new Business event stream in one transaction.
- Use one Business event stream for Business onboarding/setup for now, as recorded in ADR 0004.
- `BusinessCreated` records the generated Business id and Business name.
- `BusinessManagerInvited` records the generated invitation id, manager email, and expiry.
- `BusinessBookabilityChanged` records status `Unbookable` and a list of structured Bookability Reasons.
- Initial Bookability Reasons should include `ManagerNotAccepted` and `OnboardingIncomplete`.
- Bookability is a reversible status, not a one-way publication milestone.
- Do not create a Business Profile in this slice.
- Do not claim a Public Booking Slug in this slice.
- Do not create Business Hours, Appointment Types, Staff Members, Staff Capabilities, Staff Availability, or Staff Schedule Day streams in this slice.
- The response should return generated Business id, generated invitation id, Bookability status, and Bookability Reasons.
- The command should be implemented through the Server using the existing Wolverine/Marten direction in the codebase.
- Endpoint code should remain thin and should not directly depend on Marten, JasperFx event APIs, or Wolverine.Marten details.
- Domain decisions should be testable without going through HTTP.
- Invitation expiry orchestration through Wolverine sagas is a later slice; this slice only needs to persist invitation expiry in the event.
- Real authentication and email delivery are out of scope; domain-first actor context may be stubbed or represented minimally as long as events can carry actor metadata.
- Event payloads should stay focused on domain facts. Actor role and actor identity should be stored as event metadata/envelope data rather than duplicated into each event payload.

## Testing Decisions

- Test external behavior at the highest seam first using Server integration tests.
- Use the existing Alba integration-test style with a PostgreSQL Testcontainer as prior art.
- Integration tests should call the Create Business HTTP behavior and assert the response contains the generated identifiers, Unbookable status, and expected Bookability Reasons.
- Integration tests should verify that the Business stream is persisted with the expected three domain events.
- Integration tests should verify invalid requests are rejected: blank Business name, invalid manager email, past expiry, and expiry beyond the platform maximum.
- Unit tests should cover the Business domain decision logic without HTTP transport concerns.
- Unit tests should cover initial Business creation producing the expected domain events.
- Unit tests should cover validation of invitation expiry bounds.
- Unit tests should cover initial Bookability Reasons.
- Unit tests should cover that duplicate Business names are allowed at the domain level.
- Architecture tests should be expanded if needed to protect the intended seams.
- Architecture tests should continue preventing endpoint/features code from directly depending on Marten, JasperFx event APIs, or Wolverine.Marten infrastructure.
- Architecture tests should prefer enforcing thin endpoints and keeping infrastructure concerns out of domain decision code.
- Tests should assert observable behavior and domain facts, not incidental implementation details such as private helper names or exact internal file organization.

## Out of Scope

- Accepting Business Manager Invitations.
- Expiring Business Manager Invitations through Wolverine sagas.
- Sending invitation emails.
- Real authentication or authorization enforcement.
- Business Profile completion.
- Public Booking Slug claiming or uniqueness reservation.
- Custom Booking Domains.
- Business Hours.
- Business Closures or Business Special Openings.
- Staff Members.
- Staff Invitations.
- Staff Availability, Staff Special Availability, or Staff Time Off.
- Appointment Types.
- Staff Capabilities.
- Available Slot generation.
- Scheduling Service Appointments.
- Staff Schedule Day streams.
- Booking Contact, Customer, Business Customer, and No-Show Risk.
- Payments, deposits, or in-person payment tracking.

## Further Notes

- This PRD intentionally starts with the dependency-free foundation slice rather than scheduling. Scheduling depends on Business, manager onboarding, Business Profile, Appointment Types, Staff Members, Staff Capabilities, and availability.
- The relevant ADRs are ADR 0003 for event-sourcing scope and ADR 0004 for the Business stream boundary.
- ADR 0001 becomes relevant when scheduling starts; this slice should not create Staff Schedule Day streams yet.
- ADR 0005 becomes relevant when Public Booking Slug claiming is implemented; this slice should not create slug reservations yet.
- The current codebase has Marten and Wolverine configured, but the booking domain is still a stub. This slice should replace the direction of that stub with the first real event-sourced domain capability.
