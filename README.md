# Reference Event Driven Design

- [Reference Event Driven Design](#reference-event-driven-design)
- [Overview](#overview)
- [Aims](#aims)
- [Architectural Decisions](#architectural-decisions)
- [Requirements](#requirements)
  - [Functional Requirements](#functional-requirements)
  - [Non-functional Requirements](#non-functional-requirements)

# Overview
An example of an event driven architecture. This is a holiday booking system which books a car, hotel and flight using event driven architecture best practices and illustrates concepts such as sagas and compensating transactions.

It's set out how a normal feature would be set out in the real world

# Aims

1. Create a resilient event-driven solutoin where any component can fail and either exactly one car, hotel and flight are booked or if one fails then nothing is booked (as the entire process is rolled back using compensating transactions)
2. Aimed more at the resilience and wiring up of the services rather than message or request validation and business logic and this is why there are no Domain Driven Design practices followed
3. Not production-ready but there is pretty good unit and integration tests coverage

# Architectural Decisions
1. **MassTransit** - Messaging abstraction for .NET. Open Source with good support and documentation. MassTransit enables each message type to be consumed by a separate consumer thereby removing the need to have a single consumer which has a switch statement to route messages through to the correct service. As a result, MassTransit follows the Open Closed Principle. It also has some great features around managing retries, error handling and good integration testing functionality.
2. **Service Bus** - Selected over Azure Storage Queues due to having more advanced features and good integration with MassTransit
3. **Managed Identity Authentication** - This prevents having connection strings in environment variables or appsettings in the running service. Azure provides an identity to the container or Web App and this identity is granted access to Service Bus and other resources directly. When debugging locally, Visual Studio can use the user's credentials to access external resources by configuring the Azure Service Authentication settings.

# Requirements
## Functional Requirements
**Happy path**<br />
Given a customer<br />
When a new booking is made for a holiday<br />
Then the car is booked<br />
And the hotel is booked<br />
And the flight is booked<br />
And the booking service has a record of the order set as successful for car, hotel and flight<br />

**Failed hotel booking**<br />
Given a customer <br />
When a new booking is made for a holiday<br />
And the car fails booking (after attempting for 5 mins)<br />
Then the hotel is not booked<br />
And the flight is not booked<br />
And the booking service has a record of the order set as Failed for car<br />
And the booking service has a record of the order set as NotStarted for hotel<br />
And the booking service has a record of the order set as NotStarted for flight<br />

**Failed hotel booking**<br />
Given a customer <br />
When a new booking is made for a holiday<br />
And the car is booked<br />
And the hotel fails booking (after attempting for 5 mins)<br />
Then the flight is not booked<br />
And the booking service has a record of the order set as Cancelled for car<br />
And the booking service has a record of the order set as Failed for hotel<br />
And the booking service has a record of the order set as NontStarted for flight<br />

**Failed flight booking**<br />
Given a customer<br />
When a new booking is made for a holiday<br />
And the car is booked<br />
And the hotel is booked<br />
And the flight fails booking (after attempting for 5 mins)<br />
Then the car is cancelled<br />
And the hotel is cancelled<br />
And the booking service has a record of the order set as Cancelled for car<br />
And the booking service has a record of the order set as Cancelled for hotel<br />
And the booking service has a record of the order set as Failed for flight<br />

## Non-functional Requirements
- Don't lose a single order
- No duplicate orders
- No duplicate car, hotel or flight bookings
- Only rollback orders if booking fails for a car, hotel or flight after 5mins of trying
- Orders must show history in the Bookings database owned by the Bookings Service (i.e. add a row for each change to an order)
- Must be secure (ideally with Managed Identity throughout)
Correlation IDs used throughout in order to track orders through the entire pipeline