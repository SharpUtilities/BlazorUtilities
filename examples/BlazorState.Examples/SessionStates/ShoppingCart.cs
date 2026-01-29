// BlazorState.Examples/SessionStates/ShoppingCart.cs

using BlazorState.Core;
using BlazorState.Core.Options;

namespace BlazorState.Examples.SessionStates;

public sealed class ShoppingCart : BlazorStateTypeBase<ShoppingCart>, IBlazorStateType<ShoppingCart>
{
    public static Expiration SlidingExpiration => Expiration.AfterMinutes(30);
    public static Expiration AbsoluteExpiration => Expiration.AfterHours(24);

    private List<CartItem> _items = new();
    private string _couponCode = string.Empty;

    public IReadOnlyList<CartItem> Items => _items.AsReadOnly();

    public string CouponCode
    {
        get => _couponCode;
        set => SetField(ref _couponCode, value);
    }

    public decimal Total => _items.Sum(i => i.Price * i.Quantity);
    public int ItemCount => _items.Sum(i => i.Quantity);

    public void AddItem(string name, decimal price, int quantity = 1)
    {
        var existing = _items.FirstOrDefault(i => i.Name == name);
        if (existing is not null)
        {
            existing.Quantity += quantity;
        }
        else
        {
            _items.Add(new CartItem { Name = name, Price = price, Quantity = quantity });
        }
        OnPropertyChanged(nameof(Items));
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(ItemCount));
    }

    public void RemoveItem(string name)
    {
        _items.RemoveAll(i => i.Name == name);
        OnPropertyChanged(nameof(Items));
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(ItemCount));
    }

    public void Clear()
    {
        _items.Clear();
        _couponCode = string.Empty;
        OnPropertyChanged(nameof(Items));
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(ItemCount));
        OnPropertyChanged(nameof(CouponCode));
    }

    public override bool Equals(ShoppingCart? other)
    {
        if (other is null) return false;
        if (_couponCode != other._couponCode) return false;
        if (_items.Count != other._items.Count) return false;
        return _items.SequenceEqual(other._items);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(_couponCode);
        foreach (var item in _items)
        {
            hash.Add(item.GetHashCode());
        }
        return hash.ToHashCode();
    }
}

public class CartItem
{
    public required string Name { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }

    public override bool Equals(object? obj)
    {
        if (obj is not CartItem other) return false;
        return Name == other.Name && Price == other.Price && Quantity == other.Quantity;
    }

    public override int GetHashCode() => HashCode.Combine(Name, Price, Quantity);
}
