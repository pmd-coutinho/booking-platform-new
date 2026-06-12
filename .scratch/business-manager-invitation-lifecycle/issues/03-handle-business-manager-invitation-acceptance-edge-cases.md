Status: ready-for-agent

# Handle Business Manager Invitation Acceptance Edge Cases

## Parent

`.scratch/business-manager-invitation-lifecycle/PRD.md`

## What to build

Complete the acceptance decision tree for invalid, duplicate, and blocked Business Manager Invitation acceptance attempts. Acceptance should append no events unless the addressed invitation is pending, belongs to the supplied normalized email identity, and has not passed its expiry deadline. Same-email retries after successful acceptance should be harmless and return the current accepted state without duplicating events.

The completed slice should make acceptance safe under user retries and invalid links while keeping Business stream history clean.

## Acceptance criteria

- [x] Acceptance for a missing Business rejects and appends no events.
- [x] Acceptance for a missing Business Manager Invitation rejects and appends no events.
- [x] Acceptance with a different manager email identity rejects and appends no events.
- [x] Acceptance after the invitation expiry deadline rejects and appends no events.
- [x] Acceptance after the invitation expiry deadline does not opportunistically append `BusinessManagerInvitationExpired`.
- [x] Repeating acceptance with the same manager email identity after prior acceptance returns current accepted state and appends no events.
- [x] Repeating acceptance with a different manager email identity after prior acceptance rejects and appends no events.
- [x] Tests assert event counts or stream facts to prove no-event outcomes where required.
- [x] Unit tests cover all acceptance no-event outcomes at the Business domain decision seam.
- [x] Integration tests cover representative HTTP no-event outcomes through the Server.

## Blocked by

- `.scratch/business-manager-invitation-lifecycle/issues/02-accept-business-manager-invitation-happy-path.md`
