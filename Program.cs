using System;
using System.Linq;
public abstract class MenuItem
{
    public string Name { get; set; }
    public abstract double GetPrice();
}
public interface IOrderItem
{
    void PrintInfo();
}
public class Drink : MenuItem, IOrderItem
{
    public int Volume { get; set; }
    public override double GetPrice()
    {
        return Volume * 0.05;
    }
    public void PrintInfo()
    {
        Console.WriteLine($"Напиток: {Name}, Объем: {Volume} мл, Цена: {GetPrice():F2} руб.");
    }
}
public class Food : MenuItem, IOrderItem
{
    public int Weight { get; set; }
    public override double GetPrice()
    {
        return Weight * 0.02;
    }
    public void PrintInfo()
    {
        Console.WriteLine($"Блюдо: {Name}, Вес: {Weight} г, Цена: {GetPrice():F2} руб.");
    }
}
class Program
{
    static void Main()
    {
        IOrderItem[] order =
        {
            new Drink { Name = "Кофе", Volume = 200 },
            new Food { Name = "Сэндвич", Weight = 250 },
        };
        double totalSum = 0;
        Console.WriteLine("Заказ");
        foreach (var item in order)
        {
            item.PrintInfo();
            if (item is MenuItem menuItem)
            {
                totalSum += menuItem.GetPrice();
            }
        }
        Console.WriteLine($"Общая сумма заказа: {totalSum:F2} руб.");
    }
}