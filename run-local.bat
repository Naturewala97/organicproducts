@echo off
echo Starting full React + ASP.NET Core + PostgreSQL stack...
if not exist .env copy .env.example .env
docker compose up --build
pause
