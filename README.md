# FowCampaignApp

FowCampaignApp is a web-based, multiplayer campaign management application designed to track map-based tabletop wargaming campaigns (such as Flames of War). It features an interactive tactical map, custom faction creation, turn-based progression, and integrated Excel spreadsheet editing for unit rosters.

## Features

* **Interactive Tactical Map**
  * Upload custom map images to serve as the campaign board.
  * Click-to-capture territory control utilizing a canvas-based flood-fill algorithm.
  * Drag-and-drop unit tokens directly on the map interface.
  * Integrated OCR scanning to automatically detect and name regions based on map text.

* **Turn-Based Multiplayer**
  * Secure user registration and authentication via JWT.
  * Host campaigns and invite other players using unique 6-character Operation Codes.
  * Phased turn system (Planning, Capture, Deployment) with automatic turn passing between factions.

* **Advanced Unit & Roster Management**
  * Create custom unit definitions with uploaded icons.
  * **Integrated Excel Support:** Attach `.xlsx` files to deployed units to track detailed stats, rosters, or damage.
  * **In-Browser Editor:** View, edit, add rows/columns, and save Excel files directly within the browser using the integrated tactical intel panel (powered by ClosedXML).

* **Automated Conflict Detection**
  * Automatically detects when opposing units occupy the same sector.
  * Built-in battle resolution panel to input Major and Minor Victory Points (VP).
  * Persistent battle logging to track campaign history.

## Tech Stack

* **Frontend:** Blazor WebAssembly (.NET 10.0), HTML5 Canvas with JS Interop
* **Backend:** ASP.NET Core Web API (.NET 10.0)
* **Database:** SQLite with Entity Framework Core
* **Authentication:** JSON Web Tokens (JWT)
* **File Processing:** ClosedXML (Excel manipulation)
* **Containerization:** Docker & Nginx

## Getting Started

### Prerequisites

* [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
* Docker (Optional, for containerized deployment)

### Local Development Setup

1. **Clone the repository**
   ```bash
   git clone [https://github.com/Barsol6/FowCampaignApp.git](https://github.com/Barsol6/FowCampaignApp.git)
   cd FowCampaignApp

2. **Configure the Backend**

     Navigate to the API directory and ensure your `appsettings.json` is configured. The application uses a local SQLite database (`campaign.db`) by default.

  ```json
  {
    "Jwt": {
      "Key": "your-secure-key-here",
      "Issuer": "FowCampaign.Api",
      "Audience": "FowCampaign.App"
    },
    "ConnectionStrings": {
      "DefaultConnection": "Data Source=campaign.db"
    }
  }
  ```

3. **Run the API**

     The backend will start and automatically apply Entity Framework migrations to create the SQLite database.

  ```bash
        cd FowCampaign.Api
        dotnet build
        dotnet run
  ```

4. **Run the Frontend App**
 
      Open a new terminal window and run the Blazor WebAssembly project.

   ```bash
    cd FowCampaign.App
    dotnet build
    dotnet run
   ```

## Docker Deployment
### The repository includes Dockerfiles for both the API and the Web frontend.

  * API: Uses the standard .NET ASP.NET 10.0 runtime.

  * Web: Builds the Blazor WebAssembly static files and serves them using an Nginx Alpine container. You can pass the backend URL during the build phase using the API_BASE_URL build argument.

## License
  This project is licensed under the GNU General Public License Version 3. See the LICENSE.txt file for details.


  
