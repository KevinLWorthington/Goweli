# Goweli - Personal Book Inventory System

![Goweli Logo](https://github.com/KevinLWorthington/SDProjectPlan/blob/main/Assets/GOWELILOGOPLAIN.png)

## 📚 Project Overview

Goweli is a web-based book collection management application built with AvaloniaUI and .NET 9.0. It allows users to maintain a personal library by adding, viewing, editing, and deleting books. The application fetches book cover images from the Open Library API, stores book data in a SQLite database, and provides an intuitive interface for managing your reading collection.

## ✨ Features

- **Add Books**: Add books to your collection with title, author, ISBN, synopsis, and reading status
- **View Books**: Browse your collection in a data grid with sortable columns
- **Edit Books**: Update any book information after adding it to your collection
- **Delete Books**: Remove books from your collection
- **Book Cover Images**: Automatically fetches and displays book covers from Open Library API
- **Persistent Storage**: SQLite database to store your book collection
- **Responsive UI**: Clean, modern interface that works in a desktop browser

## 🔍 Code:You Capstone Requirements Met

1. **API Integration** ✓
   - Integrates with the Open Library API to fetch book cover images
   - Uses HTTP client to make API requests and handle responses

2. **Database Interaction (SQLite)** ✓
   - Implements Entity Framework Core with SQLite database
   - Includes Book model class with properties for storing book information
   - Performs CRUD operations through database service

3. **Functions/Methods** ✓
   - `GetBookCoverUrlAsync()`: Fetches book cover URLs from the Open Library API
   - `LoadBookCoverAsync()`: Loads book covers from URLs
   - `AddBookAsync()`: Adds books to the database

4. **Additional Features:**

   - **Created dictionary/list and used it** ✓
     - Used ObservableCollection to manage and display books

   - **Added comments with SOLID principles** ✓
     - Single Responsibility Principle: Services are separated by concern
     - Interface Segregation: `IDatabaseService` defines clear interface for database operations

## 🛠️ Technologies Used

- **.NET 9.0**: Core framework
- **Avalonia UI**: Cross-platform UI framework
- **Entity Framework Core**: ORM for database interactions
- **SQLite**: Lightweight database engine
- **OpenLibrary.NET**: Client library for Open Library API
- **CommunityToolkit.Mvvm**: MVVM toolkit for UI architecture

## 📋 Prerequisites

- .NET 9.0 SDK or later
- Visual Studio 2022 or later, Visual Studio Code, or JetBrains Rider

## 🚀 Getting Started

### Clone the Repository

```bash
git clone https://github.com/yourusername/Goweli.git
cd Goweli
```

### Build and Run the Project

**Using Visual Studio**:
1. Open the `Goweli.Browser.sln` solution file in Visual Studio
2. Restore NuGet packages
3. Build the solution
4. Run the project (press F5 or click the run button)

**Using .NET CLI**:
```bash
dotnet restore
dotnet build
dotnet run --project Goweli.Browser
```

### Database Setup

The application will automatically create and initialize the SQLite database on first run. No additional setup is required.

## 📱 Screenshots

![Main Screen](https://github.com/KevinLWorthington/SDProjectPlan/blob/main/Assets/GoweliMain.png)
![Add Book](https://github.com/KevinLWorthington/SDProjectPlan/blob/main/Assets/GoweliAdd.png)
![View Books](https://github.com/KevinLWorthington/SDProjectPlan/blob/main/Assets/GoweliView.png)

## 📚 Project Structure

- **Goweli**: Core library project containing models, services, and UI
  - **Data**: Contains database context and entity configurations
  - **Models**: Contains data models like Book
  - **Services**: Contains service interfaces and implementations
  - **ViewModels**: Contains MVVM view models
  - **Views**: Contains Avalonia UI views

- **Goweli.Browser**: WebAssembly project for running in the browser

## 💡 Future Enhancements

- Book categorization and tagging
- Reading progress tracking
- Book recommendations based on your library
- Barcode scanning for easy book addition
- Export/import functionality
- Mobile support

## 🤝 Contributing

1. Fork the project
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 👏 Acknowledgments

- [Open Library](https://openlibrary.org/) for providing the book API
- [OpenLibrary.NET](https://github.com/Luca3317/OpenLibrary.NET/) by Luca3317 on GitHub
- [Avalonia UI](https://avaloniaui.net/) for the cross-platform UI framework
- [Akira Expanded Font](https://www.dafont.com/akira-expanded.font/) for the main font (demo version)
- [Phosphor Icons](https://phosphoricons.com/) for the menu button icons
- [Jothsa the Flexboxer at CodePen](https://codepen.io/Jothsa-the-flexboxer/pen/jOXbzod/) for the loading animation
- [AngelSix on Youtube](https://www.youtube.com/@AngelSix/) for the AvaloniaUI tutorial
- [Code:You](https://code-you.org/) program for the opportunity to build this capstone project

## AI and it's use in the project

The phrase "don't trust the AI" was ever present in the development of this project. I found it both helpful and frustrating.
I found the use of AI was most helpful in finding and explaining errors that I just couldn't see. If I found myself stuck
on a problem, I could utilize multiple AI programs to help solve that problem. By using multiple AI models and cross-
referencing the answers with documentation and results from online searches, I was often able to solve the issues I faced.
What frustrated me the most about the AI was the integration into Visual Studio. Often it would auto-complete a line of code
that I wouldn't catch and I would be chasing an error that I didn't realize I made.

## What would I have done differently?

I chose to build my project with AvaloniaUI. This was outside the recommendations of my mentors and the project requirements.
I started by building an app that ran on the desktop and had about 75% of the functionality that I needed. From my reading,
Avalonia should have been fairly easily adapted to a web app for the browser. Blazor interop was even mentioned, so I thought
everything would be smooth. It wasn't. Many times things were broken, lost, or just didn't work at all. Documentation for
Avalonia in the browser is very limited and there are few examples of projects actually built this way. I even started a Blazor
application just in case this didn't work out. In the end, I probably should have stuck with Blazor. That being said, I'm glad
I stuck with it. It was a challenge and I'm proud that I have something to submit that works (mostly).

---

Developed by Kevin Worthington for [Code:You](https://code-you.org/) Capstone Project
