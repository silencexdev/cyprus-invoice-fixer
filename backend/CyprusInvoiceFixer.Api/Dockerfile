FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy both project files first for layer-cached restore
COPY ["CyprusInvoiceFixer.Api/CyprusInvoiceFixer.Api.csproj", "CyprusInvoiceFixer.Api/"]
COPY ["CyprusInvoiceFixer.Core/CyprusInvoiceFixer.Core.csproj", "CyprusInvoiceFixer.Core/"]
RUN dotnet restore "CyprusInvoiceFixer.Api/CyprusInvoiceFixer.Api.csproj"

# Copy all source (both projects are now in context)
COPY . .
RUN dotnet publish "CyprusInvoiceFixer.Api/CyprusInvoiceFixer.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y \
    tesseract-ocr \
    tesseract-ocr-eng \
    libgdiplus \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CyprusInvoiceFixer.Api.dll"]
