@echo off
setlocal enabledelayedexpansion

REM Task 1: Check versions
echo.
echo ========== TASK 1: CHECKING VERSIONS ==========
echo.
echo Node.js version:
node --version
echo.
echo Yarn version:
yarn --version
echo.
echo Podman version:
podman --version
echo.

REM Task 2: Install dependencies
echo ========== TASK 2: YARN INSTALL ==========
echo.
cd /d C:\Users\HMEHU\Development\CodeAcademy2026\1-DevOps\idempotweet
call yarn install
if errorlevel 1 (
    echo.
    echo ERROR: yarn install failed
    exit /b 1
)
echo.

REM Task 3: Handle PostgreSQL container
echo ========== TASK 3: POSTGRESQL CONTAINER SETUP ==========
echo.

REM Check if container exists and remove it if stopped
echo Checking for existing idempotweet-postgres container...
podman ps -a --filter "name=idempotweet-postgres" --format "{{.Names}}" > temp_container.txt
set /p EXISTING_CONTAINER=< temp_container.txt
del temp_container.txt

if not "!EXISTING_CONTAINER!"=="" (
    echo Container found: !EXISTING_CONTAINER!
    echo Checking if running...
    podman ps --filter "name=idempotweet-postgres" --format "{{.Names}}" > temp_running.txt
    set /p RUNNING_CONTAINER=< temp_running.txt
    del temp_running.txt
    
    if "!RUNNING_CONTAINER!"=="" (
        echo Container exists but is not running. Removing it...
        podman rm idempotweet-postgres
        echo Removed stopped container.
    ) else (
        echo Container is already running.
    )
) else (
    echo No existing container found.
)

REM Start the PostgreSQL container if not running
echo.
echo Starting PostgreSQL 17 container...
podman run -d ^
  --name idempotweet-postgres ^
  -e POSTGRES_USER=codeacademy ^
  -e POSTGRES_PASSWORD=codeacademy ^
  -e POSTGRES_DB=codeacademy ^
  -p 5432:5432 ^
  --health-cmd="pg_isready -U codeacademy" ^
  --health-interval=10s ^
  --health-timeout=5s ^
  --health-retries=5 ^
  postgres:17

if errorlevel 1 (
    echo Checking if container is already running...
    podman ps --filter "name=idempotweet-postgres" --format "{{.Names}}" > temp_check.txt
    set /p CHECK_RESULT=< temp_check.txt
    del temp_check.txt
    
    if "!CHECK_RESULT!"=="idempotweet-postgres" (
        echo Container is already running. Continuing...
    ) else (
        echo ERROR: Failed to start container
        exit /b 1
    )
) else (
    echo Container started successfully.
)

echo.
echo Waiting for PostgreSQL to be ready...
timeout /t 3 /nobreak

REM Wait for container health
setlocal enabledelayedexpansion
set RETRY=0
set MAX_RETRIES=30

:wait_loop
podman ps --filter "name=idempotweet-postgres" --filter "health=healthy" --format "{{.Names}}" > temp_health.txt
set /p HEALTH=< temp_health.txt
del temp_health.txt

if "!HEALTH!"=="idempotweet-postgres" (
    echo PostgreSQL is healthy and ready!
    goto :health_ok
)

if !RETRY! LSS !MAX_RETRIES! (
    set /a RETRY=!RETRY!+1
    echo Waiting for PostgreSQL... attempt !RETRY!/!MAX_RETRIES!
    timeout /t 2 /nobreak
    goto wait_loop
)

echo WARNING: PostgreSQL health check did not confirm healthy status, but proceeding...

:health_ok
echo.

REM Task 4: Seed database
echo ========== TASK 4: YARN SEED ==========
echo.
set "DATABASE_URL=postgresql://codeacademy:codeacademy@localhost:5432/codeacademy"
call yarn seed
if errorlevel 1 (
    echo.
    echo ERROR: yarn seed failed
    exit /b 1
)
echo.

REM Task 5: Lint and test
echo ========== TASK 5: YARN LINT ==========
echo.
call yarn lint
echo.

echo ========== TASK 6: YARN TEST ==========
echo.
call yarn test

REM Capture test result
set TEST_RESULT=%errorlevel%

echo.
echo ========== ALL TASKS COMPLETED ==========
echo.

exit /b !TEST_RESULT!
