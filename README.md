Build and Run
1. Build the entire solution by running:
bash
dotnet build

2. Now, you can either:
Start the server using the launch configuration.
Start multiple clients using the launch configuration for the client.

If you prefer using the terminal:
Run the server:

bash
dotnet run --project Server/Server.csproj

Run the client:

bash
dotnet run --project Client/Client.csproj
