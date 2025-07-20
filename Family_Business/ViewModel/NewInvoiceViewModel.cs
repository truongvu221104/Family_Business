using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Family_Business.ViewModels
{
    public class NewInvoiceViewModel : INotifyPropertyChanged
    {
        private decimal _unitPrice;
        private int _quantity;
        private decimal _paid;

        public decimal UnitPrice
        {
            get => _unitPrice;
            set
            {
                if (_unitPrice == value) return;
                _unitPrice = value;
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

        public decimal Total => UnitPrice * Quantity;

        public string Status =>
            Paid >= Total
                ? "Đã thanh toán đủ"
                : $"Còn nợ: {(Total - Paid):N2}";

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

        private DateTime _invoiceDateTime = DateTime.Now;
        public DateTime InvoiceDateTime
        {
            get => _invoiceDateTime;
            set { _invoiceDateTime = value; OnPropertyChanged(); }
        }

        private DateTime? _dueDate;
        public DateTime? DueDate
        {
            get => _dueDate;
            set { _dueDate = value; OnPropertyChanged(); }
        }

        // true khi khách trả ít hơn tổng
        public bool IsDebt => Paid < Total;
    }
}
