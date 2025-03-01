#!/bin/bash

echo "[INFO] Deleting app.db"
rm -rf app.db

echo "[INFO] Deleting migrations"
rm ./Migrations/*

echo "[INFO] Recreating migrations"
dotnet ef migrations add "initial"

echo "[INFO] Run migrations"
dotnet ef database update