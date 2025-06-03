# EBISX_POS

A point-of-sale system designed for efficient restaurant management, built with Avalonia UI framework.

## Project Overview
This project aims to provide a user-friendly POS system with features for menu management, order tracking, and customer authentication.

## Key Features
- **Menu Management**: Organized menu categories for easy navigation.
- **Order Tracking**: Real-time order summary and management.
- **User Authentication**: Login functionality for secure access.

## Getting Started
1. Clone the repository.
2. Install dependencies:
   ```
   dotnet restore
   ```
3. Run the application:
   ```
   dotnet run
   ```

## Code Structure
```
EBISX_POS/
  ├── Assets/                 # Images and other media assets
  ├── Models/                # Data models and business logic
  ├── ViewModels/            # View models for data binding
  │   ├── LogInWindowViewModel.cs
  │   ├── MainWindowViewModel.cs
  │   └── OrderSummaryViewModel.cs
  ├── Views/                 # UI views
  │   ├── LogInWindow.axaml
  │   ├── MainWindow.axaml
  │   └── OrderSummaryView.axaml
  ├── App.axaml              # Application entry point
  └── Program.cs            # Program execution
```

## Improvements
- **Data Models**: Implement classes for MenuItem, Order, and Customer.
- **Validation**: Add data validation and business logic.
- **Documentation**: Enhance code comments for better understanding.
- **Code Organization**: Organize view models into subfolders.

## Contributing
1. Fork the repository.
2. Create a feature branch.
3. Commit changes with clear messages.
4. Push to the branch.
5. Open a Pull Request.
