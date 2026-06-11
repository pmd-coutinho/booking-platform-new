Status: ready-for-human

# Record Create Business Actor Metadata

## Parent

`.scratch/create-business/PRD.md`

## What to build

Record domain-first actor metadata for `CreateBusiness` events. The slice should capture the actor role and actor identity for the Platform Admin who created the Business, storing that information as event metadata/envelope data rather than duplicating it inside each event payload.

Real authentication remains out of scope. Use the smallest domain-first mechanism needed to supply actor context for this command, such as a testable request context or temporary header-based context, while keeping the event payloads focused on domain facts.

## Acceptance criteria

- [x] Successful `CreateBusiness` events include actor role metadata identifying a Platform Admin.
- [x] Successful `CreateBusiness` events include actor identity metadata.
- [x] Actor role and actor identity are stored as event metadata/envelope data, not as duplicated fields inside every domain event payload.
- [x] Integration tests verify actor metadata is persisted for the Business stream events created by `CreateBusiness`.
- [x] Unit tests cover actor context handling where domain decisions require it.
- [x] The implementation does not introduce real authentication or email delivery.
- [x] Architecture tests are expanded if needed to keep actor-context plumbing out of domain event payloads and preserve thin endpoint behavior.

## Blocked by

- `.scratch/create-business/issues/01-create-business-happy-path.md`
