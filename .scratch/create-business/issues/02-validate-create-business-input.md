Status: ready-for-human

# Validate Create Business Input

## Parent

`.scratch/create-business/PRD.md`

## What to build

Add end-to-end validation to `CreateBusiness` so invalid requests do not append Business events. The command should reject unusable Business names, unusable manager emails, invitation expiry values in the past, and invitation expiry values beyond the platform maximum.

## Acceptance criteria

- [x] Blank or whitespace-only Business names are rejected.
- [x] Invalid manager emails are rejected.
- [x] Invitation expiry in the past is rejected.
- [x] Invitation expiry beyond the platform maximum is rejected.
- [x] Rejected requests do not append `BusinessCreated`, `BusinessManagerInvited`, or `BusinessBookabilityChanged` events.
- [x] Rejected requests return clear problem details suitable for API clients.
- [x] Integration tests cover each invalid request path through the Server.
- [x] Unit tests cover validation rules without HTTP transport concerns.
- [x] Validation preserves the happy path from issue 01.

## Blocked by

- `.scratch/create-business/issues/01-create-business-happy-path.md`
