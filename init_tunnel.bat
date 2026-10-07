@echo off
echo ========================================================
echo   KHOI TAO TAILSCALE FUNNEL CHO FRONTEND (PORT 3000)
echo ========================================================

docker exec cafe_management_tailscale tailscale serve reset
docker exec cafe_management_tailscale tailscale funnel --bg http://host.docker.internal:3000
docker exec cafe_management_tailscale tailscale funnel status
echo.
pause
