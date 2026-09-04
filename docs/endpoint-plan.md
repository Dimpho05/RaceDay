# RaceDay API Endpoint Plan

## Authentication

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/auth/register | Creates a new user account with a specified role (Organiser or Participant). Password is hashed before storage. | None (public) | { firstName, lastName, email, password, role, phoneNumber } | 201 Created – user created (no password returned)<br>400 Bad Request – validation failed<br>409 Conflict – email already registered |
| POST | /api/auth/login | Authenticates a user and starts a session, returning the user's role for subsequent requests. | None (public) | { email, password } | 200 OK – session established, returns userId + role<br>401 Unauthorized – invalid credentials |
| POST | /api/auth/logout | Ends the current authenticated session. | Any (logged in) | None | 200 OK – session ended |



## User Profile

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/profile | Retrieves the logged-in user's own profile details. | Any (logged in) | None | 200 OK – returns profile (firstName, lastName, email, role, phoneNumber)<br>401 Unauthorized – not logged in |
| PUT | /api/profile | Updates the logged-in user's own profile information. | Any (logged in) | { firstName, lastName, phoneNumber } | 200 OK – updated profile returned<br>400 Bad Request – validation failed<br>401 Unauthorized – not logged in |



## Events

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/events | Retrieves a list of all events, viewable by any user. | Any (logged in) | None | 200 OK – list of events |
| GET | /api/events/{id} | Retrieves details of a single event by its ID. | Any (logged in) | None | 200 OK – event details<br>404 Not Found – event does not exist |
| POST | /api/events | Creates a new event. The logged-in Organiser is set as the event's owner. | Organiser | { name, description, eventDate, location, distanceKm, eventTypeId } | 201 Created – event created<br>400 Bad Request – validation failed<br>403 Forbidden – not an Organiser |
| PUT | /api/events/{id} | Updates an existing event. Only the Organiser who created it may update it. | Organiser | { name, description, eventDate, location, distanceKm, eventTypeId } | 200 OK – event updated<br>403 Forbidden – not the owning Organiser<br>404 Not Found – event does not exist |
| DELETE | /api/events/{id} | Deletes an event. Only the Organiser who created it may delete it. | Organiser | None | 200 OK / 204 No Content – event deleted<br>403 Forbidden – not the owning Organiser<br>404 Not Found – event does not exist |



## Categories

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/events/{eventId}/categories | Retrieves all categories defined for a specific event. | Any (logged in) | None | 200 OK – list of categories<br>404 Not Found – event does not exist |
| GET | /api/categories/{id} | Retrieves details of a single category by its ID. | Any (logged in) | None | 200 OK – category details<br>404 Not Found – category does not exist |
| POST | /api/events/{eventId}/categories | Creates a new category for a specific event. Only the Organiser who owns the event may add categories to it. | Organiser | { categoryName, minimumAge, maximumAge, categoryDistance } | 201 Created – category created<br>403 Forbidden – not the owning Organiser<br>404 Not Found – event does not exist |
| PUT | /api/categories/{id} | Updates an existing category. Only the Organiser who owns the parent event may update it. | Organiser | { categoryName, minimumAge, maximumAge, categoryDistance } | 200 OK – category updated<br>403 Forbidden – not the owning Organiser<br>404 Not Found – category does not exist |
| DELETE | /api/categories/{id} | Deletes a category. Only the Organiser who owns the parent event may delete it. | Organiser | None | 200 OK / 204 No Content – category deleted<br>403 Forbidden – not the owning Organiser<br>404 Not Found – category does not exist |


## Event Enrolments

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/enrolments | Enrols the logged-in Participant into an event by selecting a category. Records the link between Participant, Event, and Category. | Participant | { eventId, categoryId } | 201 Created – enrolment record created<br>400 Bad Request – category does not belong to event<br>403 Forbidden – not a Participant<br>404 Not Found – event or category does not exist<br>409 Conflict – already enrolled in this event |
| GET | /api/enrolments/my | Retrieves all enrolments belonging to the logged-in Participant. | Participant | None | 200 OK – list of the Participant's enrolments |
| GET | /api/events/{eventId}/enrolments | Retrieves all enrolments for a specific event. Only the Organiser who owns the event may view them. | Organiser | None | 200 OK – list of enrolments for the event<br>403 Forbidden – not the owning Organiser<br>404 Not Found – event does not exist |
| DELETE | /api/enrolments/{id} | Cancels the logged-in Participant's own enrolment. | Participant | None | 200 OK / 204 No Content – enrolment cancelled<br>403 Forbidden – not the owning Participant<br>404 Not Found – enrolment does not exist |


## Results

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/results | Captures a finish time and finishing position for a Participant's enrolment. Only the Organiser who owns the related event may capture results. | Organiser | { enrolmentId, finishTime, finishingPosition } | 201 Created – result recorded<br>403 Forbidden – not the owning Organiser<br>404 Not Found – enrolment does not exist<br>409 Conflict – result already recorded for this enrolment |
| PUT | /api/results/{id} | Updates a previously captured result. Only the Organiser who owns the related event may update it. | Organiser | { finishTime, finishingPosition } | 200 OK – result updated<br>403 Forbidden – not the owning Organiser<br>404 Not Found – result does not exist |
| GET | /api/results/my | Retrieves all of the logged-in Participant's own results. | Participant | None | 200 OK – list of the Participant's results |
| GET | /api/events/{eventId}/results | Retrieves all results for a specific event. Viewable by the owning Organiser and any Participant enrolled in the event. | Any (logged in) | None | 200 OK – list of results for the event<br>403 Forbidden – not the owning Organiser or an enrolled Participant<br>404 Not Found – event does not exist |
