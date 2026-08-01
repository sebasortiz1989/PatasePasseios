
<div align="center">
    <h1>Dapper Demo</h1>
    <img src="images/logo.png" width="150">
</div>

Objective: Create a demo project with simple CRUD operations using dapper, so I can learn dapper in the process.

The project will consist of an application to keep track of a pet sitting service business.

# Entities

1. **PetSitter:** 
    - Properties:
        - `PetSitterID` (INT, Primary Key, Identity/Auto-increment)
        - `Email` (VARCHAR, Unique)
        - `Password` (VARCHAR)
        - `Name` (VARCHAR)
        - `Birth Date` (DATETIME)

2. **PetSitterClient:** _**(Junction Table)**_
    - Properties:
        - `(PetSitterID, ClientID)` Primary Key
        - `PetSitterID` (INT, Foreign Key referencing PetSitter.PetSitterID)
        - ``ClientID`` (INT, Foreign Key referencing Client.ClientID)
        - Description: Represents the Client Petsitter relation.

3. **Client:**
    - Properties:
        - `ClientID` (INT, Primary Key, Identity/Auto-increment)
        - `ClientName` (VARCHAR)
        - `ClientTelephone` (INT)
        - `NeightborHood` (VARCHAR)
    - Description: Stores the client information.

4. **Dog:**
    - Properties:
        - `DogID` (INT, Primary Key, Identity/Auto-increment)
        - `DogName` (VARCHAR)
        - `ClientID` (INT, Foreign Key referencing Client.ClientID)
        - `DogDescription` (VARCHAR)
    - Description: Stores the name and category of a purchased product.

5. **WalkingService:**
    - Properties:
        - `WalkingServiceID` (INT, Primary Key, Identity/Auto-increment)
        - `PetSitterID` (INT, Foreign Key referencing PetSitter.PetSitterID)
        - `DogID` (INT, Foreign Key referencing Dog.DogID)
        - `DateAndTime` (DATETIME)
        - `Price` (FLOAT)
        - `Paid` (BOOL)
    - Description: Records walking service details.

6. **PetSittingService:**
    - Properties:
        - `PetSittingServiceID` (INT, Primary Key, Identity/Auto-increment)
        - `PetSitterID` (INT, Foreign Key referencing PetSitter.PetSitterID)
        - `DogID` (INT, Foreign Key referencing Dog.DogID)
        - `DateAndTime` (DATETIME)
        - `Price` (FLOAT)
        - `Paid` (BOOL)
    - Description: Records pet sitting service details.

7. **PetHotelService:**
    - Properties:
        - `PetHotelServiceID` (INT, Primary Key, Identity/Auto-increment)
        - `PetSitterID` (INT, Foreign Key referencing PetSitter.PetSitterID)
        - `DogID` (INT, Foreign Key referencing Dog.DogID)
        - `RequiresWalkingService` (BOOL)
        - `InitialDateAndTime` (DATETIME)
        - `FinalDateAndTime` (DATETIME)
        - `PricePerDay` (FLOAT)
        - `Paid` (BOOL)
    - Description: Records pet hotel service details.

## **Relationships:**

- **PetSitter**:
    - Can be associated with multiple **WalkingService**, **PetSittingService**, and **PetHotelService** entries (one-to-many).
    - Can have relationships with multiple clients via the **PetSitterClient** junction table (many-to-many).

- **Client**:
    - Can have multiple **Dog** entries (one-to-many).
    - Can be linked to multiple **PetSitter** entities through the **PetSitterClient** junction table (many-to-many).

- **PetSitterClient** (junction table):
    - Links **PetSitter** and **Client** (many-to-many relationship).

- **Dog**:
    - Belongs to one **Client** (many-to-one).
    - Can be associated with multiple services:
        - **WalkingService**
        - **PetSittingService**
        - **PetHotelService**

- **Services (WalkingService, PetSittingService, PetHotelService)**:
    - Each service is linked to:
        - One **PetSitter** (who provides the service)
        - One **Dog** (which receives the service)

# Next Steps:

1. **Database Implementation:** Translate this entity diagram into actual database tables using your chosen database system (SQL Server, PostgreSQL, MySQL, etc.). Define appropriate data types, constraints (e.g., NOT NULL), and indexes.
2. **C# Classes:** Create C# classes that correspond to these entities. These classes will be used by Dapper to map data between your database and your application.
3. **Dapper Queries:** Write Dapper queries to perform CRUD (Create, Read, Update, Delete) operations on these entities.
4. **UI Design:** Design the user interface for your application, including forms for user registration, adding bank accounts, recording expenses and income, and displaying reports.

# References

Repository:
- https://github.com/DapperLib/Dapper/tree/main
- https://github.com/DapperLib/Dapper/tree/main/tests/Dapper.Tests

Chat GPT:
- https://chat.chatbotapp.ai/chats/-OcLlKIx4ti-bolurpIB?model=gemini

Tutorials:
- https://dappertutorial.net/online-examples
- https://www.learndapper.com/non-query

# Views

- LoginView: email, password, checkbox login automatically
- SignupView: email, password, name, birth date
- MainView: Bottom navigation tabs like instagram (Home, Dogs, Tutors, Services, User)

- Dogs tab: Shows dog list, and you can tap on them for more info.
	- Dog Info view: Dog name, photo, owner, description, future services

- Tutors tab: Shows a Tutor list, and you can tab on them for more info.
	- Tutor Info view: Shows information of the Tutor. Name, telephone, neighborhood, dogs, future services

- Services tab: From this view you can create a new service
	- Create Service View: Creates a new services whether walking, pet sitting or hotel. 
	        - `DogName`
	        - `RequiresWalkingService` Only on hotel
	        - `DateAndTime` or `InitialDateAndTime` on hotel
	        - `FinalDateAndTime` Only on hotel
	        - `Price` or `PricePerDay` on hotel

- User tab: Requires authentication.
	- Shows name
	- Password and option to change it.
	- Income for the month, divided by the services.

- Home tab (Shows upcoming services information including today, 1 week), filters, and basically from there Shows a lists of services with filters, you can tab on them to show info. The list can be filtered by date, as well as (Show paid boolean) by default always shows unpaid.
- Service Info View: Shows information
    - Service Type
    - Dog Name
    - Tutor Name
    - Date
    - Price or Price per day 
    - Paid