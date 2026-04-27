# Open Mods Tracker

An open-source desktop application to track and manage your Nexus Mods portfolio with real-time statistics and analytics.

<p align="center">
  <img src="https://img.shields.io/badge/Platform-Windows%2011-blue" alt="Platform">
  <img src="https://img.shields.io/badge/Framework-WinUI3-purple" alt="Framework">
  <img src="https://img.shields.io/badge/License-MIT-green" alt="License">
</p>

## Features

- **Dashboard Overview** — View total downloads, unique downloads, endorsements, and mod counts at a glance
- **Portfolio Management** — Add mods manually or import your entire Nexus profile automatically
- **Real-time Statistics** — Track top downloaded mods and most endorsed content
- **Rate Limit Monitoring** — Keep track of your Nexus API usage
- **Multi-language Support** — Available in English, Chinese, German,Spanish, French, Italian, Japanese, Portuguese, Russian and Nederlands.

## Requirements

| Requirement | Details |
|-------------|---------|
| **OS** | Windows 10 (1809+) or Windows 11 |
| **API Key** | Nexus Mods personal API key |
| **.NET** | .NET 10.0 Runtime (included in Windows 11) |

### Getting Your API Key

1. Go to [Nexus Mods Settings](https://next.nexusmods.com/settings/api-keys)
2. Scroll to **Personal API Key** at the bottom
3. Copy your API key and paste it in the app settings

## Installation

### Option 1: Installer (Recommended)

Download the latest installer from the [Releases](https://github.com/obewan/OpenModsTracker/releases) page:

```
OpenModsTracker-Setup-x.x.x.exe
```

Run the installer and follow the prompts. The app will be installed to `C:\Program Files\Open Mods Tracker`.

### Option 2: Portable

1. Download the latest release from the [Releases](https://github.com/obewan/OpenModsTracker/releases) page
2. Extract the ZIP to your desired location
3. Run `Open Mods Tracker.exe`

## Building from Source

### Prerequisites

- [Visual Studio 2022](https://visualstudio.microsoft.com/) with:
  - .NET Desktop Development workload
  - Windows App SDK C# Templates
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Build Commands

```bash
# Clone the repository
git clone https://github.com/obewan/OpenModsTracker.git
cd OpenModsTracker

# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run in development mode
dotnet watch run

# Publish for release
dotnet publish -c Release
```

### Creating the Installer

1. Download and install [Inno Setup](https://jrsoftware.org/isinfo.php)
2. Open `Open Mods Tracker\installer.iss` in Inno Setup Compiler
3. Press `Ctrl+F9` to compile

The installer will be generated in the `installer` folder.

## Usage

### First Setup

1. Launch the application
2. Go to **Settings** → **API Key**
3. Enter your Nexus Mods API key
4. Click **Save**

### Adding Mods

**Manual Addition:**
- Click **Add Mod** and enter the Nexus Mod URL

**Import from Profile:**
- Go to **Settings** → **Import Portfolio**
- Enter your Nexus profile name or URL
- Click **Import**

### Dashboard

The dashboard displays:
- **Downloads** — Total downloads across all tracked mods
- **Unique DLs** — Unique download count
- **Endorsements** — Total endorsements received
- **Mods suivis** — Number of mods being tracked

## Configuration

Settings are stored in:
- `%LOCALAPPDATA%\OpenModsTracker\` — App data
- `%APPDATA%\OpenModsTracker\` — User preferences

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Acknowledgments

- [Nexus Mods API](https://app.swaggerhub.com/apis-docs/NexusMods/nexus-mods_public_api_params_in_form_data/1.0) — For providing the API
- [WinUI 3](https://learn.microsoft.com/en-us/windows/apps/winui/) — For the UI framework

## Links
- [OMT on Nexus](https://www.nexusmods.com/site/mods/1849)
- [OMT on Dams-Labs](https://dams-labs.net/omt)



