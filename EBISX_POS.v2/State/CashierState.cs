using System;

public static class CashierState
{
    public static event Action? OnCashierStateChanged;
    public static event Action<bool>? OnTrainingModeChanged;

    private static string? _cashierName;
    public static string? CashierName
    {
        get => _cashierName;
        set
        {
            if (_cashierName != value)
            {
                _cashierName = value;
                OnCashierStateChanged?.Invoke();
            }
        }
    }

    private static string? _cashierEmail;
    public static string? CashierEmail
    {
        get => _cashierEmail;
        set
        {
            if (_cashierEmail != value)
            {
                _cashierEmail = value;
                OnCashierStateChanged?.Invoke();
            }
        }
    }

    private static string? _managerEmail;
    public static string? ManagerEmail
    {
        get => _managerEmail;
        set
        {
            if (_managerEmail != value)
            {
                _managerEmail = value;
                OnCashierStateChanged?.Invoke();
            }
        }
    }

    private static bool _isTrainMode = false;
    public static bool IsTrainMode
    {
        get => _isTrainMode;
        set
        {
            if (_isTrainMode != value)
            {
                _isTrainMode = value;
                OnTrainingModeChanged?.Invoke(value);
            }
        }
    }

    public static void CashierStateReset()
    {
        CashierName = null;
        CashierEmail = null;
        ManagerEmail = null;
    }
}
