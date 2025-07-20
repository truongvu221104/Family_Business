using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Family_Business.ViewModels
{
    public class PurchaseInvoiceViewModel : INotifyPropertyChanged
    {
        private decimal _unitCost;
        private int _quantity;
        private decimal _paid;
        private DateTime _invoiceDateTime = DateTime.Now;
        private DateTime? _dueDate;

        public decimal UnitCost
        {
            get => _unitCost;
            set
            {
                if (_unitCost == value) return;
                _unitCost = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Total));
                OnPropertyChanged(nameof(Status));
            }
        }

        public int Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity == value) return;
                _quantity = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Total));
                OnPropertyChanged(nameof(Status));
            }
        }

        public decimal Paid
        {
            get => _paid;
            set
            {
                if (_paid == value) return;
                _paid = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Status));
            }
        }

        public decimal Total => UnitCost * Quantity;

        public string Status =>
            Paid >= Total
                ? "Đã thanh toán đủ"
                : $"Còn nợ: {(Total - Paid):N2}";

        public DateTime InvoiceDateTime
        {
            get => _invoiceDateTime;
            set { _invoiceDateTime = value; OnPropertyChanged(); }
        }

        public DateTime? DueDate
        {
            get => _dueDate;
            set { _dueDate = value; OnPropertyChanged(); }
        }

        public bool IsDebt => Paid < Total;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
