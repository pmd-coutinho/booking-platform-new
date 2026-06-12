# Booking Platform

A booking platform for customers to schedule appointment-style services with businesses and their staff members.

## Language

**Booking Database**:
The PostgreSQL database used for operational persistence and intended as the backing store for Marten (document database / event store).
_Avoid_: App database, Postgres instance

**Cache**:
The Redis instance used for output caching and transient data.
_Avoid_: Redis cache

**Server**:
The ASP.NET Core API that serves the backend.
_Avoid_: API, backend service

**Web Frontend**:
The Vite-based SPA served by the AppHost.
_Avoid_: Frontend app, client

**Service Appointment**:
A scheduled time for a customer to receive an appointment type from a staff member.
_Avoid_: Reservation, resource booking

**Requested Service Appointment**:
A service appointment that is waiting for business approval before it becomes booked.
_Avoid_: Pending appointment, unconfirmed appointment

**Booked Service Appointment**:
A service appointment that is committed to a staff member's schedule.
_Avoid_: Confirmed appointment, reservation

**Declined Service Appointment**:
A requested service appointment that the business will not book.
_Avoid_: Rejected appointment, refused appointment

**Expired Service Appointment**:
A requested service appointment that ended because the business did not respond within its response window.
_Avoid_: Timed-out appointment, stale request

**Cancelled Service Appointment**:
A requested or booked service appointment that the customer or business ended before it occurred.
_Avoid_: Deleted appointment, removed appointment

**Completed Service Appointment**:
A booked service appointment that the business records as having happened.
_Avoid_: Finished appointment, fulfilled appointment

**No-Show Service Appointment**:
A booked service appointment that the customer missed without completing the appointment.
_Avoid_: Missed appointment, absent appointment

**No-Show Risk**:
An aggregate signal about a booking contact's likelihood of missing booked service appointments across the platform.
_Avoid_: No-show history, customer score

**Rescheduled Service Appointment**:
A service appointment moved to a different time with the same staff member and appointment type.
_Avoid_: Rebooked appointment, changed appointment

**Appointment Type**:
A business-defined kind of appointment that customers can schedule, including default booking rules.
_Avoid_: Service, service offering

**Disabled Appointment Type**:
An appointment type that is no longer offered for new service appointments but may remain referenced by existing appointments.
_Avoid_: Deleted service, inactive service

**Appointment Terms**:
The customer-facing price and duration of a service appointment at the time it is scheduled.
_Avoid_: Current price, current duration

**Slot Interval**:
The spacing between possible start times for available slots.
_Avoid_: Slot granularity, booking interval

**Staff Capability**:
A staff member's ability to perform an appointment type, including any staff-specific overrides to the appointment type's default booking rules.
_Avoid_: Assignment, join, staff service

**Disabled Staff Capability**:
A staff capability that is no longer available for new service appointments but may remain referenced by existing appointments.
_Avoid_: Removed assignment, inactive staff service

**Confirmation Mode**:
The rule for whether a service appointment is booked immediately or must be requested first.
_Avoid_: Approval setting, booking type

**Requested Slot Policy**:
The rule for whether a requested service appointment exclusively holds its requested available slot or competes with other requested service appointments for the same available slot.
_Avoid_: Pending slot policy, hold setting, request policy

**Available Slot**:
A concrete time range that a customer can choose for a service appointment.
_Avoid_: Time slot, opening

**Business**:
An organization that offers appointment types customers can schedule with staff members.
_Avoid_: Provider, vendor, merchant, salon

**Business Profile**:
The public-facing name, booking slug, contact information, and physical address for a business.
_Avoid_: Company profile, merchant profile

**Public Booking Slug**:
A unique platform URL identifier customers use to find a business's booking page.
_Avoid_: Business slug, vanity URL

**Custom Booking Domain**:
A business-owned web domain used for the business's booking page.
_Avoid_: Custom URL, external domain

**Business Currency**:
The currency a business uses for appointment prices.
_Avoid_: Platform currency, price currency

**Registered Business**:
A business that exists on the platform but may not yet have a manager, complete profile, or scheduling setup.
_Avoid_: Draft business, incomplete business

**Bookable Business**:
A business that has completed the setup required for customers to schedule service appointments.
_Avoid_: Active business, published business

**Unbookable Business**:
A registered business that customers cannot currently schedule service appointments with because required setup is incomplete.
_Avoid_: Inactive business, unpublished business

**Bookability Reason**:
A structured explanation for why a business is or is not currently bookable.
_Avoid_: Status message, validation error

**Business Manager**:
A person who can configure their business on the platform.
_Avoid_: Business owner, business admin

**Platform Admin**:
A platform operator who can create businesses, invite business managers, and perform business setup when needed.
_Avoid_: Super admin, system admin

**Business Manager Invitation**:
An invitation, addressed to a person's email identity, for that person to become a business manager for a registered business.
_Avoid_: Owner invite, admin invite

**Accepted Business Manager Invitation**:
A business manager invitation that has been used by its invitee to become a business manager.
_Avoid_: Claimed invite, completed invite

**Expired Business Manager Invitation**:
A business manager invitation that can no longer be accepted because its response period ended.
_Avoid_: Timed-out invite, stale invite

**Business Onboarding**:
The process of completing a registered business's profile and scheduling setup so it can become bookable.
_Avoid_: Setup wizard, registration

**Business Time Zone**:
The time zone a business uses to define local appointment dates and available slots.
_Avoid_: Provider time zone, calendar time zone

**Business Hours**:
The normal times a business is open for service appointments.
_Avoid_: Opening hours, operating schedule

**Business Closure**:
A date or time range when a business is not open for service appointments.
_Avoid_: Business holiday, closed day

**Business Special Opening**:
A date or time range when a business is explicitly open outside normal business hours or despite a closure.
_Avoid_: Extra hours, holiday opening

**Response Window**:
The amount of time a business has to respond to a requested service appointment before it expires.
_Avoid_: Expiry time, approval timeout

**Staff Member**:
A person who belongs to a business and can perform one or more appointment types.
_Avoid_: Provider, employee, resource

**Disabled Staff Member**:
A staff member who is no longer available for new service appointments but may remain referenced by existing appointments.
_Avoid_: Deleted staff member, inactive employee

**Staff Invitation**:
An invitation for a staff member to access the platform with their own login.
_Avoid_: Staff account, employee account

**Staff Availability**:
The times a staff member is willing to accept service appointments.
_Avoid_: Provider availability, schedule, calendar

**Staff Special Availability**:
A date or time range when a staff member is explicitly available outside normal staff availability.
_Avoid_: Extra staff hours, special shift

**Staff Time Off**:
A date or time range when a staff member is unavailable for service appointments.
_Avoid_: Staff holiday, day off

**Customer**:
A person known to the platform through booking history, whether or not they have a login.
_Avoid_: User, account, guest

**Booking Contact**:
The contact identity presented when scheduling a service appointment, including name, phone number, and email address.
_Avoid_: Guest, anonymous customer

**Business Customer**:
A customer as known by a specific business, including that business's appointment history with the customer.
_Avoid_: Client, business guest
