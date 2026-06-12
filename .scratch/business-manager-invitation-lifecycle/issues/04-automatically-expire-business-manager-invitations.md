Status: ready-for-agent

# Automatically Expire Business Manager Invitations

## Parent

`.scratch/business-manager-invitation-lifecycle/PRD.md`

## What to build

Add automatic expiry for pending Business Manager Invitations at their original expiry deadline through Wolverine scheduling. Expiry should reconstruct Business state, append an expired invitation fact only when the invitation is pending and due, and otherwise complete as a no-op. Expiry records lifecycle history but does not change Bookability because the Business remains blocked by the same missing-manager and incomplete-onboarding state unless an invitation has already been accepted.

The completed slice should be verifiable by creating a Business with an invitation expiry, observing scheduled expiry behavior, and confirming the Business stream records exactly one expiry event when due.

## Acceptance criteria

- [ ] Creating a Business schedules automatic expiry for its initial Business Manager Invitation at the original invitation expiry deadline.
- [ ] When the scheduled expiry runs for a pending due invitation, it appends `BusinessManagerInvitationExpired` with invitation identity and the original expiry deadline as expired time.
- [ ] Expiry reconstructs Business state from Business stream history before deciding.
- [ ] Expiry appends no `BusinessBookabilityChanged` event.
- [ ] Expiry is a no-op if the Business Manager Invitation has already been accepted.
- [ ] Expiry is a no-op if the Business Manager Invitation has already expired.
- [ ] Expiry is a no-op if the Business Manager Invitation is not due yet.
- [ ] Duplicate or retried scheduled expiry delivery does not append duplicate expiry events.
- [ ] Wolverine scheduling tests verify the expiry message is scheduled and can be played/executed in tests.
- [ ] Integration or handler-level tests verify due expiry and no-op expiry outcomes against persisted Business stream facts.

## Blocked by

- `.scratch/business-manager-invitation-lifecycle/issues/02-accept-business-manager-invitation-happy-path.md`
