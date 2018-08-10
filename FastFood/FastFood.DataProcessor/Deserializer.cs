using System;
using System.Collections.Generic;
using DataValidation = System.ComponentModel.DataAnnotations;
using FastFood.Data;
using Newtonsoft.Json;
using FastFood.DataProcessor.Dto.Import;
using System.Text;
using System.Linq;
using FastFood.Models;
using System.Xml.Serialization;
using System.IO;
using System.Globalization;
using FastFood.Models.Enums;

namespace FastFood.DataProcessor
{
    public static class Deserializer
    {
        private const string FailureMessage = "Invalid data format.";
        private const string SuccessMessage = "Record {0} successfully imported.";

        public static string ImportEmployees(FastFoodDbContext context, string jsonString)
        {
            var deserializedEmployees = JsonConvert.DeserializeObject<EmployeeDto[]>(jsonString);

            var sb = new StringBuilder();

            var employees = new List<Employee>();

            foreach (var emp in deserializedEmployees)
            {
                if (!IsValid(emp))
                {
                    sb.AppendLine(FailureMessage);
                    continue;
                }

                var positionName = emp.Position;

                Position position = context.Positions.FirstOrDefault(p => p.Name == positionName);

                if (position == null)
                {
                    position = new Position
                    {
                        Name = positionName
                    };

                    context.Positions.Add(position);
                    context.SaveChanges();
                }

                Employee employee = new Employee
                {
                    Name = emp.Name,
                    Age = emp.Age,
                    Position = position
                };

                employees.Add(employee);

                sb.AppendLine(string.Format(SuccessMessage, employee.Name));
            }

            context.Employees.AddRange(employees);
            context.SaveChanges();

            return sb.ToString().TrimEnd();
        }

        public static string ImportItems(FastFoodDbContext context, string jsonString)
        {
            var deserializedItems = JsonConvert.DeserializeObject<ItemDto[]>(jsonString);

            var sb = new StringBuilder();

            var items = new List<Item>();

            foreach (var itemDto in deserializedItems)
            {
                if (!IsValid(itemDto) || context.Items.Any(i => i.Name == itemDto.Name) || items.Any(i => i.Name == itemDto.Name))
                {
                    sb.AppendLine(FailureMessage);
                    continue;
                }

                var category = context.Categories.FirstOrDefault(c => c.Name == itemDto.Category);

                if (category == null)
                {
                    category = new Category
                    {
                        Name = itemDto.Category
                    };
                    context.Categories.Add(category);
                    context.SaveChanges();
                }

                var item = new Item
                {
                    Name = itemDto.Name,
                    Price = itemDto.Price,
                    Category = category
                };

                items.Add(item);
                sb.AppendLine(string.Format(SuccessMessage, item.Name));
            }

            context.Items.AddRange(items);
            context.SaveChanges();

            return sb.ToString().TrimEnd();
        }

        public static string ImportOrders(FastFoodDbContext context, string xmlString)
        {
            var serializer = new XmlSerializer(typeof(OrderDto[]), new XmlRootAttribute("Orders"));

            var deserializedOrders = (OrderDto[])serializer.Deserialize(new StringReader(xmlString));

            var sb = new StringBuilder();

            var orders = new List<Order>();

            var orderItems = new List<OrderItem>();

            foreach (var orderDto in deserializedOrders)
            {
                if (!IsValid(orderDto))
                {
                    sb.AppendLine(FailureMessage);
                    continue;
                }

                var employee = context.Employees.FirstOrDefault(e => e.Name == orderDto.Employee);
                if (employee == null)
                {
                    sb.AppendLine(FailureMessage);
                    continue;
                }

                bool inExistingItem = false;

                foreach (var itemDto in orderDto.Items)
                {
                    if (!context.Items.Any(i => i.Name == itemDto.Name))
                    {
                        sb.AppendLine(FailureMessage);
                        inExistingItem = true;
                        break;
                    }
                }

                if (inExistingItem)
                {
                    continue;
                }

                Order order = new Order
                {
                    Customer = orderDto.Customer,
                    DateTime = DateTime.ParseExact(orderDto.DateTime, "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
                    Type = Enum.Parse<OrderType>(orderDto.Type),
                    Employee = employee,
                };

                orders.Add(order);

                foreach (var itemDto in orderDto.Items)
                {
                    var item = context.Items.First(i => i.Name == itemDto.Name);

                    orderItems.Add(new OrderItem
                    {
                        Order = order,
                        Item = item,
                        Quantity = itemDto.Quantity
                    });
                    
                }

                sb.AppendLine($"Order for {order.Customer} on {order.DateTime.ToString("dd/MM/yyyy HH:mm")} added");
            }

            context.Orders.AddRange(orders);
            context.OrderItems.AddRange(orderItems);
            context.SaveChanges();

            return sb.ToString().TrimEnd();
        }

        private static bool IsValid(object obj)
        {
            var validationContext = new DataValidation.ValidationContext(obj);
            var validationResults = new List<DataValidation.ValidationResult>();

            return DataValidation.Validator.TryValidateObject(obj, validationContext, validationResults, true);
        }
    }
}