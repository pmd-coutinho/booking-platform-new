Status: ready-for-human

# Create Business Happy Path

## Parent

`.scratch/create-business/PRD.md`

## What to build

Build the valid end-to-end `CreateBusiness` path for a Platform Admin. A Platform Admin provides a Business name, manager email, and invitation expiry. The Server generates the Business id and Business Manager Invitation id, appends `BusinessCreated`, `BusinessManagerInvited`, and `BusinessBookabilityChanged` to a new Business event stream in one transaction, and returns the generated ids plus the current Unbookable status and Bookability Reasons.

The Business Manager Invitation does not send email yet. For this slice, the invitation lands as persisted domain history in the Business event stream and is returned to the Platform Admin in the API response so the flow can be inspected and demoed before invitation acceptance/email delivery exists.

## Acceptance criteria

- [x] A valid `CreateBusiness` request creates a Registered Business with a generated Business id.
- [x] A valid `CreateBusiness` request creates a generated Business Manager Invitation id.
- [x] The Business stream contains `BusinessCreated`, `BusinessManagerInvited`, and `BusinessBookabilityChanged` in one successful transaction.
- [x] `BusinessBookabilityChanged` records status `Unbookable` with structured reasons including `ManagerNotAccepted` and `OnboardingIncomplete`.
- [x] The response includes Business id, invitation id, Bookability status, and Bookability Reasons.
- [x] The invitation details are persisted in the `BusinessManagerInvited` event and returned in the response; no email is sent.
- [x] Duplicate Business names are allowed.
- [x] Integration tests cover the successful HTTP/API behavior using the Server and Booking Database.
- [x] Integration tests verify the expected Business stream events are persisted.
- [x] Unit tests cover the Business domain decision that produces the initial events and Bookability status.
- [x] Architecture tests are expanded if needed to preserve thin endpoint behavior and keep Marten/JasperFx/Wolverine.Marten infrastructure dependencies out of endpoint/domain decision code.

## Blocked by

None - can start immediately
