using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EBISX_POS.ViewModels
{
    public class ViewModelBase : ObservableObject
    {
        // Unique identifier for the ViewModel instance for tracking purposes
        public string ViewModelId { get; } = Guid.NewGuid().ToString();

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Raises a property changed event and logs the change for easy identification in transaction logs.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            // Log the property change with timestamp, ViewModel type, and unique identifier.
            Console.WriteLine($"[{DateTime.Now}] {GetType().Name} (ID: {ViewModelId}) - Property changed: {propertyName}");
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Sets the field and raises a property changed event if the value has changed.
        /// </summary>
        /// <typeparam name="T">The type of the field.</typeparam>
        /// <param name="field">Reference to the field.</param>
        /// <param name="value">New value.</param>
        /// <param name="propertyName">The name of the property being set.</param>
        /// <returns>True if the field was changed; otherwise, false.</returns>
        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;
            field = value;
            RaisePropertyChanged(propertyName);
            return true;
        }
    }
}
