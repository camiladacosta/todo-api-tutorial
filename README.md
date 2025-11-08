### **Step 1: How to Install .NET on Linux**

The easiest way is using your distribution's package manager or the official install script.

**For Ubuntu (using apt):**

```Bash
# Register the Microsoft package feed
sudo apt-get update
sudo apt-get install -y apt-transport-https ca-certificates
# This command may change slightly, so checking docs is good
wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Install the .NET SDK (which includes the runtime)
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0 # Or .NET 9, etc.
```
For other distros (or a universal method):  
You can use the dotnet-install script.

```Bash
curl -L https://dot.net/v1/dotnet-install.sh -o dotnet-install.sh
chmod +x ./dotnet-install.sh
./dotnet-install.sh --version latest
```

After installing, they must add the dotnet tool to their path. The script will output the necessary command, which looks like this:

```Bash
# Add this to your ~/.bashrc or ~/.zshrc
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$PATH:$HOME/.dotnet:$HOME/.dotnet/tools
```

Then you must run `source ~/.zshrc` or `source ~/.bashrc` and restart your terminal.

**Verify installation:**

```Bash
dotnet --version
```

### **Step 1 (Alternate): How to Install .NET on Windows**

For Windows users, the process is simpler and usually graphical.

**Method 1: The Official Installer (Recommended)**

