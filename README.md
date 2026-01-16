# 🚀 Omni-Shell

> ⚠️ **Alpha Build** - This is the first test build with many missing functionalities. Expect bugs and incomplete features.

A modern Windows shell extension and folder management tool built with WPF. Omni-Shell provides a sleek interface for customizing folder icons, managing files, and enhancing your Windows Explorer experience.

## ✨ Features (In Development)

### Current Features

- **Folder Icon Customization** - Change folder icons with a beautiful UI
- **Context Menu Integration** - Right-click integration with Windows Explorer
- **Modern WPF Interface** - Sleek, modern design with dark mode support
- **File Management Tools** - Basic file operations and organization
- **Widget System** - Extensible widget framework (partially implemented)

### Planned Features

- Advanced file search and filtering
- Batch folder operations
- Custom themes and color schemes
- Plugin system for extensions
- Cloud storage integration
- Enhanced performance optimizations

## 🛠️ Technologies

- **Framework**: .NET 6.0 / WPF
- **Language**: C#
- **UI**: XAML with modern design patterns
- **Windows Integration**: Native Win32 API interop

## 📋 Prerequisites

- Windows 10/11
- .NET 6.0 SDK or later
- Visual Studio 2022 (recommended) or JetBrains Rider

## 🚀 Getting Started

### Installation

1. **Clone the repository**

   ```bash
   git clone https://github.com/Almoksha/Omni-Shell.git
   cd Omni-Shell
   ```

2. **Restore dependencies**

   ```bash
   dotnet restore
   ```

3. **Build the project**

   ```bash
   dotnet build
   ```

4. **Run the application**
   ```bash
   dotnet run
   ```

### Using Visual Studio

1. Open `OmniShell.csproj` or the solution file in Visual Studio 2022
2. Press `F5` to build and run the application
3. The Omni-Shell window should appear

## 📖 Usage

### Changing Folder Icons

1. Launch Omni-Shell
2. Navigate to the folder icon customization section
3. Select a folder you want to customize
4. Choose from the built-in icon library or use a custom icon
5. Apply the changes

### Context Menu Integration

1. Go to Settings in Omni-Shell
2. Enable "Add to Context Menu"
3. Right-click any folder in Windows Explorer
4. You should see "Omni-Shell" in the context menu

## 🏗️ Project Structure

```
Omni-Shell/
├── Core/              # Core application logic
├── Interop/           # Windows API interop layer
├── Models/            # Data models
├── Services/          # Business logic and services
├── Tools/             # Utility tools and helpers
├── ViewModels/        # MVVM view models
├── Views/             # UI views and pages
├── Resources/         # Images, icons, and assets
├── App.xaml           # Application entry point
└── MainWindow.xaml    # Main application window
```

## ⚠️ Known Issues

- Some features are incomplete or non-functional
- Performance may not be optimized
- UI may have visual glitches
- Limited error handling in some areas
- Context menu integration may require administrator privileges

## 🤝 Contributing

This is an early alpha build. Contributions, bug reports, and feature requests are welcome!

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📝 License

This project is currently unlicensed. Please contact the repository owner for usage rights.

## 🐛 Reporting Issues

Found a bug? Please open an issue on GitHub with:

- Description of the problem
- Steps to reproduce
- Expected vs actual behavior
- Screenshots (if applicable)
- System information (Windows version, .NET version)

## 🔮 Roadmap

- [ ] Complete core functionality
- [ ] Improve performance and stability
- [ ] Add comprehensive error handling
- [ ] Implement remaining widget features
- [ ] Add automated tests
- [ ] Create installer package
- [ ] Write detailed documentation
- [ ] Add localization support

## 📧 Contact

For questions or feedback, please open an issue on GitHub.

---

**Note**: This is an experimental project under active development. Use at your own risk and always back up important data.
