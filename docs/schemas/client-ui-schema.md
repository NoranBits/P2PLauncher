# Client UI Schema (WPF + MVVM)

This document outlines the initial schema for the modernized client application UI. It will evolve as features are wired to services.

## ViewModels
- MainViewModel
  - Host: string
  - Port: int (default 12000)
  - Status: string
  - DebugEnabled: bool
  - RecentHosts: ObservableCollection<string>
  - Commands:
    - ConnectCommand()
    - StopCommand()

## Views
- MainLauncherWindow.xaml
  - DataContext: MainViewModel
  - Regions:
    - Connection panel (Host, Port, Connect/Stop)
    - Toggle DebugEnabled (maps legacy CheckBoxDebug)
    - Recent Hosts list
    - Status panel

## Theming
- Resource dictionaries: `Themes/Colors.xaml`, `Themes/Styles.xaml` merged in `App.xaml`.
- Tokens: PrimaryBrush, AccentBrush, spacing keys.

## Future extensions
- Diagnostics panel bound to connectivity checks
- Persisted settings (client defaults, last host)
- Validation rules and error templates for inputs
- Fluent/WinUI-inspired styling (light/dark)
