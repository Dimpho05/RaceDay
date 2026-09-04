# RaceDay API Endpoint Plan

## Authentication

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/auth/register | Creates a new user account with a specified role (Organiser or Participant). Password is hashed before storage. | None (public) | { firstName, lastName, email, password, role, phoneNumber } | 201 Created – user created (no password returned)<br>400 Bad Request – validation failed<br>409 Conflict – email already registered |
| POST | /api/auth/login | Authenticates a user and starts a session, returning the user's role for subsequent requests. | None (public) | { email, password } | 200 OK – session established, returns userId + role<br>401 Unauthorized – invalid credentials |
| POST | /api/auth/logout | Ends the current authenticated session. | Any (logged in) | None | 200 OK – session ended |
