#!/bin/bash
export ASPNETCORE_ENVIRONMENT=local
cd src/DShop.Api
dotnet run --no-restore

read -p "Press enter to continue..."
trap "trap - SIGTERM && kill -- -$$" SIGINT SIGTERM EXIT