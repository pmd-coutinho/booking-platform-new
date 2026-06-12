Status: ready-for-agent

# PRD: Business Manager Invitation Lifecycle

## Problem Statement

Platform Admins can create a Registered Business with an initial Business Manager Invitation, but the invitation currently has no lifecycle after creation. A future Business Manager cannot accept responsibility for Business Onboarding, expired invitations cannot be represented as domain history, and the Business remains blocked by `ManagerNotAccepted` with no way to move forward.

The previous slice also established the intended event-sourced Business foundation, but the next slice must ensure the Critter Stack seams do not drift: Server endpoints should stay thin, Business decisions should be reconstructed from Business stream history, and persistence/scheduling concerns should remain outside domain decision code.

## Solution

Add a Business Manager Invitation lifecycle for the initial invitation created during Business creation.

A future Business Manager accepts a pending Business Manager Invitation by providing the Business identity, invitation identity, and their email identity. The Server normalizes the email identity, reconstructs the current Business state from the Business stream, verifies that the invitation exists, belongs to that email identity, has not expired, and has not already been accepted, then appends the accepted invitation fact. Acceptance clears the `ManagerNotAccepted` Bookability Reason while leaving the Business `Unbookable` because Business Onboarding remains incomplete.

The platform also automatically expires pending Business Manager Invitations at their original expiry deadline through Wolverine scheduling. Expiry records the expired invitation fact in the Business stream and is safe to run repeatedly or after acceptance. Expiry does not change Bookability Reasons because the Business is still missing an accepted Business Manager for the same reason as before.

## User Stories

1. As a future Business Manager, I want to accept my Business Manager Invitation, so that I can become responsible for Business Onboarding.
2. As a future Business Manager, I want the acceptance flow to recognize my email identity regardless of casing, so that ordinary email casing differences do not block onboarding.
3. As a future Business Manager, I want accidental whitespace around my email identity to be ignored, so that copy-paste mistakes do not block acceptance.
4. As a future Business Manager, I want acceptance to succeed only for the invitation addressed to me, so that another person cannot use my invitation.
5. As a future Business Manager, I want a repeated acceptance attempt after a successful acceptance to be harmless, so that browser retries or double-clicks do not corrupt Business history.
6. As a future Business Manager, I want an expired invitation to reject acceptance, so that expired access is not granted.
7. As a future Business Manager, I want a missing invitation to be reported clearly, so that I know the acceptance link or request is invalid.
8. As a future Business Manager, I want a missing Business to be reported clearly, so that I know the acceptance link or request is invalid.
9. As a future Business Manager, I want successful acceptance to return the current Bookability status, so that the caller can show the next onboarding state immediately.
10. As a future Business Manager, I want successful acceptance to return the current Bookability Reasons, so that the caller can explain that Business Onboarding is still incomplete.
11. As a future Business Manager, I want successful acceptance to preserve the Business as Unbookable, so that customers cannot schedule Service Appointments before setup is complete.
12. As a future Business Manager, I want successful acceptance to clear `ManagerNotAccepted`, so that the remaining onboarding blocker is accurate.
13. As a future Business Manager, I want successful acceptance to keep `OnboardingIncomplete`, so that the platform still shows setup work remains.
14. As a Platform Admin, I want a pending Business Manager Invitation to expire automatically, so that invitations do not remain valid indefinitely.
15. As a Platform Admin, I want invitation expiry to be recorded as Business stream history, so that audits can explain why an invitation can no longer be accepted.
16. As a Platform Admin, I want expiry to record the original expiry deadline, so that domain history reflects when the invitation became invalid.
17. As a Platform Admin, I want expiry to be safe if the invitation was already accepted, so that scheduled work cannot undo acceptance.
18. As a Platform Admin, I want expiry to be safe if the expiry message runs more than once, so that durable message retries do not duplicate domain facts.
19. As a Platform Admin, I want expiry to leave Bookability unchanged, so that the Business does not emit redundant Bookability history for the same blocked state.
20. As a Platform Admin, I want the first accepted Business Manager Invitation to clear the missing-manager blocker, so that future support for multiple managers still has the right invariant.
21. As a Platform Admin, I want future multiple Business Manager Invitations to be possible for different email identities, so that the model does not assume only one manager forever.
22. As a Platform Admin, I want duplicate invitation-per-email enforcement to wait until additional invitation issuing exists, so that this slice does not build unused enforcement.
23. As a product builder, I want invitation acceptance to use the Business identity and invitation identity, so that the command aligns with the Business stream boundary.
24. As a product builder, I want Business state reconstructed from events before acceptance decisions, so that command behavior reflects the event-sourced model.
25. As a product builder, I want Business state reconstructed from events before expiry decisions, so that scheduled expiry is idempotent and state-aware.
26. As a product builder, I want Business domain decisions to return domain events rather than persisting directly, so that the domain remains infrastructure-free.
27. As a product builder, I want event payloads to contain domain facts rather than transport or authentication details, so that Business history remains stable as auth evolves.
28. As a product builder, I want acceptance to omit actor metadata for now, so that event headers do not duplicate the invitee identity already captured by the domain fact.
29. As a product builder, I want Server endpoints to stay thin, so that Wolverine, Marten, and JasperFx persistence details do not become endpoint logic.
30. As a product builder, I want the existing create-Business persistence seam cleaned up before adding this lifecycle, so that future slices follow a consistent Critter Stack pattern.
31. As an AFK implementation agent, I want a narrow architectural cleanup prerequisite, so that later lifecycle work can be implemented without reinforcing the current endpoint coupling.
32. As an AFK implementation agent, I want the acceptance path to be verifiable through the Server, so that tests prove the externally visible behavior and persisted Business stream facts.
33. As an AFK implementation agent, I want the expiry path to be verifiable through Wolverine scheduled message behavior, so that tests prove automatic expiry is durable and idempotent.
34. As an AFK implementation agent, I want acceptance failures to append no events, so that invalid attempts do not pollute Business history.
35. As an AFK implementation agent, I want expiry no-ops to append no events, so that retries and already-final invitations do not pollute Business history.
36. As an AFK implementation agent, I want existing Create Business behavior to keep passing, so that the lifecycle slice does not regress the foundation slice.

