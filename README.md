# Windows Volume Locker

A native Windows application that runs in the system tray and allows you to lock your system volume at a specific level. When locked, any attempts by other programs to change the volume will be immediately reverted.

## Features

- Volume slider to set desired volume level
- Lock/unlock volume at the selected level
- Automatically prevents other programs from changing volume when locked
- Runs in system tray
- Native Windows application built with C# and Windows Forms
- Real-time volume monitoring and restoration

## Requirements

- Windows 10 or later
- .NET 8.0 Runtime (download from [Microsoft](https://dotnet.microsoft.com/download/dotnet/8.0))

## Building

1. Make sure you have .NET 8.0 SDK installed
2. Open a terminal in the project directory
3. Build the project:

```bash
dotnet build
```

4. Run the application:

```bash
dotnet run
```

Or build a release version:

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

The executable will be in `bin\Release\net8.0-windows\win-x64\publish\VolumeLocker.exe`

## Technical Details

- Built with .NET 8.0 and Windows Forms
- Uses NAudio library for Windows Core Audio API access
- Real-time volume change detection and restoration
- Volume is locked to the exact level set on the slider
- Closing the window minimizes to tray (use Exit from tray menu to quit)

## License

This project is open source and available under the Apache License, Version 2.0.

## Attribution

Icon designed by Freepik

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.