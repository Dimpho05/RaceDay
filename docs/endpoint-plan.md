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