## Implementation Decisions

- Build this feature as the Business Manager Invitation lifecycle for the initial invitation created during Business creation.
- Keep Business Manager Invitation lifecycle events in the Business event stream, respecting the documented one-Business-stream onboarding boundary.
- Preserve the domain-event-sourcing direction: Business lifecycle changes are domain events; operational scheduling state is not domain history.
- Clean up the current persistence/application seam before adding the lifecycle so Server endpoint behavior remains thin and does not directly own Marten, JasperFx event APIs, or Wolverine.Marten operations.
- Reconstruct current Business state from Business stream events before making acceptance or expiry decisions.
- Add event application behavior to the Business aggregate for creation, invitation, invitation acceptance, invitation expiry, and Bookability changes.
- Track enough Business state to decide invitation lifecycle commands: Business identity, Business name, Bookability status, Bookability Reasons, and Business Manager Invitations keyed by invitation identity.
- Track invitation lifecycle state as pending, accepted, or expired.
- Keep invitation acceptance addressed by both Business identity and invitation identity.
- Use the route shape `POST /api/businesses/{businessId}/manager-invitations/{invitationId}/accept` for acceptance.
- The acceptance request supplies `managerEmail` as temporary invitee proof while real authentication remains out of scope.
- Normalize manager email identities by trimming whitespace and comparing case-insensitively.
- Successful acceptance appends `BusinessManagerInvitationAccepted` with invitation identity, normalized manager email, and server-supplied accepted time.
- Accepted time comes from Server time, not from the request body.
- Successful acceptance appends `BusinessBookabilityChanged` only because the externally visible Bookability Reasons change.
- After successful acceptance, Bookability status remains `Unbookable`.
- After successful acceptance, Bookability Reasons contain `OnboardingIncomplete` and no longer contain `ManagerNotAccepted`.
- A repeated acceptance attempt by the same manager email after successful acceptance returns the current accepted state and appends no events.
- An acceptance attempt by a different email identity rejects and appends no events.
- An acceptance attempt for a missing Business rejects and appends no events.
- An acceptance attempt for a missing Business Manager Invitation rejects and appends no events.
- An acceptance attempt after the invitation expiry deadline rejects and appends no events.
- Do not append an expiry event opportunistically from the acceptance command when acceptance arrives after the expiry deadline.
- Automatically expire pending Business Manager Invitations at their original expiry deadline through Wolverine scheduling.
- Expiry appends `BusinessManagerInvitationExpired` with invitation identity and the original expiry deadline as expired time.
- Expiry is a no-op if the invitation has already been accepted.
- Expiry is a no-op if the invitation has already expired.
- Expiry is a no-op if the invitation is not due yet.
- Expiry does not append `BusinessBookabilityChanged` because Bookability status and reasons do not change.
- The first accepted Business Manager Invitation clears the missing-manager blocker, preserving room for future multiple Business Managers.
- Do not introduce a separate Business Manager identity yet; normalized manager email is sufficient for this slice.
- Do not enforce duplicate invitation-per-email rules in this slice because no additional invitation-issuing command exists yet.
- Do not add actor metadata to the acceptance event in this slice because the invitee identity is already the domain fact and real authentication is out of scope.

