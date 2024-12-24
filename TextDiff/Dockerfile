# Use the official .NET runtime image
FROM mcr.microsoft.com/dotnet/runtime:8.0

WORKDIR /app
# Copy the published files into the container
COPY TextDiff/publish .
COPY TextDiff/old.json /app
COPY TextDiff/new.json /app

# Set the entry point for the application
ENTRYPOINT ["dotnet", "TextDiff.dll"]