1. Go to the official .NET download page: [https://dot.net/download](https://dot.net/download)  
2. Find the **.NET SDK** (not just the Runtime). As of now, you'd look for .NET 8.0.  
3. Download the **"x64" installer** for Windows (assuming they are on 64-bit machines, which is standard).  
4. Run the installer and follow the on-screen prompts. It's a standard "Next, Next, Finish" installation.  
5. The installer will automatically add dotnet to your system's PATH.

**Verify Installation (Both Methods)**

1. Open a **new** Command Prompt (cmd) or PowerShell terminal. (It must be a new one to get the updated PATH).  
2. Run the following command:  
```Bash  
dotnet --version
```
3. If you see a version number (e.g., 8.0.100), you are ready to go.

---

### **Step 2: Phase 1 - The Simple CRUD API**

(The rest of the guide from here is identical for both Windows and Linux users, as the dotnet commands are cross-platform.)

**1. Create the Solution and Project**

```Bash
# Create a new directory for the solution  
mkdir TodoApiSolution  
cd TodoApiSolution

# Create a solution file  
dotnet new sln -n TodoApi

# Create a new Minimal Web API project  
dotnet new webapi -n TodoApi.Api --minimal

# Add the project to the solution  
dotnet sln add TodoApi.Api
```

2. Add Initial Dependencies  
For Phase 1, we'll use the in-memory database. It's built-in, but we need EF Core's design tools for later.

```Bash
# cd into the project directory  
cd TodoApi.Api

# Add package for EF Core Design (for migrations)  
dotnet add package Microsoft.EntityFrameworkCore.Design  
# Add package for In-Memory database  
dotnet add package Microsoft.EntityFrameworkCore.InMemory
```
3. Create the Files  
Inside the TodoApi.Api folder:

* **Create TodoItem.cs (The Model/Entity):**  
```C#  
// TodoApi.Api/TodoItem.cs
public class TodoItem
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public bool IsComplete { get; set; }
}
```

* **Create TodoDbContext.cs (The Database Context):**  
```C#  
// TodoApi.Api/TodoDbContext.cs
using Microsoft.EntityFrameworkCore;

public class TodoDbContext : DbContext
{
    public TodoDbContext(DbContextOptions<TodoDbContext> options)
        : base(options) { }

    public DbSet<TodoItem> TodoItems { get; set; }
}
```

* **Update Program.cs (To wire everything up):**  
```C#  
// TodoApi.Api/Program.cs
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Add services
builder.Services.AddDbContext<TodoDbContext>(opt =>
    opt.UseInMemoryDatabase("TodoList")); // Using In-Memory for now
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 2. Define API endpoints
app.MapGet("/todos", async (TodoDbContext db) =>
    await db.TodoItems.ToListAsync());

app.MapGet("/todos/{id}", async (int id, TodoDbContext db) =>
    await db.TodoItems.FindAsync(id)
        is TodoItem todo
            ? Results.Ok(todo)
            : Results.NotFound());

app.MapPost("/todos", async (TodoItem todo, TodoDbContext db) =>
{
    db.TodoItems.Add(todo);
    await db.SaveChangesAsync();
    return Results.Created($"/todos/{todo.Id}", todo);
});

app.MapPut("/todos/{id}", async (int id, TodoItem inputTodo, TodoDbContext db) =>
{
    var todo = await db.TodoItems.FindAsync(id);
    if (todo is null) return Results.NotFound();

    todo.Title = inputTodo.Title;
    todo.IsComplete = inputTodo.IsComplete;

    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapDelete("/todos/{id}", async (int id, TodoDbContext db) =>
{
    if (await db.TodoItems.FindAsync(id) is TodoItem todo)
    {
        db.TodoItems.Remove(todo);
        await db.SaveChangesAsync();
        return Results.Ok(todo);
    }
    return Results.NotFound();
});

// 3. Run the application
app.Run();
```

**4. Run the First Application**

```Bash
# Still inside the TodoApi.Api directory  
dotnet run
```

They can now access http://localhost:5123/swagger (the port may vary) in their browser to see and test the API.

---

### **Step 3: Phase 2 - Add Docker & Postgres**

(This section is also identical for Windows users, assuming they have [Docker Desktop](https://www.docker.com/products/docker-desktop/) installed and running.)

1. Create docker-compose.yml  
In the root of the TodoApiSolution folder (where the .sln file is), create this file:

```YAML
# TodoApiSolution/docker-compose.yml
version: '3.8'

services:
  postgres-db:
    image: postgres:16 # Use a specific major version
    container_name: todo_postgres
    environment:
      POSTGRES_USER: myuser      # Change this
      POSTGRES_PASSWORD: mysecretpassword # Change this
      POSTGRES_DB: tododb        # The database to create
    ports:
      - "5432:5432" # Maps your host port 5432 to the container's 5432
    volumes:
      - postgres-data:/var/lib/postgresql/data

volumes:
  postgres-data: # This ensures your data persists even if the container is removed
```

**2. Run Docker Compose**

```Bash
# In the root folder (where docker-compose.yml is)  
docker-compose up -d
```

This will download and run Postgres in the background.

**3. Add the Postgres Dependency**

```Bash
# Go back to the API project directory  
cd TodoApi.Api

# Remove the in-memory package (optional but clean)  
dotnet remove package Microsoft.EntityFrameworkCore.InMemory

# Add the Npgsql (Postgres) package  
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

**4. Update Program.cs and appsettings.json**

* **First, add appsettings.json** (if it's not minimal, it's in appsettings.Development.json):  
```JSON  
// TodoApi.Api/appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=tododb;Username=myuser;Password=mysecretpassword"
  }
}
```

* **Second, update Program.cs** to read this connection string:  
```C#  
// ... in Program.cs

// 1. Add services

// Get the connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// OLD: builder.Services.AddDbContext<TodoDbContext>(opt => opt.UseInMemoryDatabase("TodoList"));
// NEW:
builder.Services.AddDbContext<TodoDbContext>(opt =>
    opt.UseNpgsql(connectionString)); // Use Postgres!

// ... rest of the file is the same
```

5. Add and Run EF Core Migrations  
This is the process of telling the database what the tables should look like.

```Bash
# Install the EF Core global tool (they only need to do this once)
dotnet tool install --global dotnet-ef

# Still in TodoApi.Api directory
# Create the first migration:
dotnet ef migrations add InitialCreate

# Apply the migration to the (running) Docker database:
dotnet ef database update
```

If they check their Docker Postgres database with a tool like DBeaver or pgAdmin, they will now see the TodoItems table.

**6. Run the Application**

```Bash
dotnet run
```

The app works *exactly* as before, but the data is now being saved in the Postgres database.

---

### **Step 4: Phase 3 - Refactor to "DDD-Lite"**

(This section is also identical.)

**1. Create the New Projects**

```Bash
# Go to the solution root (TodoApiSolution)
cd ..

# Create the Core (Domain) library
dotnet new classlib -n TodoApi.Core

# Create the Infrastructure (Persistence) library
dotnet new classlib -n TodoApi.Infrastructure

# Add them to the solution
dotnet sln add TodoApi.Core
dotnet sln add TodoApi.Infrastructure
```

2. Set Up Project Dependencies  
This is the most important part of DDD: controlling the dependency flow.

* Api (Presentation) \-\> depends on \-\> Infrastructure  
* Api (Presentation) \-\> depends on \-\> Core  
* Infrastructure (Persistence) \-\> depends on \-\> Core  
* **Core (Domain) \-\> depends on \-\> NOTHING**

```Bash
# From the root folder
# Add references for the API project
dotnet add TodoApi.Api reference TodoApi.Core
dotnet add TodoApi.Api reference TodoApi.Infrastructure

# Add reference for the Infrastructure project
dotnet add TodoApi.Infrastructure reference TodoApi.Core
```

3. Add Packages to the Right Projects  
Dependencies need to move.

* **In TodoApi.Infrastructure:**  
```Bash  
# cd TodoApi.Infrastructure
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.EntityFrameworkCore.Design 
# (Design package is needed here for migrations)
```
* **In TodoApi.Api:**  
```Bash  
# cd TodoApi.Api
# This project no longer needs to know about the database
dotnet remove package Npgsql.EntityFrameworkCore.PostgreSQL 
```

**4. Move the Files**

1. **Move TodoItem.cs** from TodoApi.Api to TodoApi.Core.  
   * Delete the "Class1.cs" file in TodoApi.Core first.  
   * **Crucially, change its namespace** to TodoApi.Core.  
2. **Move TodoDbContext.cs** from TodoApi.Api to TodoApi.Infrastructure.  
   * Delete the "Class1.cs" file in TodoApi.Infrastructure.  
   * **Change its namespace** to TodoApi.Infrastructure.  
   * You will need to add using TodoApi.Core; at the top.

5. Update Program.cs  
The API project doesn't know what database is being used anymore. It only knows it has a database. The configuration is now in the Infrastructure layer.

* This is tricky. The simplest way for beginners is to have the Infrastructure project "tell" the Api project how to register itself.  
* **Create DependencyInjection.cs in TodoApi.Infrastructure:**  
```C#  
// TodoApi.Infrastructure/DependencyInjection.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TodoApi.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services, 
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            services.AddDbContext<TodoDbContext>(opt =>
                opt.UseNpgsql(connectionString));

            return services;
        }
    }
}
```

* **Finally, update Program.cs in TodoApi.Api:**  
```C#  
// TodoApi.Api/Program.cs
using TodoApi.Core; // Add this
using TodoApi.Infrastructure; // Add this

var builder = WebApplication.CreateBuilder(args);

// 1. Add services

// OLD:
// var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
// builder.Services.AddDbContext<TodoDbContext>(opt => opt.UseNpgsql(connectionString));

// NEW (cleaner!):
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ... rest of the file is identical ...

// The endpoints still need the DbContext, but they don't
// know where it came from.
app.MapGet("/todos", async (TodoDbContext db) =>
    await db.TodoItems.ToListAsync());

// ... etc ...

app.Run();
```

6. Run Migrations (From the Right Place)  
Migrations now need to be run from the Infrastructure project, but they need the Api project's settings.

```Bash
# Go to the solution root
cd ..

# Run migrations by telling EF Core where everything is
dotnet ef migrations add SecondMigration --project TodoApi.Infrastructure --startup-project TodoApi.Api

# (This might just be for a change, but it's good to show)
dotnet ef database update --project TodoApi.Infrastructure --startup-project TodoApi.Api
```

**Sources**  
1\. [https://stackoverflow.com/questions/78444356/net-core-8-clean-architecture-using-unit-of-work-and-generic-repository-patter](https://stackoverflow.com/questions/78444356/net-core-8-clean-architecture-using-unit-of-work-and-generic-repository-patter)