## Testing Decisions

- Test the highest existing seam first: Server integration tests should exercise Business creation followed by Business Manager Invitation acceptance through HTTP and verify the response plus persisted Business stream events.
- Use the existing Alba integration-test style with a PostgreSQL Testcontainer as prior art.
- Acceptance integration tests should assert the accepted event and Bookability change are persisted in the Business stream.
- Acceptance integration tests should assert the response includes Business identity, invitation identity, normalized manager email, Bookability status, and Bookability Reasons.
- Acceptance integration tests should cover invalid email identity, missing Business, missing invitation, already expired invitation, and same-email retry without appending duplicate events.
- Unit tests should exercise Business domain decision logic without HTTP, Marten, Wolverine, or scheduling concerns.
- Unit tests should cover event reconstruction through Business event application behavior.
- Unit tests should cover email normalization and case-insensitive invitation matching.
- Unit tests should cover acceptance state transitions and no-event outcomes.
- Wolverine/scheduling tests should verify that invitation expiry is scheduled from the lifecycle and that due expiry appends the expired event.
- Wolverine/scheduling tests should verify scheduled expiry is no-op after acceptance and no-op when delivered more than once.
- Architecture tests should continue enforcing thin endpoint behavior and should fail if feature endpoints depend directly on Marten, JasperFx event APIs, Npgsql, or Wolverine.Marten details.
- Tests should assert externally visible behavior and domain facts, not private helper names, exact internal file organization, or incidental implementation details.

## Out of Scope

- Real authentication or authorization enforcement.
- Email delivery for Business Manager Invitations.
- Issuing replacement Business Manager Invitations after expiry.
- Issuing additional Business Manager Invitations after Business creation.
- Enforcing one invitation per email per Business through a new write path.
- Creating a separate Business Manager identity.
- Creating user accounts or linking to identity-provider subjects.
- Business Profile completion.
- Public Booking Slug claiming or uniqueness reservation.
- Custom Booking Domains.
- Business Hours.
- Business Closures or Business Special Openings.
- Appointment Types.
- Staff Members.
- Staff Invitations.
- Staff Availability, Staff Special Availability, or Staff Time Off.
- Staff Capabilities.
- Available Slot generation.
- Scheduling Service Appointments.
- Staff Schedule Day streams.
- Customer, Booking Contact, Business Customer, and No-Show Risk.

## Further Notes

- This slice follows the domain sequence established by Business creation: create Registered Business, invite Business Manager, accept or expire the Business Manager Invitation, then continue Business Onboarding.
- The slice intentionally does not make the Business Bookable. Acceptance only clears the missing-manager blocker; onboarding setup remains incomplete.
- The relevant ADRs are the domain-event-sourcing ADR, the one-Business-stream onboarding ADR, and the cross-stream uniqueness ADR by omission: Public Booking Slug claiming remains out of scope.
- No new ADR is required for this PRD because the decisions are direct consequences of the existing Business stream and event-sourcing decisions.
