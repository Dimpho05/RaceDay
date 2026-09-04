# RaceDay - Race Event Management System

RaceDay is a full-stack web-based event management platform built for the South African road running, walking, and cycling community. It allows Event Organisers to create and manage events, categories, and participant results, while Participants can browse upcoming events, enter events, and track their personal performance history.

This repository contains the Portfolio of Evidence (POE) for PROG6212, submitted in three parts:

- **Part 1** (current): System planning - Entity Relationship Diagram, API endpoint plan, and SQL database script.
- **Part 2**: RESTful API built with ASP.NET Core and Entity Framework Core.
- **Part 3**: MVC web application with Azure Blob Storage and Docker containerisation.

## User Roles

RaceDay supports two distinct user roles:

- **Organiser** - Can create, edit, and delete events, manage event categories, capture participant results, and view all enrolments for their events.
- **Participant** - Can create an account, browse events, enter an event by selecting a category, view their own enrolments, and track their own results.

## Part 1 Deliverables

All Part 1 planning documents are located in the `/docs` folder:

- `RaceDay-ERD.png` - Entity Relationship Diagram covering all 6 entities (Users, EventTypes, Events, Categories, Enrolments, Results) with primary keys, foreign keys, and cardinality.
- `endpoint-plan.md` - Full API endpoint plan covering Authentication, Profile, Events, Categories, Enrolments, and Results.
- `raceday-schema.sql` - SQL script that creates the full database schema and seeds it with sample data. Tested and verified to run cleanly on a fresh SQL Server instance in SSMS.

## Running the SQL Script

1. Open SQL Server Management Studio (SSMS) and connect to your server.
2. Create a new database (e.g. `RaceDayDB`).
3. Open a new query window scoped to that database.
4. Paste in the contents of `docs/raceday-schema.sql` and execute (F5).
5. Confirm all 6 tables are created and seeded under Object Explorer.

## CI/CD

A GitHub Actions workflow (`.github/workflows/validate-docs.yml`) runs on every push to validate that the repository structure is correct for submission - checking that the `/docs` folder exists and contains the ERD, endpoint plan, and SQL script, and that this README is present.

**CI/CD Screenshot:**

<img width="1920" height="1080" alt="image" src="https://github.com/user-attachments/assets/cd649b4b-bbbd-4fac-9ca2-5954fbbf25b4" />


## Video Presentation

**YouTube Link:** https://youtu.be/fL06aN6lej0

The video walks through the planning documents, ERD design decisions, endpoint plan choices, and demonstrates the SQL script running live in SSMS.